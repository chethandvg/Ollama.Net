using System.Text.Json;
using Ollama.Net.Abstractions;
using Ollama.Net.Configuration;
using Ollama.Net.Models.Common;
using Ollama.Net.Models.Requests;
using Ollama.Net.Models.Responses;

namespace Ollama.Net.Samples;

/// <summary>
/// Demonstrates two features added for the Ollama Cloud surface:
/// <list type="bullet">
/// <item><description>
/// <b>Structured outputs</b> — constrain the reply to a JSON schema via
/// <see cref="OllamaFormat"/>.
/// </description></item>
/// <item><description>
/// <b>Thinking models</b> — opt into the reasoning pass with
/// <see cref="GenerateRequest.Think"/> / <see cref="ChatRequest.Think"/> and read
/// the trace from <see cref="OllamaMessage.Thinking"/>.
/// </description></item>
/// </list>
/// </summary>
internal static class StructuredAndThinkingSample
{
    public static async Task<int> RunAsync(
        IOllamaClient client,
        SampleOptions sampleOptions,
        OllamaClientOptions ollamaOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(sampleOptions);
        ArgumentNullException.ThrowIfNull(ollamaOptions);

        string model = sampleOptions.ChatModel ?? ollamaOptions.DefaultModel ?? "llama3.2";

        // 1) Structured outputs: force the model to reply with a specific JSON shape.
        OllamaFormat schema = OllamaFormat.FromSchema("""
            {
              "type": "object",
              "properties": {
                "name":  { "type": "string" },
                "age":   { "type": "integer" },
                "likes": { "type": "array", "items": { "type": "string" } }
              },
              "required": ["name", "age"]
            }
            """);

        Console.WriteLine("# Structured outputs");
        GenerateResponse structured = await client.Generation.GenerateAsync(
            new GenerateRequest(
                Model:  model,
                Prompt: "Describe a fictional cat as JSON.",
                Format: schema),
            cancellationToken);

        Console.WriteLine(structured.Response);

        // Validate the reply is a JSON object matching the schema we asked for.
        using JsonDocument parsed = JsonDocument.Parse(structured.Response);
        Console.WriteLine($"→ name = {parsed.RootElement.GetProperty("name").GetString()}");
        Console.WriteLine($"→ age  = {parsed.RootElement.GetProperty("age").GetInt32()}");
        Console.WriteLine();

        // 2) Thinking: opt into the reasoning pass. Thinking chunks arrive first on
        // streaming, followed by content chunks.
        Console.WriteLine("# Thinking model (streaming)");
        await foreach (ChatResponse chunk in client.Generation.ChatStreamAsync(
            new ChatRequest(
                Model:    model,
                Messages: [ new OllamaMessage(OllamaRole.User, "What is 6 * 7? Explain briefly.") ],
                Think:    true),
            cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Message.Thinking))
            {
                Console.Write($"[thinking] {chunk.Message.Thinking}");
            }

            if (!string.IsNullOrEmpty(chunk.Message.Content))
            {
                Console.Write(chunk.Message.Content);
            }
        }

        Console.WriteLine();
        return 0;
    }
}
