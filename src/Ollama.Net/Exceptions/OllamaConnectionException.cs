namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when unable to connect to the Ollama server.</summary>
public sealed class OllamaConnectionException : OllamaException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaConnectionException"/> class.</summary>
    public OllamaConnectionException() : base("Could not connect to the Ollama server.") { }

    /// <summary>Initializes a new instance of the <see cref="OllamaConnectionException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaConnectionException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaConnectionException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
