using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NodaTime;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public abstract class BaseEntity
    {
        public Int32? Id { get; set; }
        public Guid? OpenId { get; set; }
        public String CreateBy { get; set; }
        public String UpdateBy { get; set; }
        public Instant? CreateTimestamp { get; set; }
        public Instant? UpdateTimestamp { get; set; }
    }
}
