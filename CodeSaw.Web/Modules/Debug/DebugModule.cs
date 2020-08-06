using Nancy;

namespace CodeSaw.Web.Modules.Debug
{
    public class DebugModule : NancyModule
    {
        public DebugModule(FeatureToggle features):base("/debug")
        {
            Get("/features", _ => features.EnabledFeatures);
        }
    }
}