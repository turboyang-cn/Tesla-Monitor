//using System;
//using System.Linq;
//using System.Linq.Dynamic.Core;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//using NLog;

//using NodaTime;

//using TurboYang.Tesla.Monitor.Client;
//using TurboYang.Tesla.Monitor.Database;
//using TurboYang.Tesla.Monitor.Database.Entities;
//using TurboYang.Tesla.Monitor.Model;

//namespace TurboYang.Tesla.Monitor.WebApi.Services
//{
//    public class TeslaCarWorker
//    {
//        private CancellationTokenSource CancellationTokenSource { get; set; }
//        private DatabaseContext DatabaseContext { get; }
//        private ITeslaClient TeslaClient { get; }
//        private String AccessToken { get; }
//        private String CarId { get; }
//        private Int64 VehicleId { get; }
//        private ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
//        private JsonOptions JsonOptions { get; }
//        private Task LoopTask { get; set; }
//        private Boolean _IsRunning = false;
//        public Boolean IsRunning
//        {
//            get
//            {
//                return _IsRunning;
//            }
//            private set
//            {
//                if (_IsRunning != value)
//                {
//                    _IsRunning = value;

//                    if (_IsRunning)
//                    {
//                        Logger.Info($"Start Car Loop: {VehicleId}");
//                    }
//                    else
//                    {
//                        Logger.Info($"Stop Car Loop: {VehicleId}");
//                    }
//                }
//            }
//        }
//        private CarEntity CarEntity { get; set; }
//        private StateEntity LastStateEntity { get; set; }
//        private Snapshot LastSnapshot { get; set; }
//        private StandByEntity LastStandByEntity { get; set; }
//        private StandBySnapshot LastStandBySnapshot { get; set; }
//        private DrivingEntity LastDrivingEntity { get; set; }
//        private DrivingSnapshot LastDrivingSnapshot { get; set; }
//        private ChargingEntity LastChargingEntity { get; set; }
//        private ChargingSnapshot LastChargingSnapshot { get; set; }

//        public TeslaCarWorker(DatabaseContext databaseContext, ITeslaClient teslaClient, String accessToken, String carId, Int64 vehicleId, JsonOptions jsonOptions)
//        {
//            TeslaClient = teslaClient;
//            DatabaseContext = databaseContext;
//            AccessToken = accessToken;
//            CarId = carId;
//            VehicleId = vehicleId;
//            JsonOptions = jsonOptions;

//            CarEntity = DatabaseContext.Car.FirstOrDefault(x => x.CarId == CarId);
//            if (CarEntity == null)
//            {
//                throw new TeslaServiceException("Database Error");
//            }
//            LastStateEntity = DatabaseContext.State.Where(x => x.Car == CarEntity).OrderByDescending(x => x.Id).FirstOrDefault();
//            LastStandByEntity = DatabaseContext.StandBy.Where(x => x.Car == CarEntity).OrderByDescending(x => x.Id).FirstOrDefault();
//            LastDrivingEntity = DatabaseContext.Driving.Where(x => x.Car == CarEntity).OrderByDescending(x => x.Id).FirstOrDefault();
//            LastChargingEntity = DatabaseContext.Charging.Where(x => x.Car == CarEntity).OrderByDescending(x => x.Id).FirstOrDefault();
//        }

//        public void Start()
//        {
//            IsRunning = true;

//            if (LoopTask == null)
//            {
//                CancellationTokenSource = new CancellationTokenSource();
//                LoopTask = Task.Run(async () => await CarLoop(), CancellationTokenSource.Token);
//            }
//        }

//        public void Stop()
//        {
//            IsRunning = false;
//            CancellationTokenSource?.Cancel();
//            CancellationTokenSource = null;

//            while (LoopTask != null && !LoopTask.IsCompleted)
//            {
//            }

//            LoopTask?.Dispose();
//            LoopTask = null;
//        }

//        private async Task CarLoop()
//        {
//            Int32 offlineCounter = 0;

//            do
//            {
//                try
//                {
//                    using (TeslaStreamingRecorder streamingWorker = new(AccessToken, VehicleId, JsonOptions))
//                    {
//                        do
//                        {
//                            try
//                            {
//                                CarState currentState = default;

//                                // 处理流数据
//                                while (streamingWorker.StreamingDatas.TryDequeue(out TeslaStreamingData streamingData))
//                                {
//                                    offlineCounter = 0;

//                                    if (streamingData.ShiftState != null)
//                                    {
//                                        currentState = CarState.Driving;
//                                    }
//                                    else
//                                    {
//                                        currentState = CarState.Charging;
//                                    }

//                                    await UpdateStateAsync(currentState);

//                                    await UpdateSnapshotAsync(new Snapshot(streamingData));

//                                    if (currentState == CarState.Driving)
//                                    {
//                                        await UpdateDrivingSnapshotAsync(new DrivingSnapshot(streamingData));
//                                    }

//                                    if (currentState == CarState.Charging)
//                                    {
//                                        await UpdateChargingSnapshotAsync(new ChargingSnapshot(streamingData));
//                                    }
//                                }

//                                // 处理车辆数据
//                                TeslaCar car = null;
//                                try
//                                {
//                                    car = await TeslaClient.GetCarAsync(AccessToken, CarId, CancellationToken.None);
//                                    offlineCounter = 0;
//                                    currentState = car.State;
//                                    //streamingWorker.IsAsleep = currentState == CarState.Asleep;
//                                }
//                                catch
//                                {
//                                    currentState = CarState.Offline;
//                                    offlineCounter++;
//                                }

//                                if (!streamingWorker.StreamingDatas.IsEmpty)
//                                {
//                                    continue;
//                                }

//                                if (currentState == CarState.Online /*&& !streamingWorker.IsTryAsleep*/)
//                                {
//                                    TeslaCarData carData = null;
//                                    try
//                                    {
//                                        carData = await TeslaClient.GetCarDataAsync(AccessToken, CarId, CancellationToken.None);
//                                        //streamingWorker.IsCanTryAsleep = !carData.CarState.IsSentryMode && carData.ChargeState.ChargerPower <= 0;
//                                        offlineCounter = 0;
//                                    }
//                                    catch
//                                    {
//                                        currentState = CarState.Offline;
//                                        offlineCounter++;
//                                    }

//                                    if (currentState != CarState.Offline)
//                                    {
//                                        if (carData.DriveState.ShiftState == ShiftState.N || carData.DriveState.ShiftState == ShiftState.D || carData.DriveState.ShiftState == ShiftState.R)
//                                        {
//                                            currentState = CarState.Driving;
//                                        }
//                                        else if (carData.DriveState.ShiftState == ShiftState.P)
//                                        {
//                                            currentState = CarState.Online;
//                                        }
//                                        else if (carData.DriveState.ShiftState == null && carData.ChargeState.ChargerPower > 0)
//                                        {
//                                            currentState = CarState.Charging;
//                                        }

//                                        await UpdateStateAsync(currentState);

//                                        if (currentState != CarState.Asleep)
//                                        {
//                                            if (carData.ClimateState?.InsideTemperature == null || carData.ClimateState?.InsideTemperature.Celsius <= 5)
//                                            {
//                                                Logger.Debug(JsonSerializer.Serialize(carData, JsonOptions.JsonSerializerOptions));
//                                            }

//                                            await UpdateSnapshotAsync(new Snapshot(carData));

//                                            if (currentState == CarState.Online)
//                                            {
//                                                await UpdateStandBySnapshotAsync(new StandBySnapshot(carData));
//                                            }

//                                            if (currentState == CarState.Driving)
//                                            {
//                                                await UpdateDrivingSnapshotAsync(new DrivingSnapshot(carData));
//                                            }

