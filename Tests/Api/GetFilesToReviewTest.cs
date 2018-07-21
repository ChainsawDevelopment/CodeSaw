using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using RepositoryApi;
using Web;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api;
using Web.Modules.Api.Model;
using Web.Modules.Api.Queries;
using Web.Serialization;

namespace Tests.Api
{
    public class GetFilesToReviewTest
    {
        private const string BaseHash = "BA5EBA5EBA5EBA5EBA5EBA5EBA5EBA5EBA5EBA5E";

        private ReviewUser _currentUser;
        private GetFilesToReview.Handler _handler;
        private ReviewIdentifier _reviewId;
        private Mock<IQueryRunner> _query;
        private Mock<IRepository> _repository;

        private static Constraint HasChanges
            => Has.Property(nameof(GetFilesToReview.FileToReview.HasChanges)).EqualTo(true).WithMessage("has changes");

        private static Constraint HasNoChanges
            => Has.Property(nameof(GetFilesToReview.FileToReview.HasChanges)).EqualTo(false).WithMessage("has no changes");

        [SetUp]
        public void SetUp()
        {
            _currentUser = new ReviewUser
            {
                Id = 5,
                UserName = "test"
            };

            _query = new Mock<IQueryRunner>(MockBehavior.Strict);

            _repository = new Mock<IRepository>(MockBehavior.Strict);

            _handler = new GetFilesToReview.Handler(_currentUser, _query.Object, _repository.Object);
            _reviewId = new ReviewIdentifier(101, 202);
        }

        [Test]
        public async Task DetermineFilesToReview()
        {
            _query.ForQuery<GetReviewStatus, GetReviewStatus.Result>()
                .Returns(new GetReviewStatus.Result()
                {
                    RevisionForCurrentHead = true,
                    LatestRevision = 3,
                    CurrentBase = BaseHash,
                    CurrentHead = "3-head",
                    FileReviewSummary = new FileReviewSummary()
                    {
                        [PathPair.Make("reviewed-at-1.txt")] = new Dictionary<int, List<string>>
                        {
                            [1] = new List<string> {_currentUser.UserName}
                        },
                        //    FileStatus(1, "reviewed-at-1.txt"),

                        [PathPair.Make("reviewed-at-1-changes.txt")] = new Dictionary<int, List<string>>
                        {
                            [1] = new List<string> {_currentUser.UserName}
                        },

                        //    FileStatus(1, "reviewed-at-1-changes.txt"),

                        [PathPair.Make("once-reviewed-now-removed.txt")] = new Dictionary<int, List<string>>
                        {
                            [1] = new List<string> {_currentUser.UserName}
                        },

                        //    FileStatus(1, "once-reviewed-now-removed.txt"),

                        [PathPair.Make("renamed-after-review-at-1.old")] = new Dictionary<int, List<string>>
                        {
                            [1] = new List<string> {_currentUser.UserName}
                        },

                        //    FileStatus(1, "renamed-after-review-at-1.old"),
                    }
                });

            _query.ForQuery<GetReviewRevionCommits, IDictionary<RevisionId, GetReviewRevionCommits.Revision>>()
                .Returns(new Dictionary<RevisionId,GetReviewRevionCommits.Revision>
                {
                    [new RevisionId.Selected(1)] = new GetReviewRevionCommits.Revision("1-base", "1-head"),
                    [new RevisionId.Selected(2)] = new GetReviewRevionCommits.Revision("2-base", "2-head"),
                    [new RevisionId.Selected(3)] = new GetReviewRevionCommits.Revision("3-base", "3-head"),
                });

            _repository.Setup(x => x.GetDiff(101, "1-head", "3-head")).ReturnsAsync(new List<FileDiff>
            {
                new FileDiff {Path = PathPair.Make("reviewed-at-1-changes.txt")},
                new FileDiff {Path = PathPair.Make("renamed-after-review-at-1.old", "renamed-after-review-at-1.new"), RenamedFile = true}
            });

            _repository.Setup(x => x.GetDiff(101, BaseHash, "3-head")).ReturnsAsync(new List<FileDiff>
            {
                new FileDiff {Path = PathPair.Make("never-reviewed.txt")},
                new FileDiff {Path = PathPair.Make("reviewed-at-1.txt")},
                new FileDiff {Path = PathPair.Make("new-file.txt")},
                new FileDiff {Path = PathPair.Make("renamed-after-review-at-1.old", "renamed-after-review-at-1.new"), RenamedFile = true}
            });

            var result = await Execute();
            result.FilesToReview.Select(x=>x.Path).ToList().ForEach(x=>Console.WriteLine($"{x.OldPath} -> {x.NewPath}"));

            Assert.That(ForFile(result, "never-reviewed.txt"), HasRange(BaseHash, "3") & HasChanges);
            Assert.That(ForFile(result, "reviewed-at-1.txt"), HasRange("1", "3") & HasNoChanges);
            Assert.That(ForFile(result, "reviewed-at-1-changes.txt"), HasRange("1", "3") & HasChanges);
            Assert.That(ForFile(result, "new-file.txt"), HasRange(BaseHash, "3") & HasChanges);
            Assert.That(ForFile(result, "once-reviewed-now-removed.txt"), HasRange("1", "3") & HasNoChanges);
            Assert.That(ForFile(result, "renamed-after-review-at-1.new"), HasRange("1", "3") & HasChanges);

            Assert.That(result.FilesToReview, Has.Count.EqualTo(6));
        }

        private GetReviewStatus.FileStatus FileStatus(int revisionNumber, string file)
        {
            return new GetReviewStatus.FileStatus()
            {
                Path =file,
                ReviewedBy = _currentUser.Id,
                RevisionNumber = revisionNumber,
                Status = FileReviewStatus.Reviewed
            };
        }

        private GetFilesToReview.FileToReview ForFile(GetFilesToReview.Result result, string path)
        {
            return result.FilesToReview.SingleOrDefault(x => x.Path.NewPath == path);
        }

        private Constraint HasRange(string previous, string current)
        {
            return Has
                .Property(nameof(GetFilesToReview.FileToReview.Previous)).EqualTo(RevisionId.Parse(previous))
                .And.Property(nameof(GetFilesToReview.FileToReview.Current)).EqualTo(RevisionId.Parse(current));
        }

        private async Task<GetFilesToReview.Result> Execute()
        {
            var result = await _handler.Execute(new GetFilesToReview(_reviewId));

            var serializer = new CustomSerializer()
            {
                Formatting = Formatting.Indented
            };
            serializer.Serialize(TestContext.Out, result);

            return result;
        }
    }
}