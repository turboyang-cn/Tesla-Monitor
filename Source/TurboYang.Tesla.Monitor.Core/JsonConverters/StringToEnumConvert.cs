using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using TurboYang.Tesla.Monitor.Model.Attributes;

namespace TurboYang.Tesla.Monitor.Core.JsonConverters
{
    public class CustomJsonStringEnumConverter : JsonConverterFactory
    {
        public override Boolean CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return Activator.CreateInstance(typeof(StringToEnumConvert<>).MakeGenericType(typeToConvert)) as JsonConverter;
        }
    }

    public class StringToEnumConvert<T> : JsonConverter<T>
        where T : struct, Enum
    {
        private readonly Dictionary<T, (Int32 EnumValue, List<String> EnumStrings)> Mapping = new();

        public StringToEnumConvert()
        {
            Type type = typeof(T);
            List<T> values = Enum.GetValues<T>().ToList();

            foreach (T value in values)
            {
                Int32 enumValue = Convert.ToInt32(value);
                List<String> enumStrings = new();

                MemberInfo enumMember = type.GetMember(value.ToString()).FirstOrDefault();

                if (enumMember != null)
                {
                    EnumStringAttribute enumStringAttribute = enumMember.GetCustomAttributes<EnumStringAttribute>().FirstOrDefault();

                    enumStrings = enumStringAttribute.Values.ToList();
                }

                if (!enumStrings.Any(x=>x == value.ToString()))
                {
                    enumStrings.Add(value.ToString());
                }

                Mapping.Add(value, (enumValue, enumStrings));
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                String enumString = reader.GetString();

                if (enumString != null)
                {
                    return Mapping.FirstOrDefault(x => x.Value.EnumStrings.Any(x => x == enumString)).Key;
                }

            }
            else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out Int32 enumValue))
            {
                return Mapping.FirstOrDefault(x => x.Value.EnumValue == enumValue).Key;
            }

            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (Mapping.TryGetValue(value, out (Int32 EnumValue, List<String> EnumStrings) mapping))
            {
                if (mapping.EnumStrings.Count == 0)
                {
                    writer.WriteNumberValue(mapping.EnumValue);
                }
                else
                {
                    writer.WriteStringValue(mapping.EnumStrings.FirstOrDefault());
                }
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