//                                            if (currentState == CarState.Charging)
//                                            {
//                                                await UpdateChargingSnapshotAsync(new ChargingSnapshot(carData));
//                                            }
//                                        }
//                                    }
//                                    else if (offlineCounter >= 10)
//                                    {
//                                        await UpdateStateAsync(currentState);
//                                    }
//                                }
//                                else
//                                {
//                                    if (currentState != CarState.Offline || (currentState == CarState.Offline && offlineCounter >= 10))
//                                    {
//                                        await UpdateStateAsync(currentState);
//                                    }
//                                }

//                                if (streamingWorker.StreamingDatas.IsEmpty)
//                                {
//                                    await Task.Delay(500);
//                                }
//                            }
//                            catch
//                            {
//                            }
//                        } while (IsRunning);
//                    }
//                }
//                catch
//                {
//                }
//            } while (IsRunning);
//        }

//        private Decimal? Debounce(Decimal? oldValue, Decimal? newValue, Decimal threshold, Int32 decimals)
//        {
//            if (oldValue == null && newValue != null)
//            {
//                return Math.Round(newValue.Value, decimals, MidpointRounding.AwayFromZero);
//            }
//            else if (oldValue != null && newValue == null)
//            {
//                return oldValue;
//            }
//            else if (oldValue == null && newValue == null)
//            {
//                return null;
//            }

//            oldValue = Math.Round(oldValue.Value, decimals, MidpointRounding.AwayFromZero);
//            newValue = Math.Round(newValue.Value, decimals, MidpointRounding.AwayFromZero);

//            if (Math.Abs(newValue.Value - oldValue.Value) < Math.Abs(threshold))
//            {
//                return oldValue;
//            }

//            return newValue;
//        }

//        private DrivingSnapshot Debounce(DrivingSnapshot oldValue, DrivingSnapshot newValue)
//        {
//            if (oldValue == null || newValue == null)
//            {
//                if (newValue == null)
//                {
//                    return null;
//                }

//                return newValue with
//                {
//                    ShiftState = newValue.ShiftState ?? ShiftState.P,
//                    Odometer = Debounce(null, newValue.Odometer, 1, 6),
//                    Speed = Debounce(null, newValue.Speed, 1, 2) ?? 0,
//                    IdealBatteryRange = Debounce(null, newValue.IdealBatteryRange, 1, 2),
//                    RatedBatteryRange = Debounce(null, newValue.RatedBatteryRange, 1, 2),
//                };
//            }

//            return new DrivingSnapshot()
//            {
//                BatteryLevel = newValue.BatteryLevel ?? oldValue.BatteryLevel,
//                Elevation = newValue.Elevation ?? oldValue.Elevation,
//                Heading = newValue.Heading ?? oldValue.Heading,
//                ShiftState = newValue.ShiftState ?? oldValue.ShiftState ?? ShiftState.P,
//                Latitude = newValue.Latitude ?? oldValue.Latitude,
//                Longitude = newValue.Longitude ?? oldValue.Longitude,
//                Odometer = Debounce(oldValue.Odometer, newValue.Odometer, 1, 6),
//                Speed = Debounce(oldValue.Speed, newValue.Speed, 1, 2) ?? 0,
//                Power = newValue.Power ?? oldValue.Power,
//                IdealBatteryRange = Debounce(oldValue.IdealBatteryRange, newValue.IdealBatteryRange, 1, 2),
//                RatedBatteryRange = Debounce(oldValue.RatedBatteryRange, newValue.RatedBatteryRange, 1, 2),
//                IsClimateOn = newValue.IsClimateOn ?? oldValue.IsClimateOn,
//                FanStatus = newValue.FanStatus ?? oldValue.FanStatus,
//                InsideTemperature = newValue.InsideTemperature ?? oldValue.InsideTemperature,
//                OutsideTemperature = newValue.OutsideTemperature ?? oldValue.OutsideTemperature,
//                IsBatteryHeater = newValue.IsBatteryHeater ?? oldValue.IsBatteryHeater,
//                IsBatteryHeaterOn = newValue.IsBatteryHeaterOn ?? oldValue.IsBatteryHeaterOn,
//                DriverTemperatureSetting = newValue.DriverTemperatureSetting ?? oldValue.DriverTemperatureSetting,
//                DriverSeatHeater = newValue.DriverSeatHeater ?? oldValue.DriverSeatHeater,
//                PassengerTemperatureSetting = newValue.PassengerTemperatureSetting ?? oldValue.PassengerTemperatureSetting,
//                PassengerSeatHeater = newValue.PassengerSeatHeater ?? oldValue.PassengerSeatHeater,
//                IsSideMirrorHeater = newValue.IsSideMirrorHeater ?? oldValue.IsSideMirrorHeater,
//                IsWiperBladeHeater = newValue.IsWiperBladeHeater ?? oldValue.IsWiperBladeHeater,
//                IsFrontDefrosterOn = newValue.IsFrontDefrosterOn ?? oldValue.IsFrontDefrosterOn,
//                IsRearDefrosterOn = newValue.IsRearDefrosterOn ?? oldValue.IsRearDefrosterOn,
//            };
//        }

//        private StandBySnapshot Debounce(StandBySnapshot oldValue, StandBySnapshot newValue)
//        {
//            if (oldValue == null || newValue == null)
//            {
//                if (newValue == null)
//                {
//                    return null;
//                }

//                return newValue with
//                {
//                    Odometer = Debounce(null, newValue.Odometer, 1, 6),
//                    IdealBatteryRange = Debounce(null, newValue.IdealBatteryRange, 1, 2),
//                    RatedBatteryRange = Debounce(null, newValue.RatedBatteryRange, 1, 2),
//                };
//            }

//            return new StandBySnapshot()
//            {
//                BatteryLevel = newValue.BatteryLevel ?? oldValue.BatteryLevel,
//                Elevation = newValue.Elevation ?? oldValue.Elevation,
//                Heading = newValue.Heading ?? oldValue.Heading,
//                Latitude = newValue.Latitude ?? oldValue.Latitude,
//                Longitude = newValue.Longitude ?? oldValue.Longitude,
//                Odometer = Debounce(oldValue.Odometer, newValue.Odometer, 1, 6),
//                Power = newValue.Power ?? oldValue.Power,
//                IdealBatteryRange = Debounce(oldValue.IdealBatteryRange, newValue.IdealBatteryRange, 1, 2),
//                RatedBatteryRange = Debounce(oldValue.RatedBatteryRange, newValue.RatedBatteryRange, 1, 2),
//                IsClimateOn = newValue.IsClimateOn ?? oldValue.IsClimateOn,
//                FanStatus = newValue.FanStatus ?? oldValue.FanStatus,
//                InsideTemperature = newValue.InsideTemperature ?? oldValue.InsideTemperature,
//                OutsideTemperature = newValue.OutsideTemperature ?? oldValue.OutsideTemperature,
//                IsBatteryHeater = newValue.IsBatteryHeater ?? oldValue.IsBatteryHeater,
//                IsBatteryHeaterOn = newValue.IsBatteryHeaterOn ?? oldValue.IsBatteryHeaterOn,
//                DriverTemperatureSetting = newValue.DriverTemperatureSetting ?? oldValue.DriverTemperatureSetting,
//                DriverSeatHeater = newValue.DriverSeatHeater ?? oldValue.DriverSeatHeater,
//                PassengerTemperatureSetting = newValue.PassengerTemperatureSetting ?? oldValue.PassengerTemperatureSetting,
//                PassengerSeatHeater = newValue.PassengerSeatHeater ?? oldValue.PassengerSeatHeater,
//                IsSideMirrorHeater = newValue.IsSideMirrorHeater ?? oldValue.IsSideMirrorHeater,
//                IsWiperBladeHeater = newValue.IsWiperBladeHeater ?? oldValue.IsWiperBladeHeater,
//                IsFrontDefrosterOn = newValue.IsFrontDefrosterOn ?? oldValue.IsFrontDefrosterOn,
//                IsRearDefrosterOn = newValue.IsRearDefrosterOn ?? oldValue.IsRearDefrosterOn,
//            };
//        }

