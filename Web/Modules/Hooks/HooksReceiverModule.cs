using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Features.Indexed;
using Nancy;
using RepositoryApi.Hooks;

namespace Web.Modules.Hooks
{
    public class HooksReceiverModule : NancyModule
    {
        public HooksReceiverModule(IIndex<string, IHookHandler> hookHandler, Func<ITriggerAction> action)
        {
            Post("/hooks/{type}", async _ =>
            {
                var handler = hookHandler[(string)_.type];
                
                var headers = Request.Headers.ToDictionary(x => x.Key, x => x.Value);

                var @event = new HookEvent(headers, Request.Body);

                await handler.Handle(@event, action());

                return Response.AsJson(new {ok = true});
            });
        }
    }
}