using Nancy;

namespace Web.Modules.Home
{
    public class HomeModule : NancyModule
    {
        public HomeModule() : base("/")
        {
            Get("/", _ => View["Index"]);
            Get("/{path*}", _ => View["Index"]);
        }
    }
}