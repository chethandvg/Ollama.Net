using System.Net;

namespace Krutaka.Ollama.Exceptions;

/// <summary>
/// Base exception for all Ollama-related errors.
/// </summary>
public class OllamaException : Exception
{
    /// <summary>
    /// The API endpoint that was called.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>Initializes a new instance of the <see cref="OllamaException"/> class.</summary>
    public OllamaException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public OllamaException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
