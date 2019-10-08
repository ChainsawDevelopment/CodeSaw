using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [DebuggerDisplay("{Start}-{End}")]
        public class PatchPosition
        {
            public int Start { get; set; }
            public int End { get; set; }
            public int Length { get; set; }
        }

        [DebuggerDisplay("{OldPosition} -> {NewPosition}")]
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
                            new {Commit = currentBaseCommit, Path = query.OldPath}
                        }
                        .DistinctBy(x => x.Commit)
                        .Select(async c => new
                        {
                            File = c,
                            Content = (await _api.GetFileContent(query.ReviewId.ProjectId, c.Commit, c.Path)).DecodeString()
                        })
                        .WhenAll())
                    .ToDictionary(x => x.File.Commit, x => x.Content);

                foreach (var content in contents)
                {
                    if (IsBinaryFile(content.Value))
                        return HandleBinaryFile(contents[previousCommit], contents[currentCommit], previousBaseCommit, currentBaseCommit, previousCommit, currentCommit);
                }

                var basePatch = FourWayDiff.MakePatch(contents[previousBaseCommit], contents[currentBaseCommit]);
                var reviewPatch = FourWayDiff.MakePatch(contents[previousCommit], contents[currentCommit]);

                var classifiedPatches = FourWayDiff.ClassifyPatches(
                    contents[currentBaseCommit], basePatch,
                    contents[currentCommit], reviewPatch
                );

                var previousLines = LineList.SplitLines(contents[previousCommit]);
                var currentLines = LineList.SplitLines(contents[currentCommit]);

                DiffView.AssignPatchesToLines(classifiedPatches, currentLines, previousLines);

                HunkInfo MakeHunkInfo(DiffView.HunkInfo hunk)
                {
                    var lines = new List<LineInfo>();

                    foreach (var (previous, current) in hunk.Lines)
                    {
                        if (previous != null && current != null)
                        {
                            lines.Add(new LineInfo
                            {
                                Text = previous.Text,
                                Classification = previous.Classification.ToString(),
                                Operation = previous.Diff?.Operation.ToString() ?? "Equal"
                            });

                            continue;
                        }

                        if (previous != null)
                        {
                            lines.Add(new LineInfo
                            {
                                Text = previous.Text,
                                Classification = previous.Classification.ToString(),
                                Operation = previous.Diff?.Operation.ToString() ?? "Equal"
                            });
                        }

                        if (current != null)
                        {
                            lines.Add(new LineInfo
                            {
                                Text = current.Text,
                                Classification = current.Classification.ToString(),
                                Operation = current.Diff?.Operation.ToString() ?? "Equal"
                            });
                        }
                    }

                    return new HunkInfo
                    {
                        OldPosition = new PatchPosition
                        {
                            Start = hunk.StartPrevious,
                            End = hunk.EndPrevious,
                            Length = hunk.LengthPrevious
                        },
                        NewPosition = new PatchPosition
                        {
                            Start = hunk.StartCurrent,
                            End = hunk.EndCurrent,
                            Length = hunk.LengthCurrent
                        },
                        Lines = lines
                    };
                }

                var baseHunks = DiffView.BuildHunks(currentLines, previousLines, true);
                var hunks = baseHunks.TakeLast(1).Select(MakeHunkInfo).ToList();

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

            private bool IsBinaryFile(string content)
            {
                return content.Take(8000).Contains((char)0);
            }

            private Result HandleBinaryFile(string previous, string current, string previousBaseCommit, string currentBaseCommit, string previousCommit,
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
                            Previous = "",
                            Current = ""
                        },
                        Base = new RevisionDebugInfo
                        {
                            Previous = "",
                            Current = ""
                        }
                    },

                    IsBinaryFile = true,
                    AreBinaryEqual = previous == current,
                    BinarySizes = new BinaryFileSizesInfo
                    {
                        PreviousSize = previous.Length,
                        CurrentSize = current.Length
                    }
                };
            }
        }
    }
}