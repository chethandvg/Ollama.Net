namespace Ollama.Net.Samples;

/// <summary>
/// Sample-only configuration bound from the <c>Samples</c> section of <c>appsettings.json</c>.
/// Values are consumed by the individual sample classes, not by the Ollama.Net library itself.
/// </summary>
internal sealed class SampleOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Samples";

    /// <summary>Override for chat/generate samples; <see langword="null"/> falls back to <c>Ollama:DefaultModel</c>.</summary>
    public string? ChatModel { get; set; }

    /// <summary>Embedding model used by the embeddings sample.</summary>
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>Model used by the tool-calling sample; must support function calling.</summary>
    public string ToolCallingModel { get; set; } = "llama3.2";

    /// <summary>Caps streaming output so CI/tests do not run forever.</summary>
    public int StreamingMaxChunks { get; set; } = 200;
}