//        private ChargingSnapshot Debounce(ChargingSnapshot oldValue, ChargingSnapshot newValue)
//        {
//            if (oldValue == null || newValue == null)
//            {
//                if (newValue == null)
//                {
//                    return null;
//                }

//                return newValue with
//                {
//                    IdealBatteryRange = Debounce(null, newValue.IdealBatteryRange, 1, 2),
//                    RatedBatteryRange = Debounce(null, newValue.RatedBatteryRange, 1, 2),
//                };
//            }

//            return new ChargingSnapshot()
//            {
//                BatteryLevel = newValue.BatteryLevel ?? oldValue.BatteryLevel,
//                Latitude = newValue.Latitude ?? oldValue.Latitude,
//                Longitude = newValue.Longitude ?? oldValue.Longitude,
//                Power = newValue.Power ?? oldValue.Power,
//                IdealBatteryRange = Debounce(oldValue.IdealBatteryRange, newValue.IdealBatteryRange, 1, 2),
//                RatedBatteryRange = Debounce(oldValue.RatedBatteryRange, newValue.RatedBatteryRange, 1, 2),
//                InsideTemperature = newValue.InsideTemperature ?? oldValue.InsideTemperature,
//                OutsideTemperature = newValue.OutsideTemperature ?? oldValue.OutsideTemperature,
//                IsBatteryHeater = newValue.IsBatteryHeater ?? oldValue.IsBatteryHeater,
//                IsBatteryHeaterOn = newValue.IsBatteryHeaterOn ?? oldValue.IsBatteryHeaterOn,
//                ChargeEnergyAdded = newValue.ChargeEnergyAdded ?? oldValue.ChargeEnergyAdded,
//                ChargerPhases = newValue.ChargerPhases ?? oldValue.ChargerPhases,
//                ChargerPilotCurrent = newValue.ChargerPilotCurrent ?? oldValue.ChargerPilotCurrent,
//                ChargerActualCurrent = newValue.ChargerActualCurrent ?? oldValue.ChargerActualCurrent,
//                ChargerPower = newValue.ChargerPower ?? oldValue.ChargerPower,
//                ChargerVoltage = newValue.ChargerVoltage ?? oldValue.ChargerVoltage,
//                IsFastChargerPresent = newValue.IsFastChargerPresent ?? oldValue.IsFastChargerPresent,
//                ChargeCable = newValue.ChargeCable ?? oldValue.ChargeCable,
//                FastChargerBrand = newValue.FastChargerBrand ?? oldValue.FastChargerBrand,
//                FastChargerType = newValue.FastChargerType ?? oldValue.FastChargerType,
//                ChargeRate = newValue.ChargeRate ?? oldValue.ChargeRate,
//            };
//        }

//        private Snapshot Debounce(Snapshot oldValue, Snapshot newValue)
//        {
//            if (oldValue == null || newValue == null)
//            {
//                if (newValue == null)
//                {
//                    return null;
//                }

//                return newValue with
//                {
//                    ShiftState = newValue.ShiftState ?? ShiftState.P,
//                    Odometer = Debounce(null, newValue.Odometer, 1, 6),
//                    Speed = Debounce(null, newValue.Speed, 1, 2) ?? 0,
//                    IdealBatteryRange = Debounce(null, newValue.IdealBatteryRange, 1, 2),
//                    RatedBatteryRange = Debounce(null, newValue.RatedBatteryRange, 1, 2),
//                };
//            }

//            return new Snapshot()
//            {
//                BatteryLevel = newValue.BatteryLevel ?? oldValue.BatteryLevel,
//                Elevation = newValue.Elevation ?? oldValue.Elevation,
//                Heading = newValue.Heading ?? oldValue.Heading,
//                ShiftState = newValue.ShiftState ?? oldValue.ShiftState ?? ShiftState.P,
//                Latitude = newValue.Latitude ?? oldValue.Latitude,
//                Longitude = newValue.Longitude ?? oldValue.Longitude,
//                Odometer = Debounce(oldValue.Odometer, newValue.Odometer, 1, 6),
//                Speed = Debounce(oldValue.Speed, newValue.Speed, 1, 2) ?? 0,
//                Power = newValue.Power ?? oldValue.Power,
//                IdealBatteryRange = Debounce(oldValue.IdealBatteryRange, newValue.IdealBatteryRange, 1, 2),
//                RatedBatteryRange = Debounce(oldValue.RatedBatteryRange, newValue.RatedBatteryRange, 1, 2),
//                IsClimateOn = newValue.IsClimateOn ?? oldValue.IsClimateOn,
//                FanStatus = newValue.FanStatus ?? oldValue.FanStatus,
//                InsideTemperature = newValue.InsideTemperature ?? oldValue.InsideTemperature,
//                OutsideTemperature = newValue.OutsideTemperature ?? oldValue.OutsideTemperature,
//                IsBatteryHeater = newValue.IsBatteryHeater ?? oldValue.IsBatteryHeater,
//                IsBatteryHeaterOn = newValue.IsBatteryHeaterOn ?? oldValue.IsBatteryHeaterOn,
//                DriverTemperatureSetting = newValue.DriverTemperatureSetting ?? oldValue.DriverTemperatureSetting,
//                DriverSeatHeater = newValue.DriverSeatHeater ?? oldValue.DriverSeatHeater,
//                PassengerTemperatureSetting = newValue.PassengerTemperatureSetting ?? oldValue.PassengerTemperatureSetting,
//                PassengerSeatHeater = newValue.PassengerSeatHeater ?? oldValue.PassengerSeatHeater,
//                IsSideMirrorHeater = newValue.IsSideMirrorHeater ?? oldValue.IsSideMirrorHeater,
//                IsWiperBladeHeater = newValue.IsWiperBladeHeater ?? oldValue.IsWiperBladeHeater,
//                IsFrontDefrosterOn = newValue.IsFrontDefrosterOn ?? oldValue.IsFrontDefrosterOn,
//                IsRearDefrosterOn = newValue.IsRearDefrosterOn ?? oldValue.IsRearDefrosterOn,
//            };
//        }

//        private async Task UpdateStateAsync(CarState carState)
//        {
//            Instant now = Instant.FromDateTimeUtc(DateTime.UtcNow);

//            if (LastStateEntity == null)
//            {
//                LastStateEntity = new StateEntity()
//                {
//                    State = carState,
//                    StartTimestamp = now,
//                    Car = CarEntity,
//                };

//                DatabaseContext.State.Add(LastStateEntity);

//                Logger.Info($"State: null -> {carState}");

//                if (carState == CarState.Online || carState == CarState.Asleep)
//                {
//                    await StopDrivingAsync(now);
//                    await StopChargingAsync(now);
//                    await StartStandByAsync(now);
//                }
//                else if (carState == CarState.Driving)
//                {
//                    await StopChargingAsync(now);
//                    await StopStandByAsync(now);
//                    await StartDrivingAsync(now);
//                }
//                else if (carState == CarState.Charging)
//                {
//                    await StopDrivingAsync(now);
//                    await StopStandByAsync(now);
//                    await StartChargingAsync(now);
//                }
//                else
//                {
//                    await StopDrivingAsync(now);
//                    await StopChargingAsync(now);
//                    await StopStandByAsync(now);
//                }
//            }
//            else
//            {
//                if (carState != LastStateEntity.State)
//                {
//                    LastStateEntity.EndTimestamp = now;

