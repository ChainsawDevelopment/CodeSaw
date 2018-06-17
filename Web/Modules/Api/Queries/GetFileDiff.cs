using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiffMatchPatch;
using NHibernate;
using RepositoryApi;
using Web.Cqrs;
using Web.Diff;
using Web.Modules.Api.Model;

namespace Web.Modules.Api.Queries
{
    public class GetFileDiff : IQuery<GetFileDiff.Result>
    {
        public RevisionId Previous { get; }
        public RevisionId Current { get; }
        public string OldPath { get; }
        public string NewPath { get; }
        public ReviewIdentifier ReviewId { get; }

        public GetFileDiff(int projectId, int reviewId, RevisionId previous, RevisionId current, string oldPath, string newPath)
        {
            Previous = previous;
            Current = current;
            OldPath = oldPath;
            NewPath = newPath;
            ReviewId = new ReviewIdentifier(projectId, reviewId);
        }

        public class Result
        {
            public ReviewDebugInfo Contents { get; set; }
            public ReviewDebugInfo Commits { get; set; }
            public IEnumerable<HunkInfo> Hunks { get; set; }
        }

        public class PatchPosition
        {
            public int Start { get; set; }
            public int End { get; set; }
            public int Length { get; set; }
        }

        public class HunkInfo
        {
            public PatchPosition NewPosition { get; set; }
            public PatchPosition OldPosition { get; set; }
            public List<LineInfo> Lines { get; set; }
        }

        public class LineInfo
        {
            public string Operation { get; set; }
            public string Text { get; set; }
            public string Classification { get; set; }
        }

        public class ReviewDebugInfo
        {
            public RevisionDebugInfo Review { get; set; }
            public RevisionDebugInfo Base { get; set; }
        }

        public class RevisionDebugInfo
        {
            public string Previous { get; set; }
            public string Current { get; set; }
        }
    }

    public class GetFileDiffHandler : IQueryHandler<GetFileDiff, GetFileDiff.Result>
    {
        private readonly ISession _session;
        private readonly IRepository _api;

        public GetFileDiffHandler(ISession session, IRepository api)
        {
            _session = session;
            _api = api;
        }

        public async Task<GetFileDiff.Result> Execute(GetFileDiff query)
        {
            var mergeRequest = await _api.MergeRequest(query.ReviewId.ProjectId, query.ReviewId.ReviewId);

            var commits = _session.Query<ReviewRevision>().Where(x => x.ReviewId == query.ReviewId)
                .ToDictionary(x => x.RevisionNumber, x => new {Head = x.HeadCommit, Base = x.BaseCommit});

            var previousCommit = ResolveCommitHash(query.Previous, mergeRequest, r => commits[r].Head);
            var currentCommit = ResolveCommitHash(query.Current, mergeRequest, r => commits[r].Head);

            var previousBaseCommit = ResolveBaseCommitHash(query.Previous, mergeRequest, r => commits[r].Base);
            var currentBaseCommit = ResolveBaseCommitHash(query.Current, mergeRequest, r => commits[r].Base);

            var contents = (await new[]
                    {
                        new {Commit = previousCommit, Path = query.OldPath},
                        new {Commit = currentCommit, Path = query.NewPath},
                        new {Commit = previousBaseCommit, Path = query.OldPath},
                        new {Commit = currentBaseCommit, Path = query.NewPath}
                    }
                    .DistinctBy(x => x.Commit)
                    .Select(async c => new {File = c, content = await _api.GetFileContent(query.ReviewId.ProjectId, c.Commit, c.Path)})
                    .WhenAll())
                .ToDictionary(x => x.File.Commit, x => x.content);

            var basePatch = FourWayDiff.MakePatch(contents[previousBaseCommit], contents[currentBaseCommit]);
            var reviewPatch = FourWayDiff.MakePatch(contents[previousCommit], contents[currentCommit]);

            UnrollContext(reviewPatch);

            var classifiedPatches = FourWayDiff.ClassifyPatches(
                contents[currentBaseCommit], basePatch,
                contents[currentCommit], reviewPatch
            );

            var currentPositionToLine = new PositionToLine(contents[currentCommit]);
            var previousPositionToLine = new PositionToLine(contents[previousCommit]);

            GetFileDiff.PatchPosition PatchPositionToLines(PositionToLine map, int start, int length)
            {
                var startLine = map.GetLineinPosition(start);
                var endLine = map.GetLineinPosition(start + length);
                return new GetFileDiff.PatchPosition
                {
                    Start = startLine,
                    End = endLine,
                    Length = endLine - startLine
                };
            }

            IEnumerable<GetFileDiff.LineInfo> DiffToHunkLines(DiffClassification classification, Patch patch)
            {
                int index = 0;
                foreach (var item in patch.Diffs)
                {
                    var diffText = item.Text;

                    if (diffText.EndsWith("\n") && index != patch.Diffs.Count - 1)
                    {
                        diffText = diffText.Substring(0, diffText.Length - 1);
                    }

                    var lines = diffText.Split('\n');

                    foreach (var line in lines)
                    {
                        yield return new GetFileDiff.LineInfo
                        {
                            Classification = classification.ToString(),
                            Operation = item.Operation.ToString(),
                            Text = line
                        };
                    }

                    index++;
                }
            }

            foreach (var patch in classifiedPatches)
            {
                FourWayDiff.ExpandPatchToFullLines(contents[currentCommit], patch.Patch);
            }

            var hunks = classifiedPatches.Select(patch => new GetFileDiff.HunkInfo
            {
                NewPosition = PatchPositionToLines(currentPositionToLine, patch.Patch.Start2, patch.Patch.Length2),
                OldPosition = PatchPositionToLines(previousPositionToLine, patch.Patch.Start1, patch.Patch.Length1),
                Lines = DiffToHunkLines(patch.classification, patch.Patch).ToList()
            });

            hunks = MergeAdjacentHunks(hunks);

            return new GetFileDiff.Result
            {
                Commits = new GetFileDiff.ReviewDebugInfo
                {
                    Review = new  GetFileDiff.RevisionDebugInfo
                    {
                        Previous = previousCommit,
                        Current = currentCommit
                    },
                    Base = new GetFileDiff.RevisionDebugInfo
                    {
                        Previous =  previousBaseCommit,
                        Current = currentBaseCommit
                    }
                },

                Contents = new GetFileDiff.ReviewDebugInfo
                {
                    Review = new GetFileDiff.RevisionDebugInfo
                    {
                        Previous= contents[previousCommit],
                        Current = contents[currentCommit]
                    },
                    Base = new GetFileDiff.RevisionDebugInfo
                    {
                        Previous= contents[previousBaseCommit],
                        Current = contents[currentBaseCommit]
                    }
                },

                Hunks = hunks
            };
        }

