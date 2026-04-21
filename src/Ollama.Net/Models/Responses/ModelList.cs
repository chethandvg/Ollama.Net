using System.Diagnostics.CodeAnalysis;

namespace Ollama.Net.Models.Responses;

/// <summary>
/// List of available models.
/// </summary>
/// <param name="Models">Array of model information.</param>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "DTO property backed by JSON array")]
public sealed record ModelList(
    ModelInfo[] Models
);
