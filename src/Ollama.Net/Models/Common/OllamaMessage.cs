namespace Ollama.Net.Models.Common;

/// <summary>
/// Represents a message in a conversation with the Ollama model.
/// </summary>
/// <param name="Role">The role of the message sender.</param>
/// <param name="Content">The text content of the message.</param>
/// <param name="Images">Optional base64-encoded images associated with the message.</param>
/// <param name="ToolCalls">Optional list of tool calls made by the assistant.</param>
/// <param name="ToolName">
/// Optional name of the tool whose result this message carries (only meaningful when
/// <see cref="Role"/> is <see cref="OllamaRole.Tool"/>). This maps directly to Ollama's
/// native <c>tool_name</c> wire field and identifies the <em>tool definition</em>
/// (e.g. <c>"get_weather"</c>) — <b>not</b> a per-invocation identifier. In particular,
/// this is <b>not</b> the place to store Anthropic-style <c>tool_use_id</c> or
/// OpenAI-style <c>tool_call_id</c> values — Ollama does not round-trip those through
/// the server, so any ID you need for cross-provider bridging should live in your
/// application's own conversation state alongside the <see cref="OllamaMessage"/>.
/// </param>
/// <param name="Thinking">
/// For thinking-capable models, the model's reasoning trace accompanying an assistant
/// message. Populated on responses when the request enabled thinking; ignored on
/// requests.
/// </param>
public sealed record OllamaMessage(
    OllamaRole Role,
    string Content,
    string[]? Images = null,
    ToolCall[]? ToolCalls = null,
    string? ToolName = null,
    string? Thinking = null
);
