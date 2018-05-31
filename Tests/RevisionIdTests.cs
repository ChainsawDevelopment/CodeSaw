using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Web;

namespace Tests
{
    public class RevisionIdTests
    {
        [Test]
        [TestCaseSource(nameof(ParseCases))]
        public void ParseTest(string input, IResolveConstraint assert)
        {
            Assert.That(() => RevisionId.Parse(input), assert);
        }

        [Test]
        [TestCase("-1")]
        [TestCase("0")]
        [TestCase("2.5")]
        [TestCase("abc")]
        [TestCase("b08e7d9253f4e692cb391207276392eaa1f6a0f")]
        [TestCase("b08ZZZZ253f4e692cb391207276392eaa1f6a0f2")]
        public void InvalidInputParseTest(string s)
        {
            Assert.That(() => RevisionId.Parse(s), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(RevisionId.TryParse(s, out _), Is.False);
        }

        public static IEnumerable<TestCaseData> ParseCases()
        {
            yield return new TestCaseData("base", Is.InstanceOf<RevisionId.Base>());
            yield return new TestCaseData("7", Is.InstanceOf<RevisionId.Selected>().And.Property("Revision").EqualTo(7));
            yield return new TestCaseData("b08e7d9253f4e692cb391207276392eaa1f6a0f2", Is.InstanceOf<RevisionId.Hash>().And.Property("CommitHash").EqualTo("b08e7d9253f4e692cb391207276392eaa1f6a0f2"));
        }

        [Test]
        [TestCase("base", "base")]
        [TestCase("9", "Selected 9")]
        [TestCase("b08e7d9253f4e692cb391207276392eaa1f6a0f2", "Hash b08e7d9253f4e692cb391207276392eaa1f6a0f2")]
        public void ResolveTest(string revisionId, string expected)
        {
            var result = RevisionId.Parse(revisionId).Resolve(
                resolveBase: () => "base",
                resolveSelected: s => $"Selected {s.Revision}",
                resolveHash: h => $"Hash {h.CommitHash}"
            );

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
