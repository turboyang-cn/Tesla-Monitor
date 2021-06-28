using System;

using NodaTime;

using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class FirewareEntity : BaseEntity
    {
        public String Version { get; set; }
        public Instant? Timestamp { get; set; }
        public FirewareState? State { get; set; }

        public Int32? CarId { get; set; }
        public virtual CarEntity Car { get; set; }
    }
}
