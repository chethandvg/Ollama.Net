using Krutaka.Ollama.Models.Common;

namespace Krutaka.Ollama.Models.Responses;

/// <summary>
/// Information about a currently running model.
/// </summary>
/// <param name="Name">The name of the model.</param>
/// <param name="Model">The model identifier.</param>
/// <param name="Size">Size of the model in bytes.</param>
/// <param name="Digest">SHA256 digest of the model.</param>
/// <param name="Details">Model architecture details.</param>
/// <param name="ExpiresAt">When the model will be unloaded from memory.</param>
/// <param name="SizeVram">Amount of VRAM used by the model in bytes.</param>
public sealed record RunningModel(
    string Name,
    string Model,
    long Size,
    string Digest,
    ModelDetails? Details,
    DateTimeOffset ExpiresAt,
    long SizeVram
);
