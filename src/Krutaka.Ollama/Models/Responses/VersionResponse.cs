namespace Krutaka.Ollama.Models.Responses;

/// <summary>
/// Server version information.
/// </summary>
/// <param name="Version">The Ollama server version string.</param>
public sealed record VersionResponse(
    string Version
);
