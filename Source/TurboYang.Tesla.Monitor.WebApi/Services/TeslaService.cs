using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

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
        //private IOpenStreetMapClient OpenStreetMapClient { get; }
        //private DatabaseContext DatabaseContext { get; }
        private IDatabaseService DatabaseService { get; }
        private JsonOptions JsonOptions { get; }
        private ConcurrentDictionary<String, CarRecorder> Recorders { get; } = new();

        public TeslaService(ITeslaClient teslaClient, IOpenStreetMapClient openStreetMapClient/*, DatabaseContext databaseContext*/, IDatabaseService databaseService, JsonOptions jsonOptions)
        {
            TeslaClient = teslaClient;
            //OpenStreetMapClient = openStreetMapClient;
            //DatabaseContext = databaseContext;
            DatabaseService = databaseService;
            JsonOptions = jsonOptions;
        }

        public void StartCarRecorder(String accessToken, Int32 entityId, String name, String carId, Int64 vehicleId, Int32 samplingRate, Int32 tryAsleepDelay, Boolean isSamplingCompression)
        {
            CarRecorder recorder = Recorders.GetOrAdd(carId, key =>
            {
                return new CarRecorder(/*DatabaseContext, */TeslaClient/*, OpenStreetMapClient*/, DatabaseService, entityId, name, accessToken, carId, vehicleId, JsonOptions, samplingRate, tryAsleepDelay, isSamplingCompression);
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
            //private DatabaseContext DatabaseContext { get; }
            private ITeslaClient TeslaClient { get; }
            // private IOpenStreetMapClient OpenStreetMapClient { get; }
            private IDatabaseService DatabaseService { get; }
            private Int32 EntityId { get; }
            private String Name { get; set; }
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
            //private Decimal? _FullPower;
            //private Decimal FullPower
            //{
            //    get
            //    {
            //        // Load Form Car Table

            //        if (_FullPower == null)
            //        {
            //            _FullPower = 50;
            //        }

            //        return _FullPower.Value;
            //    }
            //    set
            //    {
            //        // Save To Car Table

            //        _FullPower = value;
            //    }
            //}
            private Stopwatch TryAsleepTimer { get; set; } = new();
            //private StateEntity LastStateEntity { get; set; }
            private SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1, 1);
            private ConcurrentQueue<(CarState State, IDatabaseService.Snapshot Snapshot, Instant Timestamp)> Snapshots { get; } = new();

            public CarRecorder(/*DatabaseContext databaseContext, */ITeslaClient teslaClient/*, IOpenStreetMapClient openStreetMapClient*/, IDatabaseService databaseService, Int32 entityId, String name, String accessToken, String carId, Int64 vehicleId, JsonOptions jsonOptions, Int32 samplingRate, Int32 tryAsleepDelay, Boolean isSamplingCompression)
            {
                TeslaClient = teslaClient;
                //OpenStreetMapClient = openStreetMapClient;
                DatabaseService = databaseService;
                //DatabaseContext = databaseContext;
                EntityId = entityId;
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
                //InitialEntity();

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
                                            Name = carData.DisplayName;

                                            await DatabaseService.UpdateCarAsync(EntityId, carData.DisplayName, carData.Vin, carData.CarConfig?.ExteriorColor, carData.CarConfig?.WheelType, carData.CarConfig?.Type);
                                            //CarEntity carEntity = await DatabaseContext.Car.FirstOrDefaultAsync(x => x.Id == EntityId);

                                            //carEntity.Name = carData.DisplayName;
                                            //carEntity.Vin = carData.Vin;
                                            //carEntity.ExteriorColor = carData.CarConfig?.ExteriorColor;
                                            //carEntity.WheelType = carData.CarConfig?.WheelType;
                                            //carEntity.Type = carData.CarConfig?.Type;

                                            //await DatabaseContext.SaveChangesAsync();
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

                                        await SemaphoreSlim.WaitAsync();
                                        try
                                        {
                                            Snapshots.Enqueue((currentState, new IDatabaseService.Snapshot(carData, null), carData.CarState.Timestamp));
                                        }
                                        finally
                                        {
                                            SemaphoreSlim.Release();
                                        }
                                    }
                                }
                                else
                                {
                                    StopTryAsleep();

                                    await SemaphoreSlim.WaitAsync();
                                    try
                                    {
                                        Snapshots.Enqueue((CarState.Asleep, null, Instant.FromDateTimeUtc(DateTime.UtcNow)));
                                    }
                                    finally
                                    {
                                        SemaphoreSlim.Release();
                                    }
                                }

                                offlineCounter = 0;
                            }
                            else
                            {
                                if (offlineCounter++ < 10)
                                {
                                    Logger.Info($"[{Name ?? VehicleId.ToString()}] Offline Counter: {offlineCounter}");

                                    continue;
                                }

                                offlineCounter = 0;

                                await SemaphoreSlim.WaitAsync();
                                try
                                {
                                    Snapshots.Enqueue((CarState.Offline, null, Instant.FromDateTimeUtc(DateTime.UtcNow)));
                                }
                                finally
                                {
                                    SemaphoreSlim.Release();
                                }
                            }
                        }
                        catch
                        {
                            if (offlineCounter++ < 10)
                            {
                                Logger.Info($"[{Name ?? VehicleId.ToString()}] Offline Counter: {offlineCounter}");

                                continue;
                            }

                            offlineCounter = 0;

                            await SemaphoreSlim.WaitAsync();
                            try
                            {
                                Snapshots.Enqueue((CarState.Offline, null, Instant.FromDateTimeUtc(DateTime.UtcNow)));
                            }
                            finally
                            {
                                SemaphoreSlim.Release();
                            }
                        }

                        while (StreamingRecorder.StreamingDatas.TryDequeue(out TeslaStreamingData streamingData))
                        {
                            offlineCounter = 0;

                            await SemaphoreSlim.WaitAsync();
                            try
                            {
                                Snapshots.Enqueue((currentState, new IDatabaseService.Snapshot(carData, streamingData), streamingData.Timestamp));
                            }
                            finally
                            {
                                SemaphoreSlim.Release();
                            }
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
                        (CarState State, IDatabaseService.Snapshot Snapshot, Instant Timestamp) result = default;

                        Int32 snapshotCount = 0;

                        await SemaphoreSlim.WaitAsync();
                        try
                        {
                            if (!Snapshots.TryDequeue(out result))
                            {
                                await Task.Delay(500);

                                continue;
                            }

                            snapshotCount = Snapshots.Count;
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            SemaphoreSlim.Release();
                        }

                        Stopwatch stopwatch = new();
                        stopwatch.Start();

                        await DatabaseService.SaveSnapshotAsync(EntityId, Name, VehicleId, result.State, result.Snapshot, result.Timestamp, IsSamplingCompression);

                        stopwatch.Stop();

                        Logger.Info($"[{Name ?? VehicleId.ToString()}] Snapshot Count: {snapshotCount} / Record Snapshot: {stopwatch.Elapsed.TotalSeconds:0.000000}");

                        //Instant now = Instant.FromDateTimeUtc(DateTime.UtcNow);

                        //await RecordStateAsync(result.State, now);

                        //if (result.Snapshot != null)
                        //{
                        //    await RecordSnapshotAsync(result.State, result.Snapshot, now);

                        //    stopwatch.Stop();

                        //    Logger.Info($"[{Name ?? VehicleId.ToString()}] Snapshot Count: {snapshotCount} / Record Snapshot: {stopwatch.Elapsed.TotalSeconds:0.000000}");
                        //}
                        //else
                        //{
                        //    stopwatch.Stop();
                        //}
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

            //private async Task RecordStateAsync(CarState state, Instant timestamp)
            //{
            //    if (LastStateEntity?.State != state)
            //    {
            //        if (LastStateEntity != null && LastStateEntity.EndTimestamp == null)
            //        {
            //            LastStateEntity.EndTimestamp = timestamp;
            //        }

            //        Logger.Info($"[{Name ?? VehicleId.ToString()}] State Change: {LastStateEntity?.State?.ToString() ?? "null"} -> {state}");

            //        StateEntity currentStateEntity = new()
            //        {
            //            State = state,
            //            StartTimestamp = timestamp,
            //            CarId = EntityId,
            //        };

            //        DatabaseContext.State.Add(currentStateEntity);

            //        await DatabaseContext.SaveChangesAsync();

            //        LastStateEntity = currentStateEntity;
            //    }

            //    if (state == CarState.Online || state == CarState.Asleep)
            //    {
            //        await StopDrivingModeAsync(timestamp);
            //        await StopChargingModeAsync(timestamp);
            //        await StartStandByModeAsync(timestamp);
            //    }
            //    else if (state == CarState.Driving)
            //    {
            //        await StopStandByModeAsync(timestamp);
            //        await StopChargingModeAsync(timestamp);
            //        await StartDrivingModeAsync(timestamp);
            //    }
            //    else if (state == CarState.Charging)
            //    {
            //        await StopStandByModeAsync(timestamp);
            //        await StopDrivingModeAsync(timestamp);
            //        await StartChargingModeAsync(timestamp);
            //    }
            //}

            //#region Snapshot

            //private Snapshot LastSnapshot { get; set; }

            //private async Task RecordSnapshotAsync(CarState state, Snapshot snapshot, Instant timestamp)
            //{
            //    if (state == CarState.Online || state == CarState.Asleep)
            //    {
            //        await RecordStandBySnapshotAsync(snapshot, timestamp);
            //    }
            //    else if (state == CarState.Driving)
            //    {
            //        await RecordDrivingSnapshotAsync(snapshot, timestamp);
            //    }
            //    else if (state == CarState.Charging)
            //    {
            //        await RecordChargingSnapshotAsync(snapshot, timestamp);
            //    }

            //    Snapshot currentSnapshot = (snapshot with
            //    {
            //        Timestamp = null,
            //    }).Debounce(LastSnapshot with
            //    {
            //        Timestamp = null,
            //    });

            //    if (!IsSamplingCompression || currentSnapshot with
            //    {
            //        Timestamp = null,
            //    } != LastSnapshot with
            //    {
            //        Timestamp = null,
            //    })
            //    {
            //        currentSnapshot = currentSnapshot with
            //        {
            //            Timestamp = snapshot.Timestamp,
            //        };

            //        DatabaseContext.Snapshot.Add(new SnapshotEntity()
            //        {
            //            Location = currentSnapshot?.Location,
            //            Elevation = currentSnapshot?.Elevation,
            //            Speed = currentSnapshot?.Speed,
            //            Heading = currentSnapshot?.Heading,
            //            ShiftState = currentSnapshot?.ShiftState,
            //            Power = currentSnapshot?.Power,
            //            Odometer = currentSnapshot?.Odometer,
            //            BatteryLevel = currentSnapshot?.BatteryLevel,
            //            IdealBatteryRange = currentSnapshot?.IdealBatteryRange,
            //            RatedBatteryRange = currentSnapshot?.RatedBatteryRange,
            //            OutsideTemperature = currentSnapshot?.OutsideTemperature,
            //            InsideTemperature = currentSnapshot?.InsideTemperature,
            //            DriverTemperatureSetting = currentSnapshot?.DriverTemperatureSetting,
            //            PassengerTemperatureSetting = currentSnapshot?.PassengerTemperatureSetting,
            //            DriverSeatHeater = currentSnapshot?.DriverSeatHeater,
            //            PassengerSeatHeater = currentSnapshot?.PassengerSeatHeater,
            //            FanStatus = currentSnapshot?.FanStatus,
            //            IsSideMirrorHeater = currentSnapshot?.IsSideMirrorHeater,
            //            IsWiperBladeHeater = currentSnapshot?.IsWiperBladeHeater,
            //            IsFrontDefrosterOn = currentSnapshot?.IsFrontDefrosterOn,
            //            IsRearDefrosterOn = currentSnapshot?.IsRearDefrosterOn,
            //            IsClimateOn = currentSnapshot?.IsClimateOn,
            //            IsBatteryHeater = currentSnapshot?.IsBatteryHeater,
            //            IsBatteryHeaterOn = currentSnapshot?.IsBatteryHeaterOn,
            //            ChargeEnergyAdded = currentSnapshot?.ChargeEnergyAdded,
            //            ChargeEnergyUsed = currentSnapshot?.ChargeEnergyUsed,
            //            ChargerPhases = currentSnapshot?.ChargerPhases,
            //            ChargerPilotCurrent = currentSnapshot?.ChargerPilotCurrent,
            //            ChargerActualCurrent = currentSnapshot?.ChargerActualCurrent,
            //            ChargerPower = currentSnapshot?.ChargerPower,
            //            ChargerVoltage = currentSnapshot?.ChargerVoltage,
            //            ChargeRate = currentSnapshot?.ChargeRate,

            //            State = LastStateEntity,

            //            CarId = EntityId,

            //            CreateTimestamp = snapshot.Timestamp,
            //            UpdateTimestamp = timestamp,
            //        });

            //        await DatabaseContext.SaveChangesAsync();

            //        LastSnapshot = currentSnapshot;
            //    }
            //}

            //private record Snapshot : BaseSnapshot
            //{
            //    public Point Location { get; init; }
            //    public Decimal? Elevation { get; init; }
            //    public Decimal? Speed { get; init; }
            //    public Decimal? Heading { get; init; }
            //    public ShiftState? ShiftState { get; init; }
            //    public Decimal? Power { get; init; }
            //    public Decimal? Odometer { get; init; }
            //    public Decimal? BatteryLevel { get; init; }
            //    public Decimal? IdealBatteryRange { get; init; }
            //    public Decimal? RatedBatteryRange { get; init; }
            //    public Decimal? OutsideTemperature { get; init; }
            //    public Decimal? InsideTemperature { get; init; }
            //    public Decimal? DriverTemperatureSetting { get; init; }
            //    public Decimal? PassengerTemperatureSetting { get; init; }
            //    public Int32? DriverSeatHeater { get; init; }
            //    public Int32? PassengerSeatHeater { get; init; }
            //    public Int32? FanStatus { get; init; }
            //    public Boolean? IsSideMirrorHeater { get; init; }
            //    public Boolean? IsWiperBladeHeater { get; init; }
            //    public Boolean? IsFrontDefrosterOn { get; init; }
            //    public Boolean? IsRearDefrosterOn { get; init; }
            //    public Boolean? IsClimateOn { get; init; }
            //    public Boolean? IsBatteryHeater { get; init; }
            //    public Boolean? IsBatteryHeaterOn { get; init; }
            //    public Decimal? ChargeEnergyAdded { get; init; }
            //    public Decimal? ChargeEnergyUsed { get; init; }
            //    public Int32? ChargerPhases { get; init; }
            //    public Int32? ChargerPilotCurrent { get; init; }
            //    public Int32? ChargerActualCurrent { get; init; }
            //    public Int32? ChargerPower { get; init; }
            //    public Int32? ChargerVoltage { get; init; }
            //    public Decimal? ChargeRate { get; init; }
            //    public Boolean? IsFastChargerPresent { get; init; }
            //    public String ChargeCable { get; init; }
            //    public String FastChargerBrand { get; init; }
            //    public String FastChargerType { get; init; }

            //    private Snapshot()
            //    {
            //    }

            //    public Snapshot(TeslaCarData carData, TeslaStreamingData streamingData)
            //    {
            //        Location = new Point((Double)(streamingData?.Longitude ?? carData?.DriveState?.Longitude ?? 0), (Double)(streamingData?.Latitude ?? carData?.DriveState?.Latitude ?? 0));
            //        Elevation = streamingData?.Elevation;
            //        Speed = streamingData?.Speed?.Mile ?? carData?.DriveState?.Speed?.Mile;
            //        Heading = streamingData?.EstimateHeading ?? carData?.DriveState?.Heading;
            //        ShiftState = streamingData?.ShiftState ?? carData?.DriveState?.ShiftState;
            //        Power = streamingData?.Power ?? carData?.DriveState?.Power;
            //        Odometer = streamingData?.Odometer?.Mile ?? carData?.CarState?.Odometer?.Mile;
            //        BatteryLevel = streamingData?.BatteryLevel ?? carData?.ChargeState?.BatteryLevel;
            //        IdealBatteryRange = streamingData?.IdealBatteryRange?.Mile ?? carData?.ChargeState?.IdealBatteryRange?.Mile;
            //        RatedBatteryRange = streamingData?.RatedBatteryRange?.Mile ?? carData?.ChargeState?.RatedBatteryRange?.Mile;
            //        OutsideTemperature = carData?.ClimateState?.OutsideTemperature?.Celsius;
            //        InsideTemperature = carData?.ClimateState?.InsideTemperature?.Celsius;
            //        DriverTemperatureSetting = carData?.ClimateState?.DriverTemperatureSetting?.Celsius;
            //        PassengerTemperatureSetting = carData?.ClimateState?.PassengerTemperatureSetting?.Celsius;
            //        DriverSeatHeater = carData?.ClimateState?.DriverSeatHeater;
            //        PassengerSeatHeater = carData?.ClimateState?.PassengerSeatHeater;
            //        FanStatus = carData?.ClimateState?.FanStatus;
            //        IsSideMirrorHeater = carData?.ClimateState?.IsSideMirrorHeater;
            //        IsWiperBladeHeater = carData?.ClimateState?.IsWiperBladeHeater;
            //        IsFrontDefrosterOn = carData?.ClimateState?.IsFrontDefrosterOn;
            //        IsRearDefrosterOn = carData?.ClimateState?.IsRearDefrosterOn;
            //        IsClimateOn = carData?.ClimateState?.IsClimateOn;
            //        IsBatteryHeater = carData?.ClimateState?.IsBatteryHeater;
            //        IsBatteryHeaterOn = carData?.ChargeState?.IsBatteryHeaterOn;
            //        ChargeEnergyAdded = carData?.ChargeState?.ChargeEnergyAdded;
            //        ChargeEnergyUsed = null;
            //        ChargerPhases = carData?.ChargeState?.ChargerPhases;
            //        ChargerPilotCurrent = carData?.ChargeState?.ChargerPilotCurrent;
            //        ChargerActualCurrent = carData?.ChargeState?.ChargerActualCurrent;
            //        ChargerPower = carData?.ChargeState?.ChargerPower;
            //        ChargerVoltage = carData?.ChargeState?.ChargerVoltage;
            //        ChargeRate = carData?.ChargeState?.ChargeRate;
            //        IsFastChargerPresent = carData?.ChargeState?.IsFastChargerPresent;
            //        ChargeCable = carData?.ChargeState?.ChargeCable;
            //        FastChargerBrand = carData?.ChargeState?.FastChargerBrand;
            //        FastChargerType = carData?.ChargeState?.FastChargerType;
            //        Timestamp = streamingData?.Timestamp ?? carData?.CarConfig?.Timestamp;
            //    }

            //    public Snapshot(SnapshotEntity snapshotEntity)
            //    {
            //        Location = snapshotEntity?.Location;
            //        Elevation = snapshotEntity?.Elevation;
            //        Speed = snapshotEntity?.Speed;
            //        Heading = snapshotEntity?.Heading;
            //        ShiftState = snapshotEntity?.ShiftState;
            //        Power = snapshotEntity?.Power;
            //        Odometer = snapshotEntity?.Odometer;
            //        BatteryLevel = snapshotEntity?.BatteryLevel;
            //        IdealBatteryRange = snapshotEntity?.IdealBatteryRange;
            //        RatedBatteryRange = snapshotEntity?.RatedBatteryRange;
            //        OutsideTemperature = snapshotEntity?.OutsideTemperature;
            //        InsideTemperature = snapshotEntity?.InsideTemperature;
            //        DriverTemperatureSetting = snapshotEntity?.DriverTemperatureSetting;
            //        PassengerTemperatureSetting = snapshotEntity?.PassengerTemperatureSetting;
            //        DriverSeatHeater = snapshotEntity?.DriverSeatHeater;
            //        PassengerSeatHeater = snapshotEntity?.PassengerSeatHeater;
            //        FanStatus = snapshotEntity?.FanStatus;
            //        IsSideMirrorHeater = snapshotEntity?.IsSideMirrorHeater;
            //        IsWiperBladeHeater = snapshotEntity?.IsWiperBladeHeater;
            //        IsFrontDefrosterOn = snapshotEntity?.IsFrontDefrosterOn;
            //        IsRearDefrosterOn = snapshotEntity?.IsRearDefrosterOn;
            //        IsClimateOn = snapshotEntity?.IsClimateOn;
            //        IsBatteryHeater = snapshotEntity?.IsBatteryHeater;
            //        IsBatteryHeaterOn = snapshotEntity?.IsBatteryHeaterOn;
            //        Timestamp = snapshotEntity?.CreateTimestamp;
            //    }

            //    public Snapshot Debounce(Snapshot baseSnapshot)
            //    {
            //        if (baseSnapshot == null)
            //        {
            //            return this;
            //        }

            //        return new Snapshot()
            //        {
            //            Location = Location ?? baseSnapshot.Location,
            //            Elevation = Elevation ?? baseSnapshot.Elevation,
            //            Speed = Speed ?? baseSnapshot.Speed,
            //            Heading = Heading ?? baseSnapshot.Heading,
            //            ShiftState = ShiftState ?? baseSnapshot.ShiftState,
            //            Power = Power ?? baseSnapshot.Power,
            //            Odometer = Debounce(Odometer, baseSnapshot?.Odometer, 1),
            //            BatteryLevel = BatteryLevel ?? baseSnapshot.BatteryLevel,
            //            IdealBatteryRange = Debounce(IdealBatteryRange, baseSnapshot.IdealBatteryRange, 1),
            //            RatedBatteryRange = Debounce(RatedBatteryRange, baseSnapshot.RatedBatteryRange, 1),
            //            OutsideTemperature = OutsideTemperature ?? baseSnapshot.OutsideTemperature,
            //            InsideTemperature = InsideTemperature ?? baseSnapshot.InsideTemperature,
            //            DriverTemperatureSetting = DriverTemperatureSetting ?? baseSnapshot.DriverTemperatureSetting,
            //            PassengerTemperatureSetting = PassengerTemperatureSetting ?? baseSnapshot.PassengerTemperatureSetting,
            //            DriverSeatHeater = DriverSeatHeater ?? baseSnapshot.DriverSeatHeater,
            //            PassengerSeatHeater = PassengerSeatHeater ?? baseSnapshot.PassengerSeatHeater,
            //            FanStatus = FanStatus ?? baseSnapshot.FanStatus,
            //            IsSideMirrorHeater = IsSideMirrorHeater ?? baseSnapshot.IsSideMirrorHeater,
            //            IsWiperBladeHeater = IsWiperBladeHeater ?? baseSnapshot.IsWiperBladeHeater,
            //            IsFrontDefrosterOn = IsFrontDefrosterOn ?? baseSnapshot.IsFrontDefrosterOn,
            //            IsRearDefrosterOn = IsRearDefrosterOn ?? baseSnapshot.IsRearDefrosterOn,
            //            IsClimateOn = IsClimateOn ?? baseSnapshot.IsClimateOn,
            //            IsBatteryHeater = IsBatteryHeater ?? baseSnapshot.IsBatteryHeater,
            //            IsBatteryHeaterOn = IsBatteryHeaterOn ?? baseSnapshot.IsBatteryHeaterOn,
            //            Timestamp = Timestamp ?? baseSnapshot.Timestamp,
            //        };
            //    }
            //}

            //#endregion

            //#region StandBy

            //private StandByEntity LastStandByEntity { get; set; }
            //private StandBySnapshot LastStandBySnapshot { get; set; }

            //private async Task StartStandByModeAsync(Instant timestamp)
            //{
            //    if (LastStandByEntity != null && LastStandByEntity.EndTimestamp == null)
            //    {
            //        return;
            //    }

            //    LastStandByEntity = new StandByEntity()
            //    {
            //        StartTimestamp = timestamp,

            //        CarId = EntityId,
            //    };

            //    DatabaseContext.StandBy.Add(LastStandByEntity);

            //    await DatabaseContext.SaveChangesAsync();

            //    Logger.Info($"[{Name ?? VehicleId.ToString()}] Start Stand By");
            //}

            //private async Task RecordStandBySnapshotAsync(Snapshot snapshot, Instant timestamp)
            //{
            //    StandBySnapshot currentStandBySnapshot = (new StandBySnapshot(snapshot) with
            //    {
            //        Timestamp = null,
            //    }).Debounce(LastStandBySnapshot with
            //    {
            //        Timestamp = null,
            //    });

            //    if (!IsSamplingCompression || currentStandBySnapshot with
            //    {
            //        Timestamp = null,
            //    } != LastStandBySnapshot with
            //    {
            //        Timestamp = null,
            //    })
            //    {
            //        currentStandBySnapshot = currentStandBySnapshot with
            //        {
            //            Timestamp = snapshot.Timestamp,
            //        };

            //        DatabaseContext.StandBySnapshot.Add(new StandBySnapshotEntity()
            //        {
            //            Location = currentStandBySnapshot?.Location,
            //            Elevation = currentStandBySnapshot?.Elevation,
            //            Heading = currentStandBySnapshot?.Heading,
            //            Odometer = currentStandBySnapshot?.Odometer,
            //            Power = currentStandBySnapshot?.Power,
            //            BatteryLevel = currentStandBySnapshot?.BatteryLevel,
            //            IdealBatteryRange = currentStandBySnapshot?.IdealBatteryRange,
            //            RatedBatteryRange = currentStandBySnapshot?.RatedBatteryRange,
            //            OutsideTemperature = currentStandBySnapshot?.OutsideTemperature,
            //            InsideTemperature = currentStandBySnapshot?.InsideTemperature,
            //            DriverTemperatureSetting = currentStandBySnapshot?.DriverTemperatureSetting,
            //            PassengerTemperatureSetting = currentStandBySnapshot?.PassengerTemperatureSetting,
            //            DriverSeatHeater = currentStandBySnapshot?.DriverSeatHeater,
            //            PassengerSeatHeater = currentStandBySnapshot?.PassengerSeatHeater,
            //            FanStatus = currentStandBySnapshot?.FanStatus,
            //            IsSideMirrorHeater = currentStandBySnapshot.IsSideMirrorHeater,
            //            IsWiperBladeHeater = currentStandBySnapshot?.IsWiperBladeHeater,
            //            IsFrontDefrosterOn = currentStandBySnapshot?.IsFrontDefrosterOn,
            //            IsRearDefrosterOn = currentStandBySnapshot?.IsRearDefrosterOn,
            //            IsClimateOn = currentStandBySnapshot?.IsClimateOn,
            //            IsBatteryHeater = currentStandBySnapshot?.IsBatteryHeater,
            //            IsBatteryHeaterOn = currentStandBySnapshot?.IsBatteryHeaterOn,

            //            StandBy = LastStandByEntity,

            //            CreateTimestamp = currentStandBySnapshot.Timestamp,
            //            UpdateTimestamp = timestamp,
            //        });

            //        await DatabaseContext.SaveChangesAsync();

            //        LastStandBySnapshot = currentStandBySnapshot;
            //    }
            //}

            //private async Task StopStandByModeAsync(Instant timestamp)
            //{
            //    if (LastStandByEntity != null && LastStandByEntity.EndTimestamp == null)
            //    {
            //        StandBySnapshotEntity firstRecord = await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == LastStandByEntity).OrderBy(x => x.CreateTimestamp).FirstOrDefaultAsync();
            //        StandBySnapshotEntity lastRecord = await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == LastStandByEntity).OrderByDescending(x => x.CreateTimestamp).FirstOrDefaultAsync();

            //        Decimal? onlineDuration = DatabaseContext.State.Where(x => x.StartTimestamp >= LastStandByEntity.StartTimestamp && x.EndTimestamp <= timestamp && x.State == CarState.Online).ToList().Select(x => (Decimal?)((x.EndTimestamp - x.StartTimestamp)?.TotalSeconds)).Sum();

            //        AddressEntity addressEntity = null;
            //        if (lastRecord?.Location != null)
            //        {
            //            addressEntity = await DatabaseContext.Address.Where(x => (Decimal)x.Location.Distance(lastRecord.Location) / 1000 <= x.Radius).Select(x => new
            //            {
            //                Address = x,
            //                Distance = x.Location.Distance(lastRecord.Location) / 1000
            //            }).OrderByDescending(x => x.Distance).Select(x => x.Address).FirstOrDefaultAsync();
            //        }
            //        if (addressEntity == null)
            //        {
            //            OpenStreetMapAddress address = await OpenStreetMapClient.ReverseLookupAsync((Decimal)lastRecord.Location.Y, (Decimal)lastRecord.Location.X, Environment.GetEnvironmentVariable("Language", EnvironmentVariableTarget.Machine) ?? "en-US");

            //            addressEntity = new AddressEntity()
            //            {
            //                Country = address?.Country,
            //                State = address?.State,
            //                County = address?.County,
            //                City = address?.City,
            //                District = address?.District,
            //                Village = address?.Village,
            //                Road = address?.Road,
            //                Building = address?.Building,
            //                Postcode = address?.Postcode,
            //                Name = address?.ToString(),
            //                Location = lastRecord.Location,
            //            };

            //            DatabaseContext.Add(addressEntity);

            //            await DatabaseContext.SaveChangesAsync();
            //        }

            //        //LastStandByEntity.Location = lastRecord?.Location;
            //        LastStandByEntity.Address = addressEntity;
            //        LastStandByEntity.Elevation = lastRecord?.Elevation;
            //        LastStandByEntity.Heading = lastRecord?.Heading;
            //        LastStandByEntity.Odometer = lastRecord?.Odometer;

            //        LastStandByEntity.StartBatteryLevel = firstRecord?.BatteryLevel;
            //        LastStandByEntity.StartIdealBatteryRange = firstRecord?.IdealBatteryRange;
            //        LastStandByEntity.StartRatedBatteryRange = firstRecord?.RatedBatteryRange;
            //        LastStandByEntity.StartPower = firstRecord?.BatteryLevel / 100m * FullPower;

            //        LastStandByEntity.EndTimestamp = timestamp;
            //        LastStandByEntity.EndBatteryLevel = lastRecord?.BatteryLevel;
            //        LastStandByEntity.EndIdealBatteryRange = lastRecord?.IdealBatteryRange;
            //        LastStandByEntity.EndRatedBatteryRange = lastRecord?.RatedBatteryRange;
            //        LastStandByEntity.EndPower = lastRecord?.BatteryLevel / 100m * FullPower;

            //        LastStandByEntity.Duration = (Decimal?)((LastStandByEntity.EndTimestamp - LastStandByEntity.StartTimestamp)?.TotalSeconds);
            //        LastStandByEntity.OnlineRatio = onlineDuration / LastStandByEntity.Duration * 100m;

            //        await DatabaseContext.SaveChangesAsync();

            //        LastStandBySnapshot = null;

            //        Logger.Info($"[{Name ?? VehicleId.ToString()}] Stop Stand By");
            //    }
            //}

            //private record StandBySnapshot : BaseSnapshot
            //{
            //    public Point Location { get; init; }
            //    public Decimal? Elevation { get; init; }
            //    public Decimal? Odometer { get; init; }
            //    public Decimal? Heading { get; init; }
            //    public Decimal? Power { get; init; }
            //    public Decimal? BatteryLevel { get; init; }
            //    public Decimal? IdealBatteryRange { get; init; }
            //    public Decimal? RatedBatteryRange { get; init; }
            //    public Decimal? OutsideTemperature { get; init; }
            //    public Decimal? InsideTemperature { get; init; }
            //    public Decimal? DriverTemperatureSetting { get; init; }
            //    public Decimal? PassengerTemperatureSetting { get; init; }
            //    public Int32? DriverSeatHeater { get; init; }
            //    public Int32? PassengerSeatHeater { get; init; }
            //    public Int32? FanStatus { get; init; }
            //    public Boolean? IsSideMirrorHeater { get; init; }
            //    public Boolean? IsWiperBladeHeater { get; init; }
            //    public Boolean? IsFrontDefrosterOn { get; init; }
            //    public Boolean? IsRearDefrosterOn { get; init; }
            //    public Boolean? IsClimateOn { get; init; }
            //    public Boolean? IsBatteryHeater { get; init; }
            //    public Boolean? IsBatteryHeaterOn { get; init; }

            //    private StandBySnapshot()
            //    {
            //    }

            //    public StandBySnapshot(Snapshot snapshot)
            //    {
            //        Location = snapshot?.Location;
            //        Elevation = snapshot?.Elevation;
            //        Heading = snapshot?.Heading;
            //        Odometer = snapshot?.Odometer;
            //        Power = snapshot?.Power;
            //        BatteryLevel = snapshot?.BatteryLevel;
            //        IdealBatteryRange = snapshot?.IdealBatteryRange;
            //        RatedBatteryRange = snapshot?.RatedBatteryRange;
            //        OutsideTemperature = snapshot?.OutsideTemperature;
            //        InsideTemperature = snapshot?.InsideTemperature;
            //        DriverTemperatureSetting = snapshot?.DriverTemperatureSetting;
            //        PassengerTemperatureSetting = snapshot?.PassengerTemperatureSetting;
            //        DriverSeatHeater = snapshot?.DriverSeatHeater;
            //        PassengerSeatHeater = snapshot?.PassengerSeatHeater;
            //        FanStatus = snapshot?.FanStatus;
            //        IsSideMirrorHeater = snapshot?.IsSideMirrorHeater;
            //        IsWiperBladeHeater = snapshot?.IsWiperBladeHeater;
            //        IsFrontDefrosterOn = snapshot?.IsFrontDefrosterOn;
            //        IsRearDefrosterOn = snapshot?.IsRearDefrosterOn;
            //        IsClimateOn = snapshot?.IsClimateOn;
            //        IsBatteryHeater = snapshot?.IsBatteryHeater;
            //        IsBatteryHeaterOn = snapshot?.IsBatteryHeaterOn;
            //        Timestamp = snapshot?.Timestamp;
            //    }

            //    public StandBySnapshot(StandBySnapshotEntity standBySnapshotEntity)
            //    {
            //        Location = standBySnapshotEntity?.Location;
            //        Elevation = standBySnapshotEntity?.Elevation;
            //        Heading = standBySnapshotEntity?.Heading;
            //        Odometer = standBySnapshotEntity?.Odometer;
            //        Power = standBySnapshotEntity?.Power;
            //        BatteryLevel = standBySnapshotEntity?.BatteryLevel;
            //        IdealBatteryRange = standBySnapshotEntity?.IdealBatteryRange;
            //        RatedBatteryRange = standBySnapshotEntity?.RatedBatteryRange;
            //        OutsideTemperature = standBySnapshotEntity?.OutsideTemperature;
            //        InsideTemperature = standBySnapshotEntity?.InsideTemperature;
            //        DriverTemperatureSetting = standBySnapshotEntity?.DriverTemperatureSetting;
            //        PassengerTemperatureSetting = standBySnapshotEntity?.PassengerTemperatureSetting;
            //        DriverSeatHeater = standBySnapshotEntity?.DriverSeatHeater;
            //        PassengerSeatHeater = standBySnapshotEntity?.PassengerSeatHeater;
            //        FanStatus = standBySnapshotEntity?.FanStatus;
            //        IsSideMirrorHeater = standBySnapshotEntity?.IsSideMirrorHeater;
            //        IsWiperBladeHeater = standBySnapshotEntity?.IsWiperBladeHeater;
            //        IsFrontDefrosterOn = standBySnapshotEntity?.IsFrontDefrosterOn;
            //        IsRearDefrosterOn = standBySnapshotEntity?.IsRearDefrosterOn;
            //        IsClimateOn = standBySnapshotEntity?.IsClimateOn;
            //        IsBatteryHeater = standBySnapshotEntity?.IsBatteryHeater;
            //        IsBatteryHeaterOn = standBySnapshotEntity?.IsBatteryHeaterOn;
            //        Timestamp = standBySnapshotEntity?.CreateTimestamp;
            //    }

            //    public StandBySnapshot Debounce(StandBySnapshot baseSnapshot)
            //    {
            //        if (baseSnapshot == null)
            //        {
            //            return this;
            //        }

            //        return new StandBySnapshot()
            //        {
            //            Location = Location ?? baseSnapshot.Location,
            //            Elevation = Elevation ?? baseSnapshot.Elevation,
            //            Heading = Heading ?? baseSnapshot.Heading,
            //            Odometer = Debounce(Odometer, baseSnapshot?.Odometer, 1),
            //            Power = Power ?? baseSnapshot.Power,
            //            BatteryLevel = BatteryLevel ?? baseSnapshot.BatteryLevel,
            //            IdealBatteryRange = Debounce(IdealBatteryRange, baseSnapshot.IdealBatteryRange, 1),
            //            RatedBatteryRange = Debounce(RatedBatteryRange, baseSnapshot.RatedBatteryRange, 1),
            //            OutsideTemperature = OutsideTemperature ?? baseSnapshot.OutsideTemperature,
            //            InsideTemperature = InsideTemperature ?? baseSnapshot.InsideTemperature,
            //            DriverTemperatureSetting = DriverTemperatureSetting ?? baseSnapshot.DriverTemperatureSetting,
            //            PassengerTemperatureSetting = PassengerTemperatureSetting ?? baseSnapshot.PassengerTemperatureSetting,
            //            DriverSeatHeater = DriverSeatHeater ?? baseSnapshot.DriverSeatHeater,
            //            PassengerSeatHeater = PassengerSeatHeater ?? baseSnapshot.PassengerSeatHeater,
            //            FanStatus = FanStatus ?? baseSnapshot.FanStatus,
            //            IsSideMirrorHeater = IsSideMirrorHeater ?? baseSnapshot.IsSideMirrorHeater,
            //            IsWiperBladeHeater = IsWiperBladeHeater ?? baseSnapshot.IsWiperBladeHeater,
            //            IsFrontDefrosterOn = IsFrontDefrosterOn ?? baseSnapshot.IsFrontDefrosterOn,
            //            IsRearDefrosterOn = IsRearDefrosterOn ?? baseSnapshot.IsRearDefrosterOn,
            //            IsClimateOn = IsClimateOn ?? baseSnapshot.IsClimateOn,
            //            IsBatteryHeater = IsBatteryHeater ?? baseSnapshot.IsBatteryHeater,
            //            IsBatteryHeaterOn = IsBatteryHeaterOn ?? baseSnapshot.IsBatteryHeaterOn,
            //            Timestamp = Timestamp ?? baseSnapshot.Timestamp,
            //        };
            //    }
            //}

            //#endregion

            //#region Driving

            //private DrivingEntity LastDrivingEntity { get; set; }
            //private DrivingSnapshot LastDrivingSnapshot { get; set; }

            //private async Task StartDrivingModeAsync(Instant timestamp)
            //{
            //    if (LastDrivingEntity != null && LastDrivingEntity.EndTimestamp == null)
            //    {
            //        return;
            //    }

            //    LastDrivingEntity = new DrivingEntity()
            //    {
            //        StartTimestamp = timestamp,

            //        CarId = EntityId,
            //    };

            //    DatabaseContext.Driving.Add(LastDrivingEntity);

            //    await DatabaseContext.SaveChangesAsync();

            //    Logger.Info($"[{Name ?? VehicleId.ToString()}] Start Driving");
            //}

            //private async Task RecordDrivingSnapshotAsync(Snapshot snapshot, Instant timestamp)
            //{
            //    DrivingSnapshot currentDrivingSnapshot = (new DrivingSnapshot(snapshot) with
            //    {
            //        Timestamp = null,
            //    }).Debounce(LastDrivingSnapshot with
            //    {
            //        Timestamp = null,
            //    });

            //    if (!IsSamplingCompression || currentDrivingSnapshot with
            //    {
            //        Timestamp = null,
            //    } != LastDrivingSnapshot with
            //    {
            //        Timestamp = null,
            //    })
            //    {
            //        currentDrivingSnapshot = currentDrivingSnapshot with
            //        {
            //            Timestamp = snapshot.Timestamp,
            //        };

            //        DatabaseContext.DrivingSnapshot.Add(new DrivingSnapshotEntity()
            //        {
            //            Location = currentDrivingSnapshot?.Location,
            //            Elevation = currentDrivingSnapshot?.Elevation,
            //            Speed = currentDrivingSnapshot?.Speed,
            //            Heading = currentDrivingSnapshot?.Heading,
            //            ShiftState = currentDrivingSnapshot?.ShiftState,
            //            Power = currentDrivingSnapshot?.Power,
            //            Odometer = currentDrivingSnapshot?.Odometer,
            //            BatteryLevel = currentDrivingSnapshot?.BatteryLevel,
            //            IdealBatteryRange = currentDrivingSnapshot?.IdealBatteryRange,
            //            RatedBatteryRange = currentDrivingSnapshot?.RatedBatteryRange,
            //            OutsideTemperature = currentDrivingSnapshot?.OutsideTemperature,
            //            InsideTemperature = currentDrivingSnapshot?.InsideTemperature,
            //            DriverTemperatureSetting = currentDrivingSnapshot?.DriverTemperatureSetting,
            //            PassengerTemperatureSetting = currentDrivingSnapshot?.PassengerTemperatureSetting,
            //            DriverSeatHeater = currentDrivingSnapshot?.DriverSeatHeater,
            //            PassengerSeatHeater = currentDrivingSnapshot?.PassengerSeatHeater,
            //            FanStatus = currentDrivingSnapshot?.FanStatus,
            //            IsSideMirrorHeater = currentDrivingSnapshot?.IsSideMirrorHeater,
            //            IsWiperBladeHeater = currentDrivingSnapshot?.IsWiperBladeHeater,
            //            IsFrontDefrosterOn = currentDrivingSnapshot?.IsFrontDefrosterOn,
            //            IsRearDefrosterOn = currentDrivingSnapshot?.IsRearDefrosterOn,
            //            IsClimateOn = currentDrivingSnapshot?.IsClimateOn,
            //            IsBatteryHeater = currentDrivingSnapshot?.IsBatteryHeater,
            //            IsBatteryHeaterOn = currentDrivingSnapshot?.IsBatteryHeaterOn,

            //            Driving = LastDrivingEntity,

            //            CreateTimestamp = currentDrivingSnapshot.Timestamp,
            //            UpdateTimestamp = timestamp,
            //        });

            //        await DatabaseContext.SaveChangesAsync();

            //        LastDrivingSnapshot = currentDrivingSnapshot;
            //    }
            //}

            //private async Task StopDrivingModeAsync(Instant timestamp)
            //{
            //    if (LastDrivingEntity != null && LastDrivingEntity.EndTimestamp == null)
            //    {
            //        DrivingSnapshotEntity firstRecord = await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == LastDrivingEntity).OrderBy(x => x.CreateTimestamp).FirstOrDefaultAsync();
            //        DrivingSnapshotEntity lastRecord = await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == LastDrivingEntity).OrderByDescending(x => x.CreateTimestamp).FirstOrDefaultAsync();

            //        AddressEntity startAddressEntity = null;
            //        if (firstRecord?.Location != null)
            //        {
            //            startAddressEntity = await DatabaseContext.Address.Where(x => (Decimal)x.Location.Distance(firstRecord.Location) / 1000 <= x.Radius).Select(x => new
            //            {
            //                Address = x,
            //                Distance = x.Location.Distance(firstRecord.Location) / 1000
            //            }).OrderByDescending(x => x.Distance).Select(x => x.Address).FirstOrDefaultAsync();
            //        }
            //        if (startAddressEntity == null)
            //        {
            //            OpenStreetMapAddress address = await OpenStreetMapClient.ReverseLookupAsync((Decimal)firstRecord.Location.Y, (Decimal)firstRecord.Location.X, Environment.GetEnvironmentVariable("Language", EnvironmentVariableTarget.Machine) ?? "en-US");

            //            startAddressEntity = new AddressEntity()
            //            {
            //                Country = address?.Country,
            //                State = address?.State,
            //                County = address?.County,
            //                City = address?.City,
            //                District = address?.District,
            //                Village = address?.Village,
            //                Road = address?.Road,
            //                Building = address?.Building,
            //                Postcode = address?.Postcode,
            //                Name = address?.ToString(),
            //                Location = lastRecord.Location,
            //            };

            //            DatabaseContext.Add(startAddressEntity);

            //            await DatabaseContext.SaveChangesAsync();
            //        }

            //        AddressEntity endAddressEntity = null;
            //        if (lastRecord?.Location != null)
            //        {
            //            endAddressEntity = await DatabaseContext.Address.Where(x => (Decimal)x.Location.Distance(lastRecord.Location) / 1000 <= x.Radius).Select(x => new
            //            {
            //                Address = x,
            //                Distance = x.Location.Distance(lastRecord.Location) / 1000
            //            }).OrderByDescending(x => x.Distance).Select(x => x.Address).FirstOrDefaultAsync();
            //        }
            //        if (endAddressEntity == null)
            //        {
            //            OpenStreetMapAddress address = await OpenStreetMapClient.ReverseLookupAsync((Decimal)lastRecord.Location.Y, (Decimal)lastRecord.Location.X, Environment.GetEnvironmentVariable("Language", EnvironmentVariableTarget.Machine) ?? "en-US");

            //            endAddressEntity = new AddressEntity()
            //            {
            //                Country = address?.Country,
            //                State = address?.State,
            //                County = address?.County,
            //                City = address?.City,
            //                District = address?.District,
            //                Village = address?.Village,
            //                Road = address?.Road,
            //                Building = address?.Building,
            //                Postcode = address?.Postcode,
            //                Name = address?.ToString(),
            //                Location = lastRecord.Location,
            //            };

            //            DatabaseContext.Add(endAddressEntity);

            //            await DatabaseContext.SaveChangesAsync();
            //        }

            //        LastDrivingEntity.StartAddress = startAddressEntity;
            //        //LastDrivingEntity.StartLocation = firstRecord?.Location;
            //        LastDrivingEntity.StartBatteryLevel = firstRecord?.BatteryLevel;
            //        LastDrivingEntity.StartOdometer = firstRecord?.Odometer;
            //        LastDrivingEntity.StartIdealBatteryRange = firstRecord?.IdealBatteryRange;
            //        LastDrivingEntity.StartRatedBatteryRange = firstRecord?.RatedBatteryRange;
            //        LastDrivingEntity.StartPower = firstRecord?.BatteryLevel / 100m * FullPower;

            //        LastDrivingEntity.EndTimestamp = timestamp;
            //        LastDrivingEntity.EndAddress = endAddressEntity;
            //        LastDrivingEntity.EndLocation = lastRecord?.Location;
            //        LastDrivingEntity.EndBatteryLevel = lastRecord?.BatteryLevel;
            //        LastDrivingEntity.EndOdometer = lastRecord?.Odometer;
            //        LastDrivingEntity.EndIdealBatteryRange = lastRecord?.IdealBatteryRange;
            //        LastDrivingEntity.EndRatedBatteryRange = lastRecord?.RatedBatteryRange;
            //        LastDrivingEntity.EndPower = lastRecord?.BatteryLevel / 100m * FullPower;

            //        LastDrivingEntity.Duration = (Decimal?)((LastDrivingEntity.EndTimestamp - LastDrivingEntity.StartTimestamp)?.TotalSeconds);
            //        LastDrivingEntity.Distance = LastDrivingEntity?.EndOdometer - LastDrivingEntity?.StartOdometer;
            //        LastDrivingEntity.SpeedAverage = LastDrivingEntity.Duration > 0 ? LastDrivingEntity.Distance / LastDrivingEntity.Duration * 3600m : 0;

            //        await DatabaseContext.SaveChangesAsync();

            //        LastDrivingSnapshot = null;

            //        Logger.Info($"[{Name ?? VehicleId.ToString()}] Stop Driving");
            //    }
            //}

            //private record DrivingSnapshot : BaseSnapshot
            //{
            //    public Point Location { get; init; }
            //    public Decimal? Elevation { get; init; }
            //    public Decimal? Speed { get; init; }
            //    public Decimal? Heading { get; init; }
            //    public ShiftState? ShiftState { get; init; }
            //    public Decimal? Power { get; init; }
            //    public Decimal? Odometer { get; init; }
            //    public Decimal? BatteryLevel { get; init; }
            //    public Decimal? IdealBatteryRange { get; init; }
            //    public Decimal? RatedBatteryRange { get; init; }
            //    public Decimal? OutsideTemperature { get; init; }
            //    public Decimal? InsideTemperature { get; init; }
            //    public Decimal? DriverTemperatureSetting { get; init; }
            //    public Decimal? PassengerTemperatureSetting { get; init; }
            //    public Int32? DriverSeatHeater { get; init; }
            //    public Int32? PassengerSeatHeater { get; init; }
            //    public Int32? FanStatus { get; init; }
            //    public Boolean? IsSideMirrorHeater { get; init; }
            //    public Boolean? IsWiperBladeHeater { get; init; }
            //    public Boolean? IsFrontDefrosterOn { get; init; }
            //    public Boolean? IsRearDefrosterOn { get; init; }
            //    public Boolean? IsClimateOn { get; init; }
            //    public Boolean? IsBatteryHeater { get; init; }
            //    public Boolean? IsBatteryHeaterOn { get; init; }

            //    private DrivingSnapshot()
            //    {
            //    }

            //    public DrivingSnapshot(Snapshot snapshot)
            //    {
            //        Location = snapshot?.Location;
            //        Elevation = snapshot?.Elevation ?? null;
            //        Speed = snapshot?.Speed;
            //        Heading = snapshot?.Heading;
            //        ShiftState = snapshot?.ShiftState;
            //        Power = snapshot?.Power;
            //        Odometer = snapshot?.Odometer;
            //        BatteryLevel = snapshot?.BatteryLevel;
            //        IdealBatteryRange = snapshot?.IdealBatteryRange;
            //        RatedBatteryRange = snapshot?.RatedBatteryRange;
            //        OutsideTemperature = snapshot?.OutsideTemperature;
            //        InsideTemperature = snapshot?.InsideTemperature;
            //        DriverTemperatureSetting = snapshot?.DriverTemperatureSetting;
            //        PassengerTemperatureSetting = snapshot?.PassengerTemperatureSetting;
            //        DriverSeatHeater = snapshot?.DriverSeatHeater;
            //        PassengerSeatHeater = snapshot?.PassengerSeatHeater;
            //        FanStatus = snapshot?.FanStatus;
            //        IsSideMirrorHeater = snapshot?.IsSideMirrorHeater;
            //        IsWiperBladeHeater = snapshot?.IsWiperBladeHeater;
            //        IsFrontDefrosterOn = snapshot?.IsFrontDefrosterOn;
            //        IsRearDefrosterOn = snapshot?.IsRearDefrosterOn;
            //        IsClimateOn = snapshot?.IsClimateOn;
            //        IsBatteryHeater = snapshot?.IsBatteryHeater;
            //        IsBatteryHeaterOn = snapshot?.IsBatteryHeaterOn;
            //        Timestamp = snapshot?.Timestamp;
            //    }

            //    public DrivingSnapshot(DrivingSnapshotEntity drivingSnapshotEntity)
            //    {
            //        Location = drivingSnapshotEntity?.Location;
            //        Elevation = drivingSnapshotEntity?.Elevation;
            //        Speed = drivingSnapshotEntity?.Speed;
            //        Heading = drivingSnapshotEntity?.Heading;
            //        ShiftState = drivingSnapshotEntity?.ShiftState;
            //        Power = drivingSnapshotEntity?.Power;
            //        Odometer = drivingSnapshotEntity?.Odometer;
            //        BatteryLevel = drivingSnapshotEntity?.BatteryLevel;
            //        IdealBatteryRange = drivingSnapshotEntity?.IdealBatteryRange;
            //        RatedBatteryRange = drivingSnapshotEntity?.RatedBatteryRange;
            //        OutsideTemperature = drivingSnapshotEntity?.OutsideTemperature;
            //        InsideTemperature = drivingSnapshotEntity?.InsideTemperature;
            //        DriverTemperatureSetting = drivingSnapshotEntity?.DriverTemperatureSetting;
            //        PassengerTemperatureSetting = drivingSnapshotEntity?.PassengerTemperatureSetting;
            //        DriverSeatHeater = drivingSnapshotEntity?.DriverSeatHeater;
            //        PassengerSeatHeater = drivingSnapshotEntity?.PassengerSeatHeater;
            //        FanStatus = drivingSnapshotEntity?.FanStatus;
            //        IsSideMirrorHeater = drivingSnapshotEntity?.IsSideMirrorHeater;
            //        IsWiperBladeHeater = drivingSnapshotEntity?.IsWiperBladeHeater;
            //        IsFrontDefrosterOn = drivingSnapshotEntity?.IsFrontDefrosterOn;
            //        IsRearDefrosterOn = drivingSnapshotEntity?.IsRearDefrosterOn;
            //        IsClimateOn = drivingSnapshotEntity?.IsClimateOn;
            //        IsBatteryHeater = drivingSnapshotEntity?.IsBatteryHeater;
            //        IsBatteryHeaterOn = drivingSnapshotEntity?.IsBatteryHeaterOn;
            //        Timestamp = drivingSnapshotEntity?.CreateTimestamp;
            //    }

            //    public DrivingSnapshot Debounce(DrivingSnapshot baseSnapshot)
            //    {
            //        if (baseSnapshot == null)
            //        {
            //            return this;
            //        }

            //        return new DrivingSnapshot()
            //        {
            //            Location = Location ?? baseSnapshot.Location,
            //            Elevation = Elevation ?? baseSnapshot.Elevation,
            //            Speed = Speed ?? baseSnapshot.Speed,
            //            Heading = Heading ?? baseSnapshot.Heading,
            //            ShiftState = ShiftState ?? baseSnapshot.ShiftState,
            //            Power = Power ?? baseSnapshot.Power,
            //            Odometer = Debounce(Odometer, baseSnapshot?.Odometer, 1),
            //            BatteryLevel = BatteryLevel ?? baseSnapshot.BatteryLevel,
            //            IdealBatteryRange = Debounce(IdealBatteryRange, baseSnapshot.IdealBatteryRange, 1),
            //            RatedBatteryRange = Debounce(RatedBatteryRange, baseSnapshot.RatedBatteryRange, 1),
            //            OutsideTemperature = OutsideTemperature ?? baseSnapshot.OutsideTemperature,
            //            InsideTemperature = InsideTemperature ?? baseSnapshot.InsideTemperature,
            //            DriverTemperatureSetting = DriverTemperatureSetting ?? baseSnapshot.DriverTemperatureSetting,
            //            PassengerTemperatureSetting = PassengerTemperatureSetting ?? baseSnapshot.PassengerTemperatureSetting,
            //            DriverSeatHeater = DriverSeatHeater ?? baseSnapshot.DriverSeatHeater,
            //            PassengerSeatHeater = PassengerSeatHeater ?? baseSnapshot.PassengerSeatHeater,
            //            FanStatus = FanStatus ?? baseSnapshot.FanStatus,
            //            IsSideMirrorHeater = IsSideMirrorHeater ?? baseSnapshot.IsSideMirrorHeater,
            //            IsWiperBladeHeater = IsWiperBladeHeater ?? baseSnapshot.IsWiperBladeHeater,
            //            IsFrontDefrosterOn = IsFrontDefrosterOn ?? baseSnapshot.IsFrontDefrosterOn,
            //            IsRearDefrosterOn = IsRearDefrosterOn ?? baseSnapshot.IsRearDefrosterOn,
            //            IsClimateOn = IsClimateOn ?? baseSnapshot.IsClimateOn,
            //            IsBatteryHeater = IsBatteryHeater ?? baseSnapshot.IsBatteryHeater,
            //            IsBatteryHeaterOn = IsBatteryHeaterOn ?? baseSnapshot.IsBatteryHeaterOn,
            //            Timestamp = Timestamp ?? baseSnapshot.Timestamp,
            //        };
            //    }
            //}

            //#endregion

            //#region Charging

            //private ChargingEntity LastChargingEntity { get; set; }
            //private ChargingSnapshot LastChargingSnapshot { get; set; }

            //private async Task StartChargingModeAsync(Instant timestamp)
            //{
            //    if (LastChargingEntity != null && LastChargingEntity.EndTimestamp == null)
            //    {
            //        return;
            //    }

            //    LastChargingEntity = new ChargingEntity()
            //    {
            //        StartTimestamp = timestamp,

            //        CarId = EntityId,
            //    };

            //    DatabaseContext.Charging.Add(LastChargingEntity);

            //    await DatabaseContext.SaveChangesAsync();

            //    Logger.Info($"[{Name ?? VehicleId.ToString()}] Start Charging");
            //}

            //private async Task RecordChargingSnapshotAsync(Snapshot snapshot, Instant timestamp)
            //{
            //    ChargingSnapshot currentChargingSnapshot = new(snapshot);

            //    currentChargingSnapshot = (currentChargingSnapshot with
            //    {
            //        Timestamp = null,
            //    }).Debounce(LastChargingSnapshot with
            //    {
            //        Timestamp = null,
            //    });

            //    if (!IsSamplingCompression || currentChargingSnapshot with
            //    {
            //        Timestamp = null,
            //    } != LastChargingSnapshot with
            //    {
            //        Timestamp = null,
            //    })
            //    {
            //        currentChargingSnapshot = currentChargingSnapshot with
            //        {
            //            Timestamp = snapshot.Timestamp,
            //        };

            //        DatabaseContext.ChargingSnapshot.Add(new ChargingSnapshotEntity()
            //        {
            //            Location = currentChargingSnapshot?.Location,
            //            Elevation = currentChargingSnapshot?.Elevation,
            //            Heading = currentChargingSnapshot?.Heading,
            //            Odometer = currentChargingSnapshot?.Odometer,
            //            BatteryLevel = currentChargingSnapshot?.BatteryLevel,
            //            IdealBatteryRange = currentChargingSnapshot?.IdealBatteryRange,
            //            RatedBatteryRange = currentChargingSnapshot?.RatedBatteryRange,
            //            OutsideTemperature = currentChargingSnapshot?.OutsideTemperature,
            //            InsideTemperature = currentChargingSnapshot?.InsideTemperature,
            //            DriverTemperatureSetting = currentChargingSnapshot?.DriverTemperatureSetting,
            //            PassengerTemperatureSetting = currentChargingSnapshot?.PassengerTemperatureSetting,
            //            DriverSeatHeater = currentChargingSnapshot?.DriverSeatHeater,
            //            PassengerSeatHeater = currentChargingSnapshot?.PassengerSeatHeater,
            //            FanStatus = currentChargingSnapshot?.FanStatus,
            //            IsSideMirrorHeater = currentChargingSnapshot.IsSideMirrorHeater,
            //            IsWiperBladeHeater = currentChargingSnapshot?.IsWiperBladeHeater,
            //            IsFrontDefrosterOn = currentChargingSnapshot?.IsFrontDefrosterOn,
            //            IsRearDefrosterOn = currentChargingSnapshot?.IsRearDefrosterOn,
            //            IsClimateOn = currentChargingSnapshot?.IsClimateOn,
            //            IsBatteryHeater = currentChargingSnapshot?.IsBatteryHeater,
            //            IsBatteryHeaterOn = currentChargingSnapshot?.IsBatteryHeaterOn,
            //            ChargeEnergyAdded = currentChargingSnapshot?.ChargeEnergyAdded,
            //            ChargerPhases = currentChargingSnapshot?.ChargerPhases,
            //            ChargerPilotCurrent = currentChargingSnapshot?.ChargerPilotCurrent,
            //            ChargerActualCurrent = currentChargingSnapshot?.ChargerActualCurrent,
            //            ChargerPower = currentChargingSnapshot?.ChargerPower,
            //            ChargerVoltage = currentChargingSnapshot?.ChargerVoltage,
            //            ChargeRate = currentChargingSnapshot?.ChargeRate,
            //            IsFastChargerPresent = currentChargingSnapshot?.IsFastChargerPresent,
            //            ChargeCable = currentChargingSnapshot?.ChargeCable,
            //            FastChargerBrand = currentChargingSnapshot?.FastChargerBrand,
            //            FastChargerType = currentChargingSnapshot?.FastChargerType,

            //            Charging = LastChargingEntity,

            //            CreateTimestamp = currentChargingSnapshot.Timestamp,
            //            UpdateTimestamp = timestamp,
            //        });

            //        await DatabaseContext.SaveChangesAsync();

            //        LastChargingSnapshot = currentChargingSnapshot;
            //    }
            //}

            //private async Task StopChargingModeAsync(Instant timestamp)
            //{
            //    if (LastChargingEntity != null && LastChargingEntity.EndTimestamp == null)
            //    {
            //        ChargingSnapshotEntity firstRecord = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == LastChargingEntity).OrderBy(x => x.CreateTimestamp).FirstOrDefaultAsync();
            //        ChargingSnapshotEntity lastRecord = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == LastChargingEntity).OrderByDescending(x => x.CreateTimestamp).FirstOrDefaultAsync();

            //        Decimal chargeEnergyUsed = 0m;
            //        var details = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == LastChargingEntity).OrderBy(x => x.CreateTimestamp).Select(x => new
            //        {
            //            Timestamp = x.CreateTimestamp,
            //            Current = x.ChargerActualCurrent,
            //            Voltage = x.ChargerVoltage,
            //            Phases = x.ChargerPhases,
            //            Power = x.ChargerPower,
            //        }).ToListAsync();
            //        if (details.Count > 0)
            //        {
            //            for (Int32 i = 0; i < details.Count - 1; i++)
            //            {
            //                chargeEnergyUsed += CalculateEnergyUsed(details[i].Current, details[i].Voltage, details[i].Phases, details[i].Power) * (Decimal?)(details[i + 1].Timestamp - details[i].Timestamp)?.TotalHours ?? 0m;
            //            }
            //            chargeEnergyUsed += CalculateEnergyUsed(details[^1].Current, details[^1].Voltage, details[^1].Phases, details[^1].Power) * (Decimal?)(timestamp - details[^1].Timestamp)?.TotalHours ?? 0m;
            //        }

            //        AddressEntity addressEntity = null;
            //        if (lastRecord?.Location != null)
            //        {
            //            addressEntity = await DatabaseContext.Address.Where(x => (Decimal)x.Location.Distance(lastRecord.Location) / 1000 <= x.Radius).Select(x => new
            //            {
            //                Address = x,
            //                Distance = x.Location.Distance(lastRecord.Location) / 1000
            //            }).OrderByDescending(x => x.Distance).Select(x => x.Address).FirstOrDefaultAsync();
            //        }
            //        if (addressEntity == null)
            //        {
            //            OpenStreetMapAddress address = await OpenStreetMapClient.ReverseLookupAsync((Decimal)lastRecord.Location.Y, (Decimal)lastRecord.Location.X, Environment.GetEnvironmentVariable("Language", EnvironmentVariableTarget.Machine) ?? "en-US");

            //            addressEntity = new AddressEntity()
            //            {
            //                Country = address?.Country,
            //                State = address?.State,
            //                County = address?.County,
            //                City = address?.City,
            //                District = address?.District,
            //                Village = address?.Village,
            //                Road = address?.Road,
            //                Building = address?.Building,
            //                Postcode = address?.Postcode,
            //                Name = address?.ToString(),
            //                Location = lastRecord.Location,
            //            };

            //            DatabaseContext.Add(addressEntity);

            //            await DatabaseContext.SaveChangesAsync();
            //        }

            //        LastChargingEntity.Address = addressEntity;
            //        LastChargingEntity.Location = lastRecord?.Location;
            //        LastChargingEntity.Elevation = lastRecord?.Elevation;
            //        LastChargingEntity.Heading = lastRecord?.Heading;
            //        LastChargingEntity.Odometer = lastRecord?.Odometer;
            //        LastChargingEntity.IsFastChargerPresent = lastRecord?.IsFastChargerPresent;
            //        LastChargingEntity.ChargeCable = lastRecord?.ChargeCable;
            //        LastChargingEntity.FastChargerBrand = lastRecord?.FastChargerBrand;
            //        LastChargingEntity.FastChargerType = lastRecord?.FastChargerType;

            //        LastChargingEntity.StartBatteryLevel = firstRecord?.BatteryLevel;
            //        LastChargingEntity.StartIdealBatteryRange = firstRecord?.IdealBatteryRange;
            //        LastChargingEntity.StartRatedBatteryRange = firstRecord?.RatedBatteryRange;
            //        LastChargingEntity.StartPower = firstRecord?.BatteryLevel / 100m * FullPower;

            //        LastChargingEntity.EndTimestamp = timestamp;
            //        LastChargingEntity.EndBatteryLevel = lastRecord?.BatteryLevel;
            //        LastChargingEntity.EndIdealBatteryRange = lastRecord?.IdealBatteryRange;
            //        LastChargingEntity.EndRatedBatteryRange = lastRecord?.RatedBatteryRange;
            //        LastChargingEntity.EndPower = lastRecord?.BatteryLevel / 100m * FullPower;

            //        LastChargingEntity.Duration = (Decimal?)((LastChargingEntity.EndTimestamp - LastChargingEntity.StartTimestamp)?.TotalSeconds);
            //        LastChargingEntity.ChargeEnergyAdded = lastRecord?.ChargeEnergyAdded - firstRecord?.ChargeEnergyAdded;
            //        LastChargingEntity.ChargeEnergyUsed = chargeEnergyUsed;
            //        LastChargingEntity.Efficiency = LastChargingEntity.ChargeEnergyAdded / LastChargingEntity.ChargeEnergyUsed * 100m;

            //        await DatabaseContext.SaveChangesAsync();

            //        LastChargingSnapshot = null;

            //        Logger.Info($"[{Name ?? VehicleId.ToString()}] Stop Charging");
            //    }
            //}

            //private record ChargingSnapshot : BaseSnapshot
            //{
            //    public Point Location { get; init; }
            //    public Decimal? Elevation { get; init; }
            //    public Decimal? Odometer { get; init; }
            //    public Decimal? Heading { get; init; }
            //    public Decimal? BatteryLevel { get; init; }
            //    public Decimal? IdealBatteryRange { get; init; }
            //    public Decimal? RatedBatteryRange { get; init; }
            //    public Decimal? OutsideTemperature { get; init; }
            //    public Decimal? InsideTemperature { get; init; }
            //    public Decimal? DriverTemperatureSetting { get; init; }
            //    public Decimal? PassengerTemperatureSetting { get; init; }
            //    public Int32? DriverSeatHeater { get; init; }
            //    public Int32? PassengerSeatHeater { get; init; }
            //    public Int32? FanStatus { get; init; }
            //    public Boolean? IsSideMirrorHeater { get; init; }
            //    public Boolean? IsWiperBladeHeater { get; init; }
            //    public Boolean? IsFrontDefrosterOn { get; init; }
            //    public Boolean? IsRearDefrosterOn { get; init; }
            //    public Boolean? IsClimateOn { get; init; }
            //    public Boolean? IsBatteryHeater { get; init; }
            //    public Boolean? IsBatteryHeaterOn { get; init; }
            //    public Decimal? ChargeEnergyAdded { get; init; }
            //    public Int32? ChargerPhases { get; init; }
            //    public Int32? ChargerPilotCurrent { get; init; }
            //    public Int32? ChargerActualCurrent { get; init; }
            //    public Int32? ChargerPower { get; init; }
            //    public Int32? ChargerVoltage { get; init; }
            //    public Decimal? ChargeRate { get; init; }
            //    public Boolean? IsFastChargerPresent { get; set; }
            //    public String ChargeCable { get; set; }
            //    public String FastChargerBrand { get; set; }
            //    public String FastChargerType { get; set; }

            //    private ChargingSnapshot()
            //    {
            //    }

            //    public ChargingSnapshot(Snapshot snapshot)
            //    {
            //        Location = snapshot?.Location;
            //        Elevation = snapshot?.Elevation;
            //        Heading = snapshot?.Heading;
            //        Odometer = snapshot?.Odometer;
            //        BatteryLevel = snapshot?.BatteryLevel;
            //        IdealBatteryRange = snapshot.IdealBatteryRange;
            //        RatedBatteryRange = snapshot?.RatedBatteryRange;
            //        OutsideTemperature = snapshot?.OutsideTemperature;
            //        InsideTemperature = snapshot?.InsideTemperature;
            //        DriverTemperatureSetting = snapshot?.DriverTemperatureSetting;
            //        PassengerTemperatureSetting = snapshot?.PassengerTemperatureSetting;
            //        DriverSeatHeater = snapshot?.DriverSeatHeater;
            //        PassengerSeatHeater = snapshot?.PassengerSeatHeater;
            //        FanStatus = snapshot?.FanStatus;
            //        IsSideMirrorHeater = snapshot?.IsSideMirrorHeater;
            //        IsWiperBladeHeater = snapshot?.IsWiperBladeHeater;
            //        IsFrontDefrosterOn = snapshot?.IsFrontDefrosterOn;
            //        IsRearDefrosterOn = snapshot?.IsRearDefrosterOn;
            //        IsClimateOn = snapshot?.IsClimateOn;
            //        IsBatteryHeater = snapshot?.IsBatteryHeater;
            //        IsBatteryHeaterOn = snapshot?.IsBatteryHeaterOn;
            //        ChargeEnergyAdded = snapshot?.ChargeEnergyAdded;
            //        ChargerPhases = snapshot?.ChargerPhases;
            //        ChargerPilotCurrent = snapshot?.ChargerPilotCurrent;
            //        ChargerActualCurrent = snapshot?.ChargerActualCurrent;
            //        ChargerPower = snapshot?.ChargerPower;
            //        ChargerVoltage = snapshot?.ChargerVoltage;
            //        ChargeRate = snapshot?.ChargeRate;
            //        IsFastChargerPresent = snapshot?.IsFastChargerPresent;
            //        ChargeCable = snapshot?.ChargeCable;
            //        FastChargerBrand = snapshot?.FastChargerBrand;
            //        FastChargerType = snapshot?.FastChargerType;
            //        Timestamp = snapshot?.Timestamp;
            //    }

            //    public ChargingSnapshot(ChargingSnapshotEntity ChargingSnapshotEntity)
            //    {
            //        Location = ChargingSnapshotEntity?.Location;
            //        Elevation = ChargingSnapshotEntity?.Elevation;
            //        Heading = ChargingSnapshotEntity?.Heading;
            //        Odometer = ChargingSnapshotEntity?.Odometer;
            //        BatteryLevel = ChargingSnapshotEntity?.BatteryLevel;
            //        IdealBatteryRange = ChargingSnapshotEntity?.IdealBatteryRange;
            //        RatedBatteryRange = ChargingSnapshotEntity?.RatedBatteryRange;
            //        OutsideTemperature = ChargingSnapshotEntity?.OutsideTemperature;
            //        InsideTemperature = ChargingSnapshotEntity?.InsideTemperature;
            //        DriverTemperatureSetting = ChargingSnapshotEntity?.DriverTemperatureSetting;
            //        PassengerTemperatureSetting = ChargingSnapshotEntity?.PassengerTemperatureSetting;
            //        DriverSeatHeater = ChargingSnapshotEntity?.DriverSeatHeater;
            //        PassengerSeatHeater = ChargingSnapshotEntity?.PassengerSeatHeater;
            //        FanStatus = ChargingSnapshotEntity?.FanStatus;
            //        IsSideMirrorHeater = ChargingSnapshotEntity?.IsSideMirrorHeater;
            //        IsWiperBladeHeater = ChargingSnapshotEntity?.IsWiperBladeHeater;
            //        IsFrontDefrosterOn = ChargingSnapshotEntity?.IsFrontDefrosterOn;
            //        IsRearDefrosterOn = ChargingSnapshotEntity?.IsRearDefrosterOn;
            //        IsClimateOn = ChargingSnapshotEntity?.IsClimateOn;
            //        IsBatteryHeater = ChargingSnapshotEntity?.IsBatteryHeater;
            //        IsBatteryHeaterOn = ChargingSnapshotEntity?.IsBatteryHeaterOn;
            //        ChargeEnergyAdded = ChargingSnapshotEntity?.ChargeEnergyAdded;
            //        ChargerPhases = ChargingSnapshotEntity?.ChargerPhases;
            //        ChargerPilotCurrent = ChargingSnapshotEntity?.ChargerPilotCurrent;
            //        ChargerActualCurrent = ChargingSnapshotEntity?.ChargerActualCurrent;
            //        ChargerPower = ChargingSnapshotEntity?.ChargerPower;
            //        ChargerVoltage = ChargingSnapshotEntity?.ChargerVoltage;
            //        ChargeRate = ChargingSnapshotEntity?.ChargeRate;
            //        IsFastChargerPresent = ChargingSnapshotEntity?.IsFastChargerPresent;
            //        ChargeCable = ChargingSnapshotEntity?.ChargeCable;
            //        FastChargerBrand = ChargingSnapshotEntity?.FastChargerBrand;
            //        FastChargerType = ChargingSnapshotEntity?.FastChargerType;
            //        Timestamp = ChargingSnapshotEntity?.CreateTimestamp;
            //    }

            //    public ChargingSnapshot Debounce(ChargingSnapshot baseSnapshot)
            //    {
            //        if (baseSnapshot == null)
            //        {
            //            return this;
            //        }

            //        return new ChargingSnapshot()
            //        {
            //            Location = Location ?? baseSnapshot.Location,
            //            Elevation = Elevation ?? baseSnapshot.Elevation,
            //            Heading = Heading ?? baseSnapshot.Heading,
            //            Odometer = Debounce(Odometer, baseSnapshot?.Odometer, 1),
            //            BatteryLevel = BatteryLevel ?? baseSnapshot.BatteryLevel,
            //            IdealBatteryRange = Debounce(IdealBatteryRange, baseSnapshot.IdealBatteryRange, 1),
            //            RatedBatteryRange = Debounce(RatedBatteryRange, baseSnapshot.RatedBatteryRange, 1),
            //            OutsideTemperature = OutsideTemperature ?? baseSnapshot.OutsideTemperature,
            //            InsideTemperature = InsideTemperature ?? baseSnapshot.InsideTemperature,
            //            DriverTemperatureSetting = DriverTemperatureSetting ?? baseSnapshot.DriverTemperatureSetting,
            //            PassengerTemperatureSetting = PassengerTemperatureSetting ?? baseSnapshot.PassengerTemperatureSetting,
            //            DriverSeatHeater = DriverSeatHeater ?? baseSnapshot.DriverSeatHeater,
            //            PassengerSeatHeater = PassengerSeatHeater ?? baseSnapshot.PassengerSeatHeater,
            //            FanStatus = FanStatus ?? baseSnapshot.FanStatus,
            //            IsSideMirrorHeater = IsSideMirrorHeater ?? baseSnapshot.IsSideMirrorHeater,
            //            IsWiperBladeHeater = IsWiperBladeHeater ?? baseSnapshot.IsWiperBladeHeater,
            //            IsFrontDefrosterOn = IsFrontDefrosterOn ?? baseSnapshot.IsFrontDefrosterOn,
            //            IsRearDefrosterOn = IsRearDefrosterOn ?? baseSnapshot.IsRearDefrosterOn,
            //            IsClimateOn = IsClimateOn ?? baseSnapshot.IsClimateOn,
            //            IsBatteryHeater = IsBatteryHeater ?? baseSnapshot.IsBatteryHeater,
            //            IsBatteryHeaterOn = IsBatteryHeaterOn ?? baseSnapshot.IsBatteryHeaterOn,
            //            ChargeEnergyAdded = ChargeEnergyAdded ?? baseSnapshot.ChargeEnergyAdded,
            //            ChargerPhases = ChargerPhases ?? baseSnapshot.ChargerPhases,
            //            ChargerPilotCurrent = ChargerPilotCurrent ?? baseSnapshot.ChargerPilotCurrent,
            //            ChargerActualCurrent = ChargerActualCurrent ?? baseSnapshot.ChargerActualCurrent,
            //            ChargerPower = ChargerPower ?? baseSnapshot.ChargerPower,
            //            ChargerVoltage = ChargerVoltage ?? baseSnapshot.ChargerVoltage,
            //            ChargeRate = ChargeRate ?? baseSnapshot.ChargeRate,
            //            IsFastChargerPresent = IsFastChargerPresent ?? baseSnapshot.IsFastChargerPresent,
            //            ChargeCable = ChargeCable ?? baseSnapshot.ChargeCable,
            //            FastChargerBrand = FastChargerBrand ?? baseSnapshot.FastChargerBrand,
            //            FastChargerType = FastChargerType ?? baseSnapshot.FastChargerType,
            //            Timestamp = Timestamp ?? baseSnapshot.Timestamp,
            //        };
            //    }
            //}

            //#endregion

            //private record BaseSnapshot
            //{
            //    public Instant? Timestamp { get; init; }

            //    protected Decimal? Debounce(Decimal? newValue, Decimal? oldValue, Decimal threshold)
            //    {
            //        if (oldValue == null && newValue != null)
            //        {
            //            return newValue.Value;
            //        }
            //        else if (oldValue != null && newValue == null)
            //        {
            //            return oldValue;
            //        }
            //        else if (oldValue == null && newValue == null)
            //        {
            //            return null;
            //        }

            //        if (Math.Abs(newValue.Value - oldValue.Value) < Math.Abs(threshold))
            //        {
            //            return oldValue;
            //        }

            //        return newValue;
            //    }
            //}

            //private Decimal? CalculateEnergyUsed(Decimal? current, Decimal? voltage, Int32? phases, Decimal? power)
            //{
            //    Decimal? adjustedPhases = DeterminePhases(current, voltage, phases, power);

            //    if (adjustedPhases != null)
            //    {
            //        return power;
            //    }
            //    else
            //    {
            //        return current * voltage * adjustedPhases / 1000m;
            //    }
            //}

            //private Decimal? DeterminePhases(Decimal? current, Decimal? voltage, Int32? phases, Decimal? power)
            //{
            //    if (current == null || voltage == null || phases == null || power == null)
            //    {
            //        return null;
            //    }

            //    Decimal? predictivePhases = current * voltage == 0 ? 0 : power * 1000m / (current * voltage);

            //    if (phases == Math.Round(predictivePhases.Value, 0, MidpointRounding.AwayFromZero))
            //    {
            //        return phases;
            //    }
            //    else if (phases == 3 && Math.Abs(power.Value / (Decimal)Math.Sqrt(phases.Value)) - 1 <= 0.1m)
            //    {
            //        return (Decimal)Math.Sqrt(phases.Value);
            //    }
            //    else if (Math.Abs(Math.Round(predictivePhases.Value, 0, MidpointRounding.AwayFromZero) - predictivePhases.Value) <= 0.3m)
            //    {
            //        return Math.Round(predictivePhases.Value, 0, MidpointRounding.AwayFromZero);
            //    }

            //    return null;
            //}

            //private void InitialEntity()
            //{
            //    LastStateEntity = DatabaseContext.State.Where(x => x.CarId == EntityId).OrderByDescending(x => x.Id).FirstOrDefault();
            //    LastSnapshot = new Snapshot(DatabaseContext.Snapshot.Where(x => x.State == LastStateEntity).OrderByDescending(x => x.CreateTimestamp).FirstOrDefault());
            //    LastStandByEntity = DatabaseContext.StandBy.OrderByDescending(x => x.Id).FirstOrDefault();
            //    LastStandBySnapshot = new StandBySnapshot(DatabaseContext.StandBySnapshot.Where(x => x.StandBy == LastStandByEntity).OrderByDescending(x => x.CreateTimestamp).FirstOrDefault());
            //    LastDrivingEntity = DatabaseContext.Driving.OrderByDescending(x => x.Id).FirstOrDefault();
            //    LastDrivingSnapshot = new DrivingSnapshot(DatabaseContext.DrivingSnapshot.Where(x => x.Driving == LastDrivingEntity).OrderByDescending(x => x.CreateTimestamp).FirstOrDefault());
            //    LastChargingEntity = DatabaseContext.Charging.OrderByDescending(x => x.Id).FirstOrDefault();
            //    LastChargingSnapshot = new ChargingSnapshot(DatabaseContext.ChargingSnapshot.Where(x => x.Charging == LastChargingEntity).OrderByDescending(x => x.CreateTimestamp).FirstOrDefault());
            //}
        }
    }
}
