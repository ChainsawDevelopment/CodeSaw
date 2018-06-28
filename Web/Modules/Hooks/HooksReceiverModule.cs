using System;
using Nancy;

namespace Web.Modules.Hooks
{
    public class HooksReceiverModule : NancyModule
    {
        public HooksReceiverModule()
        {
            Post("/hooks/{type}", async _ =>
            {
                Console.WriteLine("Incoming hook {0}", (string)_.type);
                return Response.AsJson(new {ok = true});
            });
        }
    }
}