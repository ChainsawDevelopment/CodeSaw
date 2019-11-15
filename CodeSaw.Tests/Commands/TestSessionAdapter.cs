using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Model;
using NUnit.Framework;

namespace CodeSaw.Tests.Commands
{
    public class ReviewsStore : Dictionary2d<ReviewUser, ReviewRevision, Review>
    {
    }

    public class TestSessionAdapter : ISessionAdapter
    {
        private static readonly ReviewRevision EmptyRevision = new ReviewRevision();

        private static readonly ReviewIdentifier ReviewId = new ReviewIdentifier(1, 1);

        public List<ReviewRevision> Revisions { get; } = new List<ReviewRevision>();
        public Dictionary<ReviewRevision, List<FileHistoryEntry>> FileHistory { get; } = new Dictionary<ReviewRevision, List<FileHistoryEntry>>();
        public ReviewsStore Reviews { get; } = new ReviewsStore();

        public List<ReviewUser> Users { get; } = new List<ReviewUser>();

        public List<Discussion> Discussions { get; } = new List<Discussion>();

        public ReviewRevision MarkRevision(PublishReview.RevisionCommits commits)
        {
            var revision = new ReviewRevision
            {
                BaseCommit = commits.Base,
                HeadCommit = commits.Head,
                ReviewId = ReviewId,
                RevisionNumber = Revisions.Count + 1,
                Id = Guid.NewGuid()
            };

            Revisions.Add(revision);

            return revision;
        }
        public ReviewUser AddUser(string name)
        {
            var user = new ReviewUser
            {
                Id = Users.Count + 1,
                Name = name,
                UserName = name,
            };

            Users.Add(user);

            return user;
        }

        public List<string> FilesInRevision(int revisionNumber)
        {
            var revision = Revisions[revisionNumber - 1];

            return FileHistory.GetValueOrDefault(revision, new List<FileHistoryEntry>()).Select(x => x.FileName).ToList();
        }

        public List<string> ReviewedFiles(ReviewUser user, int revisionNumber)
        {
            var revision = Revisions[revisionNumber - 1];
            return Reviews[user, revision].Files.Where(x => x.Status == FileReviewStatus.Reviewed).Select(x => FileNameInRevision(revision, x.FileId)).ToList();
        }

        private string FileNameInRevision(ReviewRevision revision, Guid fileId)
        {
            return FileHistory[revision].Single(x => x.FileId == fileId).FileName;
        }

        public Guid FileIdInRevision(ReviewRevision revision, string fileName)
        {
            return FileHistory[revision].Single(x => x.FileName == fileName).FileId;
        }

        public T FindDiscussion<T>(string content)
            where T : Discussion
        {
            return Discussions.OfType<T>().SingleOrDefault(x => x.RootComment.Content == content);
        }

        Dictionary<RevisionId, Review> ISessionAdapter.GetReviews(ReviewIdentifier reviewId, ReviewUser reviewUser)
        {
            return Reviews.ForKey1(reviewUser).ToDictionary(x => (RevisionId)new RevisionId.Selected(x.Key.RevisionNumber), x => x.Value);
        }

        async Task<ReviewRevision> ISessionAdapter.GetRevision(ReviewIdentifier reviewId, PublishReview.RevisionCommits commits)
        {
            return Revisions.SingleOrDefault(x => x.BaseCommit == commits.Base && x.HeadCommit == commits.Head);
        }

        ReviewRevision ISessionAdapter.GetRevision(ReviewIdentifier reviewId, RevisionId revisionId)
        {
            Assert.That(revisionId, Is.InstanceOf<RevisionId.Selected>());

            var s = (RevisionId.Selected) revisionId;

            return Revisions[s.Revision - 1];
        }

        ReviewRevision ISessionAdapter.GetRevision(Guid revisionId)
        {
            Assert.That(revisionId, Is.AnyOf(Revisions.Select(x => (object)x.Id).ToArray()));

            return Revisions.Single(x => x.Id == revisionId);
        }

        ReviewRevision ISessionAdapter.GetPreviousRevision(ReviewRevision revision)
        {
            Assert.That(revision, Is.AnyOf(Revisions.OfType<object>().ToArray()));

            if (revision.RevisionNumber == 1)
            {
                return null;
            }

            return Revisions.Single(x => x.RevisionNumber == revision.RevisionNumber - 1);
        }

