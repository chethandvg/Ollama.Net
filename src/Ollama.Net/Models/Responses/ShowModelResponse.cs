using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Ollama.Net.Models.Common;

namespace Ollama.Net.Models.Responses;

/// <summary>
/// Detailed information about a model.
/// </summary>
/// <param name="Modelfile">The Modelfile content.</param>
/// <param name="Parameters">Model parameters as text.</param>
/// <param name="Template">The prompt template used by the model.</param>
/// <param name="Details">Model architecture details.</param>
/// <param name="ModelInfo">Additional model information as raw JSON.</param>
/// <param name="Capabilities">List of model capabilities.</param>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "DTO property backed by JSON array")]
public sealed record ShowModelResponse(
    string Modelfile,
    string? Parameters = null,
    string? Template = null,
    ModelDetails? Details = null,
    Dictionary<string, JsonElement>? ModelInfo = null,
    string[]? Capabilities = null
);
