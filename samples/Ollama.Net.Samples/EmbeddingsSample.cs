using Ollama.Net.Abstractions;
using Ollama.Net.Models.Requests;
using Ollama.Net.Models.Responses;

namespace Ollama.Net.Samples;

/// <summary>Creates embeddings for several phrases and prints pairwise cosine similarity.</summary>
internal static class EmbeddingsSample
{
    public static async Task<int> RunAsync(
        IOllamaClient client,
        SampleOptions samples,
        CancellationToken cancellationToken)
    {
        string model = samples.EmbeddingModel;
        string[] phrases =
        [
            "The cat sat on the mat.",
            "A feline rested on the rug.",
            "The stock market closed higher today."
        ];

        Console.WriteLine($"Embeddings — model: {model}");
        Console.WriteLine();

        EmbedResponse response = await client.Embeddings.EmbedAsync(
            new EmbedRequest(model, phrases),
            cancellationToken);

        Console.WriteLine($"Received {response.Embeddings.Length} vectors of dimension {response.Embeddings[0].Length}.");
        Console.WriteLine();
        Console.WriteLine("Pairwise cosine similarity:");
        for (int i = 0; i < phrases.Length; i++)
        {
            for (int j = i + 1; j < phrases.Length; j++)
            {
                double sim = CosineSimilarity(response.Embeddings[i], response.Embeddings[j]);
                Console.WriteLine($"  [{i}] \"{Truncate(phrases[i])}\"");
                Console.WriteLine($"  [{j}] \"{Truncate(phrases[j])}\"");
                Console.WriteLine($"    -> similarity = {sim:F4}");
                Console.WriteLine();
            }
        }

        return 0;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new InvalidOperationException("Vectors must be the same dimension.");
        }

        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += (double)a[i] * a[i];
            magB += (double)b[i] * b[i];
        }

        double denom = Math.Sqrt(magA) * Math.Sqrt(magB);
        return denom == 0 ? 0 : dot / denom;
    }

    private static string Truncate(string s, int max = 48)
        => s.Length <= max ? s : s[..max] + "...";
}
