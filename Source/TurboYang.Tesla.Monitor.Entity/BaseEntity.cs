using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NodaTime;

namespace TurboYang.Tesla.Monitor.Entity
{
    public class BaseEntity
    {
        public Int32 Id { get; set; }
        public Guid OpenId { get; set; }
        public String CreateBy { get; set; }
        public String UpdateBy { get; set; }
        public ZonedDateTime CreateTimestamp { get; set; }
        public ZonedDateTime UpdateTimestamp { get; set; }
    }
}
