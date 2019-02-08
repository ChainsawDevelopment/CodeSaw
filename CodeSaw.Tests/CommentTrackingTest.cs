using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeSaw.Web;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace CodeSaw.Tests
{
    public class CommentTrackingTest
    {
        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void Test(CommentSet set)
        {
            var trackedLine = CommentTracker.Track(set.CommentVersion, set.Right, set.CommentLine);

            Assert.That(trackedLine, Is.EqualTo(set.ExpectedCommentLineOnRight), "Comment should be tracked to correct line");
        }

        public static IEnumerable<TestCaseData> TestCases()
        {
            var names = typeof(CommentTrackingTest).Assembly.GetManifestResourceNames()
                .Where(x => x.StartsWith("CodeSaw.Tests.CommentTrackingData"))
                .Select(x => x.Substring("CodeSaw.Tests.CommentTrackingData.".Length))
                .Select(x => x.Substring(0, x.IndexOf(".")))
                .Distinct();

            foreach (var name in names)
            {
                yield return new TestCaseData(new CommentSet(name));
            }
        }

        public class CommentSet
        {
            public string CaseName { get; }
            public string Left { get; }
            public string Right { get; }
            public string CommentVersion { get; }
            public int CommentLine { get; }
            public int ExpectedCommentLineOnRight { get; }

            public CommentSet(string name)
            {
                CaseName = name;

                var config = (dynamic) JObject.Parse(ReadCaseFile("desc.json"));
                var leftVersion = (int) config.left;
                var rightVersion = (int) config.right;
                var commentVersion = (int) config.comment.version;

                Left = ReadCaseFile($"{leftVersion}.txt");
                Right = ReadCaseFile($"{rightVersion}.txt");
                CommentVersion = ReadCaseFile($"{commentVersion}.txt");
                CommentLine = (int) config.comment.line;
                ExpectedCommentLineOnRight = (int) config.comment.expected;
            }

            private string ReadCaseFile(string fileName)
            {
                var resourceName = $"CodeSaw.Tests.CommentTrackingData.{CaseName}.{fileName}";

                var resourceStream = typeof(CommentSet).Assembly.GetManifestResourceStream(resourceName);
                using (var streamReader = new StreamReader(resourceStream))
                {
                    return streamReader.ReadToEnd().NormalizeLineEndings();
                }
            }

            public override string ToString() => CaseName;
        }
    }
}