using System.Net;

namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when a model needs to be pulled before use.</summary>
public sealed class OllamaModelPullRequiredException : OllamaApiException
{
    /// <summary>The name of the model that needs to be pulled.</summary>
    public string? ModelName { get; set; }

    /// <summary>Initializes a new instance of the <see cref="OllamaModelPullRequiredException"/> class.</summary>
    public OllamaModelPullRequiredException()
        : base("Model must be pulled before use.", HttpStatusCode.NotFound) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaModelPullRequiredException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaModelPullRequiredException(string message) : base(message, HttpStatusCode.NotFound) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaModelPullRequiredException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaModelPullRequiredException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaModelPullRequiredException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="modelName">The model name.</param>
    public OllamaModelPullRequiredException(string message, string? modelName)
        : base(message, HttpStatusCode.NotFound)
    {
        ModelName = modelName;
    }
}
