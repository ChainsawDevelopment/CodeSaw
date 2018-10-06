using System;
using System.Collections.Generic;
using CodeSaw.Web;
using CodeSaw.Web.Diff;
using DiffMatchPatch;

namespace CodeSaw.Tests
{
    public class CommentTracker
    {
        public static int Track(string commentVersion, string newVersion, int line)
        {
            var patches = MakeDiff(commentVersion, newVersion);

            DiffUtils.UnrollContext(patches);

            foreach (var patch in patches)
            {
                Console.WriteLine($"@@ Prev: {patch.Start1}, {patch.Length1} Cur: {patch.Start2}, {patch.Length2}");

                foreach (var diff in patch.Diffs)
                {
                    Console.Write($"{diff.Operation.ToString()[0]}({diff.Text})");
                }

                Console.WriteLine("\n");
            }

            

            return line;
        }

        private static List<Patch> MakeDiff(string previous, string current)
        {
            return FourWayDiff.MakePatch(previous, current);
        }
    }
}