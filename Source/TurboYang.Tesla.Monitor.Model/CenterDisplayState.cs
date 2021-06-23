
using NpgsqlTypes;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Model
{
    [PgName("CenterDisplayState")]
    public enum CenterDisplayState
    {
        [PgName("Off")]
        [EnumString("Off")]
        Off = 0,
        [PgName("Standby")]
        [EnumString("Standby")]
        Standby = 2,
        [PgName("Charging")]
        [EnumString("Charging")]
        Charging = 3,
        [PgName("On")]
        [EnumString("On")]
        On = 4,
        [PgName("BigCharging")]
        [EnumString("BigCharging")]
        BigCharging = 5,
        [PgName("ReadyToUnlock")]
        [EnumString("ReadyToUnlock")]
        ReadyToUnlock = 6,
        [PgName("SentryMode")]
        [EnumString("SentryMode")]
        SentryMode = 7,
        [PgName("DogMode")]
        [EnumString("DogMode")]
        DogMode = 8,
        [PgName("Media")]
        [EnumString("Media")]
        Media = 9,
    }
}
