namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when the Ollama client is misconfigured.</summary>
public sealed class OllamaConfigurationException : OllamaException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaConfigurationException"/> class.</summary>
    public OllamaConfigurationException() : base("Ollama client is misconfigured.") { }

    /// <summary>Initializes a new instance of the <see cref="OllamaConfigurationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaConfigurationException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaConfigurationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
