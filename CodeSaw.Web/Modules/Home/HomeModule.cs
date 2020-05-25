using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeSaw.Web.Modules.Home
{
    public class HomeModule : NancyModule
    {
        private static List<string> FrontAssets = null;

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
                Assets = LoadAssets()
            });
        }

        private List<string> LoadAssets()
        {
            if (FrontAssets != null)
            {
                return FrontAssets;
            }

            if (Context.Environment["assetServer"] != null)
            {
                FrontAssets = new List<string>
                {
                    "dist.js"
                };
            }
            else
            {
                var p = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "webpack-assets.json");
                var assets = JObject.Parse(File.ReadAllText(p));

                FrontAssets = new List<string>
                {
                    assets.Property("main").Value.Value<JObject>().Property("js").Value.Value<string>()
                };
            }

            return FrontAssets;
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