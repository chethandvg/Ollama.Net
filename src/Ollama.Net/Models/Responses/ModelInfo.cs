using Ollama.Net.Models.Common;

namespace Ollama.Net.Models.Responses;

/// <summary>
/// Information about a model.
/// </summary>
/// <param name="Name">The name of the model.</param>
/// <param name="ModifiedAt">When the model was last modified.</param>
/// <param name="Size">Size of the model in bytes.</param>
/// <param name="Digest">SHA256 digest of the model.</param>
/// <param name="Details">Additional details about the model.</param>
public sealed record ModelInfo(
    string Name,
    DateTimeOffset ModifiedAt,
    long Size,
    string Digest,
    ModelDetails? Details = null
);
