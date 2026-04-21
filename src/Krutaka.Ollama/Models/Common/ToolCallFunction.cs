using System.Text.Json;

namespace Krutaka.Ollama.Models.Common;

/// <summary>
/// Details of a function call within a tool call.
/// </summary>
/// <param name="Name">The name of the function being called.</param>
/// <param name="Arguments">The arguments to pass to the function as a dictionary.</param>
public sealed record ToolCallFunction(
    string Name,
    Dictionary<string, JsonElement> Arguments
);
