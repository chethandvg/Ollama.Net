using System.Net;
using FluentAssertions;
using Ollama.Net.Exceptions;
using Ollama.Net.Http;
using Xunit;

namespace Ollama.Net.Tests.Http;

/// <summary>
/// Tests for <see cref="OllamaErrorTranslator"/>.
/// </summary>
public sealed class OllamaErrorTranslatorTests
{
    [Fact]
    public void Should_TranslateTo_OllamaRequestValidationException_When400()
    {
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest);
        string rawBody = "{\"error\": \"invalid request\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaRequestValidationException>();
        exception.Message.Should().Contain("invalid request");
    }

    [Fact]
    public void Should_TranslateTo_OllamaAuthenticationException_When401()
    {
        using HttpResponseMessage response = new(HttpStatusCode.Unauthorized);
        string rawBody = "{\"error\": \"unauthorized\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaAuthenticationException>();
    }

    [Fact]
    public void Should_TranslateTo_OllamaAuthorizationException_When403()
    {
        using HttpResponseMessage response = new(HttpStatusCode.Forbidden);
        string rawBody = "{\"error\": \"forbidden\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaAuthorizationException>();
    }

    [Fact]
    public void Should_TranslateTo_OllamaModelNotFoundException_When404WithModelNotFound()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NotFound);
        string rawBody = "{\"error\": \"model 'llama3' not found\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaModelNotFoundException>();
        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public void Should_TranslateTo_OllamaModelPullRequiredException_When404WithPullRequired()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NotFound);
        string rawBody = "{\"error\": \"model must be pulled first\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaModelPullRequiredException>();
    }

    [Fact]
    public void Should_TranslateTo_OllamaPayloadTooLargeException_When413()
    {
        using HttpResponseMessage response = new(HttpStatusCode.RequestEntityTooLarge);
        string rawBody = "{\"error\": \"payload too large\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaPayloadTooLargeException>();
    }

    [Fact]
    public void Should_TranslateTo_OllamaRateLimitedException_When429()
    {
        using HttpResponseMessage response = new(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
        string rawBody = "{\"error\": \"rate limited\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaRateLimitedException>();
        var rateLimited = (OllamaRateLimitedException)exception;
        rateLimited.RetryAfter.Should().BeCloseTo(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Should_TranslateTo_OllamaServerException_When500WithOutOfMemory()
    {
        using HttpResponseMessage response = new(HttpStatusCode.InternalServerError);
        string rawBody = "{\"error\": \"out of memory during generation\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaServerException>();
        var serverEx = (OllamaServerException)exception;
        serverEx.IsOutOfMemory.Should().BeTrue();
    }

    [Fact]
    public void Should_TranslateTo_OllamaPayloadTooLargeException_When500WithContextLength()
    {
        using HttpResponseMessage response = new(HttpStatusCode.InternalServerError);
        string rawBody = "{\"error\": \"context length exceeds model limit\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaPayloadTooLargeException>();
    }

    [Fact]
    public void Should_TranslateTo_OllamaServiceUnavailableException_When503()
    {
        using HttpResponseMessage response = new(HttpStatusCode.ServiceUnavailable);
        string rawBody = "{\"error\": \"service unavailable\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        exception.Should().BeOfType<OllamaServiceUnavailableException>();
    }

    [Fact]
    public void Should_SetRequestId_WhenXRequestIdHeaderPresent()
    {
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest);
        response.Headers.Add("X-Request-Id", "abc123");
        string rawBody = "{\"error\": \"bad request\"}";

        OllamaException exception = OllamaErrorTranslator.Translate(response, "/api/generate", rawBody);

        OllamaApiException apiEx = exception.Should().BeOfType<OllamaRequestValidationException>().Subject;
        apiEx.RequestId.Should().Be("abc123");
    }
}
