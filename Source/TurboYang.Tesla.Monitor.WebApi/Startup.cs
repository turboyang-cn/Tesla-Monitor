using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Threading;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NodaTime;
using NodaTime.Serialization.SystemTextJson;

using TurboYang.Tesla.Monitor.Client;
using TurboYang.Tesla.Monitor.Core;
using TurboYang.Tesla.Monitor.Core.JsonConverters;
using TurboYang.Tesla.Monitor.Database;
using TurboYang.Tesla.Monitor.Mapping;
using TurboYang.Tesla.Monitor.WebApi.Extensions;
using TurboYang.Tesla.Monitor.WebApi.Services;

namespace TurboYang.Tesla.Monitor.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            #region Entity Framework

            services.AddDbContext<DatabaseContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("TeslaMonitor"), options =>
                {
                    options.UseNodaTime();
                    options.UseNetTopologySuite();
                }).EnableSensitiveDataLogging().UseLazyLoadingProxies();
            });

            #endregion

            #region Auto Mapper

            services.AddAutoMapper(options =>
            {
                options.AllowNullCollections = true;
            }, typeof(MappingProfile).Assembly);

            #endregion

            services.AddSingleton(resolver => resolver.GetRequiredService<IOptionsMonitor<JsonOptions>>().CurrentValue);

            services.AddScoped<ILoggerService, LoggerService>();
            services.AddScoped<IDatabaseService, DatabaseService>();
            services.AddSingleton<IOpenStreetMapClient, OpenStreetMapClient>();
            services.AddSingleton<ITeslaClient, TeslaClient>();
            services.AddSingleton<ITeslaService, TeslaService>();

            services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                        options.JsonSerializerOptions.PropertyNamingPolicy = null;
                        options.JsonSerializerOptions.IgnoreNullValues = true;
                        options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                        options.JsonSerializerOptions.Converters.Add(new CustomJsonStringEnumConverter());
                    });
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                application.UseDeveloperExceptionPage();
            }

            application.UseHttpsRedirection();

            application.UseRouting();

            application.UseAuthorization();

            application.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            application.UseAutoMigration();
            application.UseTeslaMonitor();
        }
    }
}
