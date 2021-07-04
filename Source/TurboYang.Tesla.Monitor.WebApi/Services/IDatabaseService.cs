using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;

using NodaTime;

using TurboYang.Tesla.Monitor.Client;
using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.WebApi.Services
{
    public interface IDatabaseService
    {
        public Task SaveSnapshotAsync(Int32 carEntityId, String name, Int64 vehicleId, CarState state, Instant timestamp, Boolean isSamplingCompression = true);
        public Task SaveSnapshotAsync(Int32 carEntityId, String name, Int64 vehicleId, CarState state, Snapshot snapshot, Instant timestamp, Boolean isSamplingCompression = true);
        public Task UpdateCarAsync(Int32 carEntityId, String name, String vin, String exteriorColor, String wheelType, CarType? carType);

        public record BaseSnapshot
        {
            protected Decimal? Debounce(Decimal? newValue, Decimal? oldValue, Decimal threshold)
            {
                if (oldValue == null && newValue != null)
                {
                    return newValue.Value;
                }
                else if (oldValue != null && newValue == null)
                {
                    return oldValue;
                }
                else if (oldValue == null && newValue == null)
                {
                    return null;
                }

                if (Math.Abs(newValue.Value - oldValue.Value) < Math.Abs(threshold))
                {
                    return oldValue;
                }

                return newValue;
            }
        }

        public record Snapshot : BaseSnapshot
        {
            public Point Location { get; init; }
            public Decimal? Elevation { get; init; }
            public Decimal? Speed { get; init; }
            public Decimal? Heading { get; init; }
            public ShiftState? ShiftState { get; init; }
            public Decimal? Power { get; init; }
            public Decimal? Odometer { get; init; }
            public Decimal? BatteryLevel { get; init; }
            public Decimal? BatteryRange { get; init; }
            public Decimal? OutsideTemperature { get; init; }
            public Decimal? InsideTemperature { get; init; }
            public Decimal? DriverTemperatureSetting { get; init; }
            public Decimal? PassengerTemperatureSetting { get; init; }
            public Int32? DriverSeatHeater { get; init; }
            public Int32? PassengerSeatHeater { get; init; }
            public Int32? FanStatus { get; init; }
            public Boolean? IsSideMirrorHeater { get; init; }
            public Boolean? IsWiperBladeHeater { get; init; }
            public Boolean? IsFrontDefrosterOn { get; init; }
            public Boolean? IsRearDefrosterOn { get; init; }
            public Boolean? IsClimateOn { get; init; }
            public Boolean? IsBatteryHeater { get; init; }
            public Boolean? IsBatteryHeaterOn { get; init; }
            public Decimal? ChargeEnergyAdded { get; init; }
            public Decimal? ChargeEnergyUsed { get; init; }
            public Int32? ChargerPhases { get; init; }
            public Int32? ChargerPilotCurrent { get; init; }
            public Int32? ChargerActualCurrent { get; init; }
            public Int32? ChargerPower { get; init; }
            public Int32? ChargerVoltage { get; init; }
            public Decimal? ChargeRate { get; init; }
            public Boolean? IsFastChargerPresent { get; init; }
            public String ChargeCable { get; init; }
            public String FastChargerBrand { get; init; }
            public String FastChargerType { get; init; }

            private Snapshot()
            {
            }

            public Snapshot(TeslaCarData carData, TeslaStreamingData streamingData)
            {
                Location = new Point((Double)(streamingData?.Longitude ?? carData?.DriveState?.Longitude ?? 0), (Double)(streamingData?.Latitude ?? carData?.DriveState?.Latitude ?? 0));
                Elevation = streamingData?.Elevation;
                Speed = streamingData?.Speed?.Mile ?? carData?.DriveState?.Speed?.Mile;
                Heading = streamingData?.EstimateHeading ?? carData?.DriveState?.Heading;
                ShiftState = streamingData?.ShiftState ?? carData?.DriveState?.ShiftState;
                Power = streamingData?.Power ?? carData?.DriveState?.Power;
                Odometer = streamingData?.Odometer?.Mile ?? carData?.CarState?.Odometer?.Mile;
                BatteryLevel = streamingData?.BatteryLevel ?? carData?.ChargeState?.BatteryLevel;
                BatteryRange = streamingData?.BatteryRange?.Mile ?? carData?.ChargeState?.RatedBatteryRange?.Mile;
                OutsideTemperature = carData?.ClimateState?.OutsideTemperature?.Celsius;
                InsideTemperature = carData?.ClimateState?.InsideTemperature?.Celsius;
                DriverTemperatureSetting = carData?.ClimateState?.DriverTemperatureSetting?.Celsius;
                PassengerTemperatureSetting = carData?.ClimateState?.PassengerTemperatureSetting?.Celsius;
                DriverSeatHeater = carData?.ClimateState?.DriverSeatHeater;
                PassengerSeatHeater = carData?.ClimateState?.PassengerSeatHeater;
                FanStatus = carData?.ClimateState?.FanStatus;
                IsSideMirrorHeater = carData?.ClimateState?.IsSideMirrorHeater;
                IsWiperBladeHeater = carData?.ClimateState?.IsWiperBladeHeater;
                IsFrontDefrosterOn = carData?.ClimateState?.IsFrontDefrosterOn;
                IsRearDefrosterOn = carData?.ClimateState?.IsRearDefrosterOn;
                IsClimateOn = carData?.ClimateState?.IsClimateOn;
                IsBatteryHeater = carData?.ClimateState?.IsBatteryHeater;
                IsBatteryHeaterOn = carData?.ChargeState?.IsBatteryHeaterOn;
                ChargeEnergyAdded = carData?.ChargeState?.ChargeEnergyAdded;
                ChargeEnergyUsed = null;
                ChargerPhases = carData?.ChargeState?.ChargerPhases;
                ChargerPilotCurrent = carData?.ChargeState?.ChargerPilotCurrent;
                ChargerActualCurrent = carData?.ChargeState?.ChargerActualCurrent;
                ChargerPower = carData?.ChargeState?.ChargerPower;
                ChargerVoltage = carData?.ChargeState?.ChargerVoltage;
                ChargeRate = carData?.ChargeState?.ChargeRate;
                IsFastChargerPresent = carData?.ChargeState?.IsFastChargerPresent;
                ChargeCable = carData?.ChargeState?.ChargeCable;
                FastChargerBrand = carData?.ChargeState?.FastChargerBrand;
                FastChargerType = carData?.ChargeState?.FastChargerType;
            }

            public Snapshot(SnapshotEntity snapshotEntity)
            {
                Location = snapshotEntity?.Location;
                Elevation = snapshotEntity?.Elevation;
                Speed = snapshotEntity?.Speed;
                Heading = snapshotEntity?.Heading;
                ShiftState = snapshotEntity?.ShiftState;
                Power = snapshotEntity?.Power;
                Odometer = snapshotEntity?.Odometer;
                BatteryLevel = snapshotEntity?.BatteryLevel;
                BatteryRange = snapshotEntity?.BatteryRange;
                OutsideTemperature = snapshotEntity?.OutsideTemperature;
                InsideTemperature = snapshotEntity?.InsideTemperature;
                DriverTemperatureSetting = snapshotEntity?.DriverTemperatureSetting;
                PassengerTemperatureSetting = snapshotEntity?.PassengerTemperatureSetting;
                DriverSeatHeater = snapshotEntity?.DriverSeatHeater;
                PassengerSeatHeater = snapshotEntity?.PassengerSeatHeater;
                FanStatus = snapshotEntity?.FanStatus;
                IsSideMirrorHeater = snapshotEntity?.IsSideMirrorHeater;
                IsWiperBladeHeater = snapshotEntity?.IsWiperBladeHeater;
                IsFrontDefrosterOn = snapshotEntity?.IsFrontDefrosterOn;
                IsRearDefrosterOn = snapshotEntity?.IsRearDefrosterOn;
                IsClimateOn = snapshotEntity?.IsClimateOn;
                IsBatteryHeater = snapshotEntity?.IsBatteryHeater;
                IsBatteryHeaterOn = snapshotEntity?.IsBatteryHeaterOn; 
                ChargeEnergyAdded = snapshotEntity?.ChargeEnergyAdded;
                ChargeEnergyUsed = snapshotEntity?.ChargeEnergyUsed;
                ChargerPhases = snapshotEntity?.ChargerPhases;
                ChargerPilotCurrent = snapshotEntity?.ChargerPilotCurrent;
                ChargerActualCurrent = snapshotEntity?.ChargerActualCurrent;
                ChargerPower = snapshotEntity?.ChargerPower;
                ChargerVoltage = snapshotEntity?.ChargerVoltage;
                ChargeRate = snapshotEntity?.ChargeRate;
            }

            public Snapshot Debounce(Snapshot baseSnapshot)
            {
                if (baseSnapshot == null)
                {
                    return this;
                }

                return new Snapshot()
                {
                    Location = Location ?? baseSnapshot.Location,
                    Elevation = Elevation ?? baseSnapshot.Elevation,
                    Speed = Speed ?? baseSnapshot.Speed,
                    Heading = Heading ?? baseSnapshot.Heading,
                    ShiftState = ShiftState ?? baseSnapshot.ShiftState,
                    Power = Power ?? baseSnapshot.Power,
                    Odometer = Debounce(Odometer, baseSnapshot?.Odometer, 1),
                    BatteryLevel = BatteryLevel ?? baseSnapshot.BatteryLevel,
                    BatteryRange = Debounce(BatteryRange, baseSnapshot.BatteryRange, 1),
                    OutsideTemperature = OutsideTemperature ?? baseSnapshot.OutsideTemperature,
                    InsideTemperature = InsideTemperature ?? baseSnapshot.InsideTemperature,
                    DriverTemperatureSetting = DriverTemperatureSetting ?? baseSnapshot.DriverTemperatureSetting,
                    PassengerTemperatureSetting = PassengerTemperatureSetting ?? baseSnapshot.PassengerTemperatureSetting,
                    DriverSeatHeater = DriverSeatHeater ?? baseSnapshot.DriverSeatHeater,
                    PassengerSeatHeater = PassengerSeatHeater ?? baseSnapshot.PassengerSeatHeater,
                    FanStatus = FanStatus ?? baseSnapshot.FanStatus,
                    IsSideMirrorHeater = IsSideMirrorHeater ?? baseSnapshot.IsSideMirrorHeater,
                    IsWiperBladeHeater = IsWiperBladeHeater ?? baseSnapshot.IsWiperBladeHeater,
                    IsFrontDefrosterOn = IsFrontDefrosterOn ?? baseSnapshot.IsFrontDefrosterOn,
                    IsRearDefrosterOn = IsRearDefrosterOn ?? baseSnapshot.IsRearDefrosterOn,
                    IsClimateOn = IsClimateOn ?? baseSnapshot.IsClimateOn,
                    IsBatteryHeater = IsBatteryHeater ?? baseSnapshot.IsBatteryHeater,
                    IsBatteryHeaterOn = IsBatteryHeaterOn ?? baseSnapshot.IsBatteryHeaterOn,
                    ChargeEnergyAdded = ChargeEnergyAdded ?? baseSnapshot.ChargeEnergyAdded,
                    ChargeEnergyUsed = ChargeEnergyUsed ?? baseSnapshot.ChargeEnergyUsed,
                    ChargerPhases = ChargerPhases ?? baseSnapshot.ChargerPhases,
                    ChargerPilotCurrent = ChargerPilotCurrent ?? baseSnapshot.ChargerPilotCurrent,
                    ChargerActualCurrent = ChargerActualCurrent ?? baseSnapshot.ChargerActualCurrent,
                    ChargerPower = ChargerPower ?? baseSnapshot.ChargerPower,
                    ChargerVoltage = ChargerVoltage ?? baseSnapshot.ChargerVoltage,
                    ChargeRate = ChargeRate ?? baseSnapshot.ChargeRate,
                    IsFastChargerPresent = IsFastChargerPresent ?? baseSnapshot.IsFastChargerPresent,
                    ChargeCable = ChargeCable ?? baseSnapshot.ChargeCable,
                    FastChargerBrand = FastChargerBrand ?? baseSnapshot.FastChargerBrand,
                    FastChargerType = FastChargerType ?? baseSnapshot.FastChargerType,
                };
            }
        }

        public record StandBySnapshot : BaseSnapshot
        {
            public Point Location { get; init; }
            public Decimal? Elevation { get; init; }
            public Decimal? Odometer { get; init; }
            public Decimal? Heading { get; init; }
            public Decimal? Power { get; init; }
            public Decimal? BatteryLevel { get; init; }
            public Decimal? BatteryRange { get; init; }
            public Decimal? OutsideTemperature { get; init; }
            public Decimal? InsideTemperature { get; init; }
            public Decimal? DriverTemperatureSetting { get; init; }
            public Decimal? PassengerTemperatureSetting { get; init; }
            public Int32? DriverSeatHeater { get; init; }
            public Int32? PassengerSeatHeater { get; init; }
            public Int32? FanStatus { get; init; }
            public Boolean? IsSideMirrorHeater { get; init; }
            public Boolean? IsWiperBladeHeater { get; init; }
            public Boolean? IsFrontDefrosterOn { get; init; }
            public Boolean? IsRearDefrosterOn { get; init; }
            public Boolean? IsClimateOn { get; init; }
            public Boolean? IsBatteryHeater { get; init; }
            public Boolean? IsBatteryHeaterOn { get; init; }

            private StandBySnapshot()
            {
            }

            public StandBySnapshot(Snapshot snapshot)
            {
                Location = snapshot?.Location;
                Elevation = snapshot?.Elevation;
                Heading = snapshot?.Heading;
                Odometer = snapshot?.Odometer;
                Power = snapshot?.Power;
                BatteryLevel = snapshot?.BatteryLevel;
                BatteryRange = snapshot?.BatteryRange;
                OutsideTemperature = snapshot?.OutsideTemperature;
                InsideTemperature = snapshot?.InsideTemperature;
                DriverTemperatureSetting = snapshot?.DriverTemperatureSetting;
                PassengerTemperatureSetting = snapshot?.PassengerTemperatureSetting;
                DriverSeatHeater = snapshot?.DriverSeatHeater;
                PassengerSeatHeater = snapshot?.PassengerSeatHeater;
                FanStatus = snapshot?.FanStatus;
                IsSideMirrorHeater = snapshot?.IsSideMirrorHeater;
                IsWiperBladeHeater = snapshot?.IsWiperBladeHeater;
                IsFrontDefrosterOn = snapshot?.IsFrontDefrosterOn;
                IsRearDefrosterOn = snapshot?.IsRearDefrosterOn;
                IsClimateOn = snapshot?.IsClimateOn;
                IsBatteryHeater = snapshot?.IsBatteryHeater;
                IsBatteryHeaterOn = snapshot?.IsBatteryHeaterOn;
            }

            public StandBySnapshot(StandBySnapshotEntity standBySnapshotEntity)
            {
                Location = standBySnapshotEntity?.Location;
                Elevation = standBySnapshotEntity?.Elevation;
                Heading = standBySnapshotEntity?.Heading;
                Odometer = standBySnapshotEntity?.Odometer;
                Power = standBySnapshotEntity?.Power;
                BatteryLevel = standBySnapshotEntity?.BatteryLevel;
                BatteryRange = standBySnapshotEntity?.BatteryRange;
                OutsideTemperature = standBySnapshotEntity?.OutsideTemperature;
                InsideTemperature = standBySnapshotEntity?.InsideTemperature;
                DriverTemperatureSetting = standBySnapshotEntity?.DriverTemperatureSetting;
                PassengerTemperatureSetting = standBySnapshotEntity?.PassengerTemperatureSetting;
                DriverSeatHeater = standBySnapshotEntity?.DriverSeatHeater;
                PassengerSeatHeater = standBySnapshotEntity?.PassengerSeatHeater;
                FanStatus = standBySnapshotEntity?.FanStatus;
                IsSideMirrorHeater = standBySnapshotEntity?.IsSideMirrorHeater;
                IsWiperBladeHeater = standBySnapshotEntity?.IsWiperBladeHeater;
                IsFrontDefrosterOn = standBySnapshotEntity?.IsFrontDefrosterOn;
                IsRearDefrosterOn = standBySnapshotEntity?.IsRearDefrosterOn;
                IsClimateOn = standBySnapshotEntity?.IsClimateOn;
                IsBatteryHeater = standBySnapshotEntity?.IsBatteryHeater;
                IsBatteryHeaterOn = standBySnapshotEntity?.IsBatteryHeaterOn;
            }

            public StandBySnapshot Debounce(StandBySnapshot baseSnapshot)
            {
                if (baseSnapshot == null)
                {
                    return this;
                }

                return new StandBySnapshot()
                {
                    Location = Location ?? baseSnapshot.Location,
                    Elevation = Elevation ?? baseSnapshot.Elevation,
                    Heading = Heading ?? baseSnapshot.Heading,
                    Odometer = Debounce(Odometer, baseSnapshot?.Odometer, 1),
                    Power = Power ?? baseSnapshot.Power,
                    BatteryLevel = BatteryLevel ?? baseSnapshot.BatteryLevel,
                    BatteryRange = Debounce(BatteryRange, baseSnapshot.BatteryRange, 1),
                    OutsideTemperature = OutsideTemperature ?? baseSnapshot.OutsideTemperature,
                    InsideTemperature = InsideTemperature ?? baseSnapshot.InsideTemperature,
                    DriverTemperatureSetting = DriverTemperatureSetting ?? baseSnapshot.DriverTemperatureSetting,
                    PassengerTemperatureSetting = PassengerTemperatureSetting ?? baseSnapshot.PassengerTemperatureSetting,
                    DriverSeatHeater = DriverSeatHeater ?? baseSnapshot.DriverSeatHeater,
                    PassengerSeatHeater = PassengerSeatHeater ?? baseSnapshot.PassengerSeatHeater,
                    FanStatus = FanStatus ?? baseSnapshot.FanStatus,
                    IsSideMirrorHeater = IsSideMirrorHeater ?? baseSnapshot.IsSideMirrorHeater,
                    IsWiperBladeHeater = IsWiperBladeHeater ?? baseSnapshot.IsWiperBladeHeater,
                    IsFrontDefrosterOn = IsFrontDefrosterOn ?? baseSnapshot.IsFrontDefrosterOn,
                    IsRearDefrosterOn = IsRearDefrosterOn ?? baseSnapshot.IsRearDefrosterOn,
                    IsClimateOn = IsClimateOn ?? baseSnapshot.IsClimateOn,
                    IsBatteryHeater = IsBatteryHeater ?? baseSnapshot.IsBatteryHeater,
                    IsBatteryHeaterOn = IsBatteryHeaterOn ?? baseSnapshot.IsBatteryHeaterOn,
                };
            }
        }

        public record DrivingSnapshot : BaseSnapshot
        {
            public Point Location { get; init; }
            public Decimal? Elevation { get; init; }
            public Decimal? Speed { get; init; }
            public Decimal? Heading { get; init; }
            public ShiftState? ShiftState { get; init; }
            public Decimal? Power { get; init; }
            public Decimal? Odometer { get; init; }
            public Decimal? BatteryLevel { get; init; }
            public Decimal? BatteryRange { get; init; }
            public Decimal? OutsideTemperature { get; init; }
            public Decimal? InsideTemperature { get; init; }
            public Decimal? DriverTemperatureSetting { get; init; }
            public Decimal? PassengerTemperatureSetting { get; init; }
            public Int32? DriverSeatHeater { get; init; }
            public Int32? PassengerSeatHeater { get; init; }
            public Int32? FanStatus { get; init; }
            public Boolean? IsSideMirrorHeater { get; init; }
            public Boolean? IsWiperBladeHeater { get; init; }
            public Boolean? IsFrontDefrosterOn { get; init; }
            public Boolean? IsRearDefrosterOn { get; init; }
            public Boolean? IsClimateOn { get; init; }
            public Boolean? IsBatteryHeater { get; init; }
            public Boolean? IsBatteryHeaterOn { get; init; }

            private DrivingSnapshot()
            {
            }

            public DrivingSnapshot(Snapshot snapshot)
            {
                Location = snapshot?.Location;
                Elevation = snapshot?.Elevation ?? null;
                Speed = snapshot?.Speed;
                Heading = snapshot?.Heading;
                ShiftState = snapshot?.ShiftState;
                Power = snapshot?.Power;
                Odometer = snapshot?.Odometer;
                BatteryLevel = snapshot?.BatteryLevel;
                BatteryRange = snapshot?.BatteryRange;
                OutsideTemperature = snapshot?.OutsideTemperature;
                InsideTemperature = snapshot?.InsideTemperature;
                DriverTemperatureSetting = snapshot?.DriverTemperatureSetting;
                PassengerTemperatureSetting = snapshot?.PassengerTemperatureSetting;
                DriverSeatHeater = snapshot?.DriverSeatHeater;
                PassengerSeatHeater = snapshot?.PassengerSeatHeater;
                FanStatus = snapshot?.FanStatus;
                IsSideMirrorHeater = snapshot?.IsSideMirrorHeater;
                IsWiperBladeHeater = snapshot?.IsWiperBladeHeater;
                IsFrontDefrosterOn = snapshot?.IsFrontDefrosterOn;
                IsRearDefrosterOn = snapshot?.IsRearDefrosterOn;
                IsClimateOn = snapshot?.IsClimateOn;
                IsBatteryHeater = snapshot?.IsBatteryHeater;
                IsBatteryHeaterOn = snapshot?.IsBatteryHeaterOn;
            }

            public DrivingSnapshot(DrivingSnapshotEntity drivingSnapshotEntity)
            {
                Location = drivingSnapshotEntity?.Location;
                Elevation = drivingSnapshotEntity?.Elevation;
                Speed = drivingSnapshotEntity?.Speed;
                Heading = drivingSnapshotEntity?.Heading;
                ShiftState = drivingSnapshotEntity?.ShiftState;
                Power = drivingSnapshotEntity?.Power;
                Odometer = drivingSnapshotEntity?.Odometer;
                BatteryLevel = drivingSnapshotEntity?.BatteryLevel;
                BatteryRange = drivingSnapshotEntity?.BatteryRange;
                OutsideTemperature = drivingSnapshotEntity?.OutsideTemperature;
                InsideTemperature = drivingSnapshotEntity?.InsideTemperature;
                DriverTemperatureSetting = drivingSnapshotEntity?.DriverTemperatureSetting;
                PassengerTemperatureSetting = drivingSnapshotEntity?.PassengerTemperatureSetting;
                DriverSeatHeater = drivingSnapshotEntity?.DriverSeatHeater;
                PassengerSeatHeater = drivingSnapshotEntity?.PassengerSeatHeater;
                FanStatus = drivingSnapshotEntity?.FanStatus;
                IsSideMirrorHeater = drivingSnapshotEntity?.IsSideMirrorHeater;
                IsWiperBladeHeater = drivingSnapshotEntity?.IsWiperBladeHeater;
                IsFrontDefrosterOn = drivingSnapshotEntity?.IsFrontDefrosterOn;
                IsRearDefrosterOn = drivingSnapshotEntity?.IsRearDefrosterOn;
                IsClimateOn = drivingSnapshotEntity?.IsClimateOn;
                IsBatteryHeater = drivingSnapshotEntity?.IsBatteryHeater;
                IsBatteryHeaterOn = drivingSnapshotEntity?.IsBatteryHeaterOn;
            }

            public DrivingSnapshot Debounce(DrivingSnapshot baseSnapshot)
            {
                if (baseSnapshot == null)
                {
                    return this;
                }

                return new DrivingSnapshot()
                {
                    Location = Location ?? baseSnapshot.Location,
                    Elevation = Elevation ?? baseSnapshot.Elevation,
                    Speed = Speed ?? baseSnapshot.Speed,
                    Heading = Heading ?? baseSnapshot.Heading,
                    ShiftState = ShiftState ?? baseSnapshot.ShiftState,
                    Power = Power ?? baseSnapshot.Power,
                    Odometer = Debounce(Odometer, baseSnapshot?.Odometer, 1),
                    BatteryLevel = BatteryLevel ?? baseSnapshot.BatteryLevel,
                    BatteryRange = Debounce(BatteryRange, baseSnapshot.BatteryRange, 1),
                    OutsideTemperature = OutsideTemperature ?? baseSnapshot.OutsideTemperature,
                    InsideTemperature = InsideTemperature ?? baseSnapshot.InsideTemperature,
                    DriverTemperatureSetting = DriverTemperatureSetting ?? baseSnapshot.DriverTemperatureSetting,
                    PassengerTemperatureSetting = PassengerTemperatureSetting ?? baseSnapshot.PassengerTemperatureSetting,
                    DriverSeatHeater = DriverSeatHeater ?? baseSnapshot.DriverSeatHeater,
                    PassengerSeatHeater = PassengerSeatHeater ?? baseSnapshot.PassengerSeatHeater,
                    FanStatus = FanStatus ?? baseSnapshot.FanStatus,
                    IsSideMirrorHeater = IsSideMirrorHeater ?? baseSnapshot.IsSideMirrorHeater,
                    IsWiperBladeHeater = IsWiperBladeHeater ?? baseSnapshot.IsWiperBladeHeater,
                    IsFrontDefrosterOn = IsFrontDefrosterOn ?? baseSnapshot.IsFrontDefrosterOn,
                    IsRearDefrosterOn = IsRearDefrosterOn ?? baseSnapshot.IsRearDefrosterOn,
                    IsClimateOn = IsClimateOn ?? baseSnapshot.IsClimateOn,
                    IsBatteryHeater = IsBatteryHeater ?? baseSnapshot.IsBatteryHeater,
                    IsBatteryHeaterOn = IsBatteryHeaterOn ?? baseSnapshot.IsBatteryHeaterOn,
                };
            }
        }

        public record ChargingSnapshot : BaseSnapshot
        {
            public Point Location { get; init; }
            public Decimal? Elevation { get; init; }
            public Decimal? Odometer { get; init; }
            public Decimal? Heading { get; init; }
            public Decimal? BatteryLevel { get; init; }
            public Decimal? BatteryRange { get; init; }
            public Decimal? OutsideTemperature { get; init; }
            public Decimal? InsideTemperature { get; init; }
            public Decimal? DriverTemperatureSetting { get; init; }
            public Decimal? PassengerTemperatureSetting { get; init; }
            public Int32? DriverSeatHeater { get; init; }
            public Int32? PassengerSeatHeater { get; init; }
            public Int32? FanStatus { get; init; }
            public Boolean? IsSideMirrorHeater { get; init; }
            public Boolean? IsWiperBladeHeater { get; init; }
            public Boolean? IsFrontDefrosterOn { get; init; }
            public Boolean? IsRearDefrosterOn { get; init; }
            public Boolean? IsClimateOn { get; init; }
            public Boolean? IsBatteryHeater { get; init; }
            public Boolean? IsBatteryHeaterOn { get; init; }
            public Decimal? ChargeEnergyAdded { get; init; }
            public Int32? ChargerPhases { get; init; }
            public Int32? ChargerPilotCurrent { get; init; }
            public Int32? ChargerActualCurrent { get; init; }
            public Int32? ChargerPower { get; init; }
            public Int32? ChargerVoltage { get; init; }
            public Decimal? ChargeRate { get; init; }
            public Boolean? IsFastChargerPresent { get; set; }
            public String ChargeCable { get; set; }
            public String FastChargerBrand { get; set; }
            public String FastChargerType { get; set; }

            private ChargingSnapshot()
            {
            }

            public ChargingSnapshot(Snapshot snapshot)
            {
                Location = snapshot?.Location;
                Elevation = snapshot?.Elevation;
                Heading = snapshot?.Heading;
                Odometer = snapshot?.Odometer;
                BatteryLevel = snapshot?.BatteryLevel;
                BatteryRange = snapshot?.BatteryRange;
                OutsideTemperature = snapshot?.OutsideTemperature;
                InsideTemperature = snapshot?.InsideTemperature;
                DriverTemperatureSetting = snapshot?.DriverTemperatureSetting;
                PassengerTemperatureSetting = snapshot?.PassengerTemperatureSetting;
                DriverSeatHeater = snapshot?.DriverSeatHeater;
                PassengerSeatHeater = snapshot?.PassengerSeatHeater;
                FanStatus = snapshot?.FanStatus;
                IsSideMirrorHeater = snapshot?.IsSideMirrorHeater;
                IsWiperBladeHeater = snapshot?.IsWiperBladeHeater;
                IsFrontDefrosterOn = snapshot?.IsFrontDefrosterOn;
                IsRearDefrosterOn = snapshot?.IsRearDefrosterOn;
                IsClimateOn = snapshot?.IsClimateOn;
                IsBatteryHeater = snapshot?.IsBatteryHeater;
                IsBatteryHeaterOn = snapshot?.IsBatteryHeaterOn;
                ChargeEnergyAdded = snapshot?.ChargeEnergyAdded;
                ChargerPhases = snapshot?.ChargerPhases;
                ChargerPilotCurrent = snapshot?.ChargerPilotCurrent;
                ChargerActualCurrent = snapshot?.ChargerActualCurrent;
                ChargerPower = snapshot?.ChargerPower;
                ChargerVoltage = snapshot?.ChargerVoltage;
                ChargeRate = snapshot?.ChargeRate;
                IsFastChargerPresent = snapshot?.IsFastChargerPresent;
                ChargeCable = snapshot?.ChargeCable;
                FastChargerBrand = snapshot?.FastChargerBrand;
                FastChargerType = snapshot?.FastChargerType;
            }

            public ChargingSnapshot(ChargingSnapshotEntity ChargingSnapshotEntity)
            {
                Location = ChargingSnapshotEntity?.Location;
                Elevation = ChargingSnapshotEntity?.Elevation;
                Heading = ChargingSnapshotEntity?.Heading;
                Odometer = ChargingSnapshotEntity?.Odometer;
                BatteryLevel = ChargingSnapshotEntity?.BatteryLevel;
                BatteryRange = ChargingSnapshotEntity?.BatteryRange;
                OutsideTemperature = ChargingSnapshotEntity?.OutsideTemperature;
                InsideTemperature = ChargingSnapshotEntity?.InsideTemperature;
                DriverTemperatureSetting = ChargingSnapshotEntity?.DriverTemperatureSetting;
                PassengerTemperatureSetting = ChargingSnapshotEntity?.PassengerTemperatureSetting;
                DriverSeatHeater = ChargingSnapshotEntity?.DriverSeatHeater;
                PassengerSeatHeater = ChargingSnapshotEntity?.PassengerSeatHeater;
                FanStatus = ChargingSnapshotEntity?.FanStatus;
                IsSideMirrorHeater = ChargingSnapshotEntity?.IsSideMirrorHeater;
                IsWiperBladeHeater = ChargingSnapshotEntity?.IsWiperBladeHeater;
                IsFrontDefrosterOn = ChargingSnapshotEntity?.IsFrontDefrosterOn;
                IsRearDefrosterOn = ChargingSnapshotEntity?.IsRearDefrosterOn;
                IsClimateOn = ChargingSnapshotEntity?.IsClimateOn;
                IsBatteryHeater = ChargingSnapshotEntity?.IsBatteryHeater;
                IsBatteryHeaterOn = ChargingSnapshotEntity?.IsBatteryHeaterOn;
                ChargeEnergyAdded = ChargingSnapshotEntity?.ChargeEnergyAdded;
                ChargerPhases = ChargingSnapshotEntity?.ChargerPhases;
                ChargerPilotCurrent = ChargingSnapshotEntity?.ChargerPilotCurrent;
                ChargerActualCurrent = ChargingSnapshotEntity?.ChargerActualCurrent;
                ChargerPower = ChargingSnapshotEntity?.ChargerPower;
                ChargerVoltage = ChargingSnapshotEntity?.ChargerVoltage;
                ChargeRate = ChargingSnapshotEntity?.ChargeRate;
                IsFastChargerPresent = ChargingSnapshotEntity?.IsFastChargerPresent;
                ChargeCable = ChargingSnapshotEntity?.ChargeCable;
                FastChargerBrand = ChargingSnapshotEntity?.FastChargerBrand;
                FastChargerType = ChargingSnapshotEntity?.FastChargerType;
            }

            public ChargingSnapshot Debounce(ChargingSnapshot baseSnapshot)
            {
                if (baseSnapshot == null)
                {
                    return this;
                }

                return new ChargingSnapshot()
                {
                    Location = Location ?? baseSnapshot.Location,
                    Elevation = Elevation ?? baseSnapshot.Elevation,
                    Heading = Heading ?? baseSnapshot.Heading,
                    Odometer = Debounce(Odometer, baseSnapshot?.Odometer, 1),
                    BatteryLevel = BatteryLevel ?? baseSnapshot.BatteryLevel,
                    BatteryRange = Debounce(BatteryRange, baseSnapshot.BatteryRange, 1),
                    OutsideTemperature = OutsideTemperature ?? baseSnapshot.OutsideTemperature,
                    InsideTemperature = InsideTemperature ?? baseSnapshot.InsideTemperature,
                    DriverTemperatureSetting = DriverTemperatureSetting ?? baseSnapshot.DriverTemperatureSetting,
                    PassengerTemperatureSetting = PassengerTemperatureSetting ?? baseSnapshot.PassengerTemperatureSetting,
                    DriverSeatHeater = DriverSeatHeater ?? baseSnapshot.DriverSeatHeater,
                    PassengerSeatHeater = PassengerSeatHeater ?? baseSnapshot.PassengerSeatHeater,
                    FanStatus = FanStatus ?? baseSnapshot.FanStatus,
                    IsSideMirrorHeater = IsSideMirrorHeater ?? baseSnapshot.IsSideMirrorHeater,
                    IsWiperBladeHeater = IsWiperBladeHeater ?? baseSnapshot.IsWiperBladeHeater,
                    IsFrontDefrosterOn = IsFrontDefrosterOn ?? baseSnapshot.IsFrontDefrosterOn,
                    IsRearDefrosterOn = IsRearDefrosterOn ?? baseSnapshot.IsRearDefrosterOn,
                    IsClimateOn = IsClimateOn ?? baseSnapshot.IsClimateOn,
                    IsBatteryHeater = IsBatteryHeater ?? baseSnapshot.IsBatteryHeater,
                    IsBatteryHeaterOn = IsBatteryHeaterOn ?? baseSnapshot.IsBatteryHeaterOn,
                    ChargeEnergyAdded = ChargeEnergyAdded ?? baseSnapshot.ChargeEnergyAdded,
                    ChargerPhases = ChargerPhases ?? baseSnapshot.ChargerPhases,
                    ChargerPilotCurrent = ChargerPilotCurrent ?? baseSnapshot.ChargerPilotCurrent,
                    ChargerActualCurrent = ChargerActualCurrent ?? baseSnapshot.ChargerActualCurrent,
                    ChargerPower = ChargerPower ?? baseSnapshot.ChargerPower,
                    ChargerVoltage = ChargerVoltage ?? baseSnapshot.ChargerVoltage,
                    ChargeRate = ChargeRate ?? baseSnapshot.ChargeRate,
                    IsFastChargerPresent = IsFastChargerPresent ?? baseSnapshot.IsFastChargerPresent,
                    ChargeCable = ChargeCable ?? baseSnapshot.ChargeCable,
                    FastChargerBrand = FastChargerBrand ?? baseSnapshot.FastChargerBrand,
                    FastChargerType = FastChargerType ?? baseSnapshot.FastChargerType,
                };
            }
        }
    }
}
