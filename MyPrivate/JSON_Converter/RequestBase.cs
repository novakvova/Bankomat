using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyPrivate.JSON_Converter
{
    public abstract class RequestBase
    {
        public abstract Int32 Type { get; } // Abstract property to get the type of request
    }
    public class RequestBaseConverter : JsonConverter<RequestBase>
    {
        public override RequestBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            Int32 type = root.GetProperty("Type").GetInt32();

            return type switch
            {
                0 => JsonSerializer.Deserialize<RequestType0>(root.GetRawText(), options),
                1 => JsonSerializer.Deserialize<RequestType1>(root.GetRawText(), options),
                2 => JsonSerializer.Deserialize<RequestType2>(root.GetRawText(), options),
                _ => throw new NotSupportedException($"Unknown type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, RequestBase value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
