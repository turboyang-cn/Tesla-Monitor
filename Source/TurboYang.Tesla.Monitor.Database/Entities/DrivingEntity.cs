using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;

using NodaTime;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class DrivingEntity : BaseEntity
    {
        public Instant? StartTimestamp { get; set; }
        public Instant? EndTimestamp { get; set; }
        public Decimal? StartBatteryLevel { get; set; }
        public Decimal? EndBatteryLevel { get; set; }
        public Decimal? StartPower { get; set; }
        public Decimal? EndPower { get; set; }
        public Decimal? StartBatteryRange { get; set; }
        public Decimal? EndBatteryRange { get; set; }
        public Decimal? StartOdometer { get; set; }
        public Decimal? EndOdometer { get; set; }
        public Decimal? Distance { get; set; }
        public Decimal? Duration { get; set; }
        public Decimal? SpeedAverage { get; set; }
        public Decimal? OutsideTemperatureAverage { get; set; }
        public Decimal? InsideTemperatureAverage { get; set; }

        public Int32? CarId { get; set; }
        public virtual CarEntity Car { get; set; }
        public Int32? StartAddressId { get; set; }
        public virtual AddressEntity StartAddress { get; set; }
        public Int32? EndAddressId { get; set; }
        public virtual AddressEntity EndAddress { get; set; }
    }
}
