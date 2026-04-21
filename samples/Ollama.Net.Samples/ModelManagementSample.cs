using Ollama.Net.Abstractions;
using Ollama.Net.Configuration;
using Ollama.Net.Models.Requests;
using Ollama.Net.Models.Responses;

namespace Ollama.Net.Samples;

/// <summary>Demonstrates model management: list, show, running, and streaming pull progress.</summary>
internal static class ModelManagementSample
{
    public static async Task<int> RunAsync(
        IOllamaClient client,
        SampleOptions samples,
        OllamaClientOptions ollama,
        CancellationToken cancellationToken)
    {
        VersionResponse version = await client.System.GetVersionAsync(cancellationToken);
        Console.WriteLine($"Ollama server version: {version.Version}");
        Console.WriteLine();

        ModelList list = await client.Models.ListModelsAsync(cancellationToken);
        Console.WriteLine($"Installed models ({list.Models.Length}):");
        foreach (ModelInfo m in list.Models)
        {
            Console.WriteLine($"  - {m.Name,-40} size={FormatBytes(m.Size)}  modified={m.ModifiedAt:yyyy-MM-dd}");
        }

        Console.WriteLine();

        RunningModelList running = await client.Models.ListRunningModelsAsync(cancellationToken);
        Console.WriteLine($"Running models ({running.Models.Length}):");
        foreach (RunningModel m in running.Models)
        {
            Console.WriteLine($"  - {m.Name} (expires_at={m.ExpiresAt:HH:mm:ss})");
        }

        Console.WriteLine();

        if (list.Models.Length > 0)
        {
            string first = list.Models[0].Name;
            Console.WriteLine($"Show details for '{first}':");
            ShowModelResponse details = await client.Models.ShowModelAsync(
                new ShowModelRequest(first),
                cancellationToken);
            Console.WriteLine($"  modelfile (first 100 chars): {Head(details.Modelfile)}");
            Console.WriteLine($"  parameters: {Head(details.Parameters)}");
            Console.WriteLine($"  template: {Head(details.Template)}");
            Console.WriteLine();
        }

        if (string.Equals(Environment.GetEnvironmentVariable("PULL_MODEL"), "1", StringComparison.Ordinal))
        {
            string pullTarget = !string.IsNullOrWhiteSpace(ollama.DefaultModel)
                ? ollama.DefaultModel!
                : "tinyllama";
            Console.WriteLine($"Streaming pull for '{pullTarget}' (set PULL_MODEL=0 to skip):");
            await foreach (ProgressResponse progress in client.Models.PullModelStreamAsync(
                new PullModelRequest(pullTarget), cancellationToken))
            {
                if (progress.Total.HasValue && progress.Completed.HasValue && progress.Total.Value > 0)
                {
                    double pct = 100.0 * progress.Completed.Value / progress.Total.Value;
                    Console.Write($"\r  {progress.Status,-32} {pct,6:F2}%");
                }
                else
                {
                    Console.Write($"\r  {progress.Status,-32}                  ");
                }
            }

            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("(Set PULL_MODEL=1 to demonstrate streaming pull progress.)");
        }

        return 0;
    }

    private static string Head(string? s, int max = 100)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "(empty)";
        }

        string single = s.Replace('\n', ' ').Replace('\r', ' ');
        return single.Length <= max ? single : single[..max] + "...";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value,7:F2} {units[unit]}";
    }
}
