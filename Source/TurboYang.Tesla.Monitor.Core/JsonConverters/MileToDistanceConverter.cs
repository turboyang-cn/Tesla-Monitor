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
    public class MileToDistanceConverter : JsonConverter<Distance>
    {
        public override Distance Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetDecimal(out Decimal value))
            {
                return new Distance()
                {
                    Mile = value,
                };
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, Distance value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Mile);
        }
    }
}
