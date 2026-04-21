using Krutaka.Ollama.Models.Requests;
using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Abstractions;

/// <summary>
/// Client for embeddings operations.
/// </summary>
public interface IOllamaEmbeddingsClient
{
    /// <summary>
    /// Generates embeddings for one or more input texts.
    /// </summary>
    /// <param name="request">The embed request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embeddings response.</returns>
    Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an embedding using the legacy endpoint.
    /// </summary>
    /// <param name="request">The legacy embedding request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The legacy embedding response.</returns>
    [Obsolete("Use EmbedAsync instead. This endpoint is deprecated by Ollama.")]
    Task<LegacyEmbeddingResponse> EmbedLegacyAsync(LegacyEmbeddingRequest request, CancellationToken cancellationToken = default);
}
