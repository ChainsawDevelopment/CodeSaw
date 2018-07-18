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
        private ReviewUser _currentUser;
        private GetFilesInReview.Handler _handler;
        private ReviewIdentifier _reviewId;
        private Mock<IQueryRunner> _query;
        private Mock<IRepository> _repository;

        private static Constraint HasChanges
            => Has.Property(nameof(GetFilesInReview.FileToReview.HasChanges)).EqualTo(true).WithMessage("has changes");

        private static Constraint HasNoChanges
            => Has.Property(nameof(GetFilesInReview.FileToReview.HasChanges)).EqualTo(false).WithMessage("has no changes");

        [SetUp]
        public void SetUp()
        {
            _currentUser = new ReviewUser
            {
                Id = 5
            };

            _query = new Mock<IQueryRunner>(MockBehavior.Strict);

            _repository = new Mock<IRepository>(MockBehavior.Strict);

            _handler = new GetFilesInReview.Handler(_currentUser, _query.Object, _repository.Object);
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
                    CurrentBase = "base-base",
                    CurrentHead = "3-head",
                    FileStatuses = new[]
                    {
                        FileStatus(1, "never-reviewed.txt", FileReviewStatus.Unreviewed),
                        FileStatus(2, "never-reviewed.txt", FileReviewStatus.Unreviewed),
                        
                        FileStatus(1, "reviewed-at-1.txt", FileReviewStatus.Reviewed),
                        FileStatus(2, "reviewed-at-1.txt", FileReviewStatus.Unreviewed),

                        FileStatus(1, "reviewed-at-1-changes.txt", FileReviewStatus.Reviewed),
                        FileStatus(2, "reviewed-at-1-changes.txt", FileReviewStatus.Unreviewed),

                        FileStatus(1, "once-reviewed-now-removed.txt", FileReviewStatus.Reviewed),
                        FileStatus(2, "once-reviewed-now-removed.txt", FileReviewStatus.Unreviewed),

                        FileStatus(1, "renamed-after-review-at-1.old", FileReviewStatus.Reviewed),
                        FileStatus(2, "renamed-after-review-at-1.old", FileReviewStatus.Unreviewed),
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

            _repository.Setup(x => x.GetDiff(101, "base-base", "3-head")).ReturnsAsync(new List<FileDiff>
            {
                new FileDiff {Path = PathPair.Make("never-reviewed.txt")},
                new FileDiff {Path = PathPair.Make("reviewed-at-1.txt")},
                new FileDiff {Path = PathPair.Make("new-file.txt")},
                new FileDiff {Path = PathPair.Make("renamed-after-review-at-1.old", "renamed-after-review-at-1.new")}
            });

            var result = await Execute();

            Assert.That(ForFile(result, "never-reviewed.txt"), HasRange("base", "3") & HasChanges);
            Assert.That(ForFile(result, "reviewed-at-1.txt"), HasRange("1", "3") & HasNoChanges);
            Assert.That(ForFile(result, "reviewed-at-1-changes.txt"), HasRange("1", "3") & HasChanges);
            Assert.That(ForFile(result, "new-file.txt"), HasRange("base", "3") & HasChanges);
            Assert.That(ForFile(result, "once-reviewed-now-removed.txt"), HasRange("1", "3") & HasNoChanges);
            Assert.That(ForFile(result, "renamed-after-review-at-1.old"), HasRange("1", "3") & HasChanges);
        }

        private GetReviewStatus.FileStatus FileStatus(int revisionNumber, string file, FileReviewStatus reviewStatus)
        {
            return new GetReviewStatus.FileStatus()
            {
                Path =file,
                ReviewedBy = _currentUser.Id,
                RevisionNumber = revisionNumber,
                Status = reviewStatus
            };
        }

        private GetFilesInReview.FileToReview ForFile(GetFilesInReview.Result result, string path)
        {
            return result.FilesToReview.SingleOrDefault(x => x.Path.NewPath == path);
        }

        private Constraint HasRange(string previous, string current)
        {
            return Has
                .Property(nameof(GetFilesInReview.FileToReview.Previous)).EqualTo(RevisionId.Parse(previous))
                .And.Property(nameof(GetFilesInReview.FileToReview.Current)).EqualTo(RevisionId.Parse(current));
        }

        private async Task<GetFilesInReview.Result> Execute()
        {
            var result = await _handler.Execute(new GetFilesInReview(_reviewId));

            var serializer = new CustomSerializer()
            {
                Formatting = Formatting.Indented
            };
            serializer.Serialize(TestContext.Out, result);

            return result;
        }
    }
}