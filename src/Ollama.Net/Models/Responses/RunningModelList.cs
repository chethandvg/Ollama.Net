using System.Diagnostics.CodeAnalysis;

namespace Ollama.Net.Models.Responses;

/// <summary>
/// List of currently running models.
/// </summary>
/// <param name="Models">Array of running model information.</param>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "DTO property backed by JSON array")]
public sealed record RunningModelList(
    RunningModel[] Models
);
