using NpgsqlTypes;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Model
{
    [PgName("CarState")]
    public enum CarState
    {
        [PgName("Online")]
        [EnumString("Online", "online")]
        Online = 1,
        [PgName("Asleep")]
        [EnumString("Asleep", "asleep")]
        Asleep = 2,
        [PgName("Offline")]
        [EnumString("Offline", "offline")]
        Offline = 3,
        [PgName("Driving")]
        [EnumString("Driving", "driving")]
        Driving = 4,
        [PgName("Charging")]
        [EnumString("Charging", "charging")]
        Charging = 5,
    }
}
