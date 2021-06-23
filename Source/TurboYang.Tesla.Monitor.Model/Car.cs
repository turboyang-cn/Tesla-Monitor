using System;
using System.Text.Json.Serialization;

using NodaTime;

namespace TurboYang.Tesla.Monitor.Model
{
    public record Car
    {
        [JsonPropertyName("openId")]
        public Guid? OpenId { get; init; }
        [JsonPropertyName("carId")]
        public String CarId { get; init; }
        [JsonPropertyName("vehicleId")]
        public Int64? VehicleId { get; init; }
        [JsonPropertyName("vin")]
        public String Vin { get; init; }
        [JsonPropertyName("name")]
        public String Name { get; init; }
        [JsonPropertyName("Type")]
        public CarType? Type { get; init; } 
        [JsonPropertyName("state")]
        public CarState? State { get; init; }
        [JsonPropertyName("isInService")]
        public Boolean? IsInService { get; init; }
        [JsonPropertyName("exteriorColor")]
        public String ExteriorColor { get; init; }
        [JsonPropertyName("wheelType")]
        public String WheelType { get; init; }
        [JsonPropertyName("createBy")]
        public String CreateBy { get; init; }
        [JsonPropertyName("updateBy")]
        public String UpdateBy { get; init; }
        [JsonPropertyName("createTimestamp")]
        public Instant? CreateTimestamp { get; init; }
        [JsonPropertyName("updateTimestamp")]
        public Instant? UpdateTimestamp { get; init; }
    }
}
