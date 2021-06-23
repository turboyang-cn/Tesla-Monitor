using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NodaTime;

using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class StateEntity : BaseEntity
    {
        public CarState? State { get; set; }
        public Instant? StartTimestamp { get; set; }
        public Instant? EndTimestamp { get; set; }

        public Int32? CarId { get; set; }
        public virtual CarEntity Car { get; set; }
    }
}