//                    StateEntity newStateEntity = new()
//                    {
//                        State = carState,
//                        StartTimestamp = now,
//                        Car = CarEntity,
//                    };

//                    DatabaseContext.State.Add(newStateEntity);

//                    Logger.Info($"State: {LastStateEntity.State} -> {carState}");

//                    StateEntity lastEffectiveStateEntity = LastStateEntity;

//                    if (carState == CarState.Offline)
//                    {
//                        lastEffectiveStateEntity = await DatabaseContext.State.Where(x => x.Car == CarEntity && x.State != CarState.Offline).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
//                    }

//                    LastStateEntity = newStateEntity;

//                    if (lastEffectiveStateEntity.State == CarState.Online || lastEffectiveStateEntity.State == CarState.Asleep)
//                    {
//                        if (carState == CarState.Driving || carState == CarState.Charging)
//                        {
//                            await StopStandByAsync(now);
//                        }

//                        if (carState == CarState.Driving)
//                        {
//                            await StartDrivingAsync(now);
//                        }

//                        if (carState == CarState.Charging)
//                        {
//                            await StartChargingAsync(now);
//                        }
//                    }
//                    else if (lastEffectiveStateEntity.State == CarState.Driving)
//                    {
//                        await StopDrivingAsync(now);

//                        if (carState == CarState.Online || carState == CarState.Asleep)
//                        {
//                            await StartStandByAsync(now);
//                        }

//                        if (carState == CarState.Charging)
//                        {
//                            await StartChargingAsync(now);
//                        }
//                    }
//                    else if (lastEffectiveStateEntity.State == CarState.Charging)
//                    {
//                        await StopChargingAsync(now);

//                        if (carState == CarState.Online || carState == CarState.Asleep)
//                        {
//                            await StartStandByAsync(now);
//                        }

//                        if (carState == CarState.Driving)
//                        {
//                            await StartDrivingAsync(now);
//                        }
//                    }
//                }
//            }

//            await DatabaseContext.SaveChangesAsync();
//        }

//        private async Task UpdateSnapshotAsync(Snapshot snapshot)
//        {
//            if (LastSnapshot == null)
//            {
//                LastSnapshot = await DatabaseContext.Snapshot.Where(x => x.Car == CarEntity).OrderByDescending(x => x.Id).Select(x => new Snapshot()
//                {
//                    BatteryLevel = x.BatteryLevel,
//                    Elevation = x.Elevation,
//                    Heading = x.Heading,
//                    ShiftState = x.ShiftState,
//                    Latitude = x.Latitude,
//                    Longitude = x.Longitude,
//                    Odometer = x.Odometer,
//                    Speed = x.Speed,
//                    Power = x.Power,
//                    IdealBatteryRange = x.IdealBatteryRange,
//                    RatedBatteryRange = x.RatedBatteryRange,
//                    IsClimateOn = x.IsClimateOn,
//                    FanStatus = x.FanStatus,
//                    InsideTemperature = x.InsideTemperature,
//                    OutsideTemperature = x.OutsideTemperature,
//                    IsBatteryHeater = x.IsBatteryHeater,
//                    IsBatteryHeaterOn = x.IsBatteryHeaterOn,
//                    DriverTemperatureSetting = x.DriverTemperatureSetting,
//                    DriverSeatHeater = x.DriverSeatHeater,
//                    PassengerTemperatureSetting = x.PassengerTemperatureSetting,
//                    PassengerSeatHeater = x.PassengerSeatHeater,
//                    IsSideMirrorHeater = x.IsSideMirrorHeater,
//                    IsWiperBladeHeater = x.IsWiperBladeHeater,
//                    IsFrontDefrosterOn = x.IsFrontDefrosterOn,
//                    IsRearDefrosterOn = x.IsRearDefrosterOn,
//                }).FirstOrDefaultAsync();
//            }

//            Snapshot currentSnapshot = Debounce(LastSnapshot, snapshot);

//            if (LastSnapshot != currentSnapshot)
//            {
//                DatabaseContext.Snapshot.Add(new SnapshotEntity()
//                {
//                    BatteryLevel = currentSnapshot.BatteryLevel,
//                    Elevation = currentSnapshot.Elevation,
//                    Heading = currentSnapshot.Heading,
//                    ShiftState = currentSnapshot.ShiftState ?? ShiftState.P,
//                    Latitude = currentSnapshot.Latitude,
//                    Longitude = currentSnapshot.Longitude,
//                    Odometer = currentSnapshot.Odometer,
//                    Speed = currentSnapshot.Speed,
//                    Power = currentSnapshot.Power,
//                    IdealBatteryRange = currentSnapshot.IdealBatteryRange,
//                    RatedBatteryRange = currentSnapshot.RatedBatteryRange,
//                    IsClimateOn = currentSnapshot.IsClimateOn,
//                    FanStatus = currentSnapshot.FanStatus,
//                    InsideTemperature = currentSnapshot.InsideTemperature,
//                    OutsideTemperature = currentSnapshot.OutsideTemperature,
//                    IsBatteryHeater = currentSnapshot.IsBatteryHeater,
//                    IsBatteryHeaterOn = currentSnapshot.IsBatteryHeaterOn,
//                    DriverTemperatureSetting = currentSnapshot.DriverTemperatureSetting,
//                    DriverSeatHeater = currentSnapshot.DriverSeatHeater,
//                    PassengerTemperatureSetting = currentSnapshot.PassengerTemperatureSetting,
//                    PassengerSeatHeater = currentSnapshot.PassengerSeatHeater,
//                    IsSideMirrorHeater = currentSnapshot.IsSideMirrorHeater,
//                    IsWiperBladeHeater = currentSnapshot.IsWiperBladeHeater,
//                    IsFrontDefrosterOn = currentSnapshot.IsFrontDefrosterOn,
//                    IsRearDefrosterOn = currentSnapshot.IsRearDefrosterOn,

//                    Car = CarEntity,
//                    State = LastStateEntity,
//                });

//                LastSnapshot = currentSnapshot;
//            }

//            await DatabaseContext.SaveChangesAsync();
//        }

//        private async Task UpdateStandBySnapshotAsync(StandBySnapshot standBySnapshot)
//        {
//            if (LastStandByEntity == null)
//            {
//                await StartStandByAsync(Instant.FromDateTimeUtc(DateTime.UtcNow));
//            }

//            if (LastStandBySnapshot == null)
//            {
//                LastStandBySnapshot = await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == LastStandByEntity).OrderByDescending(x => x.Id).Select(x => new StandBySnapshot()
//                {
//                    BatteryLevel = x.BatteryLevel,
//                    //Elevation = x.Elevation,
//                    //Heading = x.Heading,
//                    //Latitude = x.Latitude,
//                    //Longitude = x.Longitude,
//                    //Odometer = x.Odometer,
//                    Power = x.Power,
//                    IdealBatteryRange = x.IdealBatteryRange,
//                    RatedBatteryRange = x.RatedBatteryRange,
//                    IsClimateOn = x.IsClimateOn,
//                    FanStatus = x.FanStatus,
//                    InsideTemperature = x.InsideTemperature,
//                    OutsideTemperature = x.OutsideTemperature,
//                    IsBatteryHeater = x.IsBatteryHeater,
//                    IsBatteryHeaterOn = x.IsBatteryHeaterOn,
//                    DriverTemperatureSetting = x.DriverTemperatureSetting,
//                    DriverSeatHeater = x.DriverSeatHeater,
//                    PassengerTemperatureSetting = x.PassengerTemperatureSetting,
//                    PassengerSeatHeater = x.PassengerSeatHeater,
//                    IsSideMirrorHeater = x.IsSideMirrorHeater,
//                    IsWiperBladeHeater = x.IsWiperBladeHeater,
//                    IsFrontDefrosterOn = x.IsFrontDefrosterOn,
//                    IsRearDefrosterOn = x.IsRearDefrosterOn,
//                }).FirstOrDefaultAsync();
//            }

