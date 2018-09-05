using System.Collections.Generic;
using DiffMatchPatch;
using LibGit2Sharp;
using Diff = DiffMatchPatch.Diff;

namespace CodeSaw.Tests
{
    public static class FindDiff
    {
        public static List<Diff> NiceDiff(this Repository repo, string path, string previousRef, string currentRef)
        {
            var previousContent = repo.CatFile(path, previousRef);
            var currentContent = repo.CatFile(path, currentRef);

            var diff = DiffMatchPatchModule.Default.DiffMain(previousContent, currentContent);

            return diff;
        }
    }
}