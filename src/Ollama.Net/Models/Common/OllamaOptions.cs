using System.Text.Json;
using System.Text.Json.Serialization;
using Ollama.Net.Internal.Json;

namespace Ollama.Net.Models.Common;

/// <summary>
/// Options for controlling model behavior and generation parameters.
/// All properties are nullable — <see langword="null"/> means use the server/Modelfile default.
/// </summary>
/// <remarks>
/// <para>
/// These map one-to-one to the keys Ollama accepts inside the <c>"options"</c> object on
/// <c>/api/generate</c>, <c>/api/chat</c>, and <c>/api/embed</c>. See the
/// <see href="https://github.com/ollama/ollama/blob/main/docs/modelfile.mdx#valid-parameters-and-values">Modelfile docs</see>
/// for the full list and accepted ranges.
/// </para>
/// <para>
/// If Ollama adds a new option before this library is updated, use the <see cref="Extra"/>
/// dictionary to forward it — its entries are flattened into the top-level <c>options</c>
/// object when serialised.
/// </para>
/// </remarks>
/// <param name="Temperature">Controls randomness (0.0 to 1.0). Higher values produce more random outputs.</param>
/// <param name="TopP">Nucleus sampling threshold (0.0 to 1.0).</param>
/// <param name="TopK">Top-K sampling parameter.</param>
/// <param name="MinP">Minimum-probability floor for sampling (0.0 to 1.0).</param>
/// <param name="TypicalP">Locally-typical sampling threshold (0.0 to 1.0).</param>
/// <param name="NumCtx">Size of the context window (tokens).</param>
/// <param name="NumPredict">Maximum number of tokens to predict.</param>
/// <param name="NumKeep">Number of initial tokens to retain when the context is truncated.</param>
/// <param name="RepeatLastN">How far back to look when applying <paramref name="RepeatPenalty"/> (tokens).</param>
/// <param name="Stop">Sequences where the model will stop generating.</param>
/// <param name="Seed">Random seed for reproducibility.</param>
/// <param name="RepeatPenalty">Penalty for repeating tokens.</param>
/// <param name="PenalizeNewline">Whether the repeat penalty should also apply to newline tokens.</param>
/// <param name="MirostatTau">Mirostat target entropy.</param>
/// <param name="MirostatEta">Mirostat learning rate.</param>
/// <param name="MirostatMode">Mirostat sampling mode (0=disabled, 1=Mirostat, 2=Mirostat 2.0).</param>
/// <param name="PresencePenalty">Presence penalty for token diversity.</param>
/// <param name="FrequencyPenalty">Frequency penalty for token diversity.</param>
/// <param name="NumGpu">Number of GPU layers to use.</param>
/// <param name="MainGpu">Index of the primary GPU for multi-GPU setups.</param>
/// <param name="NumThread">Number of CPU threads to use.</param>
/// <param name="NumBatch">Batch size for prompt processing.</param>
/// <param name="UseMmap">Whether to memory-map the model file.</param>
/// <param name="Numa">Whether to enable NUMA optimisations.</param>
/// <param name="Format">
/// <b>Deprecated.</b> The canonical <c>format</c> field lives at the top level of the request,
/// not inside <c>options</c>. Use <see cref="Requests.GenerateRequest.Format"/> or
/// <see cref="Requests.ChatRequest.Format"/> instead. Kept for binary compatibility with 0.1.
/// </param>
/// <param name="Extra">
/// Forward-compatibility bag. Any key/value pairs here are merged into the serialised
/// <c>options</c> object as if they were first-class properties. Use this to send newly
/// introduced Ollama options without waiting for a library release.
/// Values are arbitrary JSON.
/// </param>
[JsonConverter(typeof(OllamaOptionsConverter))]
public sealed record OllamaOptions(
    double? Temperature = null,
    double? TopP = null,
    int? TopK = null,
    double? MinP = null,
    double? TypicalP = null,
    int? NumCtx = null,
    int? NumPredict = null,
    int? NumKeep = null,
    int? RepeatLastN = null,
    string[]? Stop = null,
    int? Seed = null,
    double? RepeatPenalty = null,
    bool? PenalizeNewline = null,
    double? MirostatTau = null,
    double? MirostatEta = null,
    int? MirostatMode = null,
    double? PresencePenalty = null,
    double? FrequencyPenalty = null,
    int? NumGpu = null,
    int? MainGpu = null,
    int? NumThread = null,
    int? NumBatch = null,
    bool? UseMmap = null,
    bool? Numa = null,
    [property: Obsolete("Use GenerateRequest.Format or ChatRequest.Format (top-level) instead. 'format' is not a documented options-bag key.")]
    string? Format = null,
    IReadOnlyDictionary<string, JsonElement>? Extra = null
);
