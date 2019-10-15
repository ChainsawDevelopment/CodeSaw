using System.Collections.Generic;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Commands.PublishElements;
using CodeSaw.Web.Modules.Api.Model;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CodeSaw.Tests.Commands
{
    public class PublishReviewTest : PublishReviewTestBase
    {
        [Test]
        public async Task FirstReviewOnFirstRevision()
        {
            var commits = Commits(Head1);

            _repository.SetupDiff(commits.Base, commits.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            await Handle(commits, _reviewer1,
                MakeReviewDiscussion(Head1, 1),
                MakeFileDiscussion(Head1, "file1", 1),
                ReviewedFiles(Head1, "file1", "file2")
            );

            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(1));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.FindDiscussion<ReviewDiscussion>("REVIEW-1"), Is.Not.Null);
            Assert.That(_sessionAdapter.FindDiscussion<FileDiscussion>("FILE-1"), Is.Not.Null);

            Assert.That(_sessionAdapter.FileHistory, Has.Count.EqualTo(1));
            Assert.That(_sessionAdapter.FilesInRevision(1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file1", "file2"}));
        }

        [Test]
        public async Task SecondReviewOnFirstRevision()
        {
            var commits = Commits(Head1);

            _repository.SetupDiff(commits.Base, commits.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            await Handle(commits, _reviewer1,
                MakeReviewDiscussion(Head1, 1),
                MakeFileDiscussion(Head1, "file1", 1),
                ReviewedFiles(Head1, "file1", "file2")
            );

            await Handle(commits, _reviewer2,
                MakeReviewDiscussion(Head1, 2),
                MakeFileDiscussion(Head1, FindFileId(1, "file2"), 2),
                ReviewedFiles(Head1, FindFileId(1, "file2"), FindFileId(1, "file3"))
            );

            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(1));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[0]));

            Assert.That(_sessionAdapter.FindDiscussion<ReviewDiscussion>("REVIEW-1"), Is.Not.Null);
            Assert.That(_sessionAdapter.FindDiscussion<FileDiscussion>("FILE-1"), Is.Not.Null);

            Assert.That(_sessionAdapter.FindDiscussion<ReviewDiscussion>("REVIEW-2"), Is.Not.Null);
            Assert.That(_sessionAdapter.FindDiscussion<FileDiscussion>("FILE-2"), Is.Not.Null);

            Assert.That(_sessionAdapter.FileHistory, Has.Count.EqualTo(1));
            Assert.That(_sessionAdapter.FilesInRevision(1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file1", "file2"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 1), Is.EquivalentTo(new[] {"file2", "file3"}));
        }

        [Test]
        public async Task ReviewNextFile()
        {
            var commits = Commits(Head1);

            _repository.SetupDiff(commits.Base, commits.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            await Handle(commits, _reviewer1,
                MakeReviewDiscussion(Head1, 1),
                MakeFileDiscussion(Head1, "file1", 1),
                ReviewedFiles(Head1, "file1", "file2")
            );

            await Handle(commits, _reviewer1,
                ReviewedFiles(Head1, FindFileId(1, "file1"), FindFileId(1, "file2"), FindFileId(1, "file3"))
            );

            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(1));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));

            Assert.That(_sessionAdapter.FilesInRevision(1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file1", "file2", "file3"}));
        }

        [Test]
        public async Task MakeNextRevision()
        {
            var commits1 = Commits(Head1);

            var commits2 = Commits(Head2);

            _repository.SetupDiff(commits1.Base, commits1.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            await Handle(commits1, _reviewer1,
                MakeReviewDiscussion(Head1, 1),
                MakeFileDiscussion(Head1, "file1", 1),
                ReviewedFiles(Head1, "file1")
            );

            _repository.SetupDiff(commits1.Head, commits2.Head,
                Modified("file3"),
                Modified("file4"),
                Modified("file5")
            );

            await Handle(commits2, _reviewer1,
                ReviewedFiles(Head2, FindFileId(1, "file2"), FindFileId(1, "file3"))
            );

            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(2));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[1]));

            Assert.That(_sessionAdapter.FilesInRevision(1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.FilesInRevision(2), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4", "file5"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file1"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 2), Is.EquivalentTo(new[] {"file2", "file3"}));
        }

        [Test]
        [Explicit("Known to fail")]
        [TestCase("file1", "file1")]
        [TestCase("file1_old", "file1")]
        public async Task MakeReviewBasedOnProvisionalAfterRevisionHasBeenSaved(string oldFileName, string newFileName)
        {
            var commits1 = Commits(Head1);

            _repository.SetupDiff(commits1.Base, commits1.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            var file1 = PathPair.Make(oldFileName, newFileName);

            await Handle(commits1, _reviewer1,
                MakeReviewDiscussion(Head1, 1),
                MakeFileDiscussion(Head1, file1, 1),
                ReviewedFiles(Head1, file1)
            );

            await Handle(commits1, _reviewer2,
                MakeReviewDiscussion(Head1, 1),
                MakeFileDiscussion(Head1, file1, 1),
                ReviewedFiles(Head1, "file2")
            );

            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(1));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[0]));

            Assert.That(_sessionAdapter.FilesInRevision(1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file1"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 1), Is.EquivalentTo(new[] {"file2"}));
        }

        [Test]
        public async Task ReviewFileFromPreviousRevision_NoReview_PersistentTopRevision()
        {
            var commits1 = Commits(Head1);
            var commits2 = Commits(Head2);
            var commits3 = Commits(Head3);

            _repository.SetupDiff(commits1.Base, commits1.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            _repository.SetupDiff(commits1.Head, commits2.Head,
                Modified("file3"),
                Modified("file4")
            );

            _repository.SetupDiff(commits2.Head, commits3.Head,
                Modified("file4")
            );

            // R1: both reviewers review all files
            await Handle(commits1, _reviewer1,
                ReviewedFiles(Head1, "file1", "file2", "file3", "file4")
            );
            
            await Handle(commits1, _reviewer2,
                ReviewedFiles(new RevisionId.Selected(1), FindFileId(1, "file1"), FindFileId(1, "file2"), FindFileId(1, "file3"), FindFileId(1, "file4"))
            );

            // R2: Only reviewer1 reviews file
            await Handle(commits2, _reviewer1,
                ReviewedFiles(Head2, FindFileId(1, "file3"), FindFileId(1, "file4"))
            );

            // R3: Reviewer1 creates revision
            await Handle(commits3, _reviewer1,
                ReviewedFiles(Head3, FindFileId(1, "file4"))
            );

            // R3: Reviewer2 reviews file3 at R2 and file4 at R3
            await Handle(commits3, _reviewer2,
                ReviewedFiles(new RevisionId.Selected(2), FindFileId(1, "file3")),
                ReviewedFiles(new RevisionId.Selected(3), FindFileId(1, "file4"))
            );

            // Now assert review state
            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(3));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[1]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[2]));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[1]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[2]));

            Assert.That(_sessionAdapter.FilesInRevision(1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.FilesInRevision(2), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.FilesInRevision(3), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 2), Is.EquivalentTo(new[] {"file3", "file4"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 2), Is.EquivalentTo(new[] {"file3"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 3), Is.EquivalentTo(new[] { "file4"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 3), Is.EquivalentTo(new[] { "file4"}));
        }

        [Test]
        public async Task ReviewFileFromPreviousRevision_NoReview_ProvisionalTopRevision()
        {
            var commits1 = Commits(Head1);
            var commits2 = Commits(Head2);
            var commits3 = Commits(Head3);

            _repository.SetupDiff(commits1.Base, commits1.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            _repository.SetupDiff(commits1.Head, commits2.Head,
                Modified("file3"),
                Modified("file4")
            );

            _repository.SetupDiff(commits2.Head, commits3.Head,
                Modified("file4")
            );

            // R1: both reviewers review all files
            await Handle(commits1, _reviewer1,
                ReviewedFiles(Head1, "file1", "file2", "file3", "file4")
            );
            
            await Handle(commits1, _reviewer2,
                ReviewedFiles(new RevisionId.Selected(1), FindFileId(1, "file1"), FindFileId(1, "file2"), FindFileId(1, "file3"), FindFileId(1, "file4"))
            );

            // R2: Only reviewer1 reviews file
            await Handle(commits2, _reviewer1,
                ReviewedFiles(Head2, FindFileId(1, "file3"), FindFileId(1, "file4"))
            );

            // R3: Reviewer2 reviews file3 at R2 and file4 at R3
            await Handle(commits3, _reviewer2,
                ReviewedFiles(new RevisionId.Selected(2), FindFileId(1, "file3")),
                ReviewedFiles(new RevisionId.Hash(commits3.Head), FindFileId(1, "file4"))
            );

            // Now assert review state
            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(3));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[1]));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[1]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[2]));

            Assert.That(_sessionAdapter.FilesInRevision(1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.FilesInRevision(2), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.FilesInRevision(3), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 2), Is.EquivalentTo(new[] {"file3", "file4"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 2), Is.EquivalentTo(new[] {"file3"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 3), Is.EquivalentTo(new[] { "file4"}));
        }

        [Test]
        public async Task UnreviewFileInCurrentRevision()
        {
            var commits1 = Commits(Head1);

            _repository.SetupDiff(commits1.Base, commits1.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            // R1: reviewer1 review all files
            await Handle(commits1, _reviewer1,
                    ReviewedFiles(Head1, "file1", "file2", "file3", "file4")
                );
            
            // R2: reviewer1 unreviews single file
            await Handle(commits1, _reviewer1,
                UnreviewedFiles(new RevisionId.Selected(1), FindFileId(1, "file1"))
            );

            // Now assert review state
            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(1));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file2", "file3", "file4"}));
        }

        [Test]
        public async Task UnreviewFileFromPreviousRevision()
        {
            var commits1 = Commits(Head1);
            var commits2 = Commits(Head2);

            _repository.SetupDiff(commits1.Base, commits1.Head,
                Modified("file1"),
                Modified("file2"),
                Modified("file3"),
                Modified("file4")
            );

            _repository.SetupDiff(commits1.Head, commits2.Head,
                Modified("file3"),
                Modified("file4")
            );

            // R1: both reviewers review all files
            await Handle(commits1, _reviewer1,
                ReviewedFiles(Head1, "file1", "file2", "file3", "file4")
            );

            await Handle(commits1, _reviewer2,
                ReviewedFiles(new RevisionId.Selected(1), FindFileId(1, "file1"), FindFileId(1, "file2"), FindFileId(1, "file3"), FindFileId(1, "file4"))
            );

            // R2: Only reviewer1 reviews file
            await Handle(commits2, _reviewer1,
                ReviewedFiles(Head2, FindFileId(1, "file3"), FindFileId(1, "file4"))
            );

            await Handle(commits2, _reviewer2,
                UnreviewedFiles(new RevisionId.Selected(1), FindFileId(1, "file1"))
            );

            // Now assert review state
            Assert.That(_sessionAdapter.Revisions, Has.Count.EqualTo(2));

            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer1, _sessionAdapter.Revisions[1]));
            
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[0]));
            Assert.That(_sessionAdapter.Reviews, HasReviewFor(_reviewer2, _sessionAdapter.Revisions[1]));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 1), Is.EquivalentTo(new[] {"file1", "file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer1, 2), Is.EquivalentTo(new[] {"file3", "file4"}));

            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 1), Is.EquivalentTo(new[] {"file2", "file3", "file4"}));
            Assert.That(_sessionAdapter.ReviewedFiles(_reviewer2, 2), Is.Empty);
        }
    }
}