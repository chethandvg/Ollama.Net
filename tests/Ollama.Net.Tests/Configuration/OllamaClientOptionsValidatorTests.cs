using FluentAssertions;
using Ollama.Net.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ollama.Net.Tests.Configuration;

/// <summary>
/// Tests for <see cref="OllamaClientOptionsValidator"/>.
/// </summary>
public sealed class OllamaClientOptionsValidatorTests
{
    private readonly OllamaClientOptionsValidator _validator = new();

    [Fact]
    public void Should_SucceedValidation_WhenOptionsAreValid()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = new Uri("https://ollama.example.com/"),
            Timeout = TimeSpan.FromSeconds(60),
            MaxRetries = 3
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Should_FailValidation_WhenBaseAddressIsNull()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = null!
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("BaseAddress must not be null");
    }

    [Fact]
    public void Should_FailValidation_WhenBaseAddressIsRelative()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = new Uri("/relative", UriKind.Relative)
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("absolute URI");
    }

    [Fact]
    public void Should_FailValidation_WhenHttpToNonLoopbackWithoutAllowInsecure()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = new Uri("http://example.com/"),
            AllowInsecureHttp = false
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HTTP connections to non-loopback");
    }

    [Fact]
    public void Should_SucceedValidation_WhenHttpToLocalhost()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = new Uri("http://localhost:11434/")
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Should_SucceedValidation_WhenHttpToLoopbackIp()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = new Uri("http://127.0.0.1:11434/")
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Should_FailValidation_WhenTimeoutIsZeroOrNegative()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = new Uri("http://localhost:11434/"),
            Timeout = TimeSpan.Zero
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Timeout must be greater than zero");
    }

    [Fact]
    public void Should_FailValidation_WhenMaxRetriesIsNegative()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = new Uri("http://localhost:11434/"),
            MaxRetries = -1
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxRetries must be between 0 and 10");
    }

    [Fact]
    public void Should_FailValidation_WhenMaxRetriesExceedsLimit()
    {
        OllamaClientOptions options = new()
        {
            BaseAddress = new Uri("http://localhost:11434/"),
            MaxRetries = 11
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxRetries must be between 0 and 10");
    }
}
