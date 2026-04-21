namespace Krutaka.Ollama.Models.Requests;

/// <summary>
/// Request for copying a model.
/// </summary>
/// <param name="Source">The name of the source model to copy.</param>
/// <param name="Destination">The name for the destination model.</param>
public sealed record CopyModelRequest(
    string Source,
    string Destination
);
