using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Modules.Api.Commands;
using CodeSaw.Web.Modules.Api.Model;
using NHibernate;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetDiffDiscussions : IQuery<GetDiffDiscussions.Result>
    {
        public ReviewIdentifier ReviewId { get; }
        public HashSet Commits { get; }
        public ClientFileId FileId { get; }
        public string RightFileName { get; }

        public class HashSet
        {
            public string PreviousBase { get; set; }
            public string PreviousHead { get; set; }
            public string CurrentHead { get; set; }
            public string CurrentBase { get; set; }
        }

        public class RemappedDiscussion
        {
            public Guid DiscussionId { get; set; }
            public int OldLineNumber { get; set; }
            public int LineNumber { get; set; }
            public string Side { get; set; }
            public string Comment { get; set; }
        }

        public class Result
        {
            public List<RemappedDiscussion> Remapped { get; set; }
        }

        public GetDiffDiscussions(ReviewIdentifier reviewId, HashSet commits, ClientFileId fileId, string rightFileName)
        {
            ReviewId = reviewId;
            Commits = commits;
            FileId = fileId;
            RightFileName = rightFileName;
        }

        public class Handler : IQueryHandler<GetDiffDiscussions, Result>
        {
            private readonly IRepository _api;
            private readonly ISession _session;

            public Handler(IRepository api, ISession session)
            {
                _api = api;
                _session = session;
            }

            private string FindFileNameForCommit(Guid fileId, string commit)
            {
                var q = from entry in _session.Query<FileHistoryEntry>()
                    join revision in _session.Query<ReviewRevision>() on entry.RevisionId equals revision.Id
                    where entry.FileId == fileId && revision.HeadCommit == commit
                    select entry.FileName;

                return q.Single();
            }

            private string FindFileNameForRevision(Guid fileId, Guid revisionId)
            {
                var q = from entry in _session.Query<FileHistoryEntry>()
                    where entry.FileId == fileId && entry.RevisionId == revisionId
                    select entry.FileName;

                return q.Single();
            }

            public async Task<Result> Execute(GetDiffDiscussions query)
            {
                if (query.FileId.IsProvisional)
                {
                    return new Result
                    {
                        Remapped = new List<RemappedDiscussion>()
                    };
                }

                var revisions = _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .Where(x => x.HeadCommit == query.Commits.CurrentHead || x.HeadCommit == query.Commits.PreviousHead)
                    .ToDictionary(x => x.HeadCommit, x => x.RevisionNumber);

                var q = from discussion in _session.Query<FileDiscussion>()
                    join revision in _session.Query<ReviewRevision>() on discussion.RevisionId equals revision.Id
                    where revision.ReviewId == query.ReviewId
                    where discussion.FileId == query.FileId.PersistentId
                    select new
                    {
                        DiscussionId = discussion.Id,
                        RevisionId = revision.Id,
                        RevisionNo = revision.RevisionNumber,
                        RevisionHead = revision.HeadCommit,
                        RevisionBase = revision.BaseCommit,
                        Line = discussion.LineNumber,
                        Comment = discussion.RootComment.Content
                    };

                var discussions = q.ToList();

                var remapped = new List<RemappedDiscussion>();

                foreach (var discussion in discussions)
                {
                    // is left?
                    if (discussion.RevisionBase == query.Commits.PreviousBase && discussion.RevisionHead == query.Commits.PreviousHead)
                    {
                        remapped.Add(new RemappedDiscussion
                        {
                            DiscussionId = discussion.DiscussionId,
                            LineNumber = discussion.Line,
                            OldLineNumber = discussion.Line,
                            Side = "left",
                            Comment = discussion.Comment
                        });
                        continue;
                    }

                    // is right?
                    if (discussion.RevisionBase == query.Commits.CurrentBase && discussion.RevisionHead == query.Commits.CurrentHead)
                    {
                        remapped.Add(new RemappedDiscussion
                        {
                            DiscussionId = discussion.DiscussionId,
                            LineNumber = discussion.Line,
                            OldLineNumber = discussion.Line,
                            Side = "right",
                            Comment = discussion.Comment
                        });
                        continue;
                    }

                    // remap
                    var commentFileName = FindFileNameForRevision(query.FileId.PersistentId, discussion.RevisionId);
                    string rightFileName;
                    if (revisions.ContainsKey(query.Commits.CurrentHead))
                    {
                        rightFileName = FindFileNameForCommit(query.FileId.PersistentId, query.Commits.CurrentHead);
                    }
                    else
                    {
                        rightFileName = query.RightFileName;
                    }

                    var commentContent = await _api.GetFileContent(query.ReviewId.ProjectId, discussion.RevisionHead, commentFileName).Then(x => x.DecodeString());
                    var rightContent = await _api.GetFileContent(query.ReviewId.ProjectId, query.Commits.CurrentHead, rightFileName).Then(x => x.DecodeString());
                    var trackedLine = CommentTracker.Track(commentContent, rightContent, discussion.Line);

                    remapped.Add(new RemappedDiscussion
                    {
                        DiscussionId = discussion.DiscussionId,
                        LineNumber = trackedLine,
                        OldLineNumber = discussion.Line,
                        Side = "right",
                        Comment = discussion.Comment
                    });
                }

                return new Result
                {
                    Remapped = remapped,
                };
            }
        }
    }
}