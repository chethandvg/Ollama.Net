using System.Net;

namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when the request payload is invalid (400).</summary>
public sealed class OllamaRequestValidationException : OllamaApiException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaRequestValidationException"/> class.</summary>
    public OllamaRequestValidationException()
        : base("Ollama rejected the request as invalid.", HttpStatusCode.BadRequest) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaRequestValidationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaRequestValidationException(string message) : base(message, HttpStatusCode.BadRequest) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaRequestValidationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaRequestValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
