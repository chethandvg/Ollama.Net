using System.Text.Json;

namespace Ollama.Net.Models.Common;

/// <summary>
/// Describes a function that can be called by the model.
/// </summary>
/// <param name="Name">The name of the function.</param>
/// <param name="Description">Optional description of what the function does.</param>
/// <param name="Parameters">JSON schema describing the function parameters.</param>
public sealed record FunctionDefinition(
    string Name,
    string? Description,
    JsonElement Parameters
);
