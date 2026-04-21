using Ollama.Net.Models.Common;

namespace Ollama.Net.Models.Requests;

/// <summary>
/// Legacy embedding request format.
/// </summary>
/// <param name="Model">The name of the embedding model to use.</param>
/// <param name="Prompt">The text to generate an embedding for.</param>
/// <param name="Options">Additional model parameters.</param>
/// <param name="KeepAlive">How long to keep the model loaded in memory.</param>
[Obsolete("Use EmbedRequest and EmbedAsync instead. This endpoint is deprecated by Ollama.")]
public sealed record LegacyEmbeddingRequest(
    string Model,
    string Prompt,
    OllamaOptions? Options = null,
    TimeSpan? KeepAlive = null
);
