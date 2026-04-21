using System.Diagnostics;
using System.Reflection;

namespace Ollama.Net.Internal.Diagnostics;

/// <summary>
/// ActivitySource for distributed tracing of Ollama operations.
/// </summary>
internal static class OllamaActivitySource
{
    private static readonly AssemblyName AssemblyName = typeof(OllamaActivitySource).Assembly.GetName();
    private static readonly string Version = AssemblyName.Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// The ActivitySource for Ollama client operations.
    /// </summary>
    public static ActivitySource Instance { get; } = new("Ollama.Net", Version);
}
