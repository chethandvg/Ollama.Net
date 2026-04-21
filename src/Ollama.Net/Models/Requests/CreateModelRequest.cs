using Ollama.Net.Models.Common;

namespace Ollama.Net.Models.Requests;

/// <summary>
/// Request for creating a new model from a Modelfile.
/// </summary>
/// <param name="Model">The name of the model to create.</param>
/// <param name="From">The base model to create from.</param>
/// <param name="Files">Files to include in the model.</param>
/// <param name="Adapters">Adapter files to merge.</param>
/// <param name="Template">Prompt template to use.</param>
/// <param name="License">License text for the model.</param>
/// <param name="System">System message to set context.</param>
/// <param name="Parameters">Model parameters.</param>
/// <param name="Messages">Example messages.</param>
/// <param name="Stream">Whether to stream progress updates (set automatically by client).</param>
/// <param name="Quantize">Quantization level (e.g., "q4_0").</param>
public sealed record CreateModelRequest(
    string Model,
    string? From = null,
    Dictionary<string, string>? Files = null,
    string[]? Adapters = null,
    string? Template = null,
    string? License = null,
    string? System = null,
    Dictionary<string, object>? Parameters = null,
    OllamaMessage[]? Messages = null,
    bool? Stream = null,
    string? Quantize = null
);
