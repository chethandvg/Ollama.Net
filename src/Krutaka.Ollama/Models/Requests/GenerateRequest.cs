using Krutaka.Ollama.Models.Common;

namespace Krutaka.Ollama.Models.Requests;

/// <summary>
/// Request for generating text from a prompt.
/// </summary>
/// <param name="Model">The name of the model to use.</param>
/// <param name="Prompt">The prompt to generate a response for.</param>
/// <param name="Suffix">Text to append after the generated text.</param>
/// <param name="System">System message to set context.</param>
/// <param name="Template">Custom prompt template to use.</param>
/// <param name="Context">Context from a previous generation to continue from.</param>
/// <param name="Stream">Whether to stream the response (set automatically by client).</param>
/// <param name="Raw">If true, no formatting will be applied to the prompt.</param>
/// <param name="KeepAlive">How long to keep the model loaded in memory.</param>
/// <param name="Images">Base64-encoded images to include with the prompt.</param>
/// <param name="Options">Additional model parameters.</param>
/// <param name="Format">Output format (e.g., "json").</param>
public sealed record GenerateRequest(
    string Model,
    string Prompt,
    string? Suffix = null,
    string? System = null,
    string? Template = null,
    int[]? Context = null,
    bool? Stream = null,
    bool? Raw = null,
    TimeSpan? KeepAlive = null,
    string[]? Images = null,
    OllamaOptions? Options = null,
    string? Format = null
);
