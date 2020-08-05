using System.Collections.Generic;
using System.Text;
using CodeSaw.Web;
using Microsoft.FSharp.Core;
using NUnit.Framework;

namespace CodeSaw.Tests
{
    public class FeatureToggleTest
    {
        [Test]
        public void CheckIfActive_Active()
        {
            var toggler = new FeatureToggle();
            toggler.EnableFeatures(new List<string> {"Feature1"});
            var feature = toggler.For("Feature1");

            Assert.That(feature.IsActive, Is.True);
        }

        [Test]
        public void CheckIfActive_NotActive()
        {
            var toggler = new FeatureToggle();
            var feature = toggler.For("Feature1");

            Assert.That(feature.IsActive, Is.False);
        }

        [Test]
        public void DoIfActive_Active()
        {
            var toggler = new FeatureToggle();
            toggler.EnableFeatures(new List<string> { "Feature1" });
            var feature = toggler.For("Feature1");

            bool flag = false;

            feature.WhenActive(() => flag = true);

            Assert.That(flag, Is.True);
        }

        [Test]
        public void DoIfActive_NotActive()
        {
            var toggler = new FeatureToggle();
            var feature = toggler.For("Feature1");

            bool flag = false;

            feature.WhenActive(() => flag = true);

            Assert.That(flag, Is.False);
        }

        [Test]
        public void DoIfInactive_Active()
        {
            var toggler = new FeatureToggle();
            toggler.EnableFeatures(new List<string> { "Feature1" });
            var feature = toggler.For("Feature1");

            bool flag = false;

            feature.WhenInactive(() => flag = true);

            Assert.That(flag, Is.False);
        }

        [Test]
        public void DoIfInactive_NotActive()
        {
            var toggler = new FeatureToggle();
            var feature = toggler.For("Feature1");

            bool flag = false;

            feature.WhenInactive(() => flag = true);

            Assert.That(flag, Is.True);
        }

        [Test]
        public void Do_Active()
        {
            var toggler = new FeatureToggle();
            toggler.EnableFeatures(new List<string> { "Feature1" });
            var feature = toggler.For("Feature1");
            int i = 0;

            feature.Do(active: () => i = 1, inactive: () => i = 2);

            Assert.That(i, Is.EqualTo(1));
        }

        [Test]
        public void Do_Inactive()
        {
            var toggler = new FeatureToggle();
            var feature = toggler.For("Feature1");
            int i = 0;

            feature.Do(active: () => i = 1, inactive: () => i = 2);

            Assert.That(i, Is.EqualTo(2));
        }

        [Test]
        public void Map_Active()
        {
            var toggler = new FeatureToggle();
            toggler.EnableFeatures(new List<string> { "Feature1" });
            var feature = toggler.For("Feature1");

            int i = feature.Map(active: () => 1, inactive: () => 2);

            Assert.That(i, Is.EqualTo(1));
        }

        [Test]
        public void Map_Inactive()
        {
            var toggler = new FeatureToggle();
            var feature = toggler.For("Feature1");

            int i = feature.Map(active: () => 1, inactive: () => 2);

            Assert.That(i, Is.EqualTo(2));
        }
    }
}
