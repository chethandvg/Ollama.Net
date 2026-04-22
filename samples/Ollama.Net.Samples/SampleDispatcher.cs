using Ollama.Net.Abstractions;
using Ollama.Net.Configuration;
using Ollama.Net.DependencyInjection;
using Ollama.Net.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ollama.Net.Samples;

/// <summary>
/// Entry point for the sample collection.
///
/// <para>
/// Configuration is layered, highest priority first: command-line args &#8594; environment
/// variables (e.g. <c>OLLAMA_HOST</c>, <c>Ollama__ApiKey</c>) &#8594; <c>appsettings.&#123;DOTNET_ENVIRONMENT&#125;.json</c>
/// &#8594; <c>appsettings.json</c>. A ready-made Cloud profile ships in
/// <c>appsettings.Cloud.json</c>; activate it with <c>DOTNET_ENVIRONMENT=Cloud</c> or
/// <c>--environment Cloud</c>.
/// </para>
/// </summary>
internal static class SampleDispatcher
{
    private const string DefaultOllamaAddress = "http://localhost:11434/";

    public static async Task<int> RunAsync(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        string sample = args[0].ToLowerInvariant();
        string[] hostArgs = args.Length > 1 ? args[1..] : [];

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(hostArgs);

        // Configuration order: appsettings.json -> appsettings.{Environment}.json -> env vars
        // -> command-line. Host.CreateApplicationBuilder already wires all of these; the only
        // reason we remap env-var names is to preserve the user-friendly OLLAMA_* shorthand
        // from earlier sample versions.
        builder.Configuration.AddInMemoryCollection(TranslateLegacyEnvVars());

        // Bind sample-only options.
        builder.Services
            .AddOptions<SampleOptions>()
            .Bind(builder.Configuration.GetSection(SampleOptions.SectionName));

        // Register the Ollama client using the binding-from-configuration overload. This is
        // the recommended pattern for production apps: options come from appsettings/env vars,
        // not from a hard-coded Action.
        IConfigurationSection ollamaSection = builder.Configuration.GetSection("Ollama");
        builder.Services.AddOllamaClient(ollamaSection);

        builder.Logging.AddSimpleConsole(c =>
        {
            c.SingleLine = true;
            c.TimestampFormat = "HH:mm:ss ";
        });

        using IHost host = builder.Build();

        IOllamaClient client = host.Services.GetRequiredService<IOllamaClient>();
        OllamaClientOptions ollamaOptions = host.Services
            .GetRequiredService<IOptions<OllamaClientOptions>>().Value;
        SampleOptions sampleOptions = host.Services
            .GetRequiredService<IOptions<SampleOptions>>().Value;

        string baseAddress = ollamaOptions.BaseAddress?.ToString() ?? DefaultOllamaAddress;
        PrintBanner(builder.Environment.EnvironmentName, ollamaOptions, sampleOptions);

        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            if (!await EnsureOllamaReachableAsync(client, baseAddress, cts.Token))
            {
                return 2;
            }

            return sample switch
            {
                "quickstart" => await QuickStartSample.RunAsync(client, sampleOptions, ollamaOptions, cts.Token),
                "streaming" => await StreamingSample.RunAsync(client, sampleOptions, ollamaOptions, cts.Token),
                "embeddings" => await EmbeddingsSample.RunAsync(client, sampleOptions, cts.Token),
                "models" => await ModelManagementSample.RunAsync(client, sampleOptions, ollamaOptions, cts.Token),
                "toolcalling" => await ToolCallingSample.RunAsync(client, sampleOptions, cts.Token),
                "structured" => await StructuredAndThinkingSample.RunAsync(client, sampleOptions, ollamaOptions, cts.Token),
                _ => UnknownSample(sample)
            };
        }
        catch (OllamaConnectionException ex)
        {
            Console.Error.WriteLine($"ERROR: Could not reach Ollama at {baseAddress}.");
            Console.Error.WriteLine($"       {ex.Message}");
            Console.Error.WriteLine("       Start Ollama with 'ollama serve' or set the OLLAMA_HOST environment variable.");
            return 3;
        }
        catch (OllamaModelNotFoundException ex)
        {
            Console.Error.WriteLine($"ERROR: Model '{ex.ModelName}' is not available.");
            Console.Error.WriteLine($"       Run 'ollama pull {ex.ModelName}' and retry.");
            return 4;
        }
        catch (OllamaAuthenticationException ex)
        {
            Console.Error.WriteLine($"ERROR: Authentication failed: {ex.Message}");
            Console.Error.WriteLine("       For Ollama Cloud, set the Ollama__ApiKey environment variable.");
            return 6;
        }
        catch (OllamaQuotaExceededException ex)
        {
            Console.Error.WriteLine($"ERROR: Ollama Cloud quota exceeded: {ex.Message}");
            Console.Error.WriteLine("       Check https://ollama.com/settings for your plan and usage.");
            return 7;
        }
        catch (OllamaRateLimitedException ex)
        {
            Console.Error.WriteLine($"ERROR: Rate limited: {ex.Message}");
            if (ex.RetryAfter is not null)
            {
                Console.Error.WriteLine($"       Retry after {ex.RetryAfter.Value.TotalSeconds:F0}s.");
            }

            return 8;
        }
        catch (OllamaException ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
            return 5;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Sample cancelled by user.");
            return 130;
        }
    }

    private static async Task<bool> EnsureOllamaReachableAsync(
        IOllamaClient client,
        string baseAddress,
        CancellationToken cancellationToken)
    {
        bool alive = await client.System.PingAsync(cancellationToken);
        if (!alive)
        {
            Console.Error.WriteLine($"ERROR: Ollama did not respond at {baseAddress}.");
            Console.Error.WriteLine("       Start Ollama with 'ollama serve' before running samples,");
            Console.Error.WriteLine("       or activate the Cloud profile (DOTNET_ENVIRONMENT=Cloud).");
        }

        return alive;
    }

    /// <summary>
    /// Maps the legacy <c>OLLAMA_HOST</c> / <c>OLLAMA_MODEL</c> shorthand environment
    /// variables onto the structured config keys bound by the host. Leaves values untouched
    /// if the user has already set them via the canonical <c>Ollama__*</c> form.
    /// </summary>
    private static Dictionary<string, string?> TranslateLegacyEnvVars()
    {
        Dictionary<string, string?> map = new(StringComparer.OrdinalIgnoreCase);

        string? host = Environment.GetEnvironmentVariable("OLLAMA_HOST");
        if (!string.IsNullOrWhiteSpace(host))
        {
            map["Ollama:BaseAddress"] = host;
        }

        string? model = Environment.GetEnvironmentVariable("OLLAMA_MODEL");
        if (!string.IsNullOrWhiteSpace(model))
        {
            map["Ollama:DefaultModel"] = model;
        }

        return map;
    }

    private static void PrintBanner(string environment, OllamaClientOptions options, SampleOptions samples)
    {
        string authStatus = (!string.IsNullOrEmpty(options.ApiKey) || !string.IsNullOrEmpty(options.AuthorizationHeader))
            ? "(set)"
            : "(not set)";

        Console.WriteLine($"Environment      : {environment}");
        Console.WriteLine($"BaseAddress      : {options.BaseAddress}");
        Console.WriteLine($"DefaultModel     : {options.DefaultModel ?? "(unset)"}");
        Console.WriteLine($"ChatModel        : {samples.ChatModel ?? options.DefaultModel ?? "(unset)"}");
        Console.WriteLine($"EmbeddingModel   : {samples.EmbeddingModel}");
        Console.WriteLine($"ToolCallingModel : {samples.ToolCallingModel}");
        Console.WriteLine($"Authorization    : {authStatus}");
        Console.WriteLine();
    }

    private static bool IsHelp(string arg)
        => arg.Equals("help", StringComparison.OrdinalIgnoreCase)
        || arg.Equals("-h", StringComparison.OrdinalIgnoreCase)
        || arg.Equals("--help", StringComparison.OrdinalIgnoreCase);

    private static int UnknownSample(string sample)
    {
        Console.Error.WriteLine($"Unknown sample '{sample}'.");
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Ollama.Net Samples");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run --project samples/Ollama.Net.Samples -- <sample> [args...]");
        Console.WriteLine();
        Console.WriteLine("Samples:");
        Console.WriteLine("  quickstart         Single prompt/response with GenerateAsync");
        Console.WriteLine("  streaming          Streaming chat completion with ChatStreamAsync");
        Console.WriteLine("  embeddings         Create embeddings and print cosine similarity");
        Console.WriteLine("  models             List, show, and inspect running models");
        Console.WriteLine("  toolcalling        Function/tool calling with a weather tool");
        Console.WriteLine("  structured         Structured outputs (JSON schema) + thinking-model streaming");
        Console.WriteLine();
        Console.WriteLine("Configuration (precedence: CLI > env > appsettings.{env}.json > appsettings.json):");
        Console.WriteLine("  appsettings.json          Default (local) configuration with all knobs documented.");
        Console.WriteLine("  appsettings.Cloud.json    Ollama Cloud profile. Activate with DOTNET_ENVIRONMENT=Cloud.");
        Console.WriteLine();
        Console.WriteLine("Environment variables:");
        Console.WriteLine("  DOTNET_ENVIRONMENT        Selects the appsettings.<env>.json overlay (e.g. Cloud).");
        Console.WriteLine("  OLLAMA_HOST               Shorthand for Ollama:BaseAddress.");
        Console.WriteLine("  OLLAMA_MODEL              Shorthand for Ollama:DefaultModel.");
        Console.WriteLine("  Ollama__ApiKey            Bearer token for Ollama Cloud.");
        Console.WriteLine("  Ollama__Timeout           e.g. 00:02:00 for a 2-minute timeout.");
        Console.WriteLine("  Samples__ChatModel        Override the chat/generate model only.");
    }
}
