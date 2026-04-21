namespace Ollama.Net.Models.Common;

/// <summary>
/// Options for controlling model behavior and generation parameters.
/// All properties are nullable, where null means use the server's default value.
/// </summary>
/// <param name="Temperature">Controls randomness (0.0 to 1.0). Higher values produce more random outputs.</param>
/// <param name="TopP">Nucleus sampling threshold (0.0 to 1.0).</param>
/// <param name="TopK">Top-K sampling parameter.</param>
/// <param name="NumCtx">Size of context window.</param>
/// <param name="NumPredict">Maximum number of tokens to predict.</param>
/// <param name="Stop">Sequences where the model will stop generating.</param>
/// <param name="Seed">Random seed for reproducibility.</param>
/// <param name="RepeatPenalty">Penalty for repeating tokens.</param>
/// <param name="MirostatTau">Mirostat target entropy.</param>
/// <param name="MirostatEta">Mirostat learning rate.</param>
/// <param name="MirostatMode">Mirostat sampling mode (0=disabled, 1=Mirostat, 2=Mirostat 2.0).</param>
/// <param name="PresencePenalty">Presence penalty for token diversity.</param>
/// <param name="FrequencyPenalty">Frequency penalty for token diversity.</param>
/// <param name="NumGpu">Number of GPU layers to use.</param>
/// <param name="NumThread">Number of CPU threads to use.</param>
/// <param name="Format">Output format constraint (e.g., "json").</param>
public sealed record OllamaOptions(
    double? Temperature = null,
    double? TopP = null,
    int? TopK = null,
    int? NumCtx = null,
    int? NumPredict = null,
    string[]? Stop = null,
    int? Seed = null,
    double? RepeatPenalty = null,
    double? MirostatTau = null,
    double? MirostatEta = null,
    int? MirostatMode = null,
    double? PresencePenalty = null,
    double? FrequencyPenalty = null,
    int? NumGpu = null,
    int? NumThread = null,
    string? Format = null
);
