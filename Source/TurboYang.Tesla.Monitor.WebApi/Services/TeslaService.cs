using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using NLog;

using NodaTime;

using TurboYang.Tesla.Monitor.Client;
using TurboYang.Tesla.Monitor.Database;
using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.WebApi.Services
{
    public class TeslaService : ITeslaService
    {
        private ITeslaClient TeslaClient { get; }
        private IServiceScopeFactory ServiceScopeFactory { get; }
        private JsonOptions JsonOptions { get; }
        private ConcurrentDictionary<String, CarRecorder> Recorders { get; } = new();

        public TeslaService(ITeslaClient teslaClient, IServiceScopeFactory serviceScopeFactory, JsonOptions jsonOptions)
        {
            TeslaClient = teslaClient;
            ServiceScopeFactory = serviceScopeFactory;
            JsonOptions = jsonOptions;
        }

        public void StartCarRecorder(String accessToken, Int32 entityId, String name, String carId, Int64 vehicleId, Int32 samplingRate, Int32 tryAsleepDelay, Boolean isSamplingCompression)
        {
            CarRecorder recorder = Recorders.GetOrAdd(carId, key =>
            {
                return new CarRecorder(TeslaClient, ServiceScopeFactory, entityId, name, accessToken, carId, vehicleId, JsonOptions, samplingRate, tryAsleepDelay, isSamplingCompression);
            });

            recorder.Start();
        }

        public void StopCarRecorder(String carId)
        {
            if (Recorders.TryRemove(carId, out CarRecorder recorder))
            {
                recorder.Stop();
            }
        }

        public class CarRecorder
        {
            private ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
            private TeslaStreamingRecorder StreamingRecorder { get; set; }
            private Task SamplingWorker { get; set; }
            private Task RecordingWorker { get; set; }
            private CancellationTokenSource SamplingWorkerCancellationTokenSource { get; set; }
            private CancellationTokenSource RecordingWorkerCancellationTokenSource { get; set; }
            private ITeslaClient TeslaClient { get; }
            private IServiceScopeFactory ServiceScopeFactory { get; }
            private Int32 CarEntityId { get; }
            private String Name { get; set; }
            private (String Version, FirewareState? State) Fireware { get; set; }
            private String AccessToken { get; }
            private String CarId { get; }
            private Int64 VehicleId { get; }
            private JsonOptions JsonOptions { get; }
            private Int32 SamplingRate { get; }
            private Boolean IsSamplingCompression { get; }
            private Int32 TryAsleepDelay { get; }
            private Boolean _IsRunning;
            private Boolean IsRunning
            {
                get
                {
                    return _IsRunning;
                }
                set
                {
                    if (_IsRunning != value)
                    {
                        _IsRunning = value;

                        if (_IsRunning)
                        {
                            Logger.Info($"[{Name ?? VehicleId.ToString()}] Start Car Recorder");
                        }
                        else
                        {
                            Logger.Info($"[{Name ?? VehicleId.ToString()}] Stop Car Recorder");
                        }
                    }
                }
            }
            private Stopwatch TryAsleepTimer { get; set; } = new();
            private ConcurrentQueue<(CarState State, IDatabaseService.Snapshot Snapshot, Instant Timestamp)> Snapshots { get; } = new();

            public CarRecorder(ITeslaClient teslaClient, IServiceScopeFactory serviceScopeFactory, Int32 carEntityId, String name, String accessToken, String carId, Int64 vehicleId, JsonOptions jsonOptions, Int32 samplingRate, Int32 tryAsleepDelay, Boolean isSamplingCompression)
            {
                TeslaClient = teslaClient;
                ServiceScopeFactory = serviceScopeFactory;
                CarEntityId = carEntityId;
                Name = name;
                AccessToken = accessToken;
                CarId = carId;
                VehicleId = vehicleId;
                JsonOptions = jsonOptions;
                SamplingRate = samplingRate;
                TryAsleepDelay = tryAsleepDelay;
                IsSamplingCompression = isSamplingCompression;
            }

            public void Start()
            {
                IsRunning = true;

                if (SamplingWorker == null)
                {
                    SamplingWorkerCancellationTokenSource = new CancellationTokenSource();
                    SamplingWorker = Task.Run(async () => await Sampling(), SamplingWorkerCancellationTokenSource.Token);
                }

                if (StreamingRecorder == null)
                {
                    StreamingRecorder = new TeslaStreamingRecorder(Name, AccessToken, VehicleId, JsonOptions);
                    StreamingRecorder.Start();
                }

                if (RecordingWorker == null)
                {
                    RecordingWorkerCancellationTokenSource = new CancellationTokenSource();
                    RecordingWorker = Task.Run(async () => await Recording(), RecordingWorkerCancellationTokenSource.Token);
                }
            }

            public void Stop()
            {
                IsRunning = false;

                StreamingRecorder?.Stop();
                StreamingRecorder?.Dispose();
                StreamingRecorder = null;

                SamplingWorkerCancellationTokenSource?.Cancel();
                SamplingWorkerCancellationTokenSource = null;
                while (SamplingWorker != null && !SamplingWorker.IsCompleted)
                {
                }
                SamplingWorker?.Dispose();
                SamplingWorker = null;

                RecordingWorkerCancellationTokenSource?.Cancel();
                RecordingWorkerCancellationTokenSource = null;
                while (RecordingWorker != null && !RecordingWorker.IsCompleted)
                {
                }
                RecordingWorker?.Dispose();
                RecordingWorker = null;
            }

            private async Task Sampling()
            {
                Int32 offlineCounter = 0;

                while (IsRunning)
                {
                    CarState currentState = default;
                    Stopwatch samplingTimer = new();
                    samplingTimer.Restart();

                    try
                    {
                        TeslaCarData carData = null;

                        try
                        {
                            TeslaCar car = await TeslaClient.GetCarAsync(AccessToken, CarId, CancellationToken.None);
                            currentState = car.State;

                            if (currentState != CarState.Offline)
                            {
                                if (currentState == CarState.Online)
                                {
                                    if (!TryAsleepTimer.IsRunning || (TryAsleepTimer.IsRunning && TryAsleepTimer.Elapsed < TimeSpan.FromSeconds(TryAsleepDelay)))
                                    {
                                        carData = await TeslaClient.GetCarDataAsync(AccessToken, CarId, CancellationToken.None);

                                        if (carData.DriveState == null || carData.CarState == null || carData.ChargeState == null || !new String[] { "Disconnected", "Stopped", "Charging", "Complete" }.Contains(carData.ChargeState.ChargingState))
                                        {
                                            Logger.Error(JsonSerializer.Serialize(carData, JsonOptions.JsonSerializerOptions));

                                            continue;
                                        }

                                        if (Name == null)
                                        {
                                            using (IServiceScope scope = ServiceScopeFactory.CreateScope())
                                            {
                                                DatabaseContext databaseContext = scope.ServiceProvider.GetService<DatabaseContext>();

                                                CarEntity carEntity = await databaseContext.Car.FirstOrDefaultAsync(x => x.Id == CarEntityId);

                                                carEntity.Name = carData.DisplayName;
                                                carEntity.Vin = carData.Vin;
                                                carEntity.ExteriorColor = carData.CarConfig?.ExteriorColor;
                                                carEntity.WheelType = carData.CarConfig?.WheelType;
                                                carEntity.Type = carData.CarConfig?.Type;

                                                await databaseContext.SaveChangesAsync();
                                            }

                                            Name = carData.DisplayName;
                                        }

                                        if (carData.CarState?.SoftwareUpdate?.Version != null && carData.CarState?.SoftwareUpdate?.Status != null)
                                        {
                                            if (Fireware.Version == null || Fireware.State == null)
                                            {
                                                using (IServiceScope scope = ServiceScopeFactory.CreateScope())
                                                {
                                                    DatabaseContext databaseContext = scope.ServiceProvider.GetService<DatabaseContext>();

                                                    var fireware = await databaseContext.Fireware.Where(x => x.CarId == CarEntityId).OrderByDescending(x => x.Timestamp).Select(x => new
                                                    {
                                                        Version = x.Version,
                                                        State = x.State
                                                    }).FirstOrDefaultAsync();

                                                    Fireware = (fireware.Version, fireware.State);
                                                }
                                            }

                                            if (Fireware.Version != carData.CarState.SoftwareUpdate.Version)
                                            {
                                                using (IServiceScope scope = ServiceScopeFactory.CreateScope())
                                                {
                                                    DatabaseContext databaseContext = scope.ServiceProvider.GetService<DatabaseContext>();

                                                    FirewareEntity firewareEntity = await databaseContext.Fireware.Where(x => x.CarId == CarEntityId).OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync();

                                                    if (firewareEntity != null)
                                                    {
                                                        firewareEntity.State = FirewareState.Updated;
                                                    }

                                                    databaseContext.Fireware.Add(new FirewareEntity()
                                                    {
                                                        Version = carData.CarState.SoftwareUpdate.Version,
                                                        State = carData.CarState.SoftwareUpdate.Status == SoftwareUpdateState.Unavailable ? FirewareState.Updated : FirewareState.Pending,
                                                        Timestamp = Instant.FromDateTimeUtc(DateTime.UtcNow),

                                                        CarId = CarEntityId,
                                                    });

                                                    await databaseContext.SaveChangesAsync();
                                                }
                                            }
                                            else if (carData.CarState.SoftwareUpdate.Status == SoftwareUpdateState.Unavailable && Fireware.State == FirewareState.Pending)
                                            {
                                                using (IServiceScope scope = ServiceScopeFactory.CreateScope())
                                                {
                                                    DatabaseContext databaseContext = scope.ServiceProvider.GetService<DatabaseContext>();

                                                    FirewareEntity firewareEntity = await databaseContext.Fireware.Where(x => x.CarId == CarEntityId).OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync();

                                                    if (firewareEntity != null)
                                                    {
                                                        firewareEntity.State = FirewareState.Updated;
                                                    }

                                                    await databaseContext.SaveChangesAsync();
                                                }
                                            }
                                        }

                                        if (carData.DriveState.ShiftState == ShiftState.D || carData.DriveState.ShiftState == ShiftState.R || carData.DriveState.ShiftState == ShiftState.N)
                                        {
                                            currentState = CarState.Driving;

                                            StopTryAsleep();
                                        }
                                        else if (carData.DriveState.ShiftState == null && carData.ChargeState.ChargingState == "Charging")
                                        {
                                            currentState = CarState.Charging;

                                            StopTryAsleep();
                                        }
                                        else
                                        {
                                            if (!carData.CarState.IsSentryMode)
                                            {
                                                StartTryAsleep();
                                            }
                                            else
                                            {
                                                StopTryAsleep();
                                            }
                                        }

                                        Snapshots.Enqueue((currentState, new IDatabaseService.Snapshot(carData, null), carData.CarState.Timestamp));
                                    }
                                }
                                else
                                {
                                    StopTryAsleep();
                                }
                            }
                            else
                            {
                                if (offlineCounter++ < 10)
                                {
                                    Logger.Info($"[{Name ?? VehicleId.ToString()}] Offline Counter: {offlineCounter}");

                                    continue;
                                }
                            }

                            offlineCounter = 0;
                        }
                        catch
                        {
                            if (offlineCounter++ < 10)
                            {
                                Logger.Info($"[{Name ?? VehicleId.ToString()}] Offline Counter: {offlineCounter}");

                                continue;
                            }

                            currentState = CarState.Offline;

                            offlineCounter = 0;
                        }

                        while (StreamingRecorder.StreamingDatas.TryDequeue(out TeslaStreamingData streamingData))
                        {
                            offlineCounter = 0;

                            Snapshots.Enqueue((currentState, new IDatabaseService.Snapshot(carData, streamingData), streamingData.Timestamp));
                        }

                        if (currentState == CarState.Asleep || currentState == CarState.Offline)
                        {
                            Snapshots.Enqueue((currentState, null, Instant.FromDateTimeUtc(DateTime.UtcNow)));
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception);
                    }
                    finally
                    {
                        samplingTimer.Stop();

                        if (currentState != CarState.Driving && currentState != CarState.Charging && samplingTimer.Elapsed < TimeSpan.FromSeconds(SamplingRate))
                        {
                            await Task.Delay((Int32)Math.Round((TimeSpan.FromSeconds(SamplingRate) - samplingTimer.Elapsed).TotalMilliseconds, 0, MidpointRounding.AwayFromZero));
                        }
                    }
                };
            }

            private async Task Recording()
            {
                while (IsRunning)
                {
                    try
                    {
                        Int32 snapshotCount = 0;

                        if (!Snapshots.TryDequeue(out (CarState State, IDatabaseService.Snapshot Snapshot, Instant Timestamp) result))
                        {
                            await Task.Delay(500);

                            continue;
                        }

                        snapshotCount = Snapshots.Count;
                        Stopwatch stopwatch = new();
                        stopwatch.Start();

                        using (IServiceScope scope = ServiceScopeFactory.CreateScope())
                        {
                            IDatabaseService databaseService = scope.ServiceProvider.GetService<IDatabaseService>();

                            await databaseService.SaveSnapshotAsync(CarEntityId, Name, VehicleId, result.State, result.Snapshot, result.Timestamp, IsSamplingCompression);
                        }

                        stopwatch.Stop();

                        Logger.Info($"[{Name ?? VehicleId.ToString()}] Snapshot Count: {snapshotCount} / Record Snapshot: {stopwatch.Elapsed.TotalSeconds:0.000000}");
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception);
                    }
                };
            }

            private void StartTryAsleep()
            {
                //if (TryAsleepTimer == null)
                //{
                //    TryAsleepTimer = new();
                //}

                //if (!TryAsleepTimer.IsRunning)
                //{
                //    TryAsleepTimer.Restart();

                //    Logger.Info($"Start Try Asleep: {VehicleId}");
                //}
            }

            private void StopTryAsleep()
            {
                //if (TryAsleepTimer != null && TryAsleepTimer.IsRunning)
                //{
                //    TryAsleepTimer.Stop();

                //    Logger.Info($"Stop Try Asleep: {VehicleId}");

                //    TryAsleepTimer = new();
                //}
            }
        }
    }
}
