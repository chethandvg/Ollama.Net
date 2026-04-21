using Ollama.Net.Models.Common;

namespace Ollama.Net.Models.Requests;

/// <summary>
/// Request for chat-based text generation.
/// </summary>
/// <param name="Model">The name of the model to use.</param>
/// <param name="Messages">The conversation history.</param>
/// <param name="Tools">Available tools the model can call.</param>
/// <param name="Format">Output format (e.g., "json").</param>
/// <param name="Stream">Whether to stream the response (set automatically by client).</param>
/// <param name="KeepAlive">How long to keep the model loaded in memory.</param>
/// <param name="Options">Additional model parameters.</param>
public sealed record ChatRequest(
    string Model,
    OllamaMessage[] Messages,
    ToolDefinition[]? Tools = null,
    string? Format = null,
    bool? Stream = null,
    TimeSpan? KeepAlive = null,
    OllamaOptions? Options = null
);