//            StandBySnapshot currentStandBySnapshot = Debounce(LastStandBySnapshot, standBySnapshot);

//            if (LastStandBySnapshot != currentStandBySnapshot)
//            {
//                DatabaseContext.StandBySnapshot.Add(new StandBySnapshotEntity()
//                {
//                    BatteryLevel = currentStandBySnapshot.BatteryLevel,
//                    //Elevation = currentStandBySnapshot.Elevation,
//                    //Heading = currentStandBySnapshot.Heading,
//                    //Latitude = currentStandBySnapshot.Latitude,
//                    //Longitude = currentStandBySnapshot.Longitude,
//                    //Odometer = currentStandBySnapshot.Odometer,
//                    Power = currentStandBySnapshot.Power,
//                    IdealBatteryRange = currentStandBySnapshot.IdealBatteryRange,
//                    RatedBatteryRange = currentStandBySnapshot.RatedBatteryRange,
//                    IsClimateOn = currentStandBySnapshot.IsClimateOn,
//                    FanStatus = currentStandBySnapshot.FanStatus,
//                    InsideTemperature = currentStandBySnapshot.InsideTemperature,
//                    OutsideTemperature = currentStandBySnapshot.OutsideTemperature,
//                    IsBatteryHeater = currentStandBySnapshot.IsBatteryHeater,
//                    IsBatteryHeaterOn = currentStandBySnapshot.IsBatteryHeaterOn,
//                    DriverTemperatureSetting = currentStandBySnapshot.DriverTemperatureSetting,
//                    DriverSeatHeater = currentStandBySnapshot.DriverSeatHeater,
//                    PassengerTemperatureSetting = currentStandBySnapshot.PassengerTemperatureSetting,
//                    PassengerSeatHeater = currentStandBySnapshot.PassengerSeatHeater,
//                    IsSideMirrorHeater = currentStandBySnapshot.IsSideMirrorHeater,
//                    IsWiperBladeHeater = currentStandBySnapshot.IsWiperBladeHeater,
//                    IsFrontDefrosterOn = currentStandBySnapshot.IsFrontDefrosterOn,
//                    IsRearDefrosterOn = currentStandBySnapshot.IsRearDefrosterOn,

//                    StandBy = LastStandByEntity,
//                });

//                LastStandBySnapshot = currentStandBySnapshot;
//            }

//            await DatabaseContext.SaveChangesAsync();
//        }

//        private async Task UpdateDrivingSnapshotAsync(DrivingSnapshot drivingSnapshot)
//        {
//            if (LastDrivingEntity == null)
//            {
//                await StartDrivingAsync(Instant.FromDateTimeUtc(DateTime.UtcNow));
//            }

//            if (LastDrivingSnapshot == null)
//            {
//                LastDrivingSnapshot = await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == LastDrivingEntity).OrderByDescending(x => x.Id).Select(x => new DrivingSnapshot()
//                {
//                    BatteryLevel = x.BatteryLevel,
//                    Elevation = x.Elevation,
//                    Heading = x.Heading,
//                    ShiftState = x.ShiftState,
//                    Latitude = x.Latitude,
//                    Longitude = x.Longitude,
//                    Odometer = x.Odometer,
//                    Speed = x.Speed,
//                    Power = x.Power,
//                    IdealBatteryRange = x.IdealBatteryRange,
//                    RatedBatteryRange = x.RatedBatteryRange,
//                    IsClimateOn = x.IsClimateOn,
//                    FanStatus = x.FanStatus,
//                    InsideTemperature = x.InsideTemperature,
//                    OutsideTemperature = x.OutsideTemperature,
//                    IsBatteryHeater = x.IsBatteryHeater,
//                    IsBatteryHeaterOn = x.IsBatteryHeaterOn,
//                    DriverTemperatureSetting = x.DriverTemperatureSetting,
//                    DriverSeatHeater = x.DriverSeatHeater,
//                    PassengerTemperatureSetting = x.PassengerTemperatureSetting,
//                    PassengerSeatHeater = x.PassengerSeatHeater,
//                    IsSideMirrorHeater = x.IsSideMirrorHeater,
//                    IsWiperBladeHeater = x.IsWiperBladeHeater,
//                    IsFrontDefrosterOn = x.IsFrontDefrosterOn,
//                    IsRearDefrosterOn = x.IsRearDefrosterOn,
//                }).FirstOrDefaultAsync();
//            }

//            DrivingSnapshot currentDrivingSnapshot = Debounce(LastDrivingSnapshot, drivingSnapshot);

//            if (LastDrivingSnapshot != currentDrivingSnapshot)
//            {
//                DatabaseContext.DrivingSnapshot.Add(new DrivingSnapshotEntity()
//                {
//                    BatteryLevel = currentDrivingSnapshot.BatteryLevel,
//                    Elevation = currentDrivingSnapshot.Elevation,
//                    Heading = currentDrivingSnapshot.Heading,
//                    ShiftState = currentDrivingSnapshot.ShiftState,
//                    Location = currentDrivingSnapshot.Location,
//                    //Latitude = currentDrivingSnapshot.Latitude,
//                    //Longitude = currentDrivingSnapshot.Longitude,
//                    Odometer = currentDrivingSnapshot.Odometer,
//                    Speed = currentDrivingSnapshot.Speed,
//                    Power = currentDrivingSnapshot.Power,
//                    IdealBatteryRange = currentDrivingSnapshot.IdealBatteryRange,
//                    RatedBatteryRange = currentDrivingSnapshot.RatedBatteryRange,
//                    IsClimateOn = currentDrivingSnapshot.IsClimateOn,
//                    FanStatus = currentDrivingSnapshot.FanStatus,
//                    InsideTemperature = currentDrivingSnapshot.InsideTemperature,
//                    OutsideTemperature = currentDrivingSnapshot.OutsideTemperature,
//                    IsBatteryHeater = currentDrivingSnapshot.IsBatteryHeater,
//                    IsBatteryHeaterOn = currentDrivingSnapshot.IsBatteryHeaterOn,
//                    DriverTemperatureSetting = currentDrivingSnapshot.DriverTemperatureSetting,
//                    DriverSeatHeater = currentDrivingSnapshot.DriverSeatHeater,
//                    PassengerTemperatureSetting = currentDrivingSnapshot.PassengerTemperatureSetting,
//                    PassengerSeatHeater = currentDrivingSnapshot.PassengerSeatHeater,
//                    IsSideMirrorHeater = currentDrivingSnapshot.IsSideMirrorHeater,
//                    IsWiperBladeHeater = currentDrivingSnapshot.IsWiperBladeHeater,
//                    IsFrontDefrosterOn = currentDrivingSnapshot.IsFrontDefrosterOn,
//                    IsRearDefrosterOn = currentDrivingSnapshot.IsRearDefrosterOn,

//                    Driving = LastDrivingEntity,
//                });

//                LastDrivingSnapshot = currentDrivingSnapshot;
//            }

//            await DatabaseContext.SaveChangesAsync();
//        }

//        private async Task UpdateChargingSnapshotAsync(ChargingSnapshot chargingSnapshot)
//        {
//            if (LastChargingEntity == null)
//            {
//                await StartChargingAsync(Instant.FromDateTimeUtc(DateTime.UtcNow));
//            }

