using System.Net;

namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when a requested model is not found (404).</summary>
public sealed class OllamaModelNotFoundException : OllamaApiException
{
    /// <summary>The name of the model that was not found.</summary>
    public string? ModelName { get; set; }

    /// <summary>Initializes a new instance of the <see cref="OllamaModelNotFoundException"/> class.</summary>
    public OllamaModelNotFoundException()
        : base("The requested model was not found on the Ollama server.", HttpStatusCode.NotFound) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaModelNotFoundException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaModelNotFoundException(string message) : base(message, HttpStatusCode.NotFound) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaModelNotFoundException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaModelNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaModelNotFoundException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="modelName">The model name.</param>
    public OllamaModelNotFoundException(string message, string? modelName)
        : base(message, HttpStatusCode.NotFound)
    {
        ModelName = modelName;
    }
}
