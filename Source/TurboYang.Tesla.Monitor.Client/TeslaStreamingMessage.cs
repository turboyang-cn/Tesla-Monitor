using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Client
{
    public record TeslaStreamingMessage
    {
        public enum MessageTypes
        {
            [EnumString("data:update")]
            Update = 1,
            [EnumString("data:error")]
            Error = 2,
            [EnumString("control:hello")]
            Hello = 3,
        }

        public enum ErrorTypes
        {
            [EnumString("vehicle_disconnected")]
            VehicleDisconnected = 1,
            [EnumString("vehicle_error")]
            VehicleError = 2,
            [EnumString("client_error")]
            ClientError = 3,
        }

        [JsonPropertyName("msg_type")]
        public MessageTypes MessageType { get; set; }
        [JsonPropertyName("error_type")]
        public ErrorTypes ErrorType { get; set; }
        [JsonPropertyName("value")]
        public String Value { get; set; }
        [JsonPropertyName("tag")]
        public String Tag { get; set; }
    }
}
