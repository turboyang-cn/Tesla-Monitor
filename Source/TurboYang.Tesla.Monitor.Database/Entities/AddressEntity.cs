using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;

namespace TurboYang.Tesla.Monitor.Database.Entities
{
    public class AddressEntity : BaseEntity
    {
        public Point Location { get; set; }
        public Decimal Radius { get; set; }
        public String Name { get; set; }
        public String Postcode { get; set; }
        public String Country { get; set; }
        public String State { get; set; }
        public String County { get; set; }
        public String City { get; set; }
        public String District { get; set; }
        public String Village { get; set; }
        public String Road { get; set; }
        public String Building { get; set; }
    }
}
