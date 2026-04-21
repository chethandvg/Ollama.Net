using System.Net;

namespace Krutaka.Ollama.Exceptions;

/// <summary>Exception thrown when the request payload or context is too large (413 or specific 500 patterns).</summary>
public sealed class OllamaPayloadTooLargeException : OllamaApiException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaPayloadTooLargeException"/> class.</summary>
    public OllamaPayloadTooLargeException()
        : base("Request payload is too large.", HttpStatusCode.RequestEntityTooLarge) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaPayloadTooLargeException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaPayloadTooLargeException(string message)
        : base(message, HttpStatusCode.RequestEntityTooLarge) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaPayloadTooLargeException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaPayloadTooLargeException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaPayloadTooLargeException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public OllamaPayloadTooLargeException(string message, HttpStatusCode statusCode) : base(message, statusCode) { }
}
