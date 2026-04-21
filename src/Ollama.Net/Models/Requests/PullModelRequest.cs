namespace Ollama.Net.Models.Requests;

/// <summary>
/// Request for pulling a model from the Ollama registry.
/// </summary>
/// <param name="Model">The name of the model to pull.</param>
/// <param name="Insecure">Allow insecure connections to the registry.</param>
/// <param name="Stream">Whether to stream progress updates (set automatically by client).</param>
public sealed record PullModelRequest(
    string Model,
    bool? Insecure = null,
    bool? Stream = null
);
