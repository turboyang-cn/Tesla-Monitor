using NpgsqlTypes;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Model
{
    [PgName("CarType")]
    public enum CarType
    {
        [PgName("Model3")]
        [EnumString("Model3", "model3")]
        Model3 = 1,
    }
}
