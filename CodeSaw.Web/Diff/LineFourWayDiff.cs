using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using CodeSaw.Web.LineDiffs;

namespace CodeSaw.Web.Diff
{
    public class LineFourWayDiff
    {
        public static readonly DiffMatchPatch.DiffMatchPatch DMP = new DiffMatchPatch.DiffMatchPatch(1f, (short) 32, 4, 0.5f, 1000, 32, 0.5f, (short) 4);


        public static List<LinePatch> MakePatch(string text1, string text2)
        {
            var patches = MakePatchRaw(text1, text2);

            MergeEquals(patches);

            patches = patches.SelectMany(SplitPatchIntoAtoms).ToList();

            return patches;
        }

        private static void MergeEquals(List<LinePatch> patches)
        {
            foreach (var patch in patches)
            {
                if (patch.Diffs.Count < 2)
                {
                    continue;
                }

                for (int i = patch.Diffs.Count - 1; i > 0 ; i--)
                {
                    var mergeFrom = patch.Diffs[i];
                    var mergeTo = patch.Diffs[i - 1];

                    if (mergeFrom.Operation.IsEqual && mergeTo.Operation.IsEqual)
                    {
                        mergeTo.Lines.AddRange(mergeFrom.Lines);
                        patch.Diffs.RemoveAt(i);
                    }
                }
            }
        }

        public static List<LinePatch> MakePatchRaw(string text1, string text2)
        {
            var mapping = new Dictionary<string, char>();
            var lineText1 = MapLines(text1, mapping);
            var lineText2 = MapLines(text2, mapping);
            var lineArray = mapping.ToDictionary(x => x.Value, x => x.Key);

            var diffs = DMP.DiffMain(lineText1, lineText2, true);
            var patches = DMP.Patchmake(diffs);

            var starts1 = new int[lineText1.Length + 1];
            var starts2 = new int[lineText2.Length + 1];

            int sum = 0;
            for (int i = 0; i < lineText1.Length; i++)
            {
                starts1[i] = sum;
                sum += lineArray[lineText1[i]].Length;
            }

            starts1[starts1.Length - 1] = sum;

            sum = 0;
            for (int i = 0; i < lineText2.Length; i++)
            {
                starts2[i] = sum;
                sum += lineArray[lineText2[i]].Length;
            }

            starts2[starts2.Length - 1] = sum;

            var linePatches = new List<LinePatch>(patches.Count);

            foreach (var patch in patches)
            {
                var lineDiffs = patch.Diffs.Select(x => new LineDiff(x.Operation, x.Text.Select(l => lineArray[l]).ToList())).ToList();

                var linePatch = new LinePatch(patch.Start1, patch.Start2, lineDiffs);
                linePatches.Add(linePatch);
            }

            return linePatches;
        }

        private static string MapLines(string text, Dictionary<string, char> mapping)
        {
            var result = new StringBuilder();

            foreach (var line in text.SplitLinesNoRemove())
            {
                if (mapping.TryGetValue(line, out var c))
                {
                    result.Append(c);
                }
                else
                {
                    var newC = mapping[line] = (char) (mapping.Count + 1);
                    result.Append(newC);
                }
            }

            return result.ToString();
        }

        public static IEnumerable<LinePatch> SplitPatchIntoAtoms(LinePatch patch)
        {
            if (patch.Diffs.Count == 1)
            {
                yield return patch;
                yield break;
            }

            using (var diff = patch.Diffs.GetEnumerator())
            {
                var currentPatch = new List<LineDiff>();

                var start1 = patch.Start1;
                var start2 = patch.Start2;
                var length1 = 0;
                var length2 = 0;

                while (diff.MoveNext())
                {
                    var partLength1 = !diff.Current.Operation.IsInsert ? diff.Current.Lines.Count : 0;
                    var partLength2 = !diff.Current.Operation.IsDelete ? diff.Current.Lines.Count : 0;

                    length1 += partLength1;
                    length2 += partLength2;

                    currentPatch.Add(diff.Current);

                    if (diff.Current.Operation.IsEqual && currentPatch.Count > 1)
                    {
                        // sink
                        var p = new LinePatch(
                            diffs: currentPatch,
                            start1: start1,
                            start2: start2
                        );

                        yield return p;

                        var operationDiff = length2 - length1;

                        start1 = p.Start1 + p.Length1 - diff.Current.Lines.Count + operationDiff;
                        start2 = p.Start2 + p.Length2 - diff.Current.Lines.Count;

                        length1 = length2 = diff.Current.Lines.Count;

                        currentPatch = new List<LineDiff>() {diff.Current};
                    }
                }

                if (currentPatch.Count > 1)
                {
                    var p = new LinePatch(
                        diffs: currentPatch,
                        start1: start1,
                        start2: start2
                    );

                    yield return p;
                }
            }
        }

