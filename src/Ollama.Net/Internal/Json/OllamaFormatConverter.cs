using System.Text.Json;
using System.Text.Json.Serialization;
using Ollama.Net.Models.Common;

namespace Ollama.Net.Internal.Json;

/// <summary>
/// Serialises <see cref="OllamaFormat"/> as either a JSON string (mode) or an inline
/// JSON object (schema), matching Ollama's <c>format</c> field shape.
/// </summary>
internal sealed class OllamaFormatConverter : JsonConverter<OllamaFormat>
{
    public override OllamaFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return default;

            case JsonTokenType.String:
                string? s = reader.GetString();
                return string.IsNullOrEmpty(s) ? default : OllamaFormat.FromString(s);

            case JsonTokenType.StartObject:
                using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
                {
                    return OllamaFormat.FromSchema(doc.RootElement);
                }

            default:
                throw new JsonException(
                    $"Unexpected token '{reader.TokenType}' when reading OllamaFormat; expected string or object.");
        }
    }

    public override void Write(Utf8JsonWriter writer, OllamaFormat value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value.IsSchema)
        {
            value.AsSchema().WriteTo(writer);
            return;
        }

        string? mode = value.AsMode();
        if (string.IsNullOrEmpty(mode))
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(mode);
    }
}
