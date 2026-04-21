using System.Net;

namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when authentication fails (401).</summary>
public sealed class OllamaAuthenticationException : OllamaApiException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaAuthenticationException"/> class.</summary>
    public OllamaAuthenticationException() : base("Ollama authentication failed.", HttpStatusCode.Unauthorized) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaAuthenticationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaAuthenticationException(string message) : base(message, HttpStatusCode.Unauthorized) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaAuthenticationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}
