using System;
using System.Linq;
using LibGit2Sharp;
using NUnit.Framework;

namespace Tests
{
    public class Class1
    {
        [Test]
        public void ListMasterCommit()
        {
            using (var repository = GetRepo())
            {
                var branch = repository.Branches["master"];
                foreach (var commit in branch.Commits)
                {
                    TestContext.WriteLine($"Commit: {commit.Sha} {commit.Message}");
                }
            }
        }

        [Test]
        public void ShowDiff()
        {
            using (var repository = GetRepo())
            {
                var branch = repository.Branches["master"];
                var tip = branch.Tip;
                var prev = tip.Parents.First();

                var tipFile = tip["file1.txt"].Target as Blob;
                var prevFile = prev["file1.txt"].Target as Blob;

                var diff = repository.Diff.Compare(prevFile, tipFile);

                TestContext.WriteLine(diff.Patch);
            }
        }

        [Test]
        public void InvestigatePatch()
        {
            using (var repository = GetRepo())
            {
                var branch = repository.Branches["master"];
                var tip = branch.Tip;
                var prev = tip.Parents.First();

                var tipFile = tip["file1.txt"].Target as Blob;
                var prevFile = prev["file1.txt"].Target as Blob;

                var tipText = tipFile.GetContentText();
                var prevText = prevFile.GetContentText();

                var diffs = DiffMatchPatch.DiffMatchPatchModule.Default.DiffMain(prevText, tipText);

                TestContext.WriteLine("Diffs:");
                foreach (var diff in diffs)
                {
                    TestContext.WriteLine($"{diff.Operation}: {diff.Text}");
                }
            }
        }

        private static Repository GetRepo()
        {
            return new Repository(@"D:\tmp\playground-git");
        }
    }
}
