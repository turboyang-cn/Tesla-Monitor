using System;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class CarSettingEntity : BaseEntity
    {
        public Int32? SamplingRate { get; set; }
        public Boolean? IsSamplingCompression { get; set; }
        public Int32? TryAsleepDelay { get; set; }

        public Int32? CarId { get; set; }
        public virtual CarEntity Car { get; set; }
    }
}
