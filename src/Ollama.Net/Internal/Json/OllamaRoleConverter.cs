using System.Text.Json;
using System.Text.Json.Serialization;
using Ollama.Net.Models.Common;

namespace Ollama.Net.Internal.Json;

/// <summary>
/// Converts <see cref="OllamaRole"/> values to and from the lower-case string
/// representation used by the Ollama wire protocol.
/// </summary>
/// <remarks>
/// Ollama's <c>role</c> field is documented as one of <c>"system"</c>, <c>"user"</c>,
/// <c>"assistant"</c>, or <c>"tool"</c>. Without this converter, the default System.Text.Json
/// behaviour would serialize the enum as an integer, which the Ollama server rejects.
/// Unknown role strings are accepted and mapped to <see cref="OllamaRole.User"/>
/// to keep round-tripping robust against future API additions.
/// </remarks>
internal sealed class OllamaRoleConverter : JsonConverter<OllamaRole>
{
    public override OllamaRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for OllamaRole, got {reader.TokenType}.");
        }

        string? value = reader.GetString();
        return value switch
        {
            "system" => OllamaRole.System,
            "user" => OllamaRole.User,
            "assistant" => OllamaRole.Assistant,
            "tool" => OllamaRole.Tool,
            _ => OllamaRole.User
        };
    }

    public override void Write(Utf8JsonWriter writer, OllamaRole value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        string literal = value switch
        {
            OllamaRole.System => "system",
            OllamaRole.User => "user",
            OllamaRole.Assistant => "assistant",
            OllamaRole.Tool => "tool",
            _ => "user"
        };
        writer.WriteStringValue(literal);
    }
}
