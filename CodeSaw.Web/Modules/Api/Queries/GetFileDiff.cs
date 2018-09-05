using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeSaw.RepositoryApi;
using CodeSaw.Web.Cqrs;
using CodeSaw.Web.Diff;
using CodeSaw.Web.Modules.Api.Model;
using DiffMatchPatch;

namespace CodeSaw.Web.Modules.Api.Queries
{
    public class GetFileDiff : IQuery<GetFileDiff.Result>
    {
        public class HashSet
        {
            public string PreviousBase { get; set; }
            public string PreviousHead { get; set; }
            public string CurrentHead { get; set; }
            public string CurrentBase { get; set; }
        }

        public string OldPath { get; }
        public string NewPath { get; }
        public ReviewIdentifier ReviewId { get; }
        public HashSet Hashes { get; }

        public GetFileDiff(ReviewIdentifier reviewId, HashSet hashes, string oldPath, string newPath)
        {
            OldPath = oldPath;
            NewPath = newPath;
            ReviewId = reviewId;
            Hashes = hashes;
        }

        public class Result
        {
            public ReviewDebugInfo Contents { get; set; }
            public ReviewDebugInfo Commits { get; set; }
            public IEnumerable<HunkInfo> Hunks { get; set; }
            public bool IsBinaryFile { get; set; }
            public bool AreBinaryEqual { get; set; }
            public BinaryFileSizesInfo BinarySizes { get; set; }
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

        public class BinaryFileSizesInfo
        {
            public int PreviousSize { get; set; }
            public int CurrentSize { get; set; }
        }


        public class Handler : IQueryHandler<GetFileDiff, Result>
        {
            private readonly IRepository _api;

            public Handler(IRepository api)
            {
                _api = api;
            }

            public async Task<Result> Execute(GetFileDiff query)
            {
                var previousCommit = query.Hashes.PreviousHead;
                var currentCommit = query.Hashes.CurrentHead;

                var previousBaseCommit = query.Hashes.PreviousBase;
                var currentBaseCommit = query.Hashes.CurrentBase;

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

                foreach (var content in contents)
                {
                    if (IsBinaryFile(content.Value))
                        return HandleBinaryFile(contents, previousBaseCommit, currentBaseCommit, previousCommit, currentCommit);
                }

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
                    var endLine = map.GetLineinPosition(start + length);
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

                return new Result
                {
                    Commits = new ReviewDebugInfo
                    {
                        Review = new RevisionDebugInfo
                        {
                            Previous = previousCommit,
                            Current = currentCommit
                        },
                        Base = new RevisionDebugInfo
                        {
                            Previous = previousBaseCommit,
                            Current = currentBaseCommit
                        }
                    },

                    Contents = new ReviewDebugInfo
                    {
                        Review = new RevisionDebugInfo
                        {
                            Previous = contents[previousCommit],
                            Current = contents[currentCommit]
                        },
                        Base = new RevisionDebugInfo
                        {
                            Previous = contents[previousBaseCommit],
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

            private bool IsBinaryFile(string content)
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(content);
                var length = Math.Min(8000, bytes.Length);
                for (var i = 0; i < length; ++i)
                    if (bytes[i] == 0)
                        return true;

                return false;
            }

            private Result HandleBinaryFile(IDictionary<string, string> contents, string previousBaseCommit, string currentBaseCommit, string previousCommit,
                string currentCommit)
            {
                return new Result
                {
                    Commits = new ReviewDebugInfo
                    {
                        Review = new RevisionDebugInfo
                        {
                            Previous = previousCommit,
                            Current = currentCommit
                        },
                        Base = new RevisionDebugInfo
                        {
                            Previous = previousBaseCommit,
                            Current = currentBaseCommit
                        }
                    },

                    Contents = new ReviewDebugInfo
                    {
                        Review = new RevisionDebugInfo
                        {
                            Previous = contents[previousCommit],
                            Current = contents[currentCommit]
                        },
                        Base = new RevisionDebugInfo
                        {
                            Previous = contents[previousBaseCommit],
                            Current = contents[currentBaseCommit]
                        }
                    },

                    IsBinaryFile = true,
                    AreBinaryEqual = contents[previousCommit] == contents[currentCommit],
                    BinarySizes = new BinaryFileSizesInfo
                    {
                        PreviousSize = contents[previousCommit].Length,
                        CurrentSize = contents[currentCommit].Length
                    }
                };
            }
        }
    }
}