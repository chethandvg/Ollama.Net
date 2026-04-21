using System.Diagnostics.CodeAnalysis;

namespace Ollama.Net.Models.Responses;

/// <summary>
/// Legacy embedding response format.
/// </summary>
/// <param name="Embedding">The generated embedding vector.</param>
[Obsolete("Use EmbedResponse and EmbedAsync instead. This endpoint is deprecated by Ollama.")]
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "DTO property backed by JSON array")]
public sealed record LegacyEmbeddingResponse(
    float[] Embedding
);
