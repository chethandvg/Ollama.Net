using Krutaka.Ollama.Abstractions;
using Krutaka.Ollama.Http;
using Krutaka.Ollama.Internal.Json;
using Krutaka.Ollama.Internal.Validation;
using Krutaka.Ollama.Models.Requests;
using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Clients;

/// <summary>Implementation of embeddings operations.</summary>
internal sealed class OllamaEmbeddingsClient : IOllamaEmbeddingsClient
{
    private readonly OllamaHttpClient _httpClient;

    public OllamaEmbeddingsClient(OllamaHttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public Task<EmbedResponse> EmbedAsync(
        EmbedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        if (request.Input is null || request.Input.Length == 0)
        {
            throw new ArgumentException("EmbedRequest.Input must contain at least one string.", nameof(request));
        }

        foreach (string input in request.Input)
        {
            if (input is null)
            {
                throw new ArgumentException("EmbedRequest.Input entries must not be null.", nameof(request));
            }
        }

        return _httpClient.SendJsonAsync(
            HttpMethod.Post,
            "/api/embed",
            request,
            OllamaJsonContext.Default.EmbedResponse,
            OllamaJsonContext.Default.EmbedRequest,
            cancellationToken);
    }

    [Obsolete("Use EmbedAsync instead. This endpoint is deprecated by Ollama.")]
    public Task<LegacyEmbeddingResponse> EmbedLegacyAsync(
        LegacyEmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("LegacyEmbeddingRequest.Prompt is required.", nameof(request));
        }

        return _httpClient.SendJsonAsync(
            HttpMethod.Post,
            "/api/embeddings",
            request,
            OllamaJsonContext.Default.LegacyEmbeddingResponse,
            OllamaJsonContext.Default.LegacyEmbeddingRequest,
            cancellationToken);
    }
}
