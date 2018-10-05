using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.NodeIntegration;
using Newtonsoft.Json.Linq;
using NLog;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetReviewEmoji : IQuery<GetReviewEmoji.Result>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetReviewEmoji(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Result
        {
            public EmojiType[] ToAdd { get; set; }
            public EmojiType[] ToRemove { get; set; }

            public static Result AddAndRemove(EmojiType toAdd, EmojiType toRemove) => new Result
            {
                ToAdd = new[] {toAdd},
                ToRemove = new[] {toRemove}
            };

            public static Result Remove(params EmojiType[] toRemove) => new Result
            {
                ToAdd = new EmojiType[0],
                ToRemove = toRemove
            };
        }

        public class Handler : IQueryHandler<GetReviewEmoji, Result>
        {
            private static readonly Logger Log = LogManager.GetLogger("ReviewThumb");

            private readonly IQueryRunner _queryRunner;
            private readonly IRepository _api;
            private readonly NodeExecutor _node;
            private readonly ReviewUser _currentUser;

            public Handler(IQueryRunner queryRunner, IRepository api, NodeExecutor node, [CurrentUser]ReviewUser currentUser)
            {
                _queryRunner = queryRunner;
                _api = api;
                _node = node;
                _currentUser = currentUser;
            }

            public async Task<Result> Execute(GetReviewEmoji query)
            {
                var summary = await _queryRunner.Query(new GetReviewStatus(query.ReviewId));

                var fileMatrix = await _queryRunner.Query(new GetFileMatrix(query.ReviewId));

                var reviewFile = await GetReviewFiles(query, summary);

                var statusInput = new
                {
                    Matrix = fileMatrix,
                    UnresolvedDiscussions = summary.UnresolvedDiscussions,
                    ResolvedDiscussions = summary.ResolvedDiscussions
                };

                try
                {
                    var result = _node.ExecuteScriptFunction(reviewFile, "thumb", statusInput, _currentUser.UserName);

                    var numericResult = result.Value<int?>();

                    if (numericResult == 1)
                    {
                        return Result.AddAndRemove(EmojiType.ThumbsUp, EmojiType.ThumbsDown);
                    }

                    if (numericResult == -1)
                    {
                        return Result.AddAndRemove(EmojiType.ThumbsDown, EmojiType.ThumbsUp);
                    }

                    return Result.Remove(EmojiType.ThumbsUp, EmojiType.ThumbsDown);
                }
                catch (NodeException e)
                {
                    Log.Warn(e, "Thumb script failed");
                }

                return new Result()
                {
                    ToAdd = new EmojiType[0],
                    ToRemove = new EmojiType[0]
                };
            }
            
            private async Task<List<string>> GetReviewFiles(GetReviewEmoji query, GetReviewStatus.Result summary)
            {
                var result = new List<string>();

                result.Add(File.ReadAllText("DefaultReviewfile.js"));

                var reviewFile = await _api.GetFileContent(query.ReviewId.ProjectId, summary.CurrentHead, "Reviewfile.js");

                if (!string.IsNullOrEmpty(reviewFile))
                {
                    result.Add(reviewFile);
                }

                return result;
            }
        }
    }
}