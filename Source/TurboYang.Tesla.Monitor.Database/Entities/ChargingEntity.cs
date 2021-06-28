using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql.NetTopologySuite;

using NodaTime;
using NetTopologySuite.Geometries;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class ChargingEntity : BaseEntity
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
        public Decimal? Elevation { get; set; }
        public Decimal? Heading { get; set; }
        public Decimal? Odometer { get; set; }
        public Boolean? IsFastChargerPresent { get; set; }
        public String ChargeCable { get; set; }
        public String FastChargerBrand { get; set; }
        public String FastChargerType { get; set; }
        public Decimal? ChargeEnergyAdded { get; set; }
        public Decimal? ChargeEnergyUsed { get; set; }
        public Decimal? Efficiency { get; set; }
        public Decimal? Duration { get; set; }

        public Int32? CarId { get; set; }
        public virtual CarEntity Car { get; set; }
        public Int32? AddressId { get; set; }
        public virtual AddressEntity Address { get; set; }
    }
}