//            if (LastChargingSnapshot == null)
//            {
//                LastChargingSnapshot = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == LastChargingEntity).OrderByDescending(x => x.Id).Select(x => new ChargingSnapshot()
//                {
//                    BatteryLevel = x.BatteryLevel,
//                    //Power = x.Power,
//                    //Latitude = x.Latitude,
//                    //Longitude = x.Longitude,
//                    IdealBatteryRange = x.IdealBatteryRange,
//                    RatedBatteryRange = x.RatedBatteryRange,
//                    InsideTemperature = x.InsideTemperature,
//                    OutsideTemperature = x.OutsideTemperature,
//                    IsBatteryHeater = x.IsBatteryHeater,
//                    IsBatteryHeaterOn = x.IsBatteryHeaterOn,
//                    ChargeEnergyAdded = x.ChargeEnergyAdded,
//                    ChargerPhases = x.ChargerPhases,
//                    ChargerPilotCurrent = x.ChargerPilotCurrent,
//                    ChargerActualCurrent = x.ChargerActualCurrent,
//                    ChargerPower = x.ChargerPower,
//                    ChargerVoltage = x.ChargerVoltage,
//                    //IsFastChargerPresent = x.IsFastChargerPresent,
//                    //ChargeCable = x.ChargeCable,
//                    //FastChargerBrand = x.FastChargerBrand,
//                    //FastChargerType = x.FastChargerType,
//                }).FirstOrDefaultAsync();
//            }

//            ChargingSnapshot currentchargingSnapshot = Debounce(LastChargingSnapshot, chargingSnapshot);

//            if (LastChargingSnapshot != currentchargingSnapshot)
//            {
//                DatabaseContext.ChargingSnapshot.Add(new ChargingSnapshotEntity()
//                {
//                    BatteryLevel = currentchargingSnapshot.BatteryLevel,
//                    //Power = currentchargingSnapshot.Power,
//                    //Latitude = currentchargingSnapshot.Latitude,
//                    //Longitude = currentchargingSnapshot.Longitude,
//                    IdealBatteryRange = currentchargingSnapshot.IdealBatteryRange,
//                    RatedBatteryRange = currentchargingSnapshot.RatedBatteryRange,
//                    InsideTemperature = currentchargingSnapshot.InsideTemperature,
//                    OutsideTemperature = currentchargingSnapshot.OutsideTemperature,
//                    IsBatteryHeater = currentchargingSnapshot.IsBatteryHeater,
//                    IsBatteryHeaterOn = currentchargingSnapshot.IsBatteryHeaterOn,
//                    ChargeEnergyAdded = currentchargingSnapshot.ChargeEnergyAdded,
//                    ChargerPhases = currentchargingSnapshot.ChargerPhases,
//                    ChargerPilotCurrent = currentchargingSnapshot.ChargerPilotCurrent,
//                    ChargerActualCurrent = currentchargingSnapshot.ChargerActualCurrent,
//                    ChargerPower = currentchargingSnapshot.ChargerPower,
//                    ChargerVoltage = currentchargingSnapshot.ChargerVoltage,
//                    //IsFastChargerPresent = currentchargingSnapshot.IsFastChargerPresent,
//                    //ChargeCable = currentchargingSnapshot.ChargeCable,
//                    //FastChargerBrand = currentchargingSnapshot.FastChargerBrand,
//                    //FastChargerType = currentchargingSnapshot.FastChargerType,

//                    Charging = LastChargingEntity,
//                });

//                LastChargingSnapshot = currentchargingSnapshot;
//            }

//            await DatabaseContext.SaveChangesAsync();
//        }

//        private async Task StartStandByAsync(Instant timestamp)
//        {
//            if (LastStandByEntity != null && LastStandByEntity.EndTimestamp == null)
//            {
//                await StopStandByAsync(timestamp);
//            }

//            LastStandByEntity = new StandByEntity()
//            {
//                StartTimestamp = timestamp,

//                Car = CarEntity,
//            };

//            DatabaseContext.StandBy.Add(LastStandByEntity);

//            await DatabaseContext.SaveChangesAsync();

//            Logger.Info($"Start Stand By");
//        }

//        private async Task StopStandByAsync(Instant timestamp)
//        {
//            if (LastStandByEntity != null && LastStandByEntity.EndTimestamp == null)
//            {
//                LastStandByEntity.EndTimestamp = timestamp;

//                var detail = await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == LastStandByEntity).Select(x => new
//                {
//                    OutsideTemperature = x.OutsideTemperature,
//                    InsideTemperature = x.InsideTemperature,
//                    //Elevation = x.Elevation,
//                    Power = x.Power
//                }).ToListAsync();
//                var summary = new
//                {
//                    OutsideTemperatureAverage = detail.Average(x => x.OutsideTemperature),
//                    OutsideTemperatureMin = detail.Min(x => x.OutsideTemperature),
//                    OutsideTemperatureMax = detail.Max(x => x.OutsideTemperature),
//                    InsideTemperatureAverage = detail.Average(x => x.InsideTemperature),
//                    InsideTemperatureMin = detail.Min(x => x.InsideTemperature),
//                    InsideTemperatureMax = detail.Max(x => x.InsideTemperature),
//                    //ElevationAverage = detail.Average(x => x.Elevation),
//                    //ElevationMin = detail.Min(x => x.Elevation),
//                    //ElevationMax = detail.Max(x => x.Elevation),
//                    PowerMin = detail.Min(x => x.Power),
//                    PowerMax = detail.Max(x => x.Power),
//                };

//                StandBySnapshotEntity first = await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == LastStandByEntity).OrderBy(x => x.Id).FirstOrDefaultAsync();
//                StandBySnapshotEntity last = await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == LastStandByEntity).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

//                //LastStandByEntity.Latitude = first?.Latitude;
//                //LastStandByEntity.Longitude = first?.Longitude;
//            }

//            await DatabaseContext.SaveChangesAsync();

//            LastStandBySnapshot = null;

//            Logger.Info($"Stop Stand By");
//        }

//        private async Task StartDrivingAsync(Instant timestamp)
//        {
//            if (LastDrivingEntity != null && LastDrivingEntity.EndTimestamp == null)
//            {
//                await StopDrivingAsync(timestamp);
//            }

//            LastDrivingEntity = new DrivingEntity()
//            {
//                StartTimestamp = timestamp,

//                Car = CarEntity,
//            };

//            DatabaseContext.Driving.Add(LastDrivingEntity);

//            await DatabaseContext.SaveChangesAsync();

//            Logger.Info($"Start Driving");
//        }

//        private async Task StopDrivingAsync(Instant timestamp)
//        {
//            if (LastDrivingEntity != null && LastDrivingEntity.EndTimestamp == null)
//            {
//                LastDrivingEntity.EndTimestamp = timestamp;

//                var detail = await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == LastDrivingEntity).Select(x => new
//                {
//                    OutsideTemperature = x.OutsideTemperature,
//                    InsideTemperature = x.InsideTemperature,
//                    Speed = x.Speed,
//                    Elevation = x.Elevation,
//                    Power = x.Power
//                }).ToListAsync();
//                var summary = new
//                {
//                    OutsideTemperatureAverage = detail.Average(x => x.OutsideTemperature),
//                    OutsideTemperatureMin = detail.Min(x => x.OutsideTemperature),
//                    OutsideTemperatureMax = detail.Max(x => x.OutsideTemperature),
//                    InsideTemperatureAverage = detail.Average(x => x.InsideTemperature),
//                    InsideTemperatureMin = detail.Min(x => x.InsideTemperature),
//                    InsideTemperatureMax = detail.Max(x => x.InsideTemperature),
//                    SpeedMin = detail.Min(x => x.Speed),
//                    SpeedMax = detail.Max(x => x.Speed),
//                    ElevationAverage = detail.Average(x => x.Elevation),
//                    ElevationMin = detail.Min(x => x.Elevation),
//                    ElevationMax = detail.Max(x => x.Elevation),
//                    PowerMin = detail.Min(x => x.Power),
//                    PowerMax = detail.Max(x => x.Power),
//                };

