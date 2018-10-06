using System;
using System.Collections.Generic;
using System.Linq;
using DiffMatchPatch;

namespace CodeSaw.Web
{
    public static class DiffUtils
    {
        public static void UnrollContext(List<Patch> patches)
        {
            int totalDiff = 0;

            foreach (var patch in patches)
            {
                patch.Start1 -= totalDiff;
                totalDiff += patch.Length2 - patch.Length1;
            }
        }

        public static List<Patch> MergeAdjacentPatches(List<Patch> patches)
        {
            var result = new List<Patch>();

            foreach (var patch in patches)
            {
                if (result.Count == 0)
                {
                    result.Add(patch);
                    continue;
                }

                var lastPatch = result.Last();

                if (patch.Start2 < lastPatch.Start2+lastPatch.Length2)
                {
                    var commonLength = lastPatch.Start2 + lastPatch.Length2 - patch.Start2;
                    var remainingPrefix = patch.Diffs[0].Text.Substring(commonLength);
                    lastPatch.Diffs.Last().Text += remainingPrefix;
                    lastPatch.Diffs.AddRange(patch.Diffs.Skip(1));
                    lastPatch.Length2 = lastPatch.Length2 + patch.Length2 - commonLength;
                    lastPatch.Length1 = lastPatch.Length1 + patch.Length1 - commonLength;
                    continue;
                }

                result.Add(patch);
            }

            return result;
        }
    }
}