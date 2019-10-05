﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiffMatchPatch;

namespace CodeSaw.Web.Diff
{
    public class FourWayDiff
    {
        private static readonly DiffMatchPatch.DiffMatchPatch DMP = new DiffMatchPatch.DiffMatchPatch(1f, (short) 32, 4, 0.5f, 1000, 32, 0.5f, (short) 4);


        public static List<Patch> MakePatch(string text1, string text2)
        {
            var patches = MakePatchRaw(text1, text2);

            patches = patches.SelectMany(SplitPatchIntoAtoms).ToList();

            return patches;
        }

        public static List<Patch> MakePatchRaw(string text1, string text2)
        {
            var a = DMP.DiffLinesToChars(text1, text2);
            var lineText1 = a.Item1;
            var lineText2 = a.Item2;
            var lineArray = a.Item3;
            var diffs = DMP.DiffMain(lineText1, lineText2, true);
            DMP.DiffCharsToLines(diffs, lineArray);
            var patches = DMP.Patchmake(diffs);
            return patches;
        }

        public static IEnumerable<Patch> SplitPatchIntoAtoms(Patch patch)
        {
            using (var diff = patch.Diffs.GetEnumerator())
            {
                var currentPatch = new List<DiffMatchPatch.Diff>();

                var start1 = patch.Start1;
                var start2 = patch.Start2;
                var length1 = 0;
                var length2 = 0;
                var yl1 = 0;
                var yl2 = 0;

                while (diff.MoveNext())
                {
                    var partLength1 = !diff.Current.Operation.IsInsert ? diff.Current.Text.Length : 0;
                    var partLength2 = !diff.Current.Operation.IsDelete ? diff.Current.Text.Length : 0;

                    length1 += partLength1;
                    length2 += partLength2;

                    currentPatch.Add(diff.Current);

                    if (diff.Current.Operation.IsEqual && currentPatch.Count > 1)
                    {
                        // sink
                        var p = new Patch(
                            diffs: currentPatch,
                            start1: start1,
                            start2: start2,
                            length1: length1,
                            length2: length2
                        );
                        yl1 += p.Length1;
                        yl2 += p.Length2;

                        yield return p;

                        var operationDiff = length2 - length1;

                        start1 = p.Start1 + p.Length1 - diff.Current.Text.Length + operationDiff;
                        start2 = p.Start2 + p.Length2 - diff.Current.Text.Length;

                        length1 = length2 = diff.Current.Text.Length;

                        currentPatch = new List<DiffMatchPatch.Diff>() {diff.Current};
                    }
                }

                if (currentPatch.Count > 1)
                {
                    var p = new Patch(
                        diffs: currentPatch,
                        start1: start1,
                        start2: start2,
                        length1: length1,
                        length2: length2
                    );

                    yl1 += p.Length1;
                    yl2 += p.Length2;

                    yield return p;
                }

                //if (start1 + length1 != patch.Start1 + patch.Length1)
                //{
                //    throw new InvalidOperationException("A1");
                //}

                //if (start2 + length2 != patch.Start2 + patch.Length2)
                //{
                //    throw new InvalidOperationException("A2");
                //}

                //if (yl1 == patch.Length1)
                //{
                //    throw new InvalidOperationException("B1");
                //}

                //if (yl2 == patch.Length2)
                //{
                //    throw new InvalidOperationException("B2");
                //}
            }
        }

