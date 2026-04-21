using System.Net;

namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when rate limited (429).</summary>
public sealed class OllamaRateLimitedException : OllamaApiException
{
    /// <summary>The recommended time to wait before retrying.</summary>
    public TimeSpan? RetryAfter { get; set; }

    /// <summary>Initializes a new instance of the <see cref="OllamaRateLimitedException"/> class.</summary>
    public OllamaRateLimitedException()
        : base("Ollama is rate-limiting requests.", HttpStatusCode.TooManyRequests) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaRateLimitedException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaRateLimitedException(string message)
        : base(message, HttpStatusCode.TooManyRequests) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaRateLimitedException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaRateLimitedException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaRateLimitedException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAfter">Optional retry-after duration.</param>
    public OllamaRateLimitedException(string message, TimeSpan? retryAfter)
        : base(message, HttpStatusCode.TooManyRequests)
    {
        RetryAfter = retryAfter;
    }
}
