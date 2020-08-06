using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSaw.Web
{
    public class FeatureToggle
    {
        private readonly HashSet<string> _enabledFeatures;

        public FeatureToggle()
        {
            _enabledFeatures = new HashSet<string>();
        }

        public IEnumerable<string> EnabledFeatures => _enabledFeatures.ToList();

        public Actions For(string featureName)
        {
            return new Actions(featureName, _enabledFeatures.Contains(featureName));
        }

        public void EnableFeatures(IEnumerable<string> featureNames)
        {
            _enabledFeatures.UnionWith(featureNames);
        }

        public class Actions
        {
            public string FeatureName { get; }

            public bool IsActive { get; }

            public Actions(string featureName, bool active)
            {
                FeatureName = featureName;
                IsActive = active;
            }

            public void WhenActive(Action action)
            {
                if (IsActive) action();
            }

            public void WhenInactive(Action action)
            {
                if (!IsActive) action();
            }

            public void Do(Action active, Action inactive)
            {
                if (IsActive)
                    active();
                else
                    inactive();
            }

            public TResult Map<TResult>(Func<TResult> active, Func<TResult> inactive)
            {
                return IsActive ? active() : inactive();
            }
        }
    }
}