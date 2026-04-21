using Ollama.Net.Models.Common;

namespace Ollama.Net.Models.Requests;

/// <summary>
/// Request for generating embeddings.
/// </summary>
/// <param name="Model">The name of the embedding model to use.</param>
/// <param name="Input">The text input(s) to generate embeddings for.</param>
/// <param name="Truncate">Whether to truncate inputs that exceed the model's context length.</param>
/// <param name="Options">Additional model parameters.</param>
/// <param name="KeepAlive">How long to keep the model loaded in memory.</param>
public sealed record EmbedRequest(
    string Model,
    string[] Input,
    bool? Truncate = null,
    OllamaOptions? Options = null,
    TimeSpan? KeepAlive = null
)
{
    /// <summary>
    /// Creates an embed request for a single input string.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="input">The single input text.</param>
    /// <returns>An embed request.</returns>
    public static EmbedRequest FromSingleInput(string model, string input)
    {
        return new EmbedRequest(model, [input]);
    }
}
