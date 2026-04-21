using System.Net;

namespace Krutaka.Ollama.Exceptions;

/// <summary>
/// Base exception for API-level errors with HTTP status codes.
/// </summary>
public class OllamaApiException : OllamaException
{
    /// <summary>
    /// The HTTP status code returned by the server.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// The raw error response from the server.
    /// </summary>
    public string? RawServerError { get; set; }

    /// <summary>
    /// The request ID from the X-Request-Id header, if present.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>Initializes a new instance of the <see cref="OllamaApiException"/> class.</summary>
    public OllamaApiException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="OllamaApiException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaApiException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="OllamaApiException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    public OllamaApiException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
