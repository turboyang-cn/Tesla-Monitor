using System;

using NetTopologySuite.Geometries;

using NodaTime;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class StandBySnapshotEntity : BaseEntity
    {
        public Point Location { get; set; }
        public Decimal? Elevation { get; set; }
        public Decimal? Odometer { get; set; }
        public Decimal? Heading { get; set; }
        public Decimal? Power { get; set; }
        public Decimal? BatteryLevel { get; set; }
        public Decimal? IdealBatteryRange { get; set; }
        public Decimal? RatedBatteryRange { get; set; }
        public Decimal? OutsideTemperature { get; set; }
        public Decimal? InsideTemperature { get; set; }
        public Decimal? DriverTemperatureSetting { get; set; }
        public Decimal? PassengerTemperatureSetting { get; set; }
        public Int32? DriverSeatHeater { get; set; }
        public Int32? PassengerSeatHeater { get; set; }
        public Int32? FanStatus { get; set; }
        public Boolean? IsSideMirrorHeater { get; set; }
        public Boolean? IsWiperBladeHeater { get; set; }
        public Boolean? IsFrontDefrosterOn { get; set; }
        public Boolean? IsRearDefrosterOn { get; set; }
        public Boolean? IsClimateOn { get; set; }
        public Boolean? IsBatteryHeater { get; set; }
        public Boolean? IsBatteryHeaterOn { get; set; }
        public Instant? Timestamp { get; set; }

        public Int32? StandById { get; set; }
        public virtual StandByEntity StandBy { get; set; }
    }
}
