namespace Ollama.Net.Models.Errors;

/// <summary>
/// Error payload from the Ollama API.
/// </summary>
/// <param name="Error">The error message.</param>
public sealed record OllamaErrorPayload(
    string Error
);
