namespace Krutaka.Ollama.Models.Common;

/// <summary>
/// Represents a message in a conversation with the Ollama model.
/// </summary>
/// <param name="Role">The role of the message sender.</param>
/// <param name="Content">The text content of the message.</param>
/// <param name="Images">Optional base64-encoded images associated with the message.</param>
/// <param name="ToolCalls">Optional list of tool calls made by the assistant.</param>
/// <param name="ToolName">Optional name of the tool (for Tool role messages).</param>
public sealed record OllamaMessage(
    OllamaRole Role,
    string Content,
    string[]? Images = null,
    ToolCall[]? ToolCalls = null,
    string? ToolName = null
);
