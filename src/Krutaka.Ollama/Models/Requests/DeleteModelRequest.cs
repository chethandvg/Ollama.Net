namespace Krutaka.Ollama.Models.Requests;

/// <summary>
/// Request for deleting a model.
/// </summary>
/// <param name="Model">The name of the model to delete.</param>
public sealed record DeleteModelRequest(
    string Model
);
