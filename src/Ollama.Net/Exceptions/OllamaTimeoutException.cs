namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when a request to the Ollama server times out.</summary>
public sealed class OllamaTimeoutException : OllamaException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaTimeoutException"/> class.</summary>
    public OllamaTimeoutException() : base("Ollama request timed out.") { }

    /// <summary>Initializes a new instance of the <see cref="OllamaTimeoutException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaTimeoutException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaTimeoutException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaTimeoutException(string message, Exception innerException) : base(message, innerException) { }
}