//                DrivingSnapshotEntity first = await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == LastDrivingEntity).OrderBy(x => x.Id).FirstOrDefaultAsync();
//                DrivingSnapshotEntity last = await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == LastDrivingEntity).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

//                LastDrivingEntity.StartLocation = first?.Location;
//                LastDrivingEntity.EndLocation = last?.Location;
//                //LastDrivingEntity.StartLatitude = first?.Latitude;
//                //LastDrivingEntity.EndLatitude = last?.Latitude;
//                //LastDrivingEntity.StartLongitude = first?.Longitude;
//                //LastDrivingEntity.EndLongitude = last?.Longitude;
//                LastDrivingEntity.StartBatteryLevel = first?.BatteryLevel;
//                LastDrivingEntity.EndBatteryLevel = last?.BatteryLevel;
//                LastDrivingEntity.StartIdealBatteryRange = first?.IdealBatteryRange;
//                LastDrivingEntity.EndIdealBatteryRange = last?.IdealBatteryRange;
//                LastDrivingEntity.StartRatedBatteryRange = first?.RatedBatteryRange;
//                LastDrivingEntity.EndRatedBatteryRange = last?.RatedBatteryRange;
//                LastDrivingEntity.StartOdometer = first?.Odometer;
//                LastDrivingEntity.EndOdometer = last?.Odometer;
//                LastDrivingEntity.Distance = LastDrivingEntity?.EndOdometer - LastDrivingEntity?.StartOdometer;
//                LastDrivingEntity.Duration = (Decimal)(LastDrivingEntity.EndTimestamp - LastDrivingEntity.StartTimestamp).Value.TotalHours;
//                //LastDrivingEntity.OutsideTemperatureAverage = summary.OutsideTemperatureAverage;
//                //LastDrivingEntity.OutsideTemperatureMin = summary.OutsideTemperatureMin;
//                //LastDrivingEntity.OutsideTemperatureMax = summary.OutsideTemperatureMax;
//                //LastDrivingEntity.InsideTemperatureAverage = summary.InsideTemperatureAverage;
//                //LastDrivingEntity.InsideTemperatureMin = summary.InsideTemperatureMin;
//                //LastDrivingEntity.InsideTemperatureMax = summary.InsideTemperatureMax;
//                //LastDrivingEntity.SpeedMin = summary.SpeedMin;
//                //LastDrivingEntity.SpeedMax = summary.SpeedMax;
//                LastDrivingEntity.SpeedAverage = LastDrivingEntity.Duration > 0 ? LastDrivingEntity.Distance / LastDrivingEntity.Duration : 0;
//                //LastDrivingEntity.ElevationAverage = summary.ElevationAverage;
//                //LastDrivingEntity.ElevationMin = summary.ElevationMin;
//                //LastDrivingEntity.ElevationMax = summary.ElevationMax;
//                //LastDrivingEntity.PowerMin = summary.PowerMin;
//                //LastDrivingEntity.PowerMax = summary.PowerMax;
//            }

//            await DatabaseContext.SaveChangesAsync();

//            LastDrivingSnapshot = null;

//            Logger.Info($"Stop Driving");
//        }

//        private async Task StartChargingAsync(Instant timestamp)
//        {
//            if (LastChargingEntity != null && LastChargingEntity.EndTimestamp == null)
//            {
//                await StopChargingAsync(timestamp);
//            }

//            LastChargingEntity = new ChargingEntity()
//            {
//                StartTimestamp = timestamp,

//                Car = CarEntity,
//            };

//            DatabaseContext.Charging.Add(LastChargingEntity);

//            await DatabaseContext.SaveChangesAsync();

//            Logger.Info($"Start Charging");
//        }

//        private async Task StopChargingAsync(Instant timestamp)
//        {
//            if (LastChargingEntity != null && LastChargingEntity.EndTimestamp == null)
//            {
//                LastChargingEntity.EndTimestamp = timestamp;

//                var detail = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == LastChargingEntity).Select(x => new
//                {
//                    OutsideTemperature = x.OutsideTemperature,
//                    InsideTemperature = x.InsideTemperature,
//                    //Power = x.Power
//                }).ToListAsync();
//                var summary = new
//                {
//                    OutsideTemperatureAverage = detail.Average(x => x.OutsideTemperature),
//                    OutsideTemperatureMin = detail.Min(x => x.OutsideTemperature),
//                    OutsideTemperatureMax = detail.Max(x => x.OutsideTemperature),
//                    InsideTemperatureAverage = detail.Average(x => x.InsideTemperature),
//                    InsideTemperatureMin = detail.Min(x => x.InsideTemperature),
//                    InsideTemperatureMax = detail.Max(x => x.InsideTemperature),
//                    //PowerMin = detail.Min(x => x.Power),
//                    //PowerMax = detail.Max(x => x.Power),
//                };

//                ChargingSnapshotEntity first = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == LastChargingEntity).OrderBy(x => x.Id).FirstOrDefaultAsync();
//                ChargingSnapshotEntity last = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == LastChargingEntity).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

//                //LastChargingEntity.Latitude = first?.Latitude;
//                //LastChargingEntity.Longitude = first?.Longitude;
//                LastChargingEntity.StartBatteryLevel = first?.BatteryLevel;
//                LastChargingEntity.EndBatteryLevel = last?.BatteryLevel;
//                LastChargingEntity.StartIdealBatteryRange = first?.IdealBatteryRange;
//                LastChargingEntity.EndIdealBatteryRange = last?.IdealBatteryRange;
//                LastChargingEntity.StartRatedBatteryRange = first?.RatedBatteryRange;
//                LastChargingEntity.EndRatedBatteryRange = last?.RatedBatteryRange;
//                LastChargingEntity.Duration = (Decimal)(LastChargingEntity.EndTimestamp - LastChargingEntity.StartTimestamp).Value.TotalHours;
//                //LastChargingEntity.OutsideTemperatureAverage = summary.OutsideTemperatureAverage;
//                //LastChargingEntity.OutsideTemperatureMin = summary.OutsideTemperatureMin;
//                //LastChargingEntity.OutsideTemperatureMax = summary.OutsideTemperatureMax;
//                //LastChargingEntity.InsideTemperatureAverage = summary.InsideTemperatureAverage;
//                //LastChargingEntity.InsideTemperatureMin = summary.InsideTemperatureMin;
//                //LastChargingEntity.InsideTemperatureMax = summary.InsideTemperatureMax;
//                //LastChargingEntity.PowerMin = summary.PowerMin;
//                //LastChargingEntity.PowerMax = summary.PowerMax;
//                //LastChargingEntity.IsFastChargerPresent = first?.IsFastChargerPresent;
//                //LastChargingEntity.FastChargerBrand = first?.FastChargerBrand;
//                //LastChargingEntity.FastChargerType = first?.FastChargerType;
//                //LastChargingEntity.ChargeCable = first?.ChargeCable;
//            }

//            await DatabaseContext.SaveChangesAsync();

//            LastChargingSnapshot = null;

//            Logger.Info($"Stop Charging");
//        }

