using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
                .UseStartup<Startup>()
                .Build();
    }
}