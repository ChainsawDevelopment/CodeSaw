using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using RepositoryApi;
using Web.Auth;
using Web.Cqrs;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetFilesToReview : IQuery<GetFilesToReview.Result>
    {
        public ReviewIdentifier ReviewId { get; }
        public bool CurrentUserOnly { get; }

        public GetFilesToReview(ReviewIdentifier reviewId, bool currentUserOnly)
        {
            ReviewId = reviewId;
            CurrentUserOnly = currentUserOnly;
        }

        public class Handler : IQueryHandler<GetFilesToReview, Result>
        {
            private readonly ReviewUser _currentUser;
            private readonly IQueryRunner _query;
            private readonly IRepository _api;

            public Handler([CurrentUser]ReviewUser currentUser, IQueryRunner query, IRepository api)
            {
                _currentUser = currentUser;
                _query = query;
                _api = api;
            }

            public async Task<Result> Execute(GetFilesToReview query)
            {
                var reviewStatus = await _query.Query(new GetReviewStatus(query.ReviewId));

                var revisions = await _query.Query(new GetReviewRevionCommits(query.ReviewId));
                revisions[new RevisionId.Base()] = new GetReviewRevionCommits.Revision(reviewStatus.CurrentBase, reviewStatus.CurrentBase);

                RevisionId currentRevision;

                if (reviewStatus.RevisionForCurrentHead)
                {
                    currentRevision = new RevisionId.Selected(reviewStatus.LatestRevision.Value);    
                }
                else
                {
                    currentRevision = new RevisionId.Hash(reviewStatus.CurrentHead);
                    revisions[currentRevision] = new GetReviewRevionCommits.Revision(reviewStatus.CurrentBase, reviewStatus.CurrentHead);
                }

                var fullDiff = await _api.GetDiff(query.ReviewId.ProjectId, reviewStatus.CurrentBase, reviewStatus.CurrentHead);

                var fileStatuses = reviewStatus.FileReviewSummary;

                if (query.CurrentUserOnly)
                {
                    fileStatuses = fileStatuses.LimitToUser(_currentUser.UserName);
                }

                var lastReviewed = fileStatuses
                    .ToDictionary(x => x.Key, x => x.Value.RevisionReviewers.Keys.Max());

                var filesToReview = new List<FileToReview>();

                var allFiles = lastReviewed.Keys.ToList();

                foreach (var fileFromDiff in fullDiff)
                {
                    if (allFiles.Contains(fileFromDiff.Path))
                    {
                        continue;
                    }

                    var matching = allFiles.SingleOrDefault(x => x.NewPath == fileFromDiff.Path.OldPath);
                    if (matching == null)
                    {
                        allFiles.Add(fileFromDiff.Path); // new file
                    }
                    else if (matching != fileFromDiff.Path)
                    {
                        var idx = allFiles.IndexOf(matching);
                        allFiles[idx] = matching.WithNewName(fileFromDiff.Path.NewPath); // rename
                    }
                }

                foreach (var file in allFiles)
                {
                    RevisionId baseRevision;
                    if (lastReviewed.TryGetValue(file, out var revision))
                    {
                        baseRevision = new RevisionId.Selected(revision);
                    }
                    else if(lastReviewed.TryGetValue(PathPair.Make(file.OldPath), out var revision2))
                    {
                        baseRevision = new RevisionId.Selected(revision2);
                    }
                    else
                    {
                        baseRevision = new RevisionId.Base();
                    }

                    filesToReview.Add(new FileToReview
                    {
                        Path = file,
                        Previous = baseRevision,
                        Current = currentRevision,
                        HasChanges = false
                    });
                }

                var ranges = filesToReview
                    .Select(x => (Base: x.Previous, Head: x.Current))
                    .Where(x => revisions[x.Base] != revisions[currentRevision])
                    .Distinct();

                var diffs = await ranges
                    .Select(async range => (Range: range, Diff: await _api.GetDiff(query.ReviewId.ProjectId, revisions[range.Base].HeadCommit, revisions[range.Head].HeadCommit)))
                    .WhenAll()
                    .ToDictionaryAsync(x => x.Range, x => x.Diff);

                diffs[(currentRevision, currentRevision)] = new List<FileDiff>();

                foreach (var (range, diff) in diffs)
                {
                    foreach (var fileDiff in diff)
                    {
                        if (!fileDiff.RenamedFile)
                        {
                            continue;
                        }

                        var renameTarget = filesToReview.SingleOrDefault(f => f.Previous == range.Base && f.Current == range.Head && fileDiff.Path.OldPath == f.Path.NewPath);

                        if (renameTarget == null)
                        {
                            Console.WriteLine("Unmatched rename");
                            continue;
                        }

                        Console.WriteLine("Renamed detecte");

                        var toRemove = filesToReview.Single(x => 
                                x.Previous is RevisionId.Base
                                && x.Current == range.Head && x.Path == PathPair.Make(fileDiff.Path.NewPath));

                        filesToReview.Remove(toRemove);
                        
                        renameTarget.Path = renameTarget.Path.WithNewName(fileDiff.Path.NewPath);
                    }
                }

                foreach (var fileToReview in filesToReview)
                {
                    var range = (Base: fileToReview.Previous, Head: fileToReview.Current);

                    var diff = diffs[range];

                    var fileChanged = diff.SingleOrDefault(x => fileToReview.Path == x.Path);

                    fileToReview.RecordDiff(fileChanged);
                }

                foreach (var fileToReview in filesToReview)
                {
                    fileToReview.Previous = fileToReview.Previous.Resolve<RevisionId>(
                        resolveBase: () => new RevisionId.Hash(reviewStatus.CurrentBase),
                        resolveSelected: s => s,
                        resolveHash: h => h
                    );
                }

                return new Result
                {
                    FilesToReview = filesToReview
                };
            }
        }

        public class Result
        {
            public List<FileToReview> FilesToReview { get; set; }
        }

        public class FileToReview
        {
            public PathPair Path { get; set; }
            public RevisionId Previous { get; set; }
            public RevisionId Current { get; set; }
            public bool HasChanges { get; set; }

            public bool IsDeletedFile { get; set; }

            public bool IsNewFile { get; set; }

            public bool IsRenamedFile { get; set; }

            public override string ToString() => $"{Path.NewPath} {Previous} -> {Current} (changes: {HasChanges})";


            public void RecordDiff(FileDiff fileChanged)
            {
                if (fileChanged == null)
                {
                    HasChanges = false;
                    Current = Previous;
                    return;
                }

                HasChanges = true;
                IsRenamedFile = fileChanged.RenamedFile;
                IsNewFile = fileChanged.NewFile;
                IsDeletedFile = fileChanged.DeletedFile;
            }
        }
    }


    public class GetReviewRevionCommits : IQuery<IDictionary<RevisionId, GetReviewRevionCommits.Revision>>
    {
        public ReviewIdentifier ReviewId { get; }

        public GetReviewRevionCommits(ReviewIdentifier reviewId)
        {
            ReviewId = reviewId;
        }

        public class Revision
        {
            public string BaseCommit { get; }
            public string HeadCommit { get; }

            public Revision(string baseCommit, string headCommit)
            {
                BaseCommit = baseCommit;
                HeadCommit = headCommit;
            }
        }

        public class Handler : IQueryHandler<GetReviewRevionCommits, IDictionary<RevisionId, Revision>>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public async Task<IDictionary<RevisionId, Revision>> Execute(GetReviewRevionCommits query)
            {
                var revisions = await _session.Query<ReviewRevision>()
                    .Where(x => x.ReviewId == query.ReviewId)
                    .Select(x => new {x.RevisionNumber, x.HeadCommit, x.BaseCommit})
                    .ToListAsync();

                return revisions.ToDictionary(
                    x => (RevisionId)new RevisionId.Selected(x.RevisionNumber),
                    x => new Revision(x.BaseCommit, x.HeadCommit)
                );
            }
        }
    }
}