        public static bool ArePatchesMatching(LinePatch basePatch, List<string> reviewText, LinePatch reviewPatch)
        {
            var baseTextFromDiffs = basePatch.CurrentLines().ToList();
            var reviewTextFromDiffs = reviewPatch.CurrentLines().ToList();

            {
                var reviewInBase = baseTextFromDiffs.IndexOf(reviewTextFromDiffs, StringComparison.InvariantCulture);

                if (reviewInBase >= 0 && reviewPatch.Start2 - reviewInBase >= 0)
                {
                    // extend review to match base
                    var prefix = reviewText.Slice(reviewPatch.Start2 - reviewInBase, reviewInBase);
                    var suffix = reviewText.Slice(reviewPatch.Start2 + reviewPatch.Length2, baseTextFromDiffs.Count - reviewPatch.Length2 - reviewInBase);

                    var extended = prefix.Concat(reviewTextFromDiffs).Concat(suffix);

                    if (extended.SequenceEqual(baseTextFromDiffs))
                    {
                        return true;
                    }
                }
            }

            {
                var baseInReview = reviewTextFromDiffs.IndexOf(baseTextFromDiffs, StringComparison.InvariantCulture);

                if (baseInReview >= 0)
                {
                    Console.WriteLine("!!!!Base in review expand");
                    // TODO
                    return true;
                }
            }

            {
                var baseChangeNoContextDiffs = basePatch.Diffs.Where(x => !x.Operation.IsDelete).SkipWhile(x => x.Operation.IsEqual).TakeWhile(x => x.Operation.IsInsert).ToList();
                var baseChangeNoContext = LinePatchOps.DiffsCurrentText(baseChangeNoContextDiffs);

                var reviewChangeNoContextDiffs = reviewPatch.Diffs.Where(x => !x.Operation.IsDelete).SkipWhile(x => x.Operation.IsEqual).TakeWhile(x => x.Operation.IsInsert).ToList();
                var reviewChangeNoContext = LinePatchOps.DiffsCurrentText(reviewChangeNoContextDiffs);

                var reviewInBaseNoContext = baseChangeNoContext.IndexOf(reviewChangeNoContext, StringComparison.InvariantCulture);

                if (reviewInBaseNoContext >= 0 && reviewChangeNoContextDiffs.Any() && baseChangeNoContextDiffs.Any())
                {
                    var baseContextPrefixDiffs = basePatch.Diffs.Where(x => !x.Operation.IsDelete).TakeWhile(x => x.Operation.IsEqual).ToList();
                    var baseContextPrefixLength = baseContextPrefixDiffs.Sum(x => x.Lines.Count);

                    var reviewContextPrefixDiffs = reviewPatch.Diffs.Where(x => !x.Operation.IsDelete).TakeWhile(x => x.Operation.IsEqual).ToList();
                    var reviewContextPrefixLength = reviewContextPrefixDiffs.Sum(x => x.Lines.Count);

                    var reviewDiffNoContextStart = reviewPatch.Start2 + reviewContextPrefixLength;
                    var reviewDiffNoContextEnd = reviewDiffNoContextStart + reviewChangeNoContextDiffs.Sum(x => x.Lines.Count);

                    var contextPrefixLength = baseContextPrefixLength + reviewInBaseNoContext;
                    var contextSuffixLength = baseTextFromDiffs.Count(x=>x != "") - baseContextPrefixLength - baseChangeNoContextDiffs.Sum(x => x.Lines.Count);

                    if (reviewDiffNoContextStart >= contextPrefixLength)
                    {
                        var remainingReviewText = reviewText.Count - reviewDiffNoContextEnd;

                        var prefix = reviewText.Slice(reviewDiffNoContextStart - contextPrefixLength, contextPrefixLength);
                        var suffix = reviewText.Slice(reviewDiffNoContextEnd, Math.Min(contextSuffixLength, remainingReviewText));

                        var reviewTextReconstructedContext = prefix.Concat(reviewChangeNoContext).Concat(suffix);

                        if (reviewTextReconstructedContext.ConcatText() == baseTextFromDiffs.ConcatText())
                        {
                            return true;
                        }
                    }
                }
            }

            {
                var (basePrefix, baseChanges, baseSuffix) = basePatch.SplitPatchAffix();
                var (reviewPrefix, reviewChanges, reviewSuffix) = reviewPatch.SplitPatchAffix();

                if (LinePatchOps.DiffsCurrentText(baseChanges).ConcatText() == LinePatchOps.DiffsCurrentText(reviewChanges).ConcatText())
                {
                    var basePrefixLines = LinePatchOps.DiffsCurrentText(basePrefix);
                    var reviewPrefixLines = LinePatchOps.DiffsCurrentText(reviewPrefix);
                    var minPrefixLength = Math.Min(basePrefixLines.Count, reviewPrefixLines.Count);

                    if (basePrefixLines.TakeLast(minPrefixLength).ConcatText() == reviewPrefixLines.TakeLast(minPrefixLength).ConcatText())
                    {
                        var baseSuffixLines = LinePatchOps.DiffsCurrentText(baseSuffix);
                        var reviewSuffixLines = LinePatchOps.DiffsCurrentText(reviewSuffix);
                        var minSuffixLength = Math.Min(baseSuffixLines.Count, reviewSuffixLines.Count);

                        if (baseSuffixLines.Take(minSuffixLength).ConcatText() == reviewSuffixLines.Take(minSuffixLength).ConcatText())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static List<(DiffClassification classification, LinePatch Patch)> ClassifyPatches(List<string> reviewText1, List<LinePatch> basePatch,
            List<LinePatch> reviewPatch)
        {
            var classifiedPatches = new List<(DiffClassification classification, LinePatch Patch)>();
            var unusedBasePatches = new List<LinePatch>(basePatch);
            var rollingText = new List<string>(reviewText1);

            foreach (var patch in reviewPatch)
            {
                LinePatchOps.ApplyPatch(patch, rollingText);
                var basePatchIndex = unusedBasePatches.FindIndex(bp => ArePatchesMatching(bp, rollingText, patch));

                if (basePatchIndex == -1)
                {
                    classifiedPatches.Add((DiffClassification.ReviewChange, patch));
                    continue;
                }

                classifiedPatches.Add((DiffClassification.BaseChange, patch));
                unusedBasePatches.RemoveRange(0, basePatchIndex + 1);
            }

            return classifiedPatches;
        }
    }
}