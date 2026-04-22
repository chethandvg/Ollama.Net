using Ollama.Net.Models.Common;

namespace Ollama.Net.Models.Requests;

/// <summary>
/// Request for generating text from a prompt.
/// </summary>
/// <param name="Model">The name of the model to use.</param>
/// <param name="Prompt">The prompt to generate a response for.</param>
/// <param name="Suffix">Text to append after the generated text.</param>
/// <param name="System">System message to set context.</param>
/// <param name="Template">Custom prompt template to use.</param>
/// <param name="Context">
/// Context from a previous generation to continue from.
/// <b>Deprecated by Ollama</b>: prefer <c>/api/chat</c> for multi-turn memory.
/// </param>
/// <param name="Stream">Whether to stream the response (set automatically by client).</param>
/// <param name="Raw">If true, no formatting will be applied to the prompt.</param>
/// <param name="KeepAlive">How long to keep the model loaded in memory.</param>
/// <param name="Images">Base64-encoded images to include with the prompt.</param>
/// <param name="Options">Additional model parameters.</param>
/// <param name="Format">
/// Output-format constraint. Accepts either a mode string (e.g. <see cref="OllamaFormat.Json"/>
/// for JSON mode) or a JSON-schema object for structured outputs. Implicit conversions from
/// <see cref="string"/> and <see cref="System.Text.Json.JsonElement"/> make call sites concise.
/// </param>
/// <param name="Think">
/// For thinking-capable models (e.g. <c>gpt-oss</c>, <c>deepseek-v3.1</c>), enables or disables
/// the model's reasoning pass. The reasoning trace is returned out-of-band by the server and
/// is not part of <see cref="Ollama.Net.Models.Responses.GenerateResponse.Response"/>.
/// </param>
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
    OllamaFormat? Format = null,
    bool? Think = null
);
