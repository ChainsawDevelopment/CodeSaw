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
    public class GetFileDiff_Line : IQuery<GetFileDiff_Line.Result>
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

        public GetFileDiff_Line(ReviewIdentifier reviewId, HashSet hashes, string oldPath, string newPath)
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
            public bool IsImageFile { get; set; }
            public bool IsBinaryFile { get; set; }
            public bool AreBinaryEqual { get; set; }
            public BinaryFileSizesInfo BinarySizes { get; set; }
            public string PreviousFileUrl { get; set; }
            public string CurrentFileUrl { get; set; }
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
            public int PreviousTotalLines { get; set; }
            public int CurrentTotalLines { get; set; }
        }

        public class BinaryFileSizesInfo
        {
            public int PreviousSize { get; set; }
            public int CurrentSize { get; set; }
        }


        public class Handler : IQueryHandler<GetFileDiff_Line, Result>
        {
            private readonly IRepository _api;

            public Handler(IRepository api)
            {
                _api = api;
            }

            public async Task<Result> Execute(GetFileDiff_Line query)
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

                if (contents[previousCommit] == "" && query.OldPath != query.NewPath)
                {
                    contents[previousCommit] =
                        (await _api.GetFileContent(query.ReviewId.ProjectId, previousCommit, query.NewPath)).DecodeString();
                }

                foreach (var content in contents)
                {
                    if (IsImageFile(query.OldPath, query.NewPath))
                    {
                        var newUrl = await _api.GetFileUrl(query.ReviewId.ProjectId, currentCommit, query.NewPath);
                        var previousUrl = await _api.GetFileUrl(query.ReviewId.ProjectId, previousCommit, query.OldPath);
                        return HandleImageFile(contents[previousCommit], contents[currentCommit], previousBaseCommit, currentBaseCommit,
                            previousCommit, currentCommit, newUrl, previousUrl);
                    }
                    if (IsBinaryFile(content.Value))
                    {
                        return HandleBinaryFile(contents[previousCommit], contents[currentCommit], previousBaseCommit, currentBaseCommit, previousCommit, currentCommit);
                    }
                }

                var basePatch = LineFourWayDiff.MakePatch(contents[previousBaseCommit], contents[currentBaseCommit]);
                var reviewPatch = LineFourWayDiff.MakePatch(contents[previousCommit], contents[currentCommit]);

                var currentBaseSplitLines = contents[currentBaseCommit].SplitLinesNoRemove().ToList();
                var previousSplitLines = contents[previousCommit].SplitLinesNoRemove().ToList();
                var currentSplitLines = contents[currentCommit].SplitLinesNoRemove().ToList();

                var classifiedPatches = LineFourWayDiff.ClassifyPatches(previousSplitLines, basePatch, reviewPatch);


                var previousLines = previousSplitLines.Select((x, i) => new LineLine(i + 1, x)).ToList();
                var currentLines = currentSplitLines.Select((x, i) => new LineLine(i + 1, x)).ToList();

                LineDiffView.AssignPatchesToLines(classifiedPatches, currentLines, previousLines);

                var cleared = new List<LineLine>();
                LineDiffView.RemoveDiffsFromIdenticalLines(currentLines, previousLines, cleared);

                HunkInfo MakeHunkInfo(LineDiffView.HunkInfo hunk)
                {
                    var lines = new List<LineInfo>();

                    foreach (var (previous, current) in hunk.Lines)
                    {
                        if (previous != null && current != null)
                        {
                            lines.Add(new LineInfo
                            {
                                Text = previous.Text.TrimEnd('\n'),
                                Classification = previous.Classification.ToString(),
                                Operation = previous.Diff?.Operation.ToString() ?? "Equal"
                            });

                            continue;
                        }

                        if (previous != null)
                        {
                            lines.Add(new LineInfo
                            {
                                Text = previous.Text.TrimEnd('\n'),
                                Classification = previous.Classification.ToString(),
                                Operation = previous.Diff?.Operation.ToString() ?? "Equal"
                            });
                        }

                        if (current != null)
                        {
                            lines.Add(new LineInfo
                            {
                                Text = current.Text.TrimEnd('\n'),
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

                var baseHunks = LineDiffView.BuildHunks(currentLines, previousLines, true);
                var hunks = baseHunks.Select(MakeHunkInfo).ToList();

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
                            PreviousTotalLines = previousLines.Count,
                            Current = contents[currentCommit],
                            CurrentTotalLines = currentLines.Count
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

            private bool IsImageFile(string oldPath, string newPath)
            {
                string[] supportedExtensions = {".png", ".jpg", ".svg"};
                return supportedExtensions.Any(x => oldPath.EndsWith(x)) || supportedExtensions.Any(x => newPath.EndsWith(x));
            }

            private Result HandleImageFile(string previous, string current, string previousBaseCommit, string currentBaseCommit, string previousCommit,
                string currentCommit, string newUrl, string previousUrl)
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

                    IsImageFile = true,
                    IsBinaryFile = true,
                    AreBinaryEqual = previous == current,
                    BinarySizes = new BinaryFileSizesInfo
                    {
                        PreviousSize = previous.Length,
                        CurrentSize = current.Length
                    },

                    PreviousFileUrl = previousUrl,
                    CurrentFileUrl = newUrl,
                };
            }
        }
    }
}