namespace Ollama.Net.Abstractions;

/// <summary>
/// Unified client interface for all Ollama operations.
/// </summary>
public interface IOllamaClient
{
    /// <summary>
    /// Gets the text generation client.
    /// </summary>
    IOllamaGenerationClient Generation { get; }

    /// <summary>
    /// Gets the embeddings client.
    /// </summary>
    IOllamaEmbeddingsClient Embeddings { get; }

    /// <summary>
    /// Gets the models management client.
    /// </summary>
    IOllamaModelsClient Models { get; }

    /// <summary>
    /// Gets the system operations client.
    /// </summary>
    IOllamaSystemClient System { get; }
}
