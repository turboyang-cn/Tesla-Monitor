using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboYang.Tesla.Monitor.Model
{
    public record Distance
    {
        public Distance()
        {
            Kilometre = 0;
        }

        public Decimal Kilometre { get; set; }

        public Decimal Mile
        {
            get
            {
                return Kilometre * 15625 / 25146;
            }
            set
            {
                Kilometre = (value * 25146) / 15625m;
            }
        }

        public override String ToString()
        {
            return $"{Kilometre} km | {Mile} mi";
        }
    }
}
