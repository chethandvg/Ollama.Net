using System.Net;

namespace Krutaka.Ollama.Exceptions;

/// <summary>Exception thrown when authorization fails (403).</summary>
public sealed class OllamaAuthorizationException : OllamaApiException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaAuthorizationException"/> class.</summary>
    public OllamaAuthorizationException() : base("Ollama authorization failed.", HttpStatusCode.Forbidden) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaAuthorizationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaAuthorizationException(string message) : base(message, HttpStatusCode.Forbidden) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaAuthorizationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaAuthorizationException(string message, Exception innerException) : base(message, innerException) { }
}
