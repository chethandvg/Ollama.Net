using System.Text.Json;

namespace Krutaka.Ollama.Exceptions;

/// <summary>Exception thrown when JSON deserialization fails.</summary>
public sealed class OllamaDeserializationException : OllamaException
{
    /// <summary>The raw content that failed to deserialize.</summary>
    public string? RawContent { get; set; }

    /// <summary>Initializes a new instance of the <see cref="OllamaDeserializationException"/> class.</summary>
    public OllamaDeserializationException() : base("Failed to deserialize Ollama response.") { }

    /// <summary>Initializes a new instance of the <see cref="OllamaDeserializationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaDeserializationException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaDeserializationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaDeserializationException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaDeserializationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner <see cref="JsonException"/>.</param>
    public OllamaDeserializationException(string message, JsonException innerException)
        : base(message, innerException) { }
}
