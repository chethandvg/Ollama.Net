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

    /// <summary>
    /// When <see langword="true"/>, the client rejects connections whose resolved IP falls
    /// inside a private, link-local, loopback (post-DNS), unique-local, CGNAT, multicast,
    /// or otherwise non-globally-routable range. This is evaluated <em>after</em> DNS
    /// resolution and <em>per redirect hop</em>, so a hostile DNS or <c>/etc/hosts</c>
    /// entry pointing <c>example.com</c> at <c>10.0.0.1</c> still gets rejected —
    /// closing the SSRF gap left by <see cref="AllowInsecureHttp"/>, which operates
    /// on the configured URL only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <see langword="false"/> to preserve back-compat; opt-in for services
    /// that must never reach internal infrastructure (e.g., public-facing AI gateways).
    /// The check is enforced by a <see cref="System.Net.Http.SocketsHttpHandler.ConnectCallback"/>
    /// wired on the primary handler; loopback targets (<c>127.0.0.0/8</c>, <c>::1</c>)
    /// are permitted <em>only</em> when the configured <see cref="BaseAddress"/> host is
    /// itself a loopback name.
    /// </para>
    /// <para>
    /// If you need more control, take ownership of the primary handler via
    /// <c>services.ConfigureOllamaHttpClient(name).ConfigurePrimaryHttpMessageHandler(...)</c>
    /// and install your own <c>ConnectCallback</c>.
    /// </para>
    /// </remarks>
    public bool DisallowPrivateNetworks { get; set; }
}
