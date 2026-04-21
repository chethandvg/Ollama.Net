namespace Ollama.Net.Models.Responses;

/// <summary>
/// Progress update during model pull, push, or create operations.
/// </summary>
/// <param name="Status">Status message (e.g., "downloading", "verifying").</param>
/// <param name="Digest">SHA256 digest being processed.</param>
/// <param name="Total">Total size in bytes.</param>
/// <param name="Completed">Bytes completed so far.</param>
public sealed record ProgressResponse(
    string Status,
    string? Digest = null,
    long? Total = null,
    long? Completed = null
);
