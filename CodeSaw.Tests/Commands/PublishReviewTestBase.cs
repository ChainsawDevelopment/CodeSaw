using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web;
using CodeSaw.Web.Auth;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Commands.PublishElements;
using CodeSaw.Web.Modules.Api.Model;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CodeSaw.Tests.Commands
{
    public class PublishReviewTestBase
    {
        protected static readonly RevisionId.Hash Head1 = new RevisionId.Hash("abcd1aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        protected static readonly RevisionId.Hash Head2 = new RevisionId.Hash("abcd2aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        protected static readonly RevisionId.Hash Head3 = new RevisionId.Hash("abcd3aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        private static readonly ReviewIdentifier ReviewId = new ReviewIdentifier(1, 1);
        private MockRepository _mocks;
        protected TestSessionAdapter _sessionAdapter;
        protected Mock<IRepository> _repository;
        private Mock<IEventBus> _eventBus;
        private MemoryCache _cache;
        private RevisionFactory _factory;
        protected ReviewUser _reviewer1;
        protected ReviewUser _reviewer2;
        private List<(ReviewUser, PublishReview)> _commands;

        [SetUp]
        public void SetUp()
        {
            _mocks = new MockRepository(MockBehavior.Strict);
            _sessionAdapter = new TestSessionAdapter();
            _repository = _mocks.Create<IRepository>(MockBehavior.Default);
            _eventBus = _mocks.Create<IEventBus>(MockBehavior.Default);
            _cache = new MemoryCache(new MemoryCacheOptions());
            
            _factory = new RevisionFactory(_repository.Object);

            _reviewer1 = _sessionAdapter.AddUser("reviewer1");
            _reviewer2 = _sessionAdapter.AddUser("reviewer2");

            _commands = new List<(ReviewUser, PublishReview)>();
        }

        protected async Task Handle(PublishReview.RevisionCommits commits, ReviewUser user, params object[] content)
        {
            var handler = new PublishReview.Handler(_sessionAdapter, _repository.Object, user, _eventBus.Object, _factory, _cache, new FeatureToggle());

            var command = new PublishReview
            {
                ProjectId = ReviewId.ProjectId,
                ReviewId = ReviewId.ReviewId,
                Revision = commits,
                StartedReviewDiscussions = content.OfType<NewReviewDiscussion>().ToList(),
                StartedFileDiscussions = content.OfType<NewFileDiscussion>().ToList(),

                ReviewedFiles = (from c in content.OfType<ReviewedFilesContainer>()
                    from f in c.FileIds
                    select new PublishReview.FileRef
                    {
                        FileId = f,
                        Revision = c.RevisionId
                    }).ToList(),
                UnreviewedFiles = (from c in content.OfType<UnreviewedFilesContainer>()
                    from f in c.FileIds
                    select new PublishReview.FileRef
                    {
                        FileId = f,
                        Revision = c.RevisionId
                    }).ToList()
            };

            _commands.Add((user, command));

            await handler.Handle(command);
        }

        protected Guid FindFileId(int revisionNumber, string fileName)
        {
            return _sessionAdapter.FileIdInRevision(_sessionAdapter.Revisions[revisionNumber - 1], fileName);
        }

        private static NewFileDiscussion MakeFileDiscussion(RevisionId revisionId, ClientFileId fileId, int discussioId)
        {
            return new NewFileDiscussion()
            {
                TemporaryId = $"FILE-{discussioId}",
                Content = $"FILE-{discussioId}",
                FileId = fileId,
                LineNumber = 10,
                TargetRevisionId = revisionId,
                State = DiscussionState.NeedsResolution
            };
        }

        protected static NewFileDiscussion MakeFileDiscussion(RevisionId revisionId, string file, int discussioId)
        {
            return MakeFileDiscussion(revisionId, ClientFileId.Provisional(PathPair.Make(file)), discussioId);
        }

        protected static NewFileDiscussion MakeFileDiscussion(RevisionId revisionId, PathPair file, int discussioId)
        {
            return MakeFileDiscussion(revisionId, ClientFileId.Provisional(file), discussioId);
        }

        protected static NewFileDiscussion MakeFileDiscussion(RevisionId revisionId, Guid file, int discussioId)
        {
            return MakeFileDiscussion(revisionId, ClientFileId.Persistent(file), discussioId);
        }

        protected static NewReviewDiscussion MakeReviewDiscussion(RevisionId revisionId, int discussionId)
        {
            return new NewReviewDiscussion
            {
                Content = $"REVIEW-{discussionId}",
                State = DiscussionState.NeedsResolution,
                TargetRevisionId = revisionId,
                TemporaryId = $"REVIEW-{discussionId}"
            };
        }

        protected static FileDiff Modified(string oldPath)
        {
            return new FileDiff
            {
                Path = PathPair.Make(oldPath),
            };
        }

        private List<ClientFileId> GetFileIds(params object[] ids)
        {
            var result = new List<ClientFileId>();

            foreach (var id in ids)
            {
                switch (id)
                {
                    case string s:
                        result.Add(ClientFileId.Provisional(PathPair.Make(s)));
                        break;
                    case PathPair p:
                        result.Add(ClientFileId.Provisional(p));
                        break;
                    case Guid g:
                        result.Add(ClientFileId.Persistent(g));
                        break;
                    case ClientFileId f:
                        result.Add(f);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("ids", $"Unrecognized file id type {id}");

                }
            }

            return result;
        }

        protected ReviewedFilesContainer ReviewedFiles(RevisionId revision, params object[] files)
        {
            return new ReviewedFilesContainer
            {
                RevisionId = revision,
                FileIds = GetFileIds(files)
            };
        }

        protected UnreviewedFilesContainer UnreviewedFiles(RevisionId revision, params object[] files)
        {
            return new UnreviewedFilesContainer
            {
                RevisionId = revision,
                FileIds = GetFileIds(files)
            };
        }

        protected IConstraint HasReviewFor(ReviewUser user, ReviewRevision revision)
        {
            return new DelegateConstraint<ReviewsStore>(x => x.Contains(user, revision));
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("Revisions:");
            foreach (var revision in _sessionAdapter.Revisions)
            {
                Console.WriteLine($"{revision.RevisionNumber}   {revision.Id}   {revision.HeadCommit}");
            }

            Console.WriteLine();

            Console.WriteLine("Reviews");

            foreach (var revision in _sessionAdapter.Revisions)
            {
                Console.WriteLine($"\tRevision {revision.RevisionNumber}:");

                foreach (var (reviewer, review) in _sessionAdapter.Reviews.ForKey2(revision))
                {
                    Console.WriteLine($"\t\t{review.Id} {reviewer.UserName}");

                    foreach (var reviewFile in review.Files)
                    {
                        Console.WriteLine($"\t\t\t {reviewFile.Status} {reviewFile.FileId}");
                    }
                }
            }

            Console.WriteLine();
            
            Console.WriteLine("File history");

            foreach (var revision in _sessionAdapter.Revisions)
            {
                Console.WriteLine($"\tRevision {revision.RevisionNumber}:");

                foreach (var entry in _sessionAdapter.FileHistory.GetValueOrDefault(revision, new List<FileHistoryEntry>()))
                {
                    Console.WriteLine($"\t\t{entry.FileId} {entry.FileName}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Command history");

            foreach (var (reviewer, command) in _commands)
            {
                Console.WriteLine($"As {reviewer.UserName}");
                Console.WriteLine(JsonConvert.SerializeObject(command, Formatting.Indented));
            }
        }

        public class ReviewedFilesContainer
        {
            public RevisionId RevisionId { get; set; }
            public List<ClientFileId> FileIds { get; set; }
        }

        public class UnreviewedFilesContainer
        {
            public RevisionId RevisionId { get; set; }
            public List<ClientFileId> FileIds { get; set; }
        }

        protected static PublishReview.RevisionCommits Commits(RevisionId.Hash head)
        {
            var commits = new PublishReview.RevisionCommits
            {
                Base = "Base1",
                Head = head.CommitHash
            };
            return commits;
        }
    }
}