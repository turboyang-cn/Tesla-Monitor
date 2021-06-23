using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using NLog;
using NLog.Web;

namespace TurboYang.Tesla.Monitor.WebApi
{
    public class Program
    {
        public static void Main(String[] arguments)
        {
            Logger logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();

            try
            {
                logger.Info("Tesla Monitor API Start (Version: 1.2)");

                CreateHostBuilder(arguments).Build().Run();
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
            finally
            {
                logger.Info("Tesla Monitor API Stop");

                LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(String[] arguments)
        {

            return Host.CreateDefaultBuilder(arguments).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = false;
            })
            .UseNLog();
        }
    }
}
