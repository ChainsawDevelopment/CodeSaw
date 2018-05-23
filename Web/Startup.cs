using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Nancy;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Owin;

namespace Web
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/dist"),
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
            });
            app.UseOwin(owin => { owin.UseNancy(opt=>opt.Bootstrapper = new Bootstraper()); });
        }
    }

    public class Bootstraper : DefaultNancyBootstrapper
    {
        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);

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