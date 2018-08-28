using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RepositoryApi;
using Web.Cqrs;
using Web.Modules.Api.Model;
using Web.NodeIntegration;

namespace Web.Modules.Api.Queries
{
    public class GetCommitStatus : IQuery<CommitStatus>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetCommitStatus(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Handler : IQueryHandler<GetCommitStatus, CommitStatus>
        {
            private readonly IQueryRunner _queryRunner;
            private readonly string _siteBase;
            private readonly IRepository _api;
            private readonly NodeExecutor _node;

            public Handler(IQueryRunner queryRunner, [SiteBase]string siteBase, IRepository api, NodeExecutor node)
            {
                _queryRunner = queryRunner;
                _siteBase = siteBase;
                _api = api;
                _node = node;
            }

            public async Task<CommitStatus> Execute(GetCommitStatus query)
            {

                var fileMatrix = await _queryRunner.Query(new GetFileMatrix(query.ReviewId));

                var summary = await _queryRunner.Query(new GetReviewStatus(query.ReviewId));

                var reviewFile = await _api.GetFileContent(query.ReviewId.ProjectId, summary.CurrentHead, "Reviewfile.js");

                var statusInput = new
                {
                    Matrix = fileMatrix,
                    UnresolvedDiscussions = summary.UnresolvedDiscussions,
                    ResolvedDiscussions = summary.ResolvedDiscussions
                };

                var commitStatus = new CommitStatus
                {
                    Commit = summary.CurrentHead,
                    Name = "Code review (CodeSaw)",
                    TargetUrl = $"{_siteBase}/project/{query.ReviewId.ProjectId}/review/{query.ReviewId.ReviewId}"
                };

                JToken result;
                try
                {
                    result = _node.ExecuteScriptFunction(reviewFile, "status", statusInput);
                }
                catch (NodeException)
                {
                    commitStatus.State = CommitStatusState.Failed;
                    commitStatus.Description = "Review script failed";

                    return commitStatus;
                }

                if (result is JObject obj && obj.Property("ok") != null && obj.Property("reasons") != null)
                {
                    var isOk = obj.Property("ok").Value.Value<bool>();
                    var reasons = obj.Property("reasons").Value.Values<string>();

                    commitStatus.State = isOk ? CommitStatusState.Success : CommitStatusState.Pending;
                    commitStatus.Description = string.Join(", ", reasons);

                    return commitStatus;
                }
                else
                {
                    commitStatus.State = CommitStatusState.Failed;
                    commitStatus.Description = "Invalid review script output";

                    return commitStatus;
                }
            }

            public async Task<CommitStatus> Execute2(GetCommitStatus query)
            {
                var summary = await _queryRunner.Query(new GetReviewStatus(query.ReviewId));

                var items = new List<string>();
                var reviewPassed = true;

                var fileMatrix = await _queryRunner.Query(new GetFileMatrix(query.ReviewId));

                (int reviewedAtLatestRevision, int unreviewedAtLatestRevision) = fileMatrix.CalculateStatistics();

                items.Add($"{reviewedAtLatestRevision} file(s) reviewed");

                if (unreviewedAtLatestRevision > 0)
                {
                    reviewPassed = false;
                    items.Add($"{unreviewedAtLatestRevision} file(s) unreviewed");
                }

                if (!summary.RevisionForCurrentHead)
                {
                    reviewPassed = false;
                    items.Add("provisional revision for new changes");
                }

                if (summary.UnresolvedDiscussions > 0)
                {
                    reviewPassed = false;
                    items.Add($"{summary.UnresolvedDiscussions} unresolved discussion(s)");
                }

                if (summary.ResolvedDiscussions > 0)
                {
                    items.Add($"{summary.ResolvedDiscussions} resolved discussion(s)");
                }

                return new CommitStatus
                {
                    Commit = summary.CurrentHead,
                    Name = "Code review (CodeSaw)",
                    State = reviewPassed ? CommitStatusState.Success : CommitStatusState.Pending,
                    Description = string.Join(", ", items),
                    TargetUrl = $"{_siteBase}/project/{query.ReviewId.ProjectId}/review/{query.ReviewId.ReviewId}"
                };
            }
        }
    }
}