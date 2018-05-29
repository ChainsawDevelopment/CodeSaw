using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Nancy.Owin;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;

namespace Web
{
    public class Startup
    {
        private IContainer _container;
        public IHostingEnvironment HostingEnvironment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env, IConfiguration config)
        {
            HostingEnvironment = env;
            Configuration = config;
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            builder.Populate(services);

            builder.RegisterInstance(BuildSessionFactory());

            builder.RegisterModule<Cqrs.CqrsModule>();

            _container = builder.Build();

            return new AutofacServiceProvider(_container);
        }

        private ISessionFactory BuildSessionFactory()
        {
            var configuration = new NHibernate.Cfg.Configuration();

            configuration.SetProperty(NHibernate.Cfg.Environment.ConnectionString, Configuration.GetConnectionString("Store"));
            configuration.SetProperty(NHibernate.Cfg.Environment.Dialect, typeof(MsSql2012Dialect).AssemblyQualifiedName);

            var modelMapper = new ModelMapper();
            modelMapper.AddMappings(typeof(Startup).Assembly.GetExportedTypes());

            configuration.AddMapping(modelMapper.CompileMappingForAllExplicitlyAddedEntities());

            return configuration.BuildSessionFactory();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/dist"),
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")) 
            });

            var assetServer = Configuration.GetValue<string>("REVIEWER_ASSET_SERVER", null);

            app.UseOwin(owin => { owin.UseNancy(opt=>opt.Bootstrapper = new Bootstraper(assetServer, _container)); });
        }
    }
}