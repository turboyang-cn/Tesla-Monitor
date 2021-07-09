using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using NLog;

using NodaTime;

using TurboYang.Tesla.Monitor.Client;
using TurboYang.Tesla.Monitor.Database;
using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.WebApi.Services
{
    public class DatabaseService : IDatabaseService
    {
        private ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        private DatabaseContext DatabaseContext { get; }
        private IOpenStreetMapClient OpenStreetMapClient { get; }

        public DatabaseService(DatabaseContext databaseContext, IOpenStreetMapClient openStreetMapClient)
        {
            DatabaseContext = databaseContext;
            OpenStreetMapClient = openStreetMapClient;
        }

        public async Task UpdateCarAsync(Int32 carEntityId, String name, String vin, String exteriorColor, String wheelType, CarType? carType)
        {
            CarEntity carEntity = await DatabaseContext.Car.FirstOrDefaultAsync(x => x.Id == carEntityId);

            carEntity.Name = name;
            carEntity.Vin = vin;
            carEntity.ExteriorColor = exteriorColor;
            carEntity.WheelType = wheelType;
            carEntity.Type = carType;

            await DatabaseContext.SaveChangesAsync();
        }

        public async Task SaveSnapshotAsync(Int32 carEntityId, String name, Int64 vehicleId, CarState state, Instant timestamp, Boolean isSamplingCompression = true)
        {
            await SaveSnapshotAsync(carEntityId, name, vehicleId, state, null, timestamp);
        }

        public async Task SaveSnapshotAsync(Int32 carEntityId, String name, Int64 vehicleId, CarState state, IDatabaseService.Snapshot snapshot, Instant timestamp, Boolean isSamplingCompression = true)
        {
            StateEntity lastStateEntity = await DatabaseContext.State.Where(x => x.CarId == carEntityId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (lastStateEntity?.State != state)
            {
                if (lastStateEntity != null && lastStateEntity.EndTimestamp == null)
                {
                    lastStateEntity.EndTimestamp = timestamp;
                }

                Logger.Info($"[{name ?? vehicleId.ToString()}] State Change: {lastStateEntity?.State?.ToString() ?? "null"} -> {state}");

                StateEntity currentStateEntity = new()
                {
                    State = state,
                    StartTimestamp = timestamp,
                    CarId = carEntityId,
                };

                DatabaseContext.State.Add(currentStateEntity);

                await DatabaseContext.SaveChangesAsync();

                lastStateEntity = currentStateEntity;
            }

            if (state == CarState.Online || state == CarState.Asleep)
            {
                await SaveStopDrivingAsync(carEntityId, name, vehicleId, timestamp);
                await SaveStopChargingAsync(carEntityId, name, vehicleId, timestamp);
                await SaveStartStandByAsync(carEntityId, name, vehicleId, timestamp);
            }
            else if (state == CarState.Driving)
            {
                await SaveStopStandByAsync(carEntityId, name, vehicleId, timestamp);
                await SaveStopChargingAsync(carEntityId, name, vehicleId, timestamp);
                await SaveStartDrivingAsync(carEntityId, name, vehicleId, timestamp);
            }
            else if (state == CarState.Charging)
            {
                await SaveStopStandByAsync(carEntityId, name, vehicleId, timestamp);
                await SaveStopDrivingAsync(carEntityId, name, vehicleId, timestamp);
                await SaveStartChargingAsync(carEntityId, name, vehicleId, timestamp);
            }

            SnapshotEntity lastSnapshotEntity = await DatabaseContext.Snapshot.OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync();

            if (lastSnapshotEntity?.Timestamp >= timestamp)
            {
                return;
            }

            if (snapshot != null)
            {
                IDatabaseService.Snapshot lastSnapshot = new(lastSnapshotEntity);

                IDatabaseService.Snapshot currentSnapshot = snapshot.Debounce(lastSnapshot);

                if (!isSamplingCompression || currentSnapshot != lastSnapshot)
                {
                    DatabaseContext.Snapshot.Add(new SnapshotEntity()
                    {
                        Location = currentSnapshot?.Location,
                        Elevation = currentSnapshot?.Elevation,
                        Speed = currentSnapshot?.Speed,
                        Heading = currentSnapshot?.Heading,
                        ShiftState = currentSnapshot?.ShiftState,
                        Power = currentSnapshot?.Power,
                        Odometer = currentSnapshot?.Odometer,
                        BatteryLevel = currentSnapshot?.BatteryLevel,
                        BatteryRange = currentSnapshot?.BatteryRange,
                        OutsideTemperature = currentSnapshot?.OutsideTemperature,
                        InsideTemperature = currentSnapshot?.InsideTemperature,
                        DriverTemperatureSetting = currentSnapshot?.DriverTemperatureSetting,
                        PassengerTemperatureSetting = currentSnapshot?.PassengerTemperatureSetting,
                        DriverSeatHeater = currentSnapshot?.DriverSeatHeater,
                        PassengerSeatHeater = currentSnapshot?.PassengerSeatHeater,
                        FanStatus = currentSnapshot?.FanStatus,
                        IsSideMirrorHeater = currentSnapshot?.IsSideMirrorHeater,
                        IsWiperBladeHeater = currentSnapshot?.IsWiperBladeHeater,
                        IsFrontDefrosterOn = currentSnapshot?.IsFrontDefrosterOn,
                        IsRearDefrosterOn = currentSnapshot?.IsRearDefrosterOn,
                        IsClimateOn = currentSnapshot?.IsClimateOn,
                        IsBatteryHeater = currentSnapshot?.IsBatteryHeater,
                        IsBatteryHeaterOn = currentSnapshot?.IsBatteryHeaterOn,
                        ChargeEnergyAdded = currentSnapshot?.ChargeEnergyAdded,
                        ChargeEnergyUsed = currentSnapshot?.ChargeEnergyUsed,
                        ChargerPhases = currentSnapshot?.ChargerPhases,
                        ChargerPilotCurrent = currentSnapshot?.ChargerPilotCurrent,
                        ChargerActualCurrent = currentSnapshot?.ChargerActualCurrent,
                        ChargerPower = currentSnapshot?.ChargerPower,
                        ChargerVoltage = currentSnapshot?.ChargerVoltage,
                        ChargeRate = currentSnapshot?.ChargeRate,
                        IsFastChargerPresent = currentSnapshot?.IsFastChargerPresent,
                        ChargeCable = currentSnapshot?.ChargeCable,
                        FastChargerBrand = currentSnapshot?.FastChargerBrand,
                        FastChargerType = currentSnapshot?.FastChargerType,

                        State = lastStateEntity,

                        CarId = carEntityId,

                        Timestamp = timestamp,
                    });

                    await DatabaseContext.SaveChangesAsync();
                }

                if (lastStateEntity.State == CarState.Online || lastStateEntity.State == CarState.Asleep)
                {
                    await SaveStandBySnapshotAsync(snapshot, timestamp);
                }
                else if (lastStateEntity.State == CarState.Driving)
                {
                    await SaveDrivingSnapshotAsync(snapshot, timestamp);
                }
                else if (lastStateEntity.State == CarState.Charging)
                {
                    await SaveChargingSnapshotAsync(snapshot, timestamp);
                }
            }

            await DatabaseContext.SaveChangesAsync();
        }

        private async Task SaveStartStandByAsync(Int32 carEntityId, String name, Int64 vehicleId, Instant timestamp)
        {
            StandByEntity lastStandByEntity = await DatabaseContext.StandBy.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (lastStandByEntity != null && lastStandByEntity.EndTimestamp == null)
            {
                return;
            }

            lastStandByEntity = new StandByEntity()
            {
                StartTimestamp = timestamp,

                CarId = carEntityId,
            };

            DatabaseContext.StandBy.Add(lastStandByEntity);

            await DatabaseContext.SaveChangesAsync();

            Logger.Info($"[{name ?? vehicleId.ToString()}] Start Stand By");
        }

        private async Task SaveStopStandByAsync(Int32 carEntityId, String name, Int64 vehicleId, Instant timestamp)
        {
            StandByEntity lastStandByEntity = await DatabaseContext.StandBy.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (lastStandByEntity != null && lastStandByEntity.EndTimestamp == null)
            {
                Decimal? fullPower = await GetFullPowerAsync(carEntityId);
                StandBySnapshotEntity firstRecord = await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == lastStandByEntity).OrderBy(x => x.Timestamp).FirstOrDefaultAsync();
                StandBySnapshotEntity lastRecord = await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == lastStandByEntity).OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync();

                Decimal? onlineDuration = DatabaseContext.State.Where(x => x.StartTimestamp >= lastStandByEntity.StartTimestamp && x.EndTimestamp <= timestamp && x.State == CarState.Online).ToList().Select(x => (Decimal?)((x.EndTimestamp - x.StartTimestamp)?.TotalSeconds)).Sum();

                AddressEntity addressEntity = null;
                if (lastRecord?.Location != null)
                {
                    addressEntity = await DatabaseContext.Address.Where(x => (Decimal)x.Location.Distance(lastRecord.Location) / 1000 <= x.Radius).Select(x => new
                    {
                        Address = x,
                        Distance = x.Location.Distance(lastRecord.Location) / 1000
                    }).OrderByDescending(x => x.Distance).Select(x => x.Address).FirstOrDefaultAsync();
                }
                if (addressEntity == null)
                {
                    OpenStreetMapAddress address = await OpenStreetMapClient.ReverseLookupAsync((Decimal)lastRecord.Location.Y, (Decimal)lastRecord.Location.X, Environment.GetEnvironmentVariable("Language", EnvironmentVariableTarget.Process) ?? "en-US");

                    addressEntity = new AddressEntity()
                    {
                        Country = address?.Country,
                        State = address?.State,
                        County = address?.County,
                        City = address?.City,
                        District = address?.District,
                        Village = address?.Village,
                        Road = address?.Road,
                        Building = address?.Building,
                        Postcode = address?.Postcode,
                        Name = address?.ToString(),
                        Location = lastRecord.Location,
                    };

                    DatabaseContext.Add(addressEntity);
                }

                lastStandByEntity.Address = addressEntity;
                lastStandByEntity.Elevation = lastRecord?.Elevation;
                lastStandByEntity.Heading = lastRecord?.Heading;
                lastStandByEntity.Odometer = lastRecord?.Odometer;

                lastStandByEntity.StartBatteryLevel = firstRecord?.BatteryLevel;
                lastStandByEntity.StartBatteryRange = firstRecord?.BatteryRange;
                lastStandByEntity.StartPower = firstRecord?.BatteryLevel / 100m * fullPower;

                lastStandByEntity.EndTimestamp = timestamp;
                lastStandByEntity.EndBatteryLevel = lastRecord?.BatteryLevel;
                lastStandByEntity.EndBatteryRange = lastRecord?.BatteryRange;
                lastStandByEntity.EndPower = lastRecord?.BatteryLevel / 100m * fullPower;

                lastStandByEntity.Duration = (Decimal?)((lastStandByEntity.EndTimestamp - lastStandByEntity.StartTimestamp)?.TotalSeconds);
                lastStandByEntity.OnlineRatio = onlineDuration / lastStandByEntity.Duration * 100m;

                await DatabaseContext.SaveChangesAsync();

                Logger.Info($"[{name ?? vehicleId.ToString()}] Stop Stand By");
            }
        }

        private async Task SaveStartDrivingAsync(Int32 carEntityId, String name, Int64 vehicleId, Instant timestamp)
        {
            DrivingEntity lastDrivingEntity = await DatabaseContext.Driving.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (lastDrivingEntity != null && lastDrivingEntity.EndTimestamp == null)
            {
                return;
            }

            lastDrivingEntity = new DrivingEntity()
            {
                StartTimestamp = timestamp,

                CarId = carEntityId,
            };

            DatabaseContext.Driving.Add(lastDrivingEntity);

            await DatabaseContext.SaveChangesAsync();

            Logger.Info($"[{name ?? vehicleId.ToString()}] Start Driving");
        }

        private async Task SaveStopDrivingAsync(Int32 carEntityId, String name, Int64 vehicleId, Instant timestamp)
        {
            DrivingEntity lastDrivingEntity = await DatabaseContext.Driving.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (lastDrivingEntity != null && lastDrivingEntity.EndTimestamp == null)
            {
                Decimal? fullPower = await GetFullPowerAsync(carEntityId);
                DrivingSnapshotEntity firstRecord = await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == lastDrivingEntity).OrderBy(x => x.Timestamp).FirstOrDefaultAsync();
                DrivingSnapshotEntity lastRecord = await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == lastDrivingEntity).OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync();

                AddressEntity startAddressEntity = null;
                if (firstRecord?.Location != null)
                {
                    startAddressEntity = await DatabaseContext.Address.Where(x => (Decimal)x.Location.Distance(firstRecord.Location) / 1000 <= x.Radius).Select(x => new
                    {
                        Address = x,
                        Distance = x.Location.Distance(firstRecord.Location) / 1000
                    }).OrderByDescending(x => x.Distance).Select(x => x.Address).FirstOrDefaultAsync();
                }
                if (startAddressEntity == null)
                {
                    OpenStreetMapAddress address = await OpenStreetMapClient.ReverseLookupAsync((Decimal)firstRecord.Location.Y, (Decimal)firstRecord.Location.X, Environment.GetEnvironmentVariable("Language", EnvironmentVariableTarget.Process) ?? "en-US");

                    startAddressEntity = new AddressEntity()
                    {
                        Country = address?.Country,
                        State = address?.State,
                        County = address?.County,
                        City = address?.City,
                        District = address?.District,
                        Village = address?.Village,
                        Road = address?.Road,
                        Building = address?.Building,
                        Postcode = address?.Postcode,
                        Name = address?.ToString(),
                        Location = lastRecord.Location,
                    };

                    DatabaseContext.Add(startAddressEntity);
                }

                AddressEntity endAddressEntity = null;
                if (lastRecord?.Location != null)
                {
                    endAddressEntity = await DatabaseContext.Address.Where(x => (Decimal)x.Location.Distance(lastRecord.Location) / 1000 <= x.Radius).Select(x => new
                    {
                        Address = x,
                        Distance = x.Location.Distance(lastRecord.Location) / 1000
                    }).OrderByDescending(x => x.Distance).Select(x => x.Address).FirstOrDefaultAsync();
                }
                if (endAddressEntity == null)
                {
                    OpenStreetMapAddress address = await OpenStreetMapClient.ReverseLookupAsync((Decimal)lastRecord.Location.Y, (Decimal)lastRecord.Location.X, Environment.GetEnvironmentVariable("Language", EnvironmentVariableTarget.Process) ?? "en-US");

                    endAddressEntity = new AddressEntity()
                    {
                        Country = address?.Country,
                        State = address?.State,
                        County = address?.County,
                        City = address?.City,
                        District = address?.District,
                        Village = address?.Village,
                        Road = address?.Road,
                        Building = address?.Building,
                        Postcode = address?.Postcode,
                        Name = address?.ToString(),
                        Location = lastRecord.Location,
                    };

                    DatabaseContext.Add(endAddressEntity);
                }

                lastDrivingEntity.StartAddress = startAddressEntity;
                lastDrivingEntity.StartBatteryLevel = firstRecord?.BatteryLevel;
                lastDrivingEntity.StartOdometer = firstRecord?.Odometer;
                lastDrivingEntity.StartBatteryRange = firstRecord?.BatteryRange;
                lastDrivingEntity.StartPower = firstRecord?.BatteryLevel / 100m * fullPower;
                
                lastDrivingEntity.EndTimestamp = timestamp;
                lastDrivingEntity.EndAddress = endAddressEntity;
                lastDrivingEntity.EndBatteryLevel = lastRecord?.BatteryLevel;
                lastDrivingEntity.EndOdometer = lastRecord?.Odometer;
                lastDrivingEntity.EndBatteryRange = lastRecord?.BatteryRange;
                lastDrivingEntity.EndPower = lastRecord?.BatteryLevel / 100m * fullPower;
                
                lastDrivingEntity.Duration = (Decimal?)((lastDrivingEntity.EndTimestamp - lastDrivingEntity.StartTimestamp)?.TotalSeconds);
                lastDrivingEntity.Distance = lastDrivingEntity?.EndOdometer - lastDrivingEntity?.StartOdometer;
                lastDrivingEntity.SpeedAverage = lastDrivingEntity.Duration > 0 ? lastDrivingEntity.Distance / lastDrivingEntity.Duration * 3600m : 0;
                lastDrivingEntity.TemperatureAverage = Math.Abs((lastRecord?.OutsideTemperature - firstRecord?.OutsideTemperature) ?? 0) / 2;

                await DatabaseContext.SaveChangesAsync();

                Logger.Info($"[{name ?? vehicleId.ToString()}] Stop Driving");
            }
        }

        private async Task SaveStartChargingAsync(Int32 carEntityId, String name, Int64 vehicleId, Instant timestamp)
        {
            ChargingEntity lastChargingEntity = await DatabaseContext.Charging.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (lastChargingEntity != null && lastChargingEntity.EndTimestamp == null)
            {
                return;
            }

            lastChargingEntity = new ChargingEntity()
            {
                StartTimestamp = timestamp,

                CarId = carEntityId,
            };

            DatabaseContext.Charging.Add(lastChargingEntity);

            await DatabaseContext.SaveChangesAsync();

            Logger.Info($"[{name ?? vehicleId.ToString()}] Start Charging");
        }

        private async Task SaveStopChargingAsync(Int32 carEntityId, String name, Int64 vehicleId, Instant timestamp)
        {
            ChargingEntity lastChargingEntity = await DatabaseContext.Charging.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (lastChargingEntity != null && lastChargingEntity.EndTimestamp == null)
            {
                ChargingSnapshotEntity firstRecord = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == lastChargingEntity).OrderBy(x => x.Timestamp).FirstOrDefaultAsync();
                ChargingSnapshotEntity lastRecord = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == lastChargingEntity).OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync();

                Decimal chargeEnergyUsed = 0m;
                var details = await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == lastChargingEntity).OrderBy(x => x.Timestamp).Select(x => new
                {
                    Timestamp = x.CreateTimestamp,
                    Current = x.ChargerActualCurrent,
                    Voltage = x.ChargerVoltage,
                    Phases = x.ChargerPhases,
                    Power = x.ChargerPower,
                }).ToListAsync();
                if (details.Count > 0)
                {
                    for (Int32 i = 0; i < details.Count - 1; i++)
                    {
                        chargeEnergyUsed += CalculateEnergyUsed(details[i].Current, details[i].Voltage, details[i].Phases, details[i].Power) * (Decimal?)(details[i + 1].Timestamp - details[i].Timestamp)?.TotalHours ?? 0m;
                    }
                    chargeEnergyUsed += CalculateEnergyUsed(details[^1].Current, details[^1].Voltage, details[^1].Phases, details[^1].Power) * (Decimal?)(timestamp - details[^1].Timestamp)?.TotalHours ?? 0m;
                }

                AddressEntity addressEntity = null;
                if (lastRecord?.Location != null)
                {
                    addressEntity = await DatabaseContext.Address.Where(x => (Decimal)x.Location.Distance(lastRecord.Location) / 1000 <= x.Radius).Select(x => new
                    {
                        Address = x,
                        Distance = x.Location.Distance(lastRecord.Location) / 1000
                    }).OrderByDescending(x => x.Distance).Select(x => x.Address).FirstOrDefaultAsync();
                }
                if (addressEntity == null)
                {
                    OpenStreetMapAddress address = await OpenStreetMapClient.ReverseLookupAsync((Decimal)lastRecord.Location.Y, (Decimal)lastRecord.Location.X, Environment.GetEnvironmentVariable("Language", EnvironmentVariableTarget.Process) ?? "en-US");

                    addressEntity = new AddressEntity()
                    {
                        Country = address?.Country,
                        State = address?.State,
                        County = address?.County,
                        City = address?.City,
                        District = address?.District,
                        Village = address?.Village,
                        Road = address?.Road,
                        Building = address?.Building,
                        Postcode = address?.Postcode,
                        Name = address?.ToString(),
                        Location = lastRecord.Location,
                    };

                    DatabaseContext.Add(addressEntity);
                }

                Decimal? fullPower = null;
                if (lastRecord?.BatteryLevel - firstRecord?.BatteryLevel > 50 && lastRecord?.ChargeEnergyAdded > firstRecord?.ChargeEnergyAdded)
                {
                    fullPower = (lastRecord?.ChargeEnergyAdded - firstRecord?.ChargeEnergyAdded) * 100 / (lastRecord?.BatteryLevel - firstRecord?.BatteryLevel);

                    if (fullPower != null)
                    {
                        await SetFullPowerAsync(carEntityId, fullPower.Value);
                    }
                }
                else
                {
                    fullPower = await GetFullPowerAsync(carEntityId);
                }

                lastChargingEntity.Address = addressEntity;
                lastChargingEntity.Elevation = lastRecord?.Elevation;
                lastChargingEntity.Heading = lastRecord?.Heading;
                lastChargingEntity.Odometer = lastRecord?.Odometer;
                lastChargingEntity.IsFastChargerPresent = lastRecord?.IsFastChargerPresent;
                lastChargingEntity.ChargeCable = lastRecord?.ChargeCable;
                lastChargingEntity.FastChargerBrand = lastRecord?.FastChargerBrand;
                lastChargingEntity.FastChargerType = lastRecord?.FastChargerType;

                lastChargingEntity.StartBatteryLevel = firstRecord?.BatteryLevel;
                lastChargingEntity.StartBatteryRange = firstRecord?.BatteryRange;
                lastChargingEntity.StartPower = firstRecord?.BatteryLevel / 100m * fullPower;

                lastChargingEntity.EndTimestamp = timestamp;
                lastChargingEntity.EndBatteryLevel = lastRecord?.BatteryLevel;
                lastChargingEntity.EndBatteryRange = lastRecord?.BatteryRange;
                lastChargingEntity.EndPower = lastRecord?.BatteryLevel / 100m * fullPower;

                lastChargingEntity.Duration = (Decimal?)((lastChargingEntity.EndTimestamp - lastChargingEntity.StartTimestamp)?.TotalSeconds);
                lastChargingEntity.ChargeEnergyAdded = lastRecord?.ChargeEnergyAdded - firstRecord?.ChargeEnergyAdded;
                lastChargingEntity.ChargeEnergyUsed = chargeEnergyUsed;
                lastChargingEntity.Efficiency = lastChargingEntity.ChargeEnergyAdded / lastChargingEntity.ChargeEnergyUsed * 100m;

                await DatabaseContext.SaveChangesAsync();

                Logger.Info($"[{name ?? vehicleId.ToString()}] Stop Charging");
            }
        }

        private async Task SaveStandBySnapshotAsync(IDatabaseService.Snapshot snapshot, Instant timestamp, Boolean isSamplingCompression = true)
        {
            StandByEntity lastStandByEntity = await DatabaseContext.StandBy.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            IDatabaseService.StandBySnapshot lastStandBySnapshot = new(await DatabaseContext.StandBySnapshot.Where(x => x.StandBy == lastStandByEntity).OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync());

            IDatabaseService.StandBySnapshot currentStandBySnapshot = new IDatabaseService.StandBySnapshot(snapshot).Debounce(lastStandBySnapshot);

            if (!isSamplingCompression || currentStandBySnapshot != lastStandBySnapshot)
            {
                DatabaseContext.StandBySnapshot.Add(new StandBySnapshotEntity()
                {
                    Location = currentStandBySnapshot?.Location,
                    Elevation = currentStandBySnapshot?.Elevation,
                    Heading = currentStandBySnapshot?.Heading,
                    Odometer = currentStandBySnapshot?.Odometer,
                    Power = currentStandBySnapshot?.Power,
                    BatteryLevel = currentStandBySnapshot?.BatteryLevel,
                    BatteryRange = currentStandBySnapshot?.BatteryRange,
                    OutsideTemperature = currentStandBySnapshot?.OutsideTemperature,
                    InsideTemperature = currentStandBySnapshot?.InsideTemperature,
                    DriverTemperatureSetting = currentStandBySnapshot?.DriverTemperatureSetting,
                    PassengerTemperatureSetting = currentStandBySnapshot?.PassengerTemperatureSetting,
                    DriverSeatHeater = currentStandBySnapshot?.DriverSeatHeater,
                    PassengerSeatHeater = currentStandBySnapshot?.PassengerSeatHeater,
                    FanStatus = currentStandBySnapshot?.FanStatus,
                    IsSideMirrorHeater = currentStandBySnapshot.IsSideMirrorHeater,
                    IsWiperBladeHeater = currentStandBySnapshot?.IsWiperBladeHeater,
                    IsFrontDefrosterOn = currentStandBySnapshot?.IsFrontDefrosterOn,
                    IsRearDefrosterOn = currentStandBySnapshot?.IsRearDefrosterOn,
                    IsClimateOn = currentStandBySnapshot?.IsClimateOn,
                    IsBatteryHeater = currentStandBySnapshot?.IsBatteryHeater,
                    IsBatteryHeaterOn = currentStandBySnapshot?.IsBatteryHeaterOn,

                    StandBy = lastStandByEntity,

                    Timestamp = timestamp,
                });

                await DatabaseContext.SaveChangesAsync();
            }
        }

        private async Task SaveDrivingSnapshotAsync(IDatabaseService.Snapshot snapshot, Instant timestamp, Boolean isSamplingCompression = true)
        {
            DrivingEntity lastDrivingEntity = await DatabaseContext.Driving.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            IDatabaseService.DrivingSnapshot lastDrivingSnapshot = new(await DatabaseContext.DrivingSnapshot.Where(x => x.Driving == lastDrivingEntity).OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync());

            IDatabaseService.DrivingSnapshot currentDrivingSnapshot = new IDatabaseService.DrivingSnapshot(snapshot).Debounce(lastDrivingSnapshot);

            if (!isSamplingCompression || currentDrivingSnapshot != lastDrivingSnapshot)
            {
                DatabaseContext.DrivingSnapshot.Add(new DrivingSnapshotEntity()
                {
                    Location = currentDrivingSnapshot?.Location,
                    Elevation = currentDrivingSnapshot?.Elevation,
                    Speed = currentDrivingSnapshot?.Speed,
                    Heading = currentDrivingSnapshot?.Heading,
                    ShiftState = currentDrivingSnapshot?.ShiftState,
                    Power = currentDrivingSnapshot?.Power,
                    Odometer = currentDrivingSnapshot?.Odometer,
                    BatteryLevel = currentDrivingSnapshot?.BatteryLevel,
                    BatteryRange = currentDrivingSnapshot?.BatteryRange,
                    OutsideTemperature = currentDrivingSnapshot?.OutsideTemperature,
                    InsideTemperature = currentDrivingSnapshot?.InsideTemperature,
                    DriverTemperatureSetting = currentDrivingSnapshot?.DriverTemperatureSetting,
                    PassengerTemperatureSetting = currentDrivingSnapshot?.PassengerTemperatureSetting,
                    DriverSeatHeater = currentDrivingSnapshot?.DriverSeatHeater,
                    PassengerSeatHeater = currentDrivingSnapshot?.PassengerSeatHeater,
                    FanStatus = currentDrivingSnapshot?.FanStatus,
                    IsSideMirrorHeater = currentDrivingSnapshot?.IsSideMirrorHeater,
                    IsWiperBladeHeater = currentDrivingSnapshot?.IsWiperBladeHeater,
                    IsFrontDefrosterOn = currentDrivingSnapshot?.IsFrontDefrosterOn,
                    IsRearDefrosterOn = currentDrivingSnapshot?.IsRearDefrosterOn,
                    IsClimateOn = currentDrivingSnapshot?.IsClimateOn,
                    IsBatteryHeater = currentDrivingSnapshot?.IsBatteryHeater,
                    IsBatteryHeaterOn = currentDrivingSnapshot?.IsBatteryHeaterOn,

                    Driving = lastDrivingEntity,

                    Timestamp = timestamp,
                });

                await DatabaseContext.SaveChangesAsync();
            }
        }

        private async Task SaveChargingSnapshotAsync(IDatabaseService.Snapshot snapshot, Instant timestamp, Boolean isSamplingCompression = true)
        {
            ChargingEntity lastChargingEntity = await DatabaseContext.Charging.OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            IDatabaseService.ChargingSnapshot lastChargingSnapshot = new(await DatabaseContext.ChargingSnapshot.Where(x => x.Charging == lastChargingEntity).OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync());

            IDatabaseService.ChargingSnapshot currentChargingSnapshot = new IDatabaseService.ChargingSnapshot(snapshot).Debounce(lastChargingSnapshot);

            if (!isSamplingCompression || currentChargingSnapshot != lastChargingSnapshot)
            {
                DatabaseContext.ChargingSnapshot.Add(new ChargingSnapshotEntity()
                {
                    Location = currentChargingSnapshot?.Location,
                    Elevation = currentChargingSnapshot?.Elevation,
                    Heading = currentChargingSnapshot?.Heading,
                    Odometer = currentChargingSnapshot?.Odometer,
                    BatteryLevel = currentChargingSnapshot?.BatteryLevel,
                    BatteryRange = currentChargingSnapshot?.BatteryRange,
                    OutsideTemperature = currentChargingSnapshot?.OutsideTemperature,
                    InsideTemperature = currentChargingSnapshot?.InsideTemperature,
                    DriverTemperatureSetting = currentChargingSnapshot?.DriverTemperatureSetting,
                    PassengerTemperatureSetting = currentChargingSnapshot?.PassengerTemperatureSetting,
                    DriverSeatHeater = currentChargingSnapshot?.DriverSeatHeater,
                    PassengerSeatHeater = currentChargingSnapshot?.PassengerSeatHeater,
                    FanStatus = currentChargingSnapshot?.FanStatus,
                    IsSideMirrorHeater = currentChargingSnapshot.IsSideMirrorHeater,
                    IsWiperBladeHeater = currentChargingSnapshot?.IsWiperBladeHeater,
                    IsFrontDefrosterOn = currentChargingSnapshot?.IsFrontDefrosterOn,
                    IsRearDefrosterOn = currentChargingSnapshot?.IsRearDefrosterOn,
                    IsClimateOn = currentChargingSnapshot?.IsClimateOn,
                    IsBatteryHeater = currentChargingSnapshot?.IsBatteryHeater,
                    IsBatteryHeaterOn = currentChargingSnapshot?.IsBatteryHeaterOn,
                    ChargeEnergyAdded = currentChargingSnapshot?.ChargeEnergyAdded,
                    ChargerPhases = currentChargingSnapshot?.ChargerPhases,
                    ChargerPilotCurrent = currentChargingSnapshot?.ChargerPilotCurrent,
                    ChargerActualCurrent = currentChargingSnapshot?.ChargerActualCurrent,
                    ChargerPower = currentChargingSnapshot?.ChargerPower,
                    ChargerVoltage = currentChargingSnapshot?.ChargerVoltage,
                    ChargeRate = currentChargingSnapshot?.ChargeRate,
                    IsFastChargerPresent = currentChargingSnapshot?.IsFastChargerPresent,
                    ChargeCable = currentChargingSnapshot?.ChargeCable,
                    FastChargerBrand = currentChargingSnapshot?.FastChargerBrand,
                    FastChargerType = currentChargingSnapshot?.FastChargerType,

                    Charging = lastChargingEntity,

                    Timestamp = timestamp,
                });

                await DatabaseContext.SaveChangesAsync();
            }
        }

        private async Task<Decimal?> GetFullPowerAsync(Int32 carEntityId)
        {
            if (carEntityId > 0)
            {
                return await DatabaseContext.CarSetting.Where(x => x.CarId == carEntityId).Select(x => x.FullPower).FirstOrDefaultAsync();
            }

            return null;
        }

        private async Task SetFullPowerAsync(Int32 carEntityId, Decimal fullPower)
        {
            if (carEntityId > 0)
            {
                CarSettingEntity carSettingEntity = await DatabaseContext.CarSetting.FirstOrDefaultAsync(x => x.CarId == carEntityId);

                carSettingEntity.FullPower = fullPower;

                await DatabaseContext.SaveChangesAsync();
            }
        }

        private Decimal? CalculateEnergyUsed(Decimal? current, Decimal? voltage, Int32? phases, Decimal? power)
        {
            Decimal? adjustedPhases = DeterminePhases(current, voltage, phases, power);

            if (adjustedPhases != null)
            {
                return power;
            }
            else
            {
                return current * voltage * adjustedPhases / 1000m;
            }
        }

        private Decimal? DeterminePhases(Decimal? current, Decimal? voltage, Int32? phases, Decimal? power)
        {
            if (current == null || voltage == null || phases == null || power == null)
            {
                return null;
            }

            Decimal? predictivePhases = current * voltage == 0 ? 0 : power * 1000m / (current * voltage);

            if (phases == Math.Round(predictivePhases.Value, 0, MidpointRounding.AwayFromZero))
            {
                return phases;
            }
            else if (phases == 3 && Math.Abs(power.Value / (Decimal)Math.Sqrt(phases.Value)) - 1 <= 0.1m)
            {
                return (Decimal)Math.Sqrt(phases.Value);
            }
            else if (Math.Abs(Math.Round(predictivePhases.Value, 0, MidpointRounding.AwayFromZero) - predictivePhases.Value) <= 0.3m)
            {
                return Math.Round(predictivePhases.Value, 0, MidpointRounding.AwayFromZero);
            }

            return null;
        }
    }
}
