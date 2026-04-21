using Krutaka.Ollama.Abstractions;
using Krutaka.Ollama.Configuration;
using Krutaka.Ollama.Models.Requests;
using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Samples;

/// <summary>Single prompt / response using <see cref="IOllamaGenerationClient.GenerateAsync"/>.</summary>
internal static class QuickStartSample
{
    public static async Task<int> RunAsync(
        IOllamaClient client,
        SampleOptions samples,
        OllamaClientOptions ollama,
        CancellationToken cancellationToken)
    {
        string model = ResolveChatModel(samples, ollama);
        Console.WriteLine($"Quick-start — model: {model}");
        Console.WriteLine();

        GenerateResponse response = await client.Generation.GenerateAsync(
            new GenerateRequest(model, "Explain quantum entanglement in exactly two sentences."),
            cancellationToken);

        Console.WriteLine(response.Response);
        Console.WriteLine();
        Console.WriteLine($"[done_reason={response.DoneReason} eval_count={response.EvalCount} total_duration={FormatNanos(response.TotalDuration)}]");
        return 0;
    }

    /// <summary>
    /// Resolves the chat/generate model using the documented precedence:
    /// <c>Samples:ChatModel</c> &#8594; <c>Ollama:DefaultModel</c> &#8594; the caller-supplied fallback.
    /// </summary>
    internal static string ResolveChatModel(SampleOptions samples, OllamaClientOptions ollama, string fallback = "llama3.2")
    {
        if (!string.IsNullOrWhiteSpace(samples.ChatModel))
        {
            return samples.ChatModel;
        }

        if (!string.IsNullOrWhiteSpace(ollama.DefaultModel))
        {
            return ollama.DefaultModel;
        }

        return fallback;
    }

    internal static string FormatNanos(long? nanos)
        => nanos.HasValue
            ? $"{TimeSpan.FromTicks(nanos.Value / 100).TotalSeconds:F2}s"
            : "(n/a)";
}