        List<FileHistoryEntry> ISessionAdapter.GetFileHistoryEntries(ReviewRevision revision)
        {
            if (FileHistory.TryGetValue(revision, out var history))
            {
                return history;
            }
            else
            {
                return new List<FileHistoryEntry>();
            }
        }

        int ISessionAdapter.GetNextRevisionNumber(ReviewIdentifier reviewId)
        {
            return Revisions.Count + 1;
        }

        async Task ISessionAdapter.Save(ReviewRevision revision)
        {
            if (Revisions.Contains(revision))
            {
                return;
            }

            Assert.That(revision.RevisionNumber, Is.EqualTo(Revisions.Count + 1));

            Revisions.Add(revision);
        }

        void ISessionAdapter.Save(FileHistoryEntry entry)
        {
            if (entry.RevisionId.HasValue)
            {
                Assert.That(entry.RevisionId.Value, Is.AnyOf(Revisions.Select(x => (object)x.Id).ToArray()), "File history entry must be linked to valid revision");
            }

            var revision = Revisions.SingleOrDefault(x => x.Id == entry.RevisionId) ?? EmptyRevision;

            if (FileHistory.TryGetValue(revision, out var l))
            {
                if (!l.Contains(entry))
                {
                    l.Add(entry);
                }
            }
            else
            {
                FileHistory[revision] = new List<FileHistoryEntry>(){entry};
            }
        }

        void ISessionAdapter.Save(Review review)
        {
            Assert.That(review.RevisionId, Is.AnyOf(Revisions.Select(x => (object)x.Id).ToArray()), "Review must be linked to valid revision");
            Assert.That(review.UserId, Is.AnyOf(Users.Select(x => (object) x.Id).ToArray()), "Review must be linked to valid user");

            var user = Users.Single(x => x.Id == review.UserId);
            var revision = Revisions.Single(x => x.Id == review.RevisionId);

            if (Reviews.TryGetValue(user, revision, out var original))
            {
                Assert.That(review, Is.SameAs(original), "Do not overwrite review");
            }
            else
            {
                Reviews[user, revision] = review;
            }
        }

        void ISessionAdapter.Save(ReviewDiscussion discussion)
        {
            Assert.That(discussion.RevisionId, Is.AnyOf(Revisions.Select(x => (object)x.Id).ToArray()), "Review discussion must be linked to valid revision");
            Discussions.Add(discussion);
        }

        void ISessionAdapter.Save(FileDiscussion discussion)
        {
            Assert.That(discussion.RevisionId, Is.AnyOf(Revisions.Select(x => (object)x.Id).ToArray()), "File discussion must be linked to valid revision");

            Discussions.Add(discussion);
        }

        void ISessionAdapter.Save(Discussion discussion)
        {
            throw new NotImplementedException();
        }

        void ISessionAdapter.Save(Comment comment)
        {
            throw new NotImplementedException();
        }

        (Guid? revisionId, string hash) ISessionAdapter.FindPreviousRevision(ReviewIdentifier reviewId, int number, string baseCommit)
        {
            if (number <= 1)
            {
                return (null, baseCommit);
            }

            var previousRevision = Revisions.Single(x => x.ReviewId == reviewId && x.RevisionNumber == number - 1);

            return (previousRevision.Id, previousRevision.HeadCommit);
        }

        Dictionary<string, Guid> ISessionAdapter.FetchFileIds(Guid? previousRevId)
        {
            if (previousRevId == null)
            {
                return new Dictionary<string, Guid>();
            }

            Assert.That(previousRevId.Value, Is.AnyOf(Revisions.Select(x => (object)x.Id).ToArray()), "Asking for file entries from invalid revision");

            var revision = Revisions.Single(x => x.Id == previousRevId);

            return FileHistory[revision].ToDictionary(x => x.FileName, x => x.FileId);

        }

        Review ISessionAdapter.GetReview(Guid revisionId, ReviewUser user)
        {
            var revision = Revisions.Single(x => x.Id == revisionId);

            return Reviews.GetValueOrDefault(user, revision);
        }

        FileHistoryEntry ISessionAdapter.GetFileHistoryEntry(Guid fileId, ReviewRevision revision)
        {
            if (FileHistory.TryGetValue(revision ?? EmptyRevision, out var r))
            {
                return r.Single(x => x.FileId == fileId);
            }

            return null;
        }

        List<Discussion> ISessionAdapter.GetDiscussions(List<Guid> ids)
        {
            return Discussions.Where(x => ids.Contains(x.Id)).ToList();
        }
    }
}