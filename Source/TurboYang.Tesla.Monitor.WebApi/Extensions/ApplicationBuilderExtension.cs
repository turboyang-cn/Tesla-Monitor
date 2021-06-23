using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using NetTopologySuite.Geometries;

using Npgsql;

using TurboYang.Tesla.Monitor.Database;
using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Model;
using TurboYang.Tesla.Monitor.WebApi.Services;

namespace TurboYang.Tesla.Monitor.WebApi.Extensions
{
    public static class ApplicationBuilderExtension
    {
        public static IApplicationBuilder UseAutoMigration(this IApplicationBuilder application)
        {
            using (IServiceScope scope = application.ApplicationServices.CreateScope())
            {
                using (DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>())
                {
                    context.Database.Migrate();

                    using (NpgsqlConnection connection = context.Database.GetDbConnection() as NpgsqlConnection)
                    {
                        connection.Open();
                        connection.ReloadTypes();
                    }
                }
            }

            return application;
        }

        public static IApplicationBuilder UseTeslaMonitor(this IApplicationBuilder application)
        {
            using (IServiceScope scope = application.ApplicationServices.CreateScope())
            {
                ITeslaService teslaService = scope.ServiceProvider.GetRequiredService<ITeslaService>();
                using (DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>())
                {
                    List<CarEntity> carEntities = context.Car.Include(x => x.Token)
                                                             .Include(x => x.CarSetting).ToList();

                    foreach (CarEntity carEntity in carEntities)
                    {
                        teslaService.StartCarRecorder(carEntity.Token.AccessToken, carEntity.Id.Value, carEntity.Name, carEntity.CarId, carEntity.VehicleId.Value, carEntity.CarSetting.SamplingRate.Value, carEntity.CarSetting.TryAsleepDelay.Value, carEntity.CarSetting.IsSamplingCompression.Value);
                    }
                }
            }

            return application;
        }
    }
}