        public static bool ArePatchesMatching(string baseText, Patch basePatch, string reviewText, Patch reviewPatch)
        {
            var baseTextFromDiffs = DiffMatchPatchModule.Default.DiffText2(basePatch.Diffs);
            var reviewTextFromDiffs = DiffMatchPatchModule.Default.DiffText2(reviewPatch.Diffs);

            {
                var reviewInBase = baseTextFromDiffs.IndexOf(reviewTextFromDiffs, StringComparison.InvariantCulture);

                if (reviewInBase >= 0 && reviewPatch.Start2 - reviewInBase >= 0)
                {
                    // extend review to match base
                    var prefix = reviewText.Substring(reviewPatch.Start2 - reviewInBase, reviewInBase);
                    var suffix = reviewText.Substring(reviewPatch.Start2 + reviewPatch.Length2, baseTextFromDiffs.Length - reviewPatch.Length2 - reviewInBase);

                    var extended = prefix + reviewTextFromDiffs + suffix;

                    if (extended == baseTextFromDiffs)
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
                var baseChangeNoContextDiffs = basePatch.Diffs.Where(x=>!x.Operation.IsDelete).SkipWhile(x => x.Operation.IsEqual).TakeWhile(x => x.Operation.IsInsert).ToList();
                var baseChangeNoContext = DiffMatchPatchModule.Default.DiffText2(baseChangeNoContextDiffs).Trim();

                var reviewChangeNoContextDiffs = reviewPatch.Diffs.Where(x=>!x.Operation.IsDelete).SkipWhile(x => x.Operation.IsEqual).TakeWhile(x => x.Operation.IsInsert).ToList();
                var reviewChangeNoContext = DiffMatchPatchModule.Default.DiffText2(reviewChangeNoContextDiffs).Trim();

                var reviewInBaseNoContext = baseChangeNoContext.IndexOf(reviewChangeNoContext, StringComparison.InvariantCulture);

                if (reviewInBaseNoContext >= 0 && reviewChangeNoContextDiffs.Any() && baseChangeNoContextDiffs.Any())
                {
                    var baseContextPrefixDiffs = basePatch.Diffs.Where(x => !x.Operation.IsDelete).TakeWhile(x => x.Operation.IsEqual).ToList();
                    var baseContextPrefixLength = baseContextPrefixDiffs.Sum(x => x.Text.Length);

                    var reviewContextPrefixDiffs = reviewPatch.Diffs.Where(x => !x.Operation.IsDelete).TakeWhile(x => x.Operation.IsEqual).ToList();
                    var reviewContextPrefixLength = reviewContextPrefixDiffs.Sum(x => x.Text.Length);

                    var reviewDiffNoContextStart = reviewPatch.Start2 + reviewContextPrefixLength;
                    var reviewDiffNoContextEnd = reviewDiffNoContextStart + reviewChangeNoContextDiffs.Sum(x => x.Text.Length);

                    var contextPrefixLength = baseContextPrefixLength + reviewInBaseNoContext;
                    var contextSuffixLength = baseTextFromDiffs.Length - baseContextPrefixLength - baseChangeNoContextDiffs.Sum(x => x.Text.Length);

                    if (reviewDiffNoContextStart >= contextPrefixLength)
                    {
                        var prefix = reviewText.Substring(reviewDiffNoContextStart - contextPrefixLength, contextPrefixLength);
                        var suffix = reviewText.Substring(reviewDiffNoContextEnd, contextSuffixLength);

                       
                        var reviewTextReconstructedContext = prefix + reviewChangeNoContext + suffix;

                        if (reviewTextReconstructedContext == baseTextFromDiffs)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static List<(DiffClassification classification, Patch Patch)> ClassifyPatches(string baseText2, List<Patch> basePatch, string reviewText2, List<Patch> reviewPatch)
        {
            var classifiedPatches = new List<(DiffClassification classification, Patch Patch)>();
            var unusedBasePatches = new List<Patch>(basePatch);

            foreach (var patch in reviewPatch)
            {
                var basePatchIndex = unusedBasePatches.FindIndex(bp => ArePatchesMatching(baseText2, bp, reviewText2, patch));

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

        public static void ExpandPatchToFullLines(string text, Patch patch)
        {
            var middle = DMP.DiffText2(patch.Diffs);

            if (middle == "")
            {
                return;
            }

            if (middle[0] != '\n' && patch.Start2 > 0)
            {
                var prevNewLine = text.LastIndexOf('\n', patch.Start2);

                string prefix;
                if (prevNewLine == -1)
                {
                    prefix = text.Substring(0, patch.Start2);
                }
                else
                {
                    prefix = text.Substring(prevNewLine + 1, patch.Start2 - 1 - prevNewLine);
                }

                patch.Diffs[0].Text = prefix + patch.Diffs[0].Text;
                patch.Start2 -= prefix.Length;
                patch.Length2 += prefix.Length;
                patch.Start1 -= prefix.Length;
                patch.Length1 += prefix.Length;
            }

            var patchEnd = patch.Start2 + patch.Length2;
            
            if (middle[middle.Length - 1] != '\n' && patchEnd < text.Length-1)
            {
                var nextNewLine = text.IndexOf('\n', patchEnd);

                if (nextNewLine == -1)
                {
                    nextNewLine = text.Length - 1;
                }

                var suffix = text.Substring(patchEnd, nextNewLine - patchEnd);
                patch.Length2 += suffix.Length;
                patch.Length1 += suffix.Length;
                patch.Diffs[patch.Diffs.Count - 1].Text += suffix;
            }
        }
    }
}