using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Bootstrapper;
using NLog;

namespace CodeSaw.Web
{
    public static class ModuleExtensions
    {
        public static void AddToLogContext(this NancyModule module, Dictionary<string, Func<object>> values)
        {
            module.Before += async (ctx, ct) =>
            {
                var context = (Dictionary<string, object>)MappedDiagnosticsLogicalContext.GetObject("context");

                foreach (var (key, value) in values)
                {
                    context.Add(key, value());
                }

                return null;
            };

            module.After += async (ctx, ct) =>
            {
                var context = (Dictionary<string, object>)MappedDiagnosticsLogicalContext.GetObject("context");
                
                foreach (var key in values.Keys)
                {
                    context.Remove(key);
                }
            };
        }

        public static void AddToLogContext(this IPipelines pipelines, Dictionary<string, Func<NancyContext, object>> values)
        {
            pipelines.BeforeRequest += async (ctx, ct) =>
            {
                var context = (Dictionary<string, object>)MappedDiagnosticsLogicalContext.GetObject("context");
                
                foreach (var (key, value) in values)
                {
                    context.Add(key, value(ctx));
                }

                return null;
            };

            pipelines.AfterRequest += async (ctx, ct) =>
            {
                var context = (Dictionary<string, object>)MappedDiagnosticsLogicalContext.GetObject("context");

                foreach (var key in values.Keys)
                {
                    context.Remove(key);
                }
            };
        }
    }
}