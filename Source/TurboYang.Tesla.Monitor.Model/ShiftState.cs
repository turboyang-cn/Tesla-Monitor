
using NpgsqlTypes;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Model
{
    [PgName("ShiftState")]
    public enum ShiftState
    {
        [PgName("P")]
        [EnumString("P")]
        P = 1,
        [PgName("D")]
        [EnumString("D")]
        D = 2,
        [PgName("N")]
        [EnumString("N")]
        N = 3,
        [PgName("R")]
        [EnumString("R")]
        R = 4,
    }
}
