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
            var result = new List<Patch>(new [] { patches.First() });

            foreach (var patch in patches.Skip(1))
            {                
                var lastPatch = result.Last();
                
                if (patch.Start2 < lastPatch.Start2 + lastPatch.Length2)
                {
                    var commonLength = lastPatch.Start2 + lastPatch.Length2 - patch.Start2;

                    // Overwrite common patch between lastPath and patch with new incoming patch if needed                    
                    var remainingPartToDelete = commonLength;
                    while(remainingPartToDelete > 0)
                    {
                        // Remove common part from lastPatch
                        if (lastPatch.Diffs.Last().Operation == Operation.Delete)
                        {
                            lastPatch.Diffs.Remove(lastPatch.Diffs.Last());
                            continue;
                        }

                        var lastLength = lastPatch.Diffs.Last().Text.Length;
                        if (remainingPartToDelete >= lastPatch.Diffs.Last().Text.Length)
                        {
                            lastPatch.Diffs.Remove(lastPatch.Diffs.Last());
                            remainingPartToDelete -= lastLength;
                        }
                        else
                        {
                            lastPatch.Diffs.Last().Text = lastPatch.Diffs.Last().Text.Substring(0, lastPatch.Diffs.Last().Text.Length - remainingPartToDelete);
                            remainingPartToDelete = 0;
                        }
                    }

                    // Append entire new patch without changes
                    lastPatch.Diffs.AddRange(patch.Diffs);
                    lastPatch.Length2 += patch.Length2 - commonLength;
                    lastPatch.Length1 += patch.Length1 - commonLength;
                }

                result.Add(patch);
            }

            return result;
        }
    }
}