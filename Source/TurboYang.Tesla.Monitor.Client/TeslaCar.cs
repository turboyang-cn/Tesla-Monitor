using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Client
{
    public record TeslaCar
    {
        [JsonPropertyName("id_s")]
        public String CarId { get; init; }
        [JsonPropertyName("vehicle_id")]
        public Int64 VehicleId { get; init; }
        [JsonPropertyName("vin")]
        public String Vin { get; init; }
        [JsonPropertyName("display_name")]
        public String DisplayName { get; init; }
        [JsonPropertyName("state")]
        public CarState State { get; init; }
        [JsonPropertyName("in_service")]
        public Boolean IsInService { get; init; }
        [JsonPropertyName("api_version")]
        public Int32 ApiVersion { get; init; }
        [Obsolete("The meaning of the field is unknown")]
        [JsonPropertyName("calendar_enabled")]
        public Boolean IsCalendarEnabled { get; init; }
        [Obsolete("The meaning of the field is unknown")]
        [JsonPropertyName("command_signing")]
        public String CommandSigning { get; init; }
        [Obsolete("The meaning of the field is unknown")]
        [JsonPropertyName("option_codes")]
        public String OptionCodes { get; init; }
        [Obsolete("The meaning of the field is unknown")]
        [JsonPropertyName("access_type")]
        public String AccessType { get; init; }
        [Obsolete("The meaning of the field is unknown")]
        [JsonPropertyName("backseat_token")]
        public String BackseatToken { get; init; }
        [Obsolete("The meaning of the field is unknown")]
        [JsonPropertyName("backseat_token_updated_at")]
        public String BackseatTokenUpdatedAt { get; init; }
        [Obsolete("The meaning of the field is unknown")]
        [JsonPropertyName("color")]
        public String Color { get; init; }
        [Obsolete("The meaning of the field is unknown")]
        [JsonPropertyName("tokens")]
        public List<String> Tokens { get; init; }
    }
}
