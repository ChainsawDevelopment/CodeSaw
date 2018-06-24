using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core.Lifetime;
using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Diagnostics;
using NHibernate;
using Web.Auth;
using ISession = NHibernate.ISession;

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

        protected override ILifetimeScope CreateRequestContainer(NancyContext context)
        {
            Action<ContainerBuilder> register = builder =>
            {
                builder.Register(ctx =>
                    {
                        var http = ctx.Resolve<IHttpContextAccessor>();

                        var userName = http.HttpContext.User.Identity.Name;

                        if (ctx.IsRegistered<ISession>())
                        {
                            return ctx.Resolve<ISession>().Query<ReviewUser>().Single(x => x.UserName == userName);
                        }

                        using (var session = ctx.Resolve<ISessionFactory>().OpenSession())
                        {
                            return session.Query<ReviewUser>().Single(x => x.UserName == userName);
                        }
                    })
                    .Keyed<ReviewUser>("currentUser");

                builder.RegisterInstance(context.Request.Url.SiteBase).Keyed<string>("SiteBase");

            };

            return GetApplicationContainer().BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag, register);
        }
    }
}