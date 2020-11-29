using System;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace CodeSaw.Archiver
{
    class Program
    {

        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(args[0], optional: true)
                .AddCommandLine(args.Skip(1).ToArray())
                .Build();
                
            Console.WriteLine("Hello World!");


            var serviceProvider = CreateServices(config);
            var sessionFactory = BuildSessionFactory(serviceProvider);

            using (var session = sessionFactory.OpenSession())
            {
                var reviews = session.Query<CodeSaw.Web.Modules.Api.Model.ReviewRevision>()
                    .Where(x => x.ReviewId.ReviewId > 480)
                    .ToArray();

                Console.WriteLine($"Found {reviews.Length} reviews!");
            }
        }

        static private ISessionFactory BuildSessionFactory(IServiceProvider serviceProvider)
        {            
            var configuration = new NHibernate.Cfg.Configuration();

            configuration.SetProperty(NHibernate.Cfg.Environment.ConnectionString, serviceProvider.GetService<IConfiguration>().GetConnectionString("Store"));
            configuration.SetProperty(NHibernate.Cfg.Environment.Dialect, typeof(MsSql2012Dialect).AssemblyQualifiedName);

            var modelMapper = new ModelMapper();
            modelMapper.AddMappings(typeof(CodeSaw.Web.Startup).Assembly.GetExportedTypes());

            var hbm = modelMapper.CompileMappingForAllExplicitlyAddedEntities();
            
            configuration.AddMapping(hbm);

            return configuration.BuildSessionFactory();
        }

        private static IServiceProvider CreateServices(IConfiguration config)
        {
            return new ServiceCollection()
                .AddSingleton(config)
                // Enable logging to console in the FluentMigrator way
                .AddLogging()
                // Build the service provider
                .BuildServiceProvider(false);
        }
    }
}
