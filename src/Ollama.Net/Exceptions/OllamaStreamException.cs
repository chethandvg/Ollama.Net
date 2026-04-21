namespace Ollama.Net.Exceptions;

/// <summary>Exception thrown when a streaming operation fails.</summary>
public sealed class OllamaStreamException : OllamaException
{
    /// <summary>The number of chunks successfully processed before the error.</summary>
    public int ChunksProcessed { get; set; }

    /// <summary>Indicates whether the stream was truncated unexpectedly.</summary>
    public bool IsTruncated { get; set; }

    /// <summary>Initializes a new instance of the <see cref="OllamaStreamException"/> class.</summary>
    public OllamaStreamException() : base("Ollama stream failed.") { }

    /// <summary>Initializes a new instance of the <see cref="OllamaStreamException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public OllamaStreamException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaStreamException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OllamaStreamException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="OllamaStreamException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="chunksProcessed">Number of chunks processed before the error.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public OllamaStreamException(string message, int chunksProcessed, Exception? innerException = null)
        : base(message, innerException ?? new InvalidOperationException("Stream failure"))
    {
        ChunksProcessed = chunksProcessed;
    }
}
