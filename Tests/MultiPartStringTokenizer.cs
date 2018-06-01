using System;
using System.Linq;
using NUnit.Framework;
using Web;

namespace Tests
{
    public class MultiPartStringTokenizerTests
    {
        [Test]
        [TestCase(new[] {"part1|part2|part3|part4"}, new[] {"part1", "part2", "part3", "part4"})]
        [TestCase(new[] {"part1|part2|part3|part4|"}, new[] {"part1", "part2", "part3", "part4"})]
        [TestCase(new[] {"part1|part2|", "part3|part4"}, new[] {"part1", "part2", "part3", "part4"})]
        [TestCase(new[] {"part1|part2", "|part3|part4"}, new[] {"part1", "part2", "part3", "part4"})]
        [TestCase(new [] {"part1|pa", "rt2|part3|part4"}, new[] {"part1", "part2", "part3", "part4"})]
        [TestCase(new [] {"part1|pa", "rt", "2|part3|part4"}, new[] {"part1", "part2", "part3", "part4"})]
        [TestCase(new [] {"part1|pa", "rt", "2||part3|part4"}, new[] {"part1", "part2", "", "part3", "part4"})]
        [TestCase(new[]{"part1"}, new [] {"part1"})]
        [TestCase(new[]{"part1|"}, new [] {"part1"})]
        [TestCase(new[]{"|part1|"}, new [] { "", "part1"})]
        [TestCase(new[]{"|||part1|||"}, new [] { "", "", "", "part1", "", ""})]
        public void Cases(string[] parts, string[] output)
        {
            var tokenizer = new MultiPartStringTokenizer<string>(s => s);
            using (var items = tokenizer.Enumerate('|', parts).GetEnumerator())
            {
                for (var i = 0; i < output.Length; i++)
                {
                    Assert.That(items.MoveNext(), Is.True);
                    Assert.That(items.Current.Text, Is.EqualTo(output[i]));
                    Assert.That(items.Current.Parts, Has.Count.GreaterThanOrEqualTo(1));
                }

                Assert.That(items.MoveNext(), Is.False);
            }
        }

        [Test]
        public void ProperMatchingOfContainersToResults()
        {
            var input = new[] {"part1|pa", "rt", "2|part3|part4"};
            var tokenizer = new MultiPartStringTokenizer<string>(s => s);
            var items = tokenizer.Enumerate('|', input).ToList();

            Assert.That(items[0].Parts, Is.EqualTo(new[] {"part1|pa"}));
            Assert.That(items[1].Parts, Is.EqualTo(new[] {"part1|pa", "rt", "2|part3|part4"}));
            Assert.That(items[2].Parts, Is.EqualTo(new[] {"2|part3|part4"}));
            Assert.That(items[3].Parts, Is.EqualTo(new[] {"2|part3|part4"}));
        }
    }
}