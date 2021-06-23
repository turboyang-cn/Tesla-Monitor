using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using NodaTime;

namespace TurboYang.Tesla.Monitor.Core.JsonConverters
{
    public class UnixTimezoneToInstantConverter : JsonConverter<Instant>
    {
        private static readonly Instant BaseDateTime = Instant.FromUtc(1970, 1, 1, 0, 0, 0);

        public override Instant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetInt64(out Int64 timestamp))
            {
                if ( timestamp >= 1000000000000)
                {
                    return BaseDateTime.Plus(Duration.FromMilliseconds(timestamp));
                }

                return BaseDateTime.Plus(Duration.FromSeconds(timestamp));
            }

            return BaseDateTime;
        }

        public override void Write(Utf8JsonWriter writer, Instant value, JsonSerializerOptions options)
        {
            writer.WriteStringValue((value - BaseDateTime).TotalMilliseconds.ToString());
        }
    }
}
