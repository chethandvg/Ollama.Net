using Ollama.Net.Models.Requests;
using Ollama.Net.Models.Responses;

namespace Ollama.Net.Abstractions;

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
    /// <remarks>
    /// <b>Exception surface:</b> the returned enumerable is fully lazy — this method
    /// never throws synchronously. All exceptions (argument validation, transport
    /// failures, HTTP errors, deserialization, cancellation) surface from the first
    /// or subsequent <c>MoveNextAsync</c>, so a single <c>try</c> around the
    /// <c>await foreach</c> is sufficient — no defensive <c>try</c> around the call
    /// site is required.
    /// </remarks>
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
    /// <remarks>
    /// <b>Exception surface:</b> the returned enumerable is fully lazy — this method
    /// never throws synchronously. All exceptions (argument validation, transport
    /// failures, HTTP errors, deserialization, cancellation) surface from the first
    /// or subsequent <c>MoveNextAsync</c>, so a single <c>try</c> around the
    /// <c>await foreach</c> is sufficient — no defensive <c>try</c> around the call
    /// site is required.
    /// </remarks>
    IAsyncEnumerable<ChatResponse> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
