using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.NodeIntegration;
using NLog;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetCommitStatus : IQuery<CommitStatus>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetCommitStatus(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public static string CacheKey(ReviewIdentifier id) => $"CommitStatus_{id.ProjectId}_{id.ReviewId}";

        public class Handler : IQueryHandler<GetCommitStatus, CommitStatus>
        {
            private static readonly Logger Log = LogManager.GetLogger("ReviewCommitStatus");

            private readonly IQueryRunner _queryRunner;
            private readonly string _siteBase;
            private readonly IRepository _api;
            private readonly NodeExecutor _node;
            private readonly IMemoryCache _cache;

            public Handler(IQueryRunner queryRunner, [SiteBase]string siteBase, IRepository api, NodeExecutor node, IMemoryCache cache)
            {
                _queryRunner = queryRunner;
                _siteBase = siteBase;
                _api = api;
                _node = node;
                _cache = cache;
            }

            public async Task<CommitStatus> Execute(GetCommitStatus query)
            {
                var cacheKey = CacheKey(query.ReviewId);

                var summary = await _queryRunner.Query(new GetReviewStatus(query.ReviewId));

                if (_cache.TryGetValue<CommitStatus>(cacheKey, out var cachedStatus) && cachedStatus.Commit == summary.CurrentHead)
                {
                    return cachedStatus;
                }

                var fileMatrix = await _queryRunner.Query(new GetFileMatrix(query.ReviewId));

                var reviewFile = await GetReviewFiles(query, summary);

                var statusInput = new
                {
                    Matrix = fileMatrix,
                    UnresolvedDiscussions = summary.UnresolvedDiscussions,
                    ResolvedDiscussions = summary.ResolvedDiscussions,
                    Discussions = summary.Discussions
                };

                var commitStatus = new CommitStatus
                {
                    Commit = summary.CurrentHead,
                    Name = "Code review (CodeSaw)",
                    TargetUrl = $"{_siteBase}/project/{query.ReviewId.ProjectId}/review/{query.ReviewId.ReviewId}"
                };

                try
                {
                    var result = _node.ExecuteScriptFunction(reviewFile, "status", statusInput);

                    if (result is JObject obj && obj.Property("ok") != null && obj.Property("reasons") != null)
                    {
                        var isOk = obj.Property("ok").Value.Value<bool>();
                        var reasons = obj.Property("reasons").Value.Values<string>();

                        commitStatus.State = isOk ? CommitStatusState.Success : CommitStatusState.Pending;
                        commitStatus.Description = string.Join(", ", reasons);
                    }
                    else
                    {
                        commitStatus.State = CommitStatusState.Failed;
                        commitStatus.Description = "Invalid review script output";
                    }
                }
                catch (NodeException e)
                {
                    Log.Warn(e, "Review script failed");
                    commitStatus.State = CommitStatusState.Failed;
                    commitStatus.Description = "Review script failed";

                }

                _cache.Set(cacheKey, commitStatus);

                return commitStatus;
            }

            private async Task<List<string>> GetReviewFiles(GetCommitStatus query, GetReviewStatus.Result summary)
            {
                var result = new List<string>();
                result.Add(File.ReadAllText("DefaultReviewfile.js"));

                var reviewFile = await _api.GetFileContent(query.ReviewId.ProjectId, summary.CurrentHead, "Reviewfile.js").Then(x => x.DecodeString());
                if (!string.IsNullOrEmpty(reviewFile))
                {
                    result.Add(reviewFile);
                }

                return result;
            }
        }
    }
}