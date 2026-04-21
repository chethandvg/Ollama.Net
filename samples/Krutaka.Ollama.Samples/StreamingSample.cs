using Krutaka.Ollama.Abstractions;
using Krutaka.Ollama.Configuration;
using Krutaka.Ollama.Models.Common;
using Krutaka.Ollama.Models.Requests;
using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Samples;

/// <summary>Streaming chat completion using <see cref="IOllamaGenerationClient.ChatStreamAsync"/>.</summary>
internal static class StreamingSample
{
    public static async Task<int> RunAsync(
        IOllamaClient client,
        SampleOptions samples,
        OllamaClientOptions ollama,
        CancellationToken cancellationToken)
    {
        string model = QuickStartSample.ResolveChatModel(samples, ollama);
        Console.WriteLine($"Streaming chat — model: {model}  (max {samples.StreamingMaxChunks} chunks)");
        Console.WriteLine("(press Ctrl+C to cancel)");
        Console.WriteLine();

        ChatRequest request = new(
            Model: model,
            Messages:
            [
                new OllamaMessage(OllamaRole.System, "You are a concise technical writer. Keep answers under 120 words."),
                new OllamaMessage(OllamaRole.User, "Write a short poem about the CAP theorem.")
            ]);

        int chunks = 0;
        int totalChars = 0;
        await foreach (ChatResponse chunk in client.Generation.ChatStreamAsync(request, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Message.Content))
            {
                Console.Write(chunk.Message.Content);
                totalChars += chunk.Message.Content.Length;
            }

            chunks++;
            if (chunks >= samples.StreamingMaxChunks)
            {
                Console.WriteLine();
                Console.WriteLine($"(stopped at Samples:StreamingMaxChunks = {samples.StreamingMaxChunks})");
                break;
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"[{chunks} chunks, {totalChars} characters]");
        return 0;
    }
}
