
using NpgsqlTypes;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Model
{
    [PgName("FirewareState")]
    public enum FirewareState
    {
        [PgName("Pending")]
        [EnumString("Pending")]
        Pending = 1,
        [PgName("Updated")]
        [EnumString("Updated")]
        Updated = 2,
    }
}
