using System.Net;

namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when the Ollama service is unavailable (503).</summary>
public sealed class OllamaServiceUnavailableException : OllamaApiException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaServiceUnavailableException"/> class.</summary>
    public OllamaServiceUnavailableException()
        : base("Ollama service is unavailable.", HttpStatusCode.ServiceUnavailable) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaServiceUnavailableException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaServiceUnavailableException(string message)
        : base(message, HttpStatusCode.ServiceUnavailable) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaServiceUnavailableException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
