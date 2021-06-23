using System.Runtime.Serialization;

using NpgsqlTypes;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Model
{
    [PgName("SwitchState")]
    public enum SwitchState
    {
        [PgName("Off")]
        [EnumString("Off")]
        Off = 0,
        [PgName("On")]
        [EnumString("On")]
        On = 1,
    }
}
