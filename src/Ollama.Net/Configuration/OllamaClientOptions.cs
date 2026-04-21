namespace Ollama.Net.Configuration;

/// <summary>
/// Configuration options for the Ollama client.
/// </summary>
public sealed class OllamaClientOptions
{
    /// <summary>
    /// The base address of the Ollama server.
    /// </summary>
    public Uri BaseAddress { get; set; } = new Uri("http://localhost:11434/");

    /// <summary>
    /// Default model to use when not specified in requests.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Request timeout for non-streaming operations.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Default keep-alive duration for models in memory.
    /// </summary>
    public TimeSpan KeepAlive { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// User-Agent string to send with requests.
    /// </summary>
    public string UserAgent { get; set; } = "Ollama.Net";

    /// <summary>
    /// Optional authorization header value sent verbatim (e.g., <c>"Bearer &lt;token&gt;"</c>).
    /// If both this and <see cref="ApiKey"/> are set, <see cref="AuthorizationHeader"/> wins.
    /// </summary>
    public string? AuthorizationHeader { get; set; }

    /// <summary>
    /// Convenience for bearer-token auth (e.g., for Ollama Cloud).
    /// When set and <see cref="AuthorizationHeader"/> is <see langword="null"/>, the client
    /// emits <c>Authorization: Bearer {ApiKey}</c>. Get a key from
    /// <see href="https://ollama.com/settings"/>.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Allow HTTP connections to non-loopback addresses.
    /// By default, only HTTPS or HTTP to loopback/localhost is allowed.
    /// </summary>
    public bool AllowInsecureHttp { get; set; }
}
