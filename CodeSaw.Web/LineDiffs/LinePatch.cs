using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DiffMatchPatch;

namespace CodeSaw.Web.LineDiffs
{
    public class LinePatch
    {
        public int Start1 { get; }
        public int Start2 { get; }
        public List<LineDiff> Diffs { get; }
        public int Length1 { get; }
        public int Length2 { get; }

        public LinePatch(int start1, int start2, List<LineDiff> diffs)
        {
            Start1 = start1;
            Start2 = start2;
            Diffs = diffs;
            Length1 = diffs.Where(x => !x.Operation.IsInsert).Sum(x => x.Lines.Count);
            Length2 = diffs.Where(x => !x.Operation.IsDelete).Sum(x => x.Lines.Count);
        }

        public IEnumerable<string> PreviousLines()
        {
            foreach (var diff in Diffs)
            {
                if (diff.Operation.IsInsert)
                {
                    continue;
                }

                foreach (var line in diff.Lines)
                {
                    yield return line;
                }
            }
        }

        public IEnumerable<string> CurrentLines()
        {
            foreach (var diff in Diffs)
            {
                if (diff.Operation.IsDelete)
                {
                    continue;
                }

                foreach (var line in diff.Lines)
                {
                    yield return line;
                }
            }
        }

        public override string ToString()
        {
            return string.Join("", Diffs);
        }
    }

    public class LineDiff
    {
        public Operation Operation { get; }
        public List<string> Lines { get; }

        public LineDiff(Operation operation, List<string> lines)
        {
            Operation = operation;
            Lines = lines;
        }

        public override string ToString()
        {
            var items = Lines.Select(x => $"{Operation.OperationMarker()}{x}");

            return string.Join("", items);
        }
    }

    public static class LinePatchOps
    {
        public static bool[] ApplyPatches(List<LinePatch> patches, List<string> text)
        {
            var result = new bool[patches.Count];

            for (int i = 0; i < patches.Count; i++)
            {
                result[i] = ApplyPatch(patches[i], text);
            }

            return result;
        }

        public static bool ApplyPatch(LinePatch patch, List<string> text)
        {
            int previousIndex = patch.Start1;

            foreach (var diff in patch.Diffs)
            {
                if (diff.Operation.IsEqual)
                {
                    foreach (var line in diff.Lines)
                    {
                        if (text[previousIndex] != line)
                        {
                            return false;
                        }
                        previousIndex++;
                    }
                }

                if (diff.Operation.IsDelete)
                {
                    text.RemoveRange(previousIndex, diff.Lines.Count);
                }

                if (diff.Operation.IsInsert)
                {
                    text.InsertRange(previousIndex, diff.Lines);
                    previousIndex += diff.Lines.Count;
                }
            }

            return true;
        }

        public static List<string> DiffsCurrentText(IEnumerable<LineDiff> diffs)
        {
            return diffs.Where(x => !x.Operation.IsDelete).SelectMany(x => x.Lines).ToList();
        }

        public static (List<LineDiff> prefix, List<LineDiff> changes, List<LineDiff> suffix) SplitPatchAffix(this LinePatch patch)
        {
            var prefix = new List<LineDiff>();
            var changes = new List<LineDiff>();
            var suffix = new List<LineDiff>();

            int i = 0;
            while (i < patch.Diffs.Count)
            {
                if (!patch.Diffs[i].Operation.IsEqual)
                {
                    break;
                }
                prefix.Add(patch.Diffs[i]);
                i++;
            }

            while (i < patch.Diffs.Count)
            {
                if (patch.Diffs[i].Operation.IsEqual)
                {
                    break;
                }
                changes.Add(patch.Diffs[i]);
                i++;
            }

            while (i < patch.Diffs.Count)
            {
                if (!patch.Diffs[i].Operation.IsEqual)
                {
                    break;
                }
                suffix.Add(patch.Diffs[i]);
                i++;
            }

            Debug.Assert(i == patch.Diffs.Count);

            return (prefix, changes, suffix);
        }
    }
}