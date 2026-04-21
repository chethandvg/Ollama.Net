using System.Runtime.CompilerServices;
using Ollama.Net.Abstractions;
using Ollama.Net.Http;
using Ollama.Net.Internal.Json;
using Ollama.Net.Internal.Validation;
using Ollama.Net.Models.Requests;
using Ollama.Net.Models.Responses;

namespace Ollama.Net.Clients;

/// <summary>Implementation of text generation and chat operations.</summary>
internal sealed class OllamaGenerationClient : IOllamaGenerationClient
{
    private readonly OllamaHttpClient _httpClient;

    public OllamaGenerationClient(OllamaHttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public Task<GenerateResponse> GenerateAsync(
        GenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);
        RequestValidator.ValidateStreamMode(request.Stream, expectedStream: false, nameof(GenerateAsync));

        GenerateRequest body = request with { Stream = false };
        return _httpClient.SendJsonAsync(
            HttpMethod.Post,
            "/api/generate",
            body,
            OllamaJsonContext.Default.GenerateResponse,
            OllamaJsonContext.Default.GenerateRequest,
            cancellationToken);
    }

    public async IAsyncEnumerable<GenerateResponse> GenerateStreamAsync(
        GenerateRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        GenerateRequest body = request with { Stream = true };

        await foreach (GenerateResponse chunk in _httpClient.SendStreamAsync(
            HttpMethod.Post,
            "/api/generate",
            body,
            OllamaJsonContext.Default.GenerateResponse,
            OllamaJsonContext.Default.GenerateRequest,
            cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }

    public Task<ChatResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);
        RequestValidator.ValidateMessages(request.Messages);
        RequestValidator.ValidateStreamMode(request.Stream, expectedStream: false, nameof(ChatAsync));

        ChatRequest body = request with { Stream = false };
        return _httpClient.SendJsonAsync(
            HttpMethod.Post,
            "/api/chat",
            body,
            OllamaJsonContext.Default.ChatResponse,
            OllamaJsonContext.Default.ChatRequest,
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponse> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);
        RequestValidator.ValidateMessages(request.Messages);

        ChatRequest body = request with { Stream = true };

        await foreach (ChatResponse chunk in _httpClient.SendStreamAsync(
            HttpMethod.Post,
            "/api/chat",
            body,
            OllamaJsonContext.Default.ChatResponse,
            OllamaJsonContext.Default.ChatRequest,
            cancellationToken).ConfigureAwait(false))
        {
            yield return chunk;
        }
    }
}
