using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Conventions;
using Nancy.Diagnostics;

namespace Web
{
    public class Bootstraper : NancyBootstrapperWithRequestContainerBase<IServiceScope>
    {
        private readonly string assetServer;

        public Bootstraper(string assetServer)
        {
            this.assetServer = assetServer;
        }

        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);

            environment.AddValue("AssetServer", this.assetServer);

            environment.Tracing(enabled: true, displayErrorTraces: true);
        }

        protected override INancyEnvironmentConfigurator GetEnvironmentConfigurator()
        {
            throw new NotImplementedException();
        }

        protected override IDiagnostics GetDiagnostics()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IApplicationStartup> GetApplicationStartupTasks()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IRequestStartup> RegisterAndGetRequestStartupTasks(IServiceScope container, Type[] requestStartupTypes)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IRegistrations> GetRegistrationTasks()
        {
            throw new NotImplementedException();
        }

        public override INancyEnvironment GetEnvironment()
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.ViewLocationConventions.Clear();
            nancyConventions.ViewLocationConventions.Add((name, args, ctx) => $"Modules/{ctx.ModuleName}/Views/{name}");
        }

        protected override INancyEngine GetEngineInternal()
        {
            throw new NotImplementedException();
        }

        protected override IServiceScope GetApplicationContainer()
        {
            throw new NotImplementedException();
        }

        protected override void RegisterNancyEnvironment(IServiceScope container, INancyEnvironment environment)
        {
            throw new NotImplementedException();
        }

        protected override void RegisterBootstrapperTypes(IServiceScope applicationContainer)
        {
            throw new NotImplementedException();
        }

        protected override void RegisterTypes(IServiceScope container, IEnumerable<TypeRegistration> typeRegistrations)
        {
            throw new NotImplementedException();
        }

        protected override void RegisterCollectionTypes(IServiceScope container, IEnumerable<CollectionTypeRegistration> collectionTypeRegistrationsn)
        {
            throw new NotImplementedException();
        }

        protected override void RegisterInstances(IServiceScope container, IEnumerable<InstanceRegistration> instanceRegistrations)
        {
            throw new NotImplementedException();
        }

        protected override IServiceScope CreateRequestContainer(NancyContext context)
        {
            throw new NotImplementedException();
        }

        protected override void RegisterRequestContainerModules(IServiceScope container, IEnumerable<ModuleRegistration> moduleRegistrationTypes)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<INancyModule> GetAllModules(IServiceScope container)
        {
            throw new NotImplementedException();
        }

        protected override INancyModule GetModule(IServiceScope container, Type moduleType)
        {
            throw new NotImplementedException();
        }
    }
}