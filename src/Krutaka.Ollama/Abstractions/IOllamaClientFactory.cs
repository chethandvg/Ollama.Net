namespace Krutaka.Ollama.Abstractions;

/// <summary>
/// Factory for creating named Ollama client instances.
/// </summary>
public interface IOllamaClientFactory
{
    /// <summary>
    /// Creates an Ollama client with the specified name.
    /// </summary>
    /// <param name="name">The client name. Use an empty string for the default unnamed client.</param>
    /// <returns>An Ollama client instance.</returns>
    IOllamaClient CreateClient(string name = "");
}
