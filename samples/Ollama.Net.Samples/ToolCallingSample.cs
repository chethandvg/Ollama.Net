using System.Text.Json;
using Ollama.Net.Abstractions;
using Ollama.Net.Models.Common;
using Ollama.Net.Models.Requests;
using Ollama.Net.Models.Responses;

namespace Ollama.Net.Samples;

/// <summary>
/// Demonstrates tool / function calling. Declares a <c>get_current_weather</c> tool,
/// forwards tool-call invocations to a stub implementation, and feeds the tool result
/// back into a second chat turn so the model can produce a final natural-language answer.
/// </summary>
internal static class ToolCallingSample
{
    public static async Task<int> RunAsync(
        IOllamaClient client,
        SampleOptions samples,
        CancellationToken cancellationToken)
    {
        string model = samples.ToolCallingModel;
        Console.WriteLine($"Tool calling — model: {model}");
        Console.WriteLine();

        using var schema = JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "city": { "type": "string", "description": "The city name, e.g. 'Bangalore'" },
                "unit": { "type": "string", "enum": ["celsius", "fahrenheit"] }
              },
              "required": ["city"]
            }
            """);

        ToolDefinition weatherTool = new(
            Type: "function",
            Function: new FunctionDefinition(
                Name: "get_current_weather",
                Description: "Get the current weather for a city.",
                Parameters: schema.RootElement.Clone()));

        List<OllamaMessage> messages =
        [
            new OllamaMessage(OllamaRole.System, "You are a helpful assistant that uses tools when appropriate."),
            new OllamaMessage(OllamaRole.User, "What is the weather in Bangalore right now?")
        ];

        ChatResponse first = await client.Generation.ChatAsync(
            new ChatRequest(model, [.. messages], Tools: [weatherTool], Stream: false),
            cancellationToken);

        if (first.Message.ToolCalls is null || first.Message.ToolCalls.Length == 0)
        {
            Console.WriteLine("Model chose not to call a tool. Response:");
            Console.WriteLine(first.Message.Content);
            return 0;
        }

        Console.WriteLine($"Model requested {first.Message.ToolCalls.Length} tool call(s):");
        messages.Add(first.Message);

        foreach (ToolCall call in first.Message.ToolCalls)
        {
            string argsJson = JsonSerializer.Serialize(call.Function.Arguments);
            string toolResult = ExecuteTool(call.Function.Name, call.Function.Arguments);
            Console.WriteLine($"  -> {call.Function.Name}({argsJson}) = {toolResult}");
            messages.Add(new OllamaMessage(OllamaRole.Tool, toolResult, ToolName: call.Function.Name));
        }

        ChatResponse second = await client.Generation.ChatAsync(
            new ChatRequest(model, [.. messages], Stream: false),
            cancellationToken);

        Console.WriteLine();
        Console.WriteLine("Final answer:");
        Console.WriteLine(second.Message.Content);
        return 0;
    }

    private static string ExecuteTool(string name, Dictionary<string, JsonElement> arguments)
    {
        return name switch
        {
            "get_current_weather" => GetStubWeather(arguments),
            _ => JsonSerializer.Serialize(new { error = $"Unknown tool '{name}'" })
        };
    }

    private static string GetStubWeather(Dictionary<string, JsonElement> arguments)
    {
        string city = arguments.TryGetValue("city", out JsonElement c) && c.ValueKind == JsonValueKind.String
            ? c.GetString() ?? "unknown"
            : "unknown";

        string unit = arguments.TryGetValue("unit", out JsonElement u) && u.ValueKind == JsonValueKind.String
            ? u.GetString() ?? "celsius"
            : "celsius";

        double temp = string.Equals(unit, "fahrenheit", StringComparison.OrdinalIgnoreCase) ? 82.4 : 28.0;
        return JsonSerializer.Serialize(new
        {
            city,
            unit,
            temperature = temp,
            condition = "partly cloudy",
            source = "stub"
        });
    }
}
