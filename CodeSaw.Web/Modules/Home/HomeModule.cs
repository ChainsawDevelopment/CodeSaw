using System.Linq;
using Nancy;

namespace CodeSaw.Web.Modules.Home
{
    public class HomeModule : NancyModule
    {
        public HomeModule() : base("/")
        {
            Get("/", _ => View["Index"].WithModel(new {
                AssetBase = Context.Environment["assetServer"] ?? "/dist"
            }));
            Get("/a", _ => new {
                env = this.Context.Environment.Select(x=>$"{x.Key} = {x.Value}")
            });
            Get("/{path*}", _ => View["Index"].WithModel(new {
                AssetBase = Context.Environment["assetServer"] ?? "/dist"
            }));
        }
    }
}