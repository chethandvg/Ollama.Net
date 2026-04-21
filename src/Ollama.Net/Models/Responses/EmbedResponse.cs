using System.Diagnostics.CodeAnalysis;

namespace Ollama.Net.Models.Responses;

/// <summary>
/// Response from an embed request.
/// </summary>
/// <param name="Model">The model that generated the embeddings.</param>
/// <param name="Embeddings">The generated embeddings, one per input text.</param>
/// <param name="TotalDuration">Total time spent generating embeddings (nanoseconds).</param>
/// <param name="LoadDuration">Time spent loading the model (nanoseconds).</param>
/// <param name="PromptEvalCount">Number of tokens processed.</param>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "DTO property backed by JSON array")]
public sealed record EmbedResponse(
    string Model,
    float[][] Embeddings,
    long? TotalDuration = null,
    long? LoadDuration = null,
    int? PromptEvalCount = null
);
