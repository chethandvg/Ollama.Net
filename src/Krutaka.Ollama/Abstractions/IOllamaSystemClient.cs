using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Abstractions;

/// <summary>
/// Client for system-level operations.
/// </summary>
public interface IOllamaSystemClient
{
    /// <summary>
    /// Gets the Ollama server version.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version response.</returns>
    Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pings the Ollama server to check if it's running.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the server is reachable, false otherwise.</returns>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists on the server.
    /// </summary>
    /// <param name="digest">The SHA256 digest of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blob exists, false otherwise.</returns>
    Task<bool> BlobExistsAsync(string digest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes a blob to the server.
    /// </summary>
    /// <param name="digest">The SHA256 digest of the blob.</param>
    /// <param name="content">The blob content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PushBlobAsync(string digest, Stream content, CancellationToken cancellationToken = default);
}
