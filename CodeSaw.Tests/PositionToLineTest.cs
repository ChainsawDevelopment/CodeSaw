using CodeSaw.Web.Diff;
using NUnit.Framework;

namespace CodeSaw.Tests
{
    public class PositionToLineTest
    {
        [Test]
        [TestCase("test")]
        [TestCase("line1\nline2\nline3")]
        [TestCase("line1\nline2\nline3\n")]
        [TestCase("\nline1\nline2\nline3\n")]
        [TestCase("\nline1\n\nline2\nline3\n")]
        [TestCase("\nline1\n\n\nline2\nline3\n")]
        [TestCase("\n\n\nline\n\n\n\n")]
        public void X(string input)
        {
            var map = new PositionToLine(input);

            var expectedLineNo = 0;

            for (int i = 0; i < input.Length; i++)
            {
                var lineNo = map.GetLineinPosition(i);
                Assert.That(lineNo, Is.EqualTo(expectedLineNo), $"Mismatch at position {i}");

                if (input[i] == '\n')
                {
                    expectedLineNo++;
                }
            }
        }
    }
}