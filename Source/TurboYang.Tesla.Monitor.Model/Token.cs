using System;
using System.Text.Json.Serialization;

using NodaTime;

namespace TurboYang.Tesla.Monitor.Model
{
    public record Token
    {
        [JsonPropertyName("openId")]
        public Guid? OpenId { get; init; }
        [JsonPropertyName("username")]
        public String Username { get; init; }
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
