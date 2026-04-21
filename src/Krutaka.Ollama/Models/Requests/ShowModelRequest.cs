namespace Krutaka.Ollama.Models.Requests;

/// <summary>
/// Request for showing details about a model.
/// </summary>
/// <param name="Model">The name of the model to show.</param>
/// <param name="Verbose">Include full details in the response.</param>
public sealed record ShowModelRequest(
    string Model,
    bool? Verbose = null
);
