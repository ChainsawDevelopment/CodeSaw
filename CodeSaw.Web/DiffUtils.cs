using System.Collections.Generic;
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
    }
}