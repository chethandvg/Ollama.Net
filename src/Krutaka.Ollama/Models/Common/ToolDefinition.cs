using System.Text.Json;

namespace Krutaka.Ollama.Models.Common;

/// <summary>
/// Defines a tool (function) that the model can call.
/// </summary>
/// <param name="Type">The type of tool (always "function").</param>
/// <param name="Function">The function definition.</param>
public sealed record ToolDefinition(
    string Type,
    FunctionDefinition Function
)
{
    /// <summary>
    /// Creates a new function tool definition.
    /// </summary>
    /// <param name="function">The function definition.</param>
    /// <returns>A tool definition with Type="function".</returns>
    public static ToolDefinition CreateFunction(FunctionDefinition function)
    {
        return new ToolDefinition("function", function);
    }
}
