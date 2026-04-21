using System.Text.Json.Serialization;
using Ollama.Net.Internal.Json;

namespace Ollama.Net.Models.Common;

/// <summary>
/// Represents the role of a message in a conversation.
/// Serialized as the lower-case string literal expected by the Ollama API
/// (<c>"system"</c>, <c>"user"</c>, <c>"assistant"</c>, <c>"tool"</c>).
/// </summary>
[JsonConverter(typeof(OllamaRoleConverter))]
public enum OllamaRole
{
    /// <summary>
    /// System role for setting context and instructions.
    /// </summary>
    System,

    /// <summary>
    /// User role for user-provided messages.
    /// </summary>
    User,

    /// <summary>
    /// Assistant role for model-generated messages.
    /// </summary>
    Assistant,

    /// <summary>
    /// Tool role for tool execution results.
    /// </summary>
    Tool
}
