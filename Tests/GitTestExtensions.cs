using System;
using System.IO;
using System.Text;
using LibGit2Sharp;
using NUnit.Framework;

namespace Tests
{
    public static class GitTestExtensions
    {
        public static void OnBranch(this Repository repo, string branchName, Action<Repository> action)
        {
            action = AssertClean(action);

            if (repo.Head.FriendlyName == branchName)
            {
                action(repo);
                return;
            }

            var branch = repo.Branches[branchName];

            if (branch == null)
            {
                branch = repo.CreateBranch(branchName);
            }

            Commands.Checkout(repo, branch);

            Assert.That(repo.Head.FriendlyName, Is.EqualTo(branchName), "Branch is switched");

            action(repo);
        }

        public static (Repository, Commit) Commit(this Repository repo, Signature author, string message, Action<Repository> action)
        {
            return AssertClean(r =>
            {
                action(r);
                return (repo, r.Commit(message, author, author));
            })(repo);
        }

        public static void SetFile(this Repository repo, string filePath, Action<StreamWriter> action)
        {
            var path = Path.Combine(repo.Info.WorkingDirectory, filePath);

            using (var fileStream = File.Create(path))
            {
                using (var sw = new StreamWriter(fileStream, Encoding.ASCII, 100, true))
                {
                    sw.NewLine = "\n";
                    action(sw);
                }
            }
        }

        public static void SetFile(this Repository repo, string filePath, string content)
        {
            repo.SetFile(filePath, sw => sw.Write(content));
        }

        public static void AppendFile(this Repository repo, string filePath, Action<StreamWriter> action)
        {
            var path = Path.Combine(repo.Info.WorkingDirectory, filePath);

            using (var fileStream = File.OpenWrite(path))
            {
                fileStream.Seek(0, SeekOrigin.End);

                using (var sw = new StreamWriter(fileStream, Encoding.ASCII, 100, true))
                {
                    sw.NewLine = "\n";
                    action(sw);
                }
            }
        }

        public static void AppendFile(this Repository repo, string filePath, string content)
        {
            repo.AppendFile(filePath, sw => sw.Write(content));
        }

        public static void Ref(this (Repository, Commit) @this, string name)
        {
            @this.Item1.Refs.Add(name, @this.Item2.Id);
        }

        private static Action<Repository> AssertClean(Action<Repository> inner)
        {
            return r =>
            {
                var status = r.RetrieveStatus();
                Assert.That(status, Has.Property("IsDirty").False, "Repo should be clear before action");

                inner(r);

                status = r.RetrieveStatus();
                Assert.That(status, Has.Property("IsDirty").False, "Repo should be clear before action");
            };
        }

        private static Func<Repository, T> AssertClean<T>(Func<Repository, T> inner)
        {
            return r =>
            {
                var status = r.RetrieveStatus();
                Assert.That(status, Has.Property("IsDirty").EqualTo(false), "Repo should be clear before action");

                var result = inner(r);

                status = r.RetrieveStatus();
                Assert.That(status, Has.Property("IsDirty").EqualTo(false), "Repo should be clear before action");

                return result;
            };
        }

        public static void Stage(this Repository repo, string pathInTheWorkdir)
        {
            Commands.Stage(repo, pathInTheWorkdir);
        }

        public static string CatFile(this Repository repo, string filePath, string versionRef)
        {
            var commit = repo.Lookup<Commit>(versionRef);
            var blob = commit[filePath].Target as Blob;

            return blob.GetContentText();
        }
    }
}