using System;
using System.Collections.Generic;
using System.IO;
using DiffMatchPatch;
using LibGit2Sharp;
using NUnit.Framework;
using Diff = DiffMatchPatch.Diff;
using Patch = LibGit2Sharp.Patch;

namespace CodeSaw.Tests.Playground
{
    [Explicit]
    public class PlayingWithGitAndGettingDiffs
    {
        private const string File1 = "file1.txt";
        private readonly string RepoDir = @"D:\tmp\repo-test";
        private Repository _repo;

        public Signature JohnDoe => new Signature("John Doe", "john.doe@git.test", DateTimeOffset.UtcNow);
        public Signature JohnSmith => new Signature("John Smith", "john.smith@git.test", DateTimeOffset.UtcNow);

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(RepoDir))
            {
                DeleteFolder(new DirectoryInfo(RepoDir));
            }

            Repository.Init(RepoDir);
            _repo = new Repository(RepoDir);
            _repo.Config.Set("core.autocrlf", "false", ConfigurationLevel.Local);
            _repo.Config.Set("core.eol", "lf", ConfigurationLevel.Local);
        }

        private void DeleteFolder(DirectoryInfo directory)
        {
            directory.Attributes = FileAttributes.Normal;

            foreach (var fileInfo in directory.GetFiles())
            {
                fileInfo.Attributes = FileAttributes.Normal;
                fileInfo.Delete();
            }

            foreach (var directoryInfo in directory.GetDirectories())
            {
                DeleteFolder(directoryInfo);
            }

            directory.Delete(false);
        }

        [TearDown]
        public void TearDown()
        {
            _repo?.Dispose();
        }

        [Test]
        public void X()
        {
            _repo.OnBranch("master", r =>
            {
                r.Commit(JohnDoe, "M1", rc =>
                {
                    rc.SetFile(File1, s =>
                    {
                        s.WriteLine("test1");
                        s.WriteLine("test2");
                    });

                    r.Stage(File1);
                }).Ref("mark/m/1");

                r.Commit(JohnDoe, "M2", rc =>
                {
                    rc.AppendFile(File1, s =>
                    {
                        s.WriteLine("test3");
                    });

                    r.Stage(File1);
                }).Ref("mark/m/2");

                r.Commit(JohnDoe, "M3", rc =>
                {
                    rc.AppendFile(File1, s =>
                    {
                        s.WriteLine("test4");
                    });

                    r.Stage(File1);
                }).Ref("mark/m/3");
            });

            _repo.OnBranch("branch", r =>
            {
                r.Commit(JohnSmith, "B1", rc =>
                {
                    rc.AppendFile(File1, sw => sw.WriteLine("test4"));
                    rc.Stage(File1);
                }).Ref("mark/b/1");

                r.Commit(JohnSmith, "B1", rc =>
                {
                    rc.AppendFile(File1, sw => sw.WriteLine("test5"));
                    rc.Stage(File1);
                }).Ref("mark/b/2");

                r.Commit(JohnSmith, "B1", rc =>
                {
                    rc.SetFile(File1, sw =>
                    {
                        sw.WriteLine("test1");
                        sw.WriteLine("here");
                        sw.WriteLine("test2");
                        sw.WriteLine("test3");
                        sw.WriteLine("test4");
                        sw.WriteLine("test4");
                    });
                    rc.Stage(File1);
                }).Ref("mark/b/3");
            });

            var diff = _repo.NiceDiff(File1, "mark/m/3", "mark/b/3");

            PrintDiff(diff);
        }

        [Test]
        public void Y()
        {
            _repo.OnBranch("master", r =>
            {
                r.Commit(JohnSmith, "M1", rf =>
                {
                    rf.SetFile(File1, "block1\n\nblock2\n");
                    rf.Stage(File1);
                }).Ref("mark/m/1");
            });

            _repo.OnBranch("branch", r =>
            {
                r.Commit(JohnDoe, "B1", rf =>
                {
                    rf.SetFile(File1, "block1\n\nblock2\nline2.1\nline2.2\n");
                    rf.Stage(File1);
                }).Ref("mark/b/1");
            });

            _repo.OnBranch("master", r =>
            {
                r.Commit(JohnSmith, "M2", rf =>
                {
                    rf.SetFile(File1, "block1\nline1.1\nline1.2\n\nblock2\n");
                    rf.Stage(File1);
                }).Ref("mark/m/2");
            });

            _repo.OnBranch("branch", r =>
            {
                var mr = r.Merge(r.Branches["master"], JohnDoe);

                Console.WriteLine(mr);
                Assert.That(mr.Status, Is.EqualTo(MergeStatus.NonFastForward));

                r.Refs.Add("mark/b/2m", mr.Commit.Id);

                r.Commit(JohnDoe, "B2", rc =>
                {
                    rc.AppendFile(File1, "\nblock3\nline3.1\nline3.2\n");
                    rc.Stage(File1);
                }).Ref("mark/b/3");
            });

            //var diff = _repo.NiceDiff(File1, "mark/b/1", "mark/b/3");

            //PrintDiffText(_repo.CatFile(File1, "mark/b/1"), diff);

            var diff1 = _repo.Diff.Compare<Patch>(
                (_repo.Lookup<Commit>("mark/b/1").Tree),
                (_repo.Lookup<Commit>("mark/b/3").Tree)
            );

            Console.WriteLine(diff1.Content);

            var mb1 = _repo.Lookup<Commit>("mark/m/1");
            var mb2 = _repo.Lookup<Commit>("mark/b/2m");

            var diff2 = _repo.Diff.Compare<Patch>(
                (mb1.Tree),
                (mb2.Tree)
            );

            Console.WriteLine("\n\n\n===================\n\n\n");
            Console.WriteLine(diff2.Content);
        }

        private void PrintDiffText(string baseStr, List<Diff> diff)
        {
            var patchMake = DiffMatchPatch.DiffMatchPatchModule.Default.PatchMake(baseStr, diff);
            var patchToText = DiffMatchPatchModule.Default.PatchToText(patchMake).Replace("%0a", "\n");
            Console.WriteLine(patchToText);
        }

        private static void PrintDiff(List<Diff> diff)
        {
            foreach (var item in diff)
            {
                Console.WriteLine(item);
            }
        }
    }
}