using System.Net;

namespace Krutaka.Ollama.Exceptions;

/// <summary>Exception thrown for server-side errors (500).</summary>
public sealed class OllamaServerException : OllamaApiException
{
    /// <summary>Indicates whether the error is due to out-of-memory conditions.</summary>
    public bool IsOutOfMemory { get; set; }

    /// <summary>Indicates whether the error is due to disk space issues.</summary>
    public bool IsDiskFull { get; set; }

    /// <summary>Initializes a new instance of the <see cref="OllamaServerException"/> class.</summary>
    public OllamaServerException() : base("Ollama server error.", HttpStatusCode.InternalServerError) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaServerException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaServerException(string message) : base(message, HttpStatusCode.InternalServerError) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaServerException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaServerException(string message, Exception innerException) : base(message, innerException) { }
}
