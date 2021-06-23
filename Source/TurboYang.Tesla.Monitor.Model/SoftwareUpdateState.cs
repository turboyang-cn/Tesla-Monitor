using NpgsqlTypes;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Model
{
    [PgName("SoftwareUpdateState")]
    public enum SoftwareUpdateState
    {
        [PgName("Unavailable")]
        [EnumString("Unavailable", "")]
        Unavailable = 0,
        [PgName("DownloadingWifiWait")]
        [EnumString("DownloadingWifiWait", "downloading_wifi_wait")]
        DownloadingWifiWait = 1,
        [PgName("Downloading")]
        [EnumString("Downloading", "downloading")]
        Downloading = 2,
        [PgName("Available")]
        [EnumString("Available", "available")]
        Available = 3,
        [PgName("Scheduled")]
        [EnumString("Scheduled", "scheduled")]
        Scheduled = 4,
        [PgName("Installing")]
        [EnumString("Installing", "installing")]
        Installing = 5,
    }
}
