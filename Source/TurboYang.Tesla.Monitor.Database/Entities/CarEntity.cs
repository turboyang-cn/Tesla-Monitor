using System;

using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class CarEntity : BaseEntity
    {
        public String CarId { get; set; }
        public Int64? VehicleId { get; set; }
        public CarType? Type { get; set; }
        public String Name { get; set; }
        public String Vin { get; set; }
        public String ExteriorColor { get; set; }
        public String WheelType { get; set; }

        public Int32? TokenId { get; set; }
        public virtual TokenEntity Token { get; set; }
        public virtual CarSettingEntity CarSetting { get; set; }
    }
}
