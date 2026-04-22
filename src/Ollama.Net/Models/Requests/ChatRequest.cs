using Ollama.Net.Models.Common;

namespace Ollama.Net.Models.Requests;

/// <summary>
/// Request for chat-based text generation.
/// </summary>
/// <param name="Model">The name of the model to use.</param>
/// <param name="Messages">The conversation history.</param>
/// <param name="Tools">Available tools the model can call.</param>
/// <param name="Format">
/// Output-format constraint. Accepts either a mode string (e.g. <see cref="OllamaFormat.Json"/>
/// for JSON mode) or a JSON-schema object for structured outputs. Implicit conversions from
/// <see cref="string"/> and <see cref="System.Text.Json.JsonElement"/> make call sites concise.
/// </param>
/// <param name="Stream">Whether to stream the response (set automatically by client).</param>
/// <param name="KeepAlive">How long to keep the model loaded in memory.</param>
/// <param name="Options">Additional model parameters.</param>
/// <param name="Think">
/// For thinking-capable models (e.g. <c>gpt-oss</c>, <c>deepseek-v3.1</c>), enables or disables
/// the model's reasoning pass. When enabled the reasoning trace is surfaced via
/// <see cref="OllamaMessage.Thinking"/> on the assistant message, separate from
/// <see cref="OllamaMessage.Content"/>.
/// </param>
public sealed record ChatRequest(
    string Model,
    OllamaMessage[] Messages,
    ToolDefinition[]? Tools = null,
    OllamaFormat? Format = null,
    bool? Stream = null,
    TimeSpan? KeepAlive = null,
    OllamaOptions? Options = null,
    bool? Think = null
);
