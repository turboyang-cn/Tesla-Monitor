using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Core.JsonConverters
{
    public class CelsiusToTemperatureConverter : JsonConverter<Temperature>
    {
        public override Temperature Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetDecimal(out Decimal value))
            {
                return new Temperature()
                {
                    Celsius = value,
                };
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, Temperature value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Celsius);
        }
    }
}
