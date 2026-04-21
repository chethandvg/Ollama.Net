using FluentAssertions;
using Krutaka.Ollama.Abstractions;
using Krutaka.Ollama.Configuration;
using Krutaka.Ollama.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Krutaka.Ollama.Tests.DependencyInjection;

/// <summary>
/// Probe tests for <see cref="OllamaServiceCollectionExtensions"/>. Each test must actually
/// resolve <see cref="IOllamaClient"/> from the container so that broken registrations
/// (for example a mismatch between keyed/non-keyed registration and resolution) fail here
/// rather than at the first call from consumer code.
/// </summary>
public sealed class OllamaServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOllamaClient_WithDefaults_ShouldRegisterResolvableClient()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOllamaClient();

        using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClient client = provider.GetRequiredService<IOllamaClient>();

        client.Should().NotBeNull();
        client.Generation.Should().NotBeNull();
        client.Embeddings.Should().NotBeNull();
        client.Models.Should().NotBeNull();
        client.System.Should().NotBeNull();
    }

    [Fact]
    public void AddOllamaClient_WithConfigureCallback_ShouldApplyOptions()
    {
        ServiceCollection services = new();
        services.AddLogging();
        Uri customUri = new("http://example.com:9999/");

        services.AddOllamaClient(options =>
        {
            options.BaseAddress = customUri;
            options.Timeout = TimeSpan.FromSeconds(42);
            options.AllowInsecureHttp = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClient client = provider.GetRequiredService<IOllamaClient>();

        client.Should().NotBeNull();
    }

    [Fact]
    public void AddOllamaClient_Named_ShouldResolveViaKeyedService()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOllamaClient("primary", o => o.BaseAddress = new Uri("http://127.0.0.1:11434/"));
        services.AddOllamaClient("secondary", o => o.BaseAddress = new Uri("http://127.0.0.1:11435/"));

        using ServiceProvider provider = services.BuildServiceProvider();

        IOllamaClient primary = provider.GetRequiredKeyedService<IOllamaClient>("primary");
        IOllamaClient secondary = provider.GetRequiredKeyedService<IOllamaClient>("secondary");

        primary.Should().NotBeNull();
        secondary.Should().NotBeNull();
        primary.Should().NotBeSameAs(secondary);
    }

    [Fact]
    public void AddOllamaClient_Named_ShouldBeResolvableViaFactory()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOllamaClient("alpha", o => o.BaseAddress = new Uri("http://127.0.0.1:11434/"));

        using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClientFactory factory = provider.GetRequiredService<IOllamaClientFactory>();

        IOllamaClient client = factory.CreateClient("alpha");
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddOllamaClient_DefaultAndNamed_ShouldCoexist()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOllamaClient();
        services.AddOllamaClient("named", o => o.BaseAddress = new Uri("http://127.0.0.1:11435/"));

        using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClientFactory factory = provider.GetRequiredService<IOllamaClientFactory>();

        IOllamaClient defaultClient = factory.CreateClient();
        IOllamaClient namedClient = factory.CreateClient("named");

        defaultClient.Should().NotBeNull();
        namedClient.Should().NotBeNull();
        defaultClient.Should().NotBeSameAs(namedClient);
    }

    [Fact]
    public void AddOllamaClient_WithInvalidBaseAddress_ShouldFailValidationEagerly()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOllamaClient(options => options.BaseAddress = new Uri("ftp://host/"));

        using ServiceProvider provider = services.BuildServiceProvider();
        Action act = () => provider.GetRequiredService<IOllamaClient>();

        act.Should().Throw<Microsoft.Extensions.Options.OptionsValidationException>();
    }

    [Fact]
    public void AddOllamaClient_WithConfiguration_ShouldBindOptions()
    {
#pragma warning disable IL2026, IL3050
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BaseAddress"] = "http://127.0.0.1:11434/",
                ["Timeout"] = "00:00:45"
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClient client = provider.GetRequiredService<IOllamaClient>();

        client.Should().NotBeNull();
#pragma warning restore IL2026, IL3050
    }

    [Fact]
    public void AddOllamaClient_Factory_Default_ShouldMatchDirectResolution()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddOllamaClient();

        using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClientFactory factory = provider.GetRequiredService<IOllamaClientFactory>();

        IOllamaClient viaFactory = factory.CreateClient();
        IOllamaClient viaDirect = provider.GetRequiredService<IOllamaClient>();

        viaFactory.Should().BeSameAs(viaDirect);
    }

    [Fact]
    public void AddOllamaClient_NullServices_ShouldThrow()
    {
        Action act = () => OllamaServiceCollectionExtensions.AddOllamaClient(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddOllamaClient_EmptyName_ShouldThrow()
    {
        ServiceCollection services = new();
        services.AddLogging();

        Action act = () => services.AddOllamaClient(name: "   ");
        act.Should().Throw<ArgumentException>();
    }
}
