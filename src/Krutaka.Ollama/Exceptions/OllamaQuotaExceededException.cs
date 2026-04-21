using System.Net;

namespace Krutaka.Ollama.Exceptions;

/// <summary>
/// Exception thrown when the server returns <c>402 Payment Required</c>.
/// Typically raised by Ollama Cloud when the account's subscription plan is
/// exhausted (e.g., monthly token/request quota). Distinct from
/// <see cref="OllamaRateLimitedException"/>, which indicates a transient
/// per-second rate limit that should be retried after a short delay.
/// </summary>
public sealed class OllamaQuotaExceededException : OllamaApiException
{
    /// <summary>Initializes a new instance of the <see cref="OllamaQuotaExceededException"/> class.</summary>
    public OllamaQuotaExceededException()
        : base("Ollama Cloud quota exceeded.", HttpStatusCode.PaymentRequired) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaQuotaExceededException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaQuotaExceededException(string message)
        : base(message, HttpStatusCode.PaymentRequired) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaQuotaExceededException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaQuotaExceededException(string message, Exception innerException)
        : base(message, innerException) { }
}