        private void UnrollContext(List<Patch> patches)
        {
            int totalDiff = 0;

            foreach (var patch in patches)
            {
                patch.Start1 -= totalDiff;
                totalDiff += patch.Length2 - patch.Length1;
            }
        }

        private IEnumerable<GetFileDiff.HunkInfo> MergeAdjacentHunks(IEnumerable<GetFileDiff.HunkInfo> hunks)
        {
            var result = new List<GetFileDiff.HunkInfo>();

            foreach (var hunk in hunks)
            {
                if (result.Count == 0)
                {
                    result.Add(hunk);
                    continue;
                }

                var lastHunk = result.Last();

                if (lastHunk.NewPosition.End < hunk.NewPosition.Start)
                {
                    // disjoint
                    result.Add(hunk);
                    continue;
                }

                if (lastHunk.NewPosition.End == hunk.NewPosition.Start - 1)
                {
                    // hunk starts right after lastHunk
                    lastHunk.Lines.AddRange(hunk.Lines);
                    lastHunk.NewPosition.End = hunk.NewPosition.End;
                    lastHunk.NewPosition.Length += hunk.NewPosition.Length;
                    lastHunk.OldPosition.End = hunk.OldPosition.End;
                    lastHunk.OldPosition.Length += hunk.OldPosition.Length;
                    continue;
                }

                if (lastHunk.NewPosition.End == hunk.NewPosition.Start)
                {
                    // hunk's first line lastHunk's last line
                    lastHunk.Lines.AddRange(hunk.Lines.Skip(1));
                    lastHunk.NewPosition.End = hunk.NewPosition.End;
                    lastHunk.NewPosition.Length += hunk.NewPosition.Length - 1;
                    lastHunk.OldPosition.End = hunk.OldPosition.End;
                    lastHunk.OldPosition.Length += hunk.OldPosition.Length - 1;
                    continue;
                }

                result.Add(hunk);
            }

            return result;
        }

        private string ResolveCommitHash(RevisionId revisionId, MergeRequest mergeRequest, Func<int, string> selectCommit)
        {
            return revisionId.Resolve(
                () => mergeRequest.BaseCommit,
                s =>  selectCommit(s.Revision),
                h => h.CommitHash
            );
        }

        private string ResolveBaseCommitHash(RevisionId revisionId, MergeRequest mergeRequest, Func<int, string> selectCommit)
        {
            return revisionId.Resolve(
                () => mergeRequest.BaseCommit,
                s =>  selectCommit(s.Revision),
                h => h.CommitHash == mergeRequest.HeadCommit ? mergeRequest.BaseCommit : h.CommitHash
            );
        }
    }
}