using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TurboYang.Tesla.Monitor.Web
{
    public class Program
    {
        public static void Main(String[] arguments)
        {
            CreateHostBuilder(arguments).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(String[] arguments)
        {
            return Host.CreateDefaultBuilder(arguments).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
        }
    }
}
