using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Ollama.Net.Internal.Diagnostics;

/// <summary>
/// Metrics for monitoring Ollama client operations.
/// </summary>
internal static class OllamaMetrics
{
    private static readonly AssemblyName AssemblyName = typeof(OllamaMetrics).Assembly.GetName();
    private static readonly string Version = AssemblyName.Version?.ToString() ?? "1.0.0";

    private static readonly Meter Meter = new("Ollama.Net", Version);

    /// <summary>
    /// Counter for total requests sent.
    /// </summary>
    public static Counter<long> RequestsTotal { get; } = 
        Meter.CreateCounter<long>("ollama.requests.total", "requests", "Total number of requests sent");

    /// <summary>
    /// Counter for failed requests.
    /// </summary>
    public static Counter<long> RequestsFailed { get; } = 
        Meter.CreateCounter<long>("ollama.requests.failed", "requests", "Total number of failed requests");

    /// <summary>
    /// Histogram for request duration.
    /// </summary>
    public static Histogram<double> RequestDuration { get; } = 
        Meter.CreateHistogram<double>("ollama.request.duration", "ms", "Duration of requests in milliseconds");

    /// <summary>
    /// Counter for tokens generated.
    /// </summary>
    public static Counter<long> TokensGenerated { get; } = 
        Meter.CreateCounter<long>("ollama.tokens.generated", "tokens", "Total number of tokens generated");
}
