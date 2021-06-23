using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboYang.Tesla.Monitor.Database.Functions
{
    public class DatabaseFunction
    {
        public DatabaseFunction(DatabaseContext context)
        {
            Context = context;
        }

        public DatabaseContext Context { get; }
    }
}
