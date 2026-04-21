namespace Krutaka.Ollama.Models.Common;

/// <summary>
/// Represents a tool call made by the model.
/// </summary>
/// <param name="Function">The function call information.</param>
public sealed record ToolCall(
    ToolCallFunction Function
);
