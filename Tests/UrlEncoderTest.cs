using System;
using System.Web;
using NUnit.Framework;

namespace Tests
{
    public class UrlEncoderTest
    {
        [Test]
        [TestCase("gs/hamlib/Run Rotor Control.desktop", "gs%2fhamlib%2fRun%20Rotor%20Control.desktop")]
        public void Test(string input, string expected)
        {
            var encoded = Uri.EscapeDataString(input);

            Assert.That(encoded, Is.EqualTo(expected).IgnoreCase);
        }
    }
}