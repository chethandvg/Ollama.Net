using Krutaka.Ollama.Abstractions;

namespace Krutaka.Ollama.Clients;

/// <summary>
/// Unified Ollama client implementation.
/// </summary>
internal sealed class OllamaClient : IOllamaClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaClient"/> class.
    /// </summary>
    /// <param name="generation">The generation client.</param>
    /// <param name="embeddings">The embeddings client.</param>
    /// <param name="models">The models client.</param>
    /// <param name="system">The system client.</param>
    public OllamaClient(
        IOllamaGenerationClient generation,
        IOllamaEmbeddingsClient embeddings,
        IOllamaModelsClient models,
        IOllamaSystemClient system)
    {
        ArgumentNullException.ThrowIfNull(generation);
        ArgumentNullException.ThrowIfNull(embeddings);
        ArgumentNullException.ThrowIfNull(models);
        ArgumentNullException.ThrowIfNull(system);

        Generation = generation;
        Embeddings = embeddings;
        Models = models;
        System = system;
    }

    /// <inheritdoc/>
    public IOllamaGenerationClient Generation { get; }

    /// <inheritdoc/>
    public IOllamaEmbeddingsClient Embeddings { get; }

    /// <inheritdoc/>
    public IOllamaModelsClient Models { get; }

    /// <inheritdoc/>
    public IOllamaSystemClient System { get; }
}
