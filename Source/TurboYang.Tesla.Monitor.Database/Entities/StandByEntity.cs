using System;

using NetTopologySuite.Geometries;

using NodaTime;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class StandByEntity : BaseEntity
    {
        public Instant? StartTimestamp { get; set; }
        public Instant? EndTimestamp { get; set; }
        public Decimal? StartBatteryLevel { get; set; }
        public Decimal? EndBatteryLevel { get; set; }
        public Decimal? StartPower { get; set; }
        public Decimal? EndPower { get; set; }
        public Decimal? StartIdealBatteryRange { get; set; }
        public Decimal? EndIdealBatteryRange { get; set; }
        public Decimal? StartRatedBatteryRange { get; set; }
        public Decimal? EndRatedBatteryRange { get; set; }
        public Point Location { get; set; }
        public Decimal? Elevation { get; set; }
        public Decimal? Heading { get; set; }
        public Decimal? Odometer { get; set; }
        public Decimal? Duration { get; set; }
        public Decimal? OnlineRatio { get; set; }

        public Int32? CarId { get; set; }
        public virtual CarEntity Car { get; set; }
        public Int32? AddressId { get; set; }
        public virtual AddressEntity Address { get; set; }
    }
}
