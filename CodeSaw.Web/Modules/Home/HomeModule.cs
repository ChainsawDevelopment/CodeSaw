using System.Linq;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses.Negotiation;

namespace CodeSaw.Web.Modules.Home
{
    public class HomeModule : NancyModule
    {
        public HomeModule() : base("/")
        {
            Get("/", _ => Index());
            Get("/{path*}", _ => Index());
        }

        private Negotiator Index()
        {
            return View["Index"].WithModel(new {
                AssetBase = Context.Environment["assetServer"] ?? "/dist",
                IsLocal = Context.Request.IsLocal() ? "true" : "false",
                IsDebug = IsDebug ? "true" : "false",
            });
        }

        private bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else 
                return false;
#endif
            }
        }
    }
}