using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Ollama.Net.Abstractions;
using Ollama.Net.Clients;
using Ollama.Net.Configuration;
using Ollama.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Ollama.Net.DependencyInjection;
/// <summary>
/// Extension methods for registering Ollama client services with <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// <para>
/// Two registration modes are supported:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Default client</b> — <see cref="AddOllamaClient(IServiceCollection, Action{OllamaClientOptions}?)"/>
/// registers one singleton <see cref="IOllamaClient"/> backed by <see cref="IHttpClientFactory"/>.
/// Resolve it with <c>sp.GetRequiredService&lt;IOllamaClient&gt;()</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Named client</b> — <see cref="AddOllamaClient(IServiceCollection, string, Action{OllamaClientOptions}?)"/>
/// registers a keyed <see cref="IOllamaClient"/>. Resolve with
/// <c>sp.GetRequiredKeyedService&lt;IOllamaClient&gt;("name")</c> or
/// <c>sp.GetRequiredService&lt;IOllamaClientFactory&gt;().CreateClient("name")</c>.
/// Multiple named clients may coexist alongside the default one.
/// </description>
/// </item>
/// </list>
/// </remarks>
public static class OllamaServiceCollectionExtensions
{
    /// <summary>The name used for the default (unnamed) client in <see cref="IHttpClientFactory"/>.</summary>
    public const string DefaultClientName = "Ollama.Net";

    /// <summary>
    /// Base address of Ollama Cloud (the hosted, managed Ollama service).
    /// See <see href="https://ollama.com/cloud"/> for model catalogue and pricing.
    /// </summary>
    public static readonly Uri OllamaCloudBaseAddress = new("https://ollama.com/");

    /// <summary>
    /// Returns the <see cref="IHttpClientBuilder"/> for the underlying named
    /// <see cref="HttpClient"/> registered by <see cref="AddOllamaClient(IServiceCollection, Action{OllamaClientOptions}?)"/>
    /// (or one of its overloads). Use this to attach additional delegating handlers,
    /// override the primary handler (e.g., to install a custom
    /// <see cref="System.Net.Http.SocketsHttpHandler.ConnectCallback"/> enforcing
    /// per-hop SSRF invariants), or adjust the handler lifetime.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">
    /// The same name passed to <c>AddOllamaClient(name, …)</c>, or <see langword="null"/>
    /// for the default client.
    /// </param>
    /// <remarks>
    /// Must be called <em>after</em> <c>AddOllamaClient(...)</c> so the named client
    /// is already registered. Calls are idempotent — <see cref="IHttpClientBuilder"/>
    /// returned here targets the same named client and composes with the handlers
    /// already wired by the package.
    /// </remarks>
    public static IHttpClientBuilder ConfigureOllamaHttpClient(
        this IServiceCollection services,
        string? name = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        string httpClientName = name ?? DefaultClientName;
        // AddHttpClient is idempotent for an existing named client — returns a builder
        // targeting the same registration, so consumers can layer ConfigurePrimaryHttpMessageHandler,
        // AddHttpMessageHandler, etc. on top of the package defaults.
        return services.AddHttpClient(httpClientName);
    }

    /// <summary>
    /// Registers the default <see cref="IOllamaClient"/>.
    /// </summary>
    public static IServiceCollection AddOllamaClient(
        this IServiceCollection services,
        Action<OllamaClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        return AddOllamaClientCore(services, name: null, configure);
    }

    /// <summary>
    /// Registers the default <see cref="IOllamaClient"/> pre-configured for
    /// <see href="https://ollama.com/cloud">Ollama Cloud</see>.
    /// Equivalent to <see cref="AddOllamaClient(IServiceCollection, Action{OllamaClientOptions}?)"/>
    /// with <see cref="OllamaClientOptions.BaseAddress"/> set to
    /// <see cref="OllamaCloudBaseAddress"/> and <see cref="OllamaClientOptions.ApiKey"/> set
    /// to <paramref name="apiKey"/>. Additional customisation can be applied via
    /// <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">API key obtained from <see href="https://ollama.com/settings"/>.</param>
    /// <param name="configure">Optional callback to further customise options.</param>
    public static IServiceCollection AddOllamaCloudClient(
        this IServiceCollection services,
        string apiKey,
        Action<OllamaClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        return AddOllamaClientCore(services, name: null, options =>
        {
            options.BaseAddress = OllamaCloudBaseAddress;
            options.ApiKey = apiKey;
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Registers a named <see cref="IOllamaClient"/> pre-configured for Ollama Cloud.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">Name used to resolve the client via keyed DI or <see cref="IOllamaClientFactory"/>.</param>
    /// <param name="apiKey">API key obtained from <see href="https://ollama.com/settings"/>.</param>
    /// <param name="configure">Optional callback to further customise options.</param>
    public static IServiceCollection AddOllamaCloudClient(
        this IServiceCollection services,
        string name,
        string apiKey,
        Action<OllamaClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        return AddOllamaClientCore(services, name, options =>
        {
            options.BaseAddress = OllamaCloudBaseAddress;
            options.ApiKey = apiKey;
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Registers a named <see cref="IOllamaClient"/> accessible via keyed DI and <see cref="IOllamaClientFactory"/>.
    /// </summary>
    public static IServiceCollection AddOllamaClient(
        this IServiceCollection services,
        string name,
        Action<OllamaClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return AddOllamaClientCore(services, name, configure);
    }

    /// <summary>
    /// Registers the default <see cref="IOllamaClient"/> using values bound from <paramref name="configuration"/>.
    /// </summary>
    /// <remarks>
    /// This overload uses reflection-based configuration binding. For trim/AOT deployments,
    /// prefer the <see cref="AddOllamaClient(IServiceCollection, Action{OllamaClientOptions}?)"/>
    /// overload and set properties directly.
    /// </remarks>
    [RequiresUnreferencedCode("Binds OllamaClientOptions from IConfiguration via reflection.")]
    [RequiresDynamicCode("Binds OllamaClientOptions from IConfiguration via reflection.")]
    public static IServiceCollection AddOllamaClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OllamaClientOptions>(configuration);
        return AddOllamaClientCore(services, name: null, configure: null);
    }

    /// <summary>
    /// Registers a named <see cref="IOllamaClient"/> using values bound from <paramref name="configuration"/>.
    /// </summary>
    /// <remarks>
    /// This overload uses reflection-based configuration binding. For trim/AOT deployments,
    /// prefer the <see cref="AddOllamaClient(IServiceCollection, string, Action{OllamaClientOptions}?)"/>
    /// overload and set properties directly.
    /// </remarks>
    [RequiresUnreferencedCode("Binds OllamaClientOptions from IConfiguration via reflection.")]
    [RequiresDynamicCode("Binds OllamaClientOptions from IConfiguration via reflection.")]
    public static IServiceCollection AddOllamaClient(
        this IServiceCollection services,
        string name,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OllamaClientOptions>(name, configuration);
        return AddOllamaClientCore(services, name, configure: null);
    }

    private static IServiceCollection AddOllamaClientCore(
        IServiceCollection services,
        string? name,
        Action<OllamaClientOptions>? configure)
    {
        string optionsName = name ?? Options.DefaultName;
        string httpClientName = name ?? DefaultClientName;

        if (configure is not null)
        {
            services.Configure(optionsName, configure);
        }

        services.AddOptions<OllamaClientOptions>(optionsName).ValidateOnStart();
        services.TryAddSingleton<IValidateOptions<OllamaClientOptions>, OllamaClientOptionsValidator>();

        // Register a named HttpClient backed by IHttpClientFactory. The name is used to
        // resolve the correct base address per-client and is shared by the resilience handler.
        IHttpClientBuilder builder = services
            .AddHttpClient(httpClientName, (sp, client) =>
            {
                OllamaClientOptions opts = sp
                    .GetRequiredService<IOptionsMonitor<OllamaClientOptions>>()
                    .Get(optionsName);
                client.BaseAddress = opts.BaseAddress;
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                OllamaClientOptions opts = sp
                    .GetRequiredService<IOptionsMonitor<OllamaClientOptions>>()
                    .Get(optionsName);
                return BuildPrimaryHandler(opts);
            });

        builder
            .AddStandardResilienceHandler()
            .Configure((resilience, sp) =>
            {
                OllamaClientOptions opts = sp
                    .GetRequiredService<IOptionsMonitor<OllamaClientOptions>>()
                    .Get(optionsName);

                ConfigureResilience(resilience, opts);
            });

        if (name is null)
        {
            services.TryAddSingleton<IOllamaClient>(sp =>
                BuildClient(sp, httpClientName, optionsName));
        }
        else
        {
            services.AddKeyedSingleton<IOllamaClient>(
                name,
                (sp, _) => BuildClient(sp, httpClientName, optionsName));
        }

        services.TryAddSingleton<IOllamaClientFactory, OllamaClientFactory>();

        return services;
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions resilience, OllamaClientOptions options)
    {
        // Honour the per-client configured retry budget (validated to 0..10).
        // The underlying RetryStrategyOptions requires MaxRetryAttempts >= 1, so for a
        // value of 0 we leave MaxRetryAttempts at its validated default and short-circuit
        // ShouldHandle below to always return false — effectively disabling retries.
        bool retriesDisabled = options.MaxRetries <= 0;
        if (!retriesDisabled)
        {
            resilience.Retry.MaxRetryAttempts = options.MaxRetries;
        }

        // Streaming responses must never be retried because partial NDJSON would be
        // corrupted if replayed. The client sets the X-Ollama-Stream request header so the
        // handler can opt out on both the response and the exception paths.
        resilience.Retry.ShouldHandle = args =>
        {
            if (retriesDisabled)
            {
                return ValueTask.FromResult(false);
            }

            HttpRequestMessage? requestMessage = args.Outcome.Result?.RequestMessage
                                                 ?? args.Context.GetRequestMessage();
            if (requestMessage is not null
                && requestMessage.Headers.Contains(OllamaRequestHeaders.Stream))
            {
                return ValueTask.FromResult(false);
            }

            if (args.Outcome.Exception is HttpRequestException)
            {
                return ValueTask.FromResult(true);
            }

            HttpResponseMessage? result = args.Outcome.Result;
            if (result is null)
            {
                return ValueTask.FromResult(false);
            }

            int code = (int)result.StatusCode;
            // Retry only on transient gateway / service unavailable responses.
            return ValueTask.FromResult(code == 502 || code == 503 || code == 504 || code == 408);
        };
    }

    private static OllamaClient BuildClient(IServiceProvider sp, string httpClientName, string optionsName)
    {
        HttpClient http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName);
        IOptionsMonitor<OllamaClientOptions> optionsMonitor =
            sp.GetRequiredService<IOptionsMonitor<OllamaClientOptions>>();
        ILogger<OllamaHttpClient> logger = sp.GetRequiredService<ILogger<OllamaHttpClient>>();

        var ollamaHttp = new OllamaHttpClient(http, optionsMonitor, optionsName, logger);

        return new OllamaClient(
            new OllamaGenerationClient(ollamaHttp),
            new OllamaEmbeddingsClient(ollamaHttp),
            new OllamaModelsClient(ollamaHttp),
            new OllamaSystemClient(ollamaHttp));
    }

    /// <summary>
    /// Builds the primary <see cref="HttpMessageHandler"/> for the Ollama client.
    /// When <see cref="OllamaClientOptions.DisallowPrivateNetworks"/> is set we wire up a
    /// <see cref="SocketsHttpHandler.ConnectCallback"/> that re-validates every
    /// DNS-resolved IP (including each redirect hop) — the post-DNS half of SSRF
    /// defense that <see cref="OllamaClientOptions.AllowInsecureHttp"/> cannot cover
    /// because it only inspects the configured URL.
    /// </summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Handler ownership is transferred to the HttpClient created by IHttpClientFactory.")]
    private static SocketsHttpHandler BuildPrimaryHandler(OllamaClientOptions options)
    {
        SocketsHttpHandler handler = new();

        if (options.DisallowPrivateNetworks)
        {
            // Allow loopback only when the configured base address is itself a loopback
            // host — this keeps "http://localhost:11434" working in dev while still
            // rejecting a public hostname that resolves to 127.0.0.1.
            bool allowLoopback = IsLoopbackBaseAddress(options.BaseAddress);
            handler.ConnectCallback = PrivateNetworkGuard.BuildConnectCallback(allowLoopback);
        }

        return handler;
    }

    private static bool IsLoopbackBaseAddress(Uri? baseAddress)
    {
        if (baseAddress is null || !baseAddress.IsAbsoluteUri)
        {
            return false;
        }

        string host = baseAddress.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return System.Net.IPAddress.TryParse(host, out System.Net.IPAddress? ip)
               && System.Net.IPAddress.IsLoopback(ip);
    }

    [SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by the DI container.")]
    private sealed class OllamaClientFactory : IOllamaClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public OllamaClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IOllamaClient CreateClient(string name = "")
        {
            ArgumentNullException.ThrowIfNull(name);

            if (string.IsNullOrEmpty(name))
            {
                return _serviceProvider.GetRequiredService<IOllamaClient>();
            }

            return _serviceProvider.GetRequiredKeyedService<IOllamaClient>(name);
        }
    }
}