//        public record StandBySnapshot
//        {
//            public Decimal? Latitude { get; set; }
//            public Decimal? Longitude { get; set; }
//            public Decimal? Elevation { get; set; }
//            public Decimal? Heading { get; set; }
//            public Decimal? Power { get; set; }
//            public Decimal? Odometer { get; set; }
//            public Decimal? BatteryLevel { get; set; }
//            public Decimal? IdealBatteryRange { get; set; }
//            public Decimal? RatedBatteryRange { get; set; }
//            public Decimal? OutsideTemperature { get; set; }
//            public Decimal? InsideTemperature { get; set; }
//            public Decimal? DriverTemperatureSetting { get; set; }
//            public Int32? DriverSeatHeater { get; set; }
//            public Decimal? PassengerTemperatureSetting { get; set; }
//            public Int32? PassengerSeatHeater { get; set; }
//            public Boolean? IsSideMirrorHeater { get; set; }
//            public Boolean? IsWiperBladeHeater { get; set; }
//            public Boolean? IsFrontDefrosterOn { get; set; }
//            public Boolean? IsRearDefrosterOn { get; set; }
//            public Boolean? IsClimateOn { get; set; }
//            public Int32? FanStatus { get; set; }
//            public Boolean? IsBatteryHeater { get; set; }
//            public Boolean? IsBatteryHeaterOn { get; set; }

//            public StandBySnapshot()
//            {
//            }

//            public StandBySnapshot(TeslaCarData carData)
//            {
//                BatteryLevel = carData?.ChargeState?.BatteryLevel;
//                Heading = carData?.DriveState?.Heading;
//                Latitude = carData?.DriveState?.Latitude;
//                Longitude = carData?.DriveState?.Longitude;
//                Odometer = carData?.CarState?.Odometer.Mile;
//                Power = carData?.DriveState?.Power;
//                IdealBatteryRange = carData?.ChargeState?.IdealBatteryRange.Mile;
//                RatedBatteryRange = carData?.ChargeState?.RatedBatteryRange.Mile;
//                IsClimateOn = carData?.ClimateState?.IsClimateOn;
//                FanStatus = carData?.ClimateState?.FanStatus;
//                InsideTemperature = carData?.ClimateState?.InsideTemperature.Celsius;
//                OutsideTemperature = carData?.ClimateState?.OutsideTemperature.Celsius;
//                IsBatteryHeater = carData?.ClimateState?.IsBatteryHeater;
//                IsBatteryHeaterOn = carData?.ChargeState?.IsBatteryHeaterOn;
//                DriverTemperatureSetting = carData?.ClimateState?.DriverTemperatureSetting.Celsius;
//                DriverSeatHeater = carData?.ClimateState?.DriverSeatHeater;
//                PassengerTemperatureSetting = carData?.ClimateState?.PassengerTemperatureSetting.Celsius;
//                PassengerSeatHeater = carData?.ClimateState?.PassengerSeatHeater;
//                IsSideMirrorHeater = carData?.ClimateState?.IsSideMirrorHeater;
//                IsWiperBladeHeater = carData?.ClimateState?.IsWiperBladeHeater;
//                IsFrontDefrosterOn = carData?.ClimateState?.IsFrontDefrosterOn;
//                IsRearDefrosterOn = carData?.ClimateState?.IsRearDefrosterOn;
//            }

//            public StandBySnapshot(TeslaStreamingData streamingData)
//            {
//                BatteryLevel = streamingData?.Soc;
//                Elevation = streamingData?.Elevation;
//                Heading = streamingData?.EstimateHeading;
//                Latitude = streamingData?.Latitude;
//                Longitude = streamingData?.Longitude;
//                Odometer = streamingData?.Odometer.Mile;
//                Power = streamingData?.Power;
//            }
//        }

//        public record Snapshot : StandBySnapshot
//        {
//            public Decimal? Speed { get; set; }
//            public ShiftState? ShiftState { get; set; }

//            public Snapshot()
//            {
//            }

//            public Snapshot(TeslaCarData carData)
//                : base(carData)
//            {
//                ShiftState = carData?.DriveState?.ShiftState;
//                Speed = carData?.DriveState?.Speed?.Mile ?? 0;
//            }

//            public Snapshot(TeslaStreamingData streamingData)
//                : base(streamingData)
//            {
//                ShiftState = streamingData?.ShiftState;
//                Speed = streamingData?.Speed?.Mile ?? 0;
//            }
//        }

//        public record DrivingSnapshot : Snapshot
//        {
//            public DrivingSnapshot()
//            {
//            }

//            public DrivingSnapshot(TeslaCarData carData)
//                : base(carData)
//            {
//            }

//            public DrivingSnapshot(TeslaStreamingData streamingData)
//                : base(streamingData)
//            {
//            }
//        }

//        public record ChargingSnapshot
//        {
//            public Decimal? Latitude { get; set; }
//            public Decimal? Longitude { get; set; }
//            public Decimal? Power { get; set; }
//            public Decimal? BatteryLevel { get; set; }
//            public Decimal? IdealBatteryRange { get; set; }
//            public Decimal? RatedBatteryRange { get; set; }
//            public Decimal? OutsideTemperature { get; set; }
//            public Decimal? InsideTemperature { get; set; }
//            public Boolean? IsBatteryHeater { get; set; }
//            public Boolean? IsBatteryHeaterOn { get; set; }
//            public Decimal? ChargeEnergyAdded { get; set; }
//            public Int32? ChargerPhases { get; set; }
//            public Int32? ChargerPilotCurrent { get; set; }
//            public Int32? ChargerActualCurrent { get; set; }
//            public Int32? ChargerPower { get; set; }
//            public Int32? ChargerVoltage { get; set; }
//            public Boolean? IsFastChargerPresent { get; set; }
//            public String ChargeCable { get; set; }
//            public String FastChargerBrand { get; set; }
//            public String FastChargerType { get; set; }
//            public Decimal? ChargeRate { get; init; }

//            public ChargingSnapshot()
//            {
//            }

//            public ChargingSnapshot(TeslaCarData carData)
//            {
//                Latitude = carData?.DriveState?.Latitude;
//                Longitude = carData?.DriveState?.Longitude;
//                BatteryLevel = carData?.ChargeState?.BatteryLevel;
//                Power = carData?.DriveState?.Power;
//                IdealBatteryRange = carData?.ChargeState?.IdealBatteryRange.Mile;
//                RatedBatteryRange = carData?.ChargeState?.RatedBatteryRange.Mile;
//                InsideTemperature = carData?.ClimateState?.InsideTemperature.Celsius;
//                OutsideTemperature = carData?.ClimateState?.OutsideTemperature.Celsius;
//                IsBatteryHeater = carData?.ClimateState?.IsBatteryHeater;
//                IsBatteryHeaterOn = carData?.ChargeState?.IsBatteryHeaterOn;
//                ChargeEnergyAdded = carData?.ChargeState?.ChargeEnergyAdded;
//                ChargerPhases = carData?.ChargeState?.ChargerPhases;
//                ChargerPilotCurrent = carData?.ChargeState?.ChargerPilotCurrent;
//                ChargerActualCurrent = carData?.ChargeState?.ChargerActualCurrent;
//                ChargerPower = carData?.ChargeState?.ChargerPower;
//                ChargerVoltage = carData?.ChargeState?.ChargerVoltage;
//                IsFastChargerPresent = carData?.ChargeState?.IsFastChargerPresent;
//                ChargeCable = carData?.ChargeState?.ChargeCable;
//                FastChargerBrand = carData?.ChargeState?.FastChargerBrand;
//                FastChargerType = carData?.ChargeState?.FastChargerType;
//                ChargeRate = carData?.ChargeState?.ChargeRate;
//            }

//            public ChargingSnapshot(TeslaStreamingData streamingData)
//            {
//                Latitude = streamingData?.Latitude;
//                Longitude = streamingData?.Longitude;
//                BatteryLevel = streamingData?.Soc;
//                Power = streamingData?.Power;
//            }
//        }
//    }
//}
