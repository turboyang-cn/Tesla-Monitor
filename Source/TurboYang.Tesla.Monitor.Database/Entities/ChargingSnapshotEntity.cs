using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;

using NodaTime;

using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class ChargingSnapshotEntity : BaseEntity
    {
        public Boolean? IsFastChargerPresent { get; set; }
        public String ChargeCable { get; set; }
        public String FastChargerBrand { get; set; }
        public String FastChargerType { get; set; }
        public Point Location { get; set; }
        public Decimal? Elevation { get; set; }
        public Decimal? Odometer { get; set; }
        public Decimal? Heading { get; set; }
        public Decimal? BatteryLevel { get; set; }
        public Decimal? BatteryRange { get; set; }
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
        public Decimal? ChargeEnergyAdded { get; set; }
        public Int32? ChargerPhases { get; set; }
        public Int32? ChargerPilotCurrent { get; set; }
        public Int32? ChargerActualCurrent { get; set; }
        public Int32? ChargerPower { get; set; }
        public Int32? ChargerVoltage { get; set; }
        public Decimal? ChargeRate { get; set; }
        public Instant? Timestamp { get; set; }

        public Int32? ChargingId { get; set; }
        public virtual ChargingEntity Charging { get; set; }
    }
}
