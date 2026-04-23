using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ollama.Net.Abstractions;
using Ollama.Net.Configuration;
using Ollama.Net.DependencyInjection;
using Ollama.Net.Exceptions;
using Ollama.Net.Models.Requests;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Ollama.Net.Tests.Http;

/// <summary>
/// End-to-end tests for the package feedback enhancements: secret rotation via
/// <see cref="IOptionsMonitor{TOptions}"/>, DNS-failure mapping to
/// <see cref="OllamaConfigurationException"/>, and the
/// <c>ConfigureOllamaHttpClient</c> injection seam.
/// </summary>
public sealed class PackageFeedbackEndToEndTests
{
    [Fact]
    public async Task ApiKey_Rotation_IsPickedUp_PerRequest_WithoutRebuildingContainer()
    {
        using WireMockServer server = WireMockServer.Start();
        const string body = """
            {"model":"m","created_at":"2025-01-15T10:00:00Z","response":"ok","done":true}
            """;
        server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody(body));

        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient(o =>
        {
            o.BaseAddress = new Uri(server.Url! + "/");
            o.Timeout = TimeSpan.FromSeconds(10);
            o.ApiKey = "old-key";
        });

        await using ServiceProvider sp = services.BuildServiceProvider();
        IOllamaClient client = sp.GetRequiredService<IOllamaClient>();

        await client.Generation.GenerateAsync(new GenerateRequest("m", "hi"));

        // Rotate the key via PostConfigure-style update without touching the container.
        IOptionsMonitorCache<OllamaClientOptions> cache =
            sp.GetRequiredService<IOptionsMonitorCache<OllamaClientOptions>>();
        cache.TryRemove(Options.DefaultName);
        IConfigureOptions<OllamaClientOptions>[] configurators =
            sp.GetServices<IConfigureOptions<OllamaClientOptions>>().ToArray();
        OllamaClientOptions refreshed = new();
        foreach (IConfigureOptions<OllamaClientOptions> c in configurators)
        {
            c.Configure(refreshed);
        }
        refreshed.ApiKey = "new-key";
        cache.TryAdd(Options.DefaultName, refreshed);

        await client.Generation.GenerateAsync(new GenerateRequest("m", "hi"));

        WireMock.Logging.ILogEntry[] requests = server.LogEntries.ToArray();
        requests.Should().HaveCount(2);
        requests[0].RequestMessage.Headers!["Authorization"][0].Should().Be("Bearer old-key");
        requests[1].RequestMessage.Headers!["Authorization"][0].Should().Be("Bearer new-key");
    }

    [Fact]
    public async Task Request_ToUnresolvableHost_Surfaces_OllamaConfigurationException()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient(o =>
        {
            // RFC2606 reserves .invalid for guaranteed non-resolvable names.
            o.BaseAddress = new Uri("http://ollama-nope.invalid/");
            o.AllowInsecureHttp = true;
            o.Timeout = TimeSpan.FromSeconds(10);
            o.MaxRetries = 0;
        });

        await using ServiceProvider sp = services.BuildServiceProvider();
        IOllamaClient client = sp.GetRequiredService<IOllamaClient>();

        Func<Task> act = () => client.Generation.GenerateAsync(new GenerateRequest("m", "hi"));
        (await act.Should().ThrowAsync<OllamaConfigurationException>())
            .Which.Message.Should().Contain("DNS resolution failed");
    }

    [Fact]
    public void ConfigureOllamaHttpClient_ReturnsBuilder_ForDefaultClient()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient(o => o.BaseAddress = new Uri("http://localhost:11434/"));

        Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder =
            services.ConfigureOllamaHttpClient();

        builder.Should().NotBeNull();
        builder.Name.Should().Be(OllamaServiceCollectionExtensions.DefaultClientName);
    }

    [Fact]
    public void ConfigureOllamaHttpClient_ReturnsBuilder_ForNamedClient()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient("analytics", o => o.BaseAddress = new Uri("http://localhost:11434/"));

        Microsoft.Extensions.DependencyInjection.IHttpClientBuilder builder =
            services.ConfigureOllamaHttpClient("analytics");

        builder.Name.Should().Be("analytics");
    }

    [Fact]
    public async Task ConfigureOllamaHttpClient_ConsumerHandler_IsInvoked()
    {
        using WireMockServer server = WireMockServer.Start();
        server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"model":"m","created_at":"2025-01-15T10:00:00Z","response":"ok","done":true}"""));

        int handlerInvocations = 0;
        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient(o =>
        {
            o.BaseAddress = new Uri(server.Url! + "/");
            o.Timeout = TimeSpan.FromSeconds(10);
        });

        services.ConfigureOllamaHttpClient()
            .AddHttpMessageHandler(() => new CountingHandler(() => Interlocked.Increment(ref handlerInvocations)));

        await using ServiceProvider sp = services.BuildServiceProvider();
        IOllamaClient client = sp.GetRequiredService<IOllamaClient>();

        await client.Generation.GenerateAsync(new GenerateRequest("m", "hi"));

        handlerInvocations.Should().Be(1);
    }

    private sealed class CountingHandler : DelegatingHandler
    {
        private readonly Action _onSend;
        public CountingHandler(Action onSend) => _onSend = onSend;
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _onSend();
            return base.SendAsync(request, cancellationToken);
        }
    }
}
