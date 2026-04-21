namespace Ollama.Net.Models.Responses;

/// <summary>
/// Response from a text generation request.
/// </summary>
/// <param name="Model">The model that generated the response.</param>
/// <param name="CreatedAt">Timestamp when the response was created.</param>
/// <param name="Response">The generated text.</param>
/// <param name="Done">Whether generation is complete.</param>
/// <param name="DoneReason">Reason generation stopped (e.g., "stop", "length").</param>
/// <param name="Context">Context array for continuing the conversation.</param>
/// <param name="TotalDuration">Total time spent generating the response (nanoseconds).</param>
/// <param name="LoadDuration">Time spent loading the model (nanoseconds).</param>
/// <param name="PromptEvalCount">Number of tokens in the prompt.</param>
/// <param name="PromptEvalDuration">Time spent evaluating the prompt (nanoseconds).</param>
/// <param name="EvalCount">Number of tokens generated.</param>
/// <param name="EvalDuration">Time spent generating tokens (nanoseconds).</param>
public sealed record GenerateResponse(
    string Model,
    DateTimeOffset CreatedAt,
    string Response,
    bool Done,
    string? DoneReason = null,
    int[]? Context = null,
    long? TotalDuration = null,
    long? LoadDuration = null,
    int? PromptEvalCount = null,
    long? PromptEvalDuration = null,
    int? EvalCount = null,
    long? EvalDuration = null
);
