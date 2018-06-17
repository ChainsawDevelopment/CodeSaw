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
    public class GetFileDiff : IQuery<object>
    {
        private readonly RevisionId _previous;
        private readonly RevisionId _current;
        private readonly string _oldPath;
        private readonly string _newPath;
        private readonly IRepository _api;
        private readonly ReviewIdentifier _reviewId;

        public GetFileDiff(int projectId, int reviewId, RevisionId previous, RevisionId current, string oldPath, string newPath, IRepository api)
        {
            _previous = previous;
            _current = current;
            _oldPath = oldPath;
            _newPath = newPath;
            _api = api;
            _reviewId = new ReviewIdentifier(projectId, reviewId);
        }

        public async Task<object> Execute(ISession session)
        {
            var mergeRequest = await _api.MergeRequest(_reviewId.ProjectId, _reviewId.ReviewId);

            var commits = session.Query<ReviewRevision>().Where(x => x.ReviewId == _reviewId)
                .ToDictionary(x => x.RevisionNumber, x => new {Head = x.HeadCommit, Base = x.BaseCommit});

            var previousCommit = ResolveCommitHash(_previous, mergeRequest, r => commits[r].Head);
            var currentCommit = ResolveCommitHash(_current, mergeRequest, r => commits[r].Head);

            var previousBaseCommit = ResolveBaseCommitHash(_previous, mergeRequest, r => commits[r].Base);
            var currentBaseCommit = ResolveBaseCommitHash(_current, mergeRequest, r => commits[r].Base);

            var contents = (await new[]
                    {
                        new {Commit = previousCommit, Path = _oldPath},
                        new {Commit = currentCommit, Path = _newPath},
                        new {Commit = previousBaseCommit, Path = _oldPath},
                        new {Commit = currentBaseCommit, Path = _newPath}
                    }
                    .DistinctBy(x => x.Commit)
                    .Select(async c => new {File = c, content = await _api.GetFileContent(_reviewId.ProjectId, c.Commit, c.Path)})
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

            PatchPosition PatchPositionToLines(PositionToLine map, int start, int length)
            {
                var startLine = map.GetLineinPosition(start);
                var endLine = map.GetLineinPosition(start +length);
                return new PatchPosition
                {
                    Start = startLine,
                    End = endLine,
                    Length = endLine - startLine
                };
            }

            IEnumerable<LineInfo> DiffToHunkLines(DiffClassification classification, Patch patch)
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
                        yield return new LineInfo
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

            var hunks = classifiedPatches.Select(patch => new HunkInfo
            {
                NewPosition = PatchPositionToLines(currentPositionToLine, patch.Patch.Start2, patch.Patch.Length2),
                OldPosition = PatchPositionToLines(previousPositionToLine, patch.Patch.Start1, patch.Patch.Length1),
                Lines = DiffToHunkLines(patch.classification, patch.Patch).ToList()
            });

            hunks = MergeAdjacentHunks(hunks);

            return new
            {
                commits = new
                {
                    review = new
                    {
                        prevous = previousCommit,
                        current = currentCommit
                    },
                    @base = new
                    {
                        prevous = previousBaseCommit,
                        current = currentBaseCommit
                    }
                },

                contents = new
                {
                    review = new
                    {
                        prevous = contents[previousCommit],
                        current = contents[currentCommit]
                    },
                    @base = new
                    {
                        prevous = contents[previousBaseCommit],
                        current = contents[currentBaseCommit]
                    }
                },
                pos = currentPositionToLine,

                hunks = hunks
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

        private IEnumerable<HunkInfo> MergeAdjacentHunks(IEnumerable<HunkInfo> hunks)
        {
            var result = new List<HunkInfo>();

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

        public class LineInfo
        {
            public string Operation { get; set; }
            public string Text { get; set; }
            public string Classification { get; set; }
        }
    }
}