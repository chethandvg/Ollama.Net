namespace Krutaka.Ollama.Models.Common;

/// <summary>
/// Details about a model's architecture and configuration.
/// </summary>
/// <param name="ParentModel">The parent model this model is based on.</param>
/// <param name="Format">The format of the model file.</param>
/// <param name="Family">The model family.</param>
/// <param name="Families">Multiple model families this model belongs to.</param>
/// <param name="ParameterSize">The size of the model parameters (e.g., "7B").</param>
/// <param name="QuantizationLevel">The quantization level (e.g., "Q4_0").</param>
public sealed record ModelDetails(
    string? ParentModel = null,
    string? Format = null,
    string? Family = null,
    string[]? Families = null,
    string? ParameterSize = null,
    string? QuantizationLevel = null
);
