﻿using System;
using System.Data.SqlClient;
using System.Linq;
using CodeSaw.Db.Migrations;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSaw.Db.Migrator
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(args[1], optional: true)
                .AddCommandLine(args.Skip(2).ToArray())
                .Build();

            var serviceProvider = CreateServices(config);

            // Put the database update into a scope to ensure
            // that all resources will be disposed.
            using (var scope = serviceProvider.CreateScope())
            {
                if (args[0] == "CreateDB")
                {
                    CreateDatabase(scope.ServiceProvider);
                }
                else if(args[0] == "UpdateDB")
                {
                    UpdateDatabase(serviceProvider);
                }
                else if (args[0] == "RedoLast")
                {
                    RedoLastMigration(serviceProvider);
                }
            }
        }

        private static void RedoLastMigration(IServiceProvider serviceProvider)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.Rollback(1);
            runner.MigrateUp();
        }

        private static void UpdateDatabase(IServiceProvider serviceProvider)
        {
            var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

            // Execute the migrations
            runner.MigrateUp();
        }

        private static void CreateDatabase(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Store");

            var targetDbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

            var masterConnectionString = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            }.ToString();

            using (var connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = $"ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;\n DROP DATABASE [{targetDbName}]";
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                }

                cmd.CommandText = $"CREATE DATABASE [{targetDbName}]";
                cmd.ExecuteNonQuery();
            }

            UpdateDatabase(serviceProvider);
        }

        private static IServiceProvider CreateServices(IConfiguration config)
        {
            return new ServiceCollection()
                .AddSingleton(config)
                // Add common FluentMigrator services
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    // Add SQLite support to FluentMigrator
                    .AddSqlServer2016()
                    // Set the connection string
                    .WithGlobalConnectionString(config.GetConnectionString("Store"))
                    // Define the assembly containing the migrations
                    .WithMigrationsIn(Marker.ThisAssembly)
                )
                // Enable logging to console in the FluentMigrator way
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                // Build the service provider
                .BuildServiceProvider(false);
        }
    }
}
