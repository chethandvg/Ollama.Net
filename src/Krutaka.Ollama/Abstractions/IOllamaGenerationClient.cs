using Krutaka.Ollama.Models.Requests;
using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Abstractions;

/// <summary>
/// Client for text generation operations.
/// </summary>
public interface IOllamaGenerationClient
{
    /// <summary>
    /// Generates text from a prompt (non-streaming).
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete generation response.</returns>
    Task<GenerateResponse> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates text from a prompt with streaming.
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of generation response chunks.</returns>
    IAsyncEnumerable<GenerateResponse> GenerateStreamAsync(GenerateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a chat request (non-streaming).
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete chat response.</returns>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a chat request with streaming.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of chat response chunks.</returns>
    IAsyncEnumerable<ChatResponse> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
