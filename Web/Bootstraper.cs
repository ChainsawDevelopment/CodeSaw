using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Diagnostics;

namespace Web
{
    public class Bootstraper : AutofacNancyBootstrapper
    {
        private readonly string assetServer;
        private readonly ILifetimeScope _rootContext;

        public Bootstraper(string assetServer, ILifetimeScope rootContext)
        {
            this.assetServer = assetServer;
            _rootContext = rootContext;
        }

        protected override ILifetimeScope GetApplicationContainer()
        {
            return _rootContext;
        }

        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);

            environment.AddValue("AssetServer", this.assetServer);

            environment.Tracing(enabled: true, displayErrorTraces: true);
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.ViewLocationConventions.Clear();
            nancyConventions.ViewLocationConventions.Add((name, args, ctx) => $"Modules/{ctx.ModuleName}/Views/{name}");
        }
    }
}