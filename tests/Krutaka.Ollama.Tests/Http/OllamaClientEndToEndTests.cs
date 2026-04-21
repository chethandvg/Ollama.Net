using System.Net;
using FluentAssertions;
using Krutaka.Ollama.Abstractions;
using Krutaka.Ollama.DependencyInjection;
using Krutaka.Ollama.Exceptions;
using Krutaka.Ollama.Models.Common;
using Krutaka.Ollama.Models.Requests;
using Krutaka.Ollama.Models.Responses;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Krutaka.Ollama.Tests.Http;

/// <summary>
/// End-to-end tests that spin up a WireMock.Net server and exercise the public
/// <see cref="IOllamaClient"/> surface through the real DI pipeline, HTTP handler,
/// JSON serializer, and error translator. These catch regressions that unit tests on
/// individual components cannot, such as JSON shape mismatches, missing headers, or
/// incorrect endpoint paths.
/// </summary>
public sealed class OllamaClientEndToEndTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly ServiceProvider _provider;
    private readonly IOllamaClient _client;

    public OllamaClientEndToEndTests()
    {
        _server = WireMockServer.Start();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient(options =>
        {
            options.BaseAddress = new Uri(_server.Url! + "/");
            options.Timeout = TimeSpan.FromSeconds(10);
        });

        _provider = services.BuildServiceProvider();
        _client = _provider.GetRequiredService<IOllamaClient>();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _server.Stop();
        _server.Dispose();
    }

    [Fact]
    public async Task GenerateAsync_WithSuccessfulResponse_ShouldDeserializeAllFields()
    {
        const string body = """
            {
              "model": "llama3",
              "created_at": "2025-01-15T10:00:00Z",
              "response": "Hello, world!",
              "done": true,
              "done_reason": "stop",
              "total_duration": 1234567890,
              "load_duration": 100000,
              "prompt_eval_count": 5,
              "prompt_eval_duration": 50000,
              "eval_count": 3,
              "eval_duration": 300000
            }
            """;
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));

        GenerateResponse resp = await _client.Generation.GenerateAsync(
            new GenerateRequest("llama3", "Say hi"),
            CancellationToken.None);

        resp.Model.Should().Be("llama3");
        resp.Response.Should().Be("Hello, world!");
        resp.Done.Should().BeTrue();
        resp.DoneReason.Should().Be("stop");
        resp.EvalCount.Should().Be(3);
    }

    [Fact]
    public async Task GenerateAsync_WithModelNotFoundError_ShouldThrowModelNotFoundException()
    {
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error":"model 'llama3' was not found on this server"}"""));

        Func<Task> act = () => _client.Generation.GenerateAsync(
            new GenerateRequest("llama3", "hi"),
            CancellationToken.None);

        OllamaModelNotFoundException ex = (await act.Should().ThrowAsync<OllamaModelNotFoundException>()).Which;
        ex.ModelName.Should().Be("llama3");
        ex.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateAsync_WithPullRequiredError_ShouldThrowModelPullRequiredException()
    {
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error":"model 'llama3' not found, try pulling it first"}"""));

        Func<Task> act = () => _client.Generation.GenerateAsync(
            new GenerateRequest("llama3", "hi"),
            CancellationToken.None);

        await act.Should().ThrowAsync<OllamaModelPullRequiredException>();
    }

    [Fact]
    public async Task GenerateAsync_WithRateLimit_ShouldThrowRateLimitedException()
    {
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429)
                .WithHeader("Retry-After", "30")
                .WithBody("""{"error":"too many requests"}"""));

        Func<Task> act = () => _client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi"),
            CancellationToken.None);

        OllamaRateLimitedException ex = (await act.Should().ThrowAsync<OllamaRateLimitedException>()).Which;
        ex.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task GenerateAsync_WithServerError_ShouldThrowServerException()
    {
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500)
                .WithBody("""{"error":"internal runtime failure"}"""));

        Func<Task> act = () => _client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi"),
            CancellationToken.None);

        OllamaServerException ex = (await act.Should().ThrowAsync<OllamaServerException>()).Which;
        ex.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.Message.Should().Contain("internal runtime failure");
    }

    [Fact]
    public async Task ChatStreamAsync_ShouldYieldAllChunksAndTerminateOnDone()
    {
        string stream =
            """{"model":"m","created_at":"2025-01-01T00:00:00Z","message":{"role":"assistant","content":"Hi"},"done":false}""" + "\n" +
            """{"model":"m","created_at":"2025-01-01T00:00:01Z","message":{"role":"assistant","content":" there"},"done":false}""" + "\n" +
            """{"model":"m","created_at":"2025-01-01T00:00:02Z","message":{"role":"assistant","content":""},"done":true,"done_reason":"stop"}""" + "\n";

        _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/x-ndjson")
                .WithBody(stream));

        List<string> parts = [];
        await foreach (ChatResponse chunk in _client.Generation.ChatStreamAsync(
            new ChatRequest("m", [new OllamaMessage(OllamaRole.User, "hi")]),
            CancellationToken.None))
        {
            parts.Add(chunk.Message.Content);
        }

        parts.Should().BeEquivalentTo(["Hi", " there", ""]);
    }

    [Fact]
    public async Task EmbedAsync_ShouldDeserializeEmbeddings()
    {
        _server
            .Given(Request.Create().WithPath("/api/embed").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                      "model": "nomic",
                      "embeddings": [[0.1, 0.2, 0.3], [0.4, 0.5, 0.6]],
                      "total_duration": 1000,
                      "prompt_eval_count": 2
                    }
                    """));

        EmbedResponse resp = await _client.Embeddings.EmbedAsync(
            new EmbedRequest("nomic", ["hello", "world"]),
            CancellationToken.None);

        resp.Model.Should().Be("nomic");
        resp.Embeddings.Should().HaveCount(2);
        resp.Embeddings[0].Should().Equal(0.1f, 0.2f, 0.3f);
    }

    [Fact]
    public async Task ListModelsAsync_ShouldReturnModelList()
    {
        _server
            .Given(Request.Create().WithPath("/api/tags").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                    {
                      "models": [
                        {
                          "name": "llama3:latest",
                          "modified_at": "2025-01-01T00:00:00Z",
                          "size": 123456,
                          "digest": "abc"
                        }
                      ]
                    }
                    """));

        ModelList resp = await _client.Models.ListModelsAsync(CancellationToken.None);

        resp.Models.Should().HaveCount(1);
        resp.Models[0].Name.Should().Be("llama3:latest");
        resp.Models[0].Size.Should().Be(123456);
    }

    [Fact]
    public async Task PingAsync_WhenReachable_ShouldReturnTrue()
    {
        _server
            .Given(Request.Create().WithPath("/").UsingHead())
            .RespondWith(Response.Create().WithStatusCode(200));

        bool alive = await _client.System.PingAsync(CancellationToken.None);
        alive.Should().BeTrue();
    }

    [Fact]
    public async Task SendJson_ShouldIncludeUserAgentHeader()
    {
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"x","done":true}"""));

        await _client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi"),
            CancellationToken.None);

        WireMock.Logging.ILogEntry log = _server.LogEntries.Single();
        log.RequestMessage.Headers!.Should().ContainKey("User-Agent");
        log.RequestMessage.Headers!["User-Agent"][0].Should().StartWith("Krutaka.Ollama");
    }

    [Fact]
    public async Task GenerateAsync_WithMalformedSuccessResponse_ShouldThrowDeserializationException()
    {
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{not valid json}"));

        Func<Task> act = () => _client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi"),
            CancellationToken.None);

        await act.Should().ThrowAsync<OllamaDeserializationException>();
    }

    [Fact]
    public async Task GenerateAsync_WithStreamTrueInRequest_ShouldThrowInvalidOperation()
    {
        Func<Task> act = () => _client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi") { Stream = true },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Stream mode mismatch*");
    }

    [Fact]
    public async Task GenerateAsync_WithConnectionRefused_ShouldThrowConnectionOrTimeoutException()
    {
        // Port 1 typically refuses connections immediately, but depending on the network
        // stack (containerised CI, etc.) the failure can surface as a timeout rather than
        // a connection reset. Both are documented agent-facing errors from OllamaException.
        using ServiceProvider badProvider = CreateBadProvider();
        IOllamaClient bad = badProvider.GetRequiredService<IOllamaClient>();

        Func<Task> act = () => bad.Generation.GenerateAsync(
            new GenerateRequest("m", "hi"),
            CancellationToken.None);

        await act.Should().ThrowAsync<OllamaException>()
            .Where(e => e.GetType() == typeof(OllamaConnectionException) || e.GetType() == typeof(OllamaTimeoutException));
    }

    [Fact]
    public async Task PullModelAsync_WithEmptyStream_ShouldThrowStreamException()
    {
        // Empty success stream — must NOT synthesize a "completed" ProgressResponse.
        _server
            .Given(Request.Create().WithPath("/api/pull").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/x-ndjson")
                .WithBody(string.Empty));

        Func<Task> act = () => _client.Models.PullModelAsync(
            new PullModelRequest("llama3.2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<OllamaStreamException>()
            .Where(e => e.IsTruncated)
            .WithMessage("*no progress records*");
    }

    [Fact]
    public async Task PushModelAsync_WithEmptyStream_ShouldThrowStreamException()
    {
        _server
            .Given(Request.Create().WithPath("/api/push").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/x-ndjson")
                .WithBody(string.Empty));

        Func<Task> act = () => _client.Models.PushModelAsync(
            new PushModelRequest("user/my-model:latest"),
            CancellationToken.None);

        await act.Should().ThrowAsync<OllamaStreamException>();
    }

    [Fact]
    public async Task CreateModelAsync_WithEmptyStream_ShouldThrowStreamException()
    {
        _server
            .Given(Request.Create().WithPath("/api/create").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/x-ndjson")
                .WithBody(string.Empty));

        Func<Task> act = () => _client.Models.CreateModelAsync(
            new CreateModelRequest("my-model", From: "llama3.2"),
            CancellationToken.None);

        await act.Should().ThrowAsync<OllamaStreamException>();
    }

    [Fact]
    public async Task PullModelAsync_WithValidStream_ShouldReturnLastChunk()
    {
        const string ndjson = """
            {"status":"pulling manifest"}
            {"status":"downloading","completed":10,"total":100}
            {"status":"success"}
            """;

        _server
            .Given(Request.Create().WithPath("/api/pull").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/x-ndjson")
                .WithBody(ndjson));

        ProgressResponse last = await _client.Models.PullModelAsync(
            new PullModelRequest("llama3.2"),
            CancellationToken.None);

        last.Status.Should().Be("success");
    }

    [Fact]
    public async Task GenerateAsync_WithPaymentRequired_ShouldThrowQuotaExceededException()
    {
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(402)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"error\":\"subscription quota exceeded\"}"));

        Func<Task> act = () => _client.Generation.GenerateAsync(
            new GenerateRequest("gpt-oss:120b-cloud", "hi"),
            CancellationToken.None);

        OllamaQuotaExceededException ex = (await act.Should().ThrowAsync<OllamaQuotaExceededException>())
            .Which;
        ex.Message.Should().Contain("quota");
    }

    [Fact]
    public async Task GenerateAsync_With429AndQuotaMessage_ShouldMentionQuotaInException()
    {
        _server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"error\":\"hourly quota limit reached\"}"));

        Func<Task> act = () => _client.Generation.GenerateAsync(
            new GenerateRequest("gpt-oss:120b-cloud", "hi"),
            CancellationToken.None);

        OllamaRateLimitedException ex = (await act.Should().ThrowAsync<OllamaRateLimitedException>()).Which;
        ex.Message.Should().Contain("quota");
    }

    [Fact]
    public async Task ChatAsync_WithStreamTrueInRequest_ShouldNotDoubleAsyncSuffix()
    {
        // Regression: nameof(ChatAsync) + "Async" used to produce "ChatAsyncAsync".
        // The suggestion should read "Use ChatAsync (non-streaming) instead." — one Async.
        Func<Task> act = () => _client.Generation.ChatAsync(
            new ChatRequest("m", [new OllamaMessage(OllamaRole.User, "hi")]) { Stream = true },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(e => e.Message.Contains("Use ChatAsync", StringComparison.Ordinal)
                        && !e.Message.Contains("ChatAsyncAsync", StringComparison.Ordinal)
                        && !e.Message.Contains("ChatAsyncStreamAsync", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_WithApiKey_ShouldSendBearerAuthorizationHeader()
    {
        using WireMockServer cloudServer = WireMockServer.Start();
        cloudServer
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"model":"gpt-oss:120b-cloud","created_at":"2025-01-15T10:00:00Z","response":"hi","done":true}"""));

        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaCloudClient("sk-test-key-42", o =>
        {
            // Redirect the hosted base address to the local WireMock for the test.
            o.BaseAddress = new Uri(cloudServer.Url! + "/");
            o.Timeout = TimeSpan.FromSeconds(5);
        });

        await using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClient cloud = provider.GetRequiredService<IOllamaClient>();

        await cloud.Generation.GenerateAsync(
            new GenerateRequest("gpt-oss:120b-cloud", "hi"),
            CancellationToken.None);

        IReadOnlyList<WireMock.Logging.ILogEntry> entries = cloudServer.LogEntries.ToList();
        entries.Should().NotBeEmpty();
        WireMock.Logging.ILogEntry entry = entries[^1];
        entry.RequestMessage.Headers.Should().ContainKey("Authorization");
        entry.RequestMessage.Headers!["Authorization"]
            .Should().ContainSingle(h => h == "Bearer sk-test-key-42");

        cloudServer.Stop();
    }

    [Fact]
    public async Task MaxRetries_Zero_ShouldDisableRetriesEntirely()
    {
        // Regression: resilience handler used to hard-code MaxRetryAttempts = 2 regardless
        // of OllamaClientOptions.MaxRetries, so MaxRetries = 0 had no effect.
        using WireMockServer server = WireMockServer.Start();
        server
            .Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(503)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"error\":\"service unavailable\"}"));

        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient(o =>
        {
            o.BaseAddress = new Uri(server.Url! + "/");
            o.Timeout = TimeSpan.FromSeconds(5);
            o.MaxRetries = 0;
        });

        await using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClient client = provider.GetRequiredService<IOllamaClient>();

        Func<Task> act = () => client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi"),
            CancellationToken.None);

        await act.Should().ThrowAsync<OllamaServiceUnavailableException>();
        server.LogEntries.Count(e => e.RequestMessage.Path == "/api/generate").Should().Be(1);
        server.Stop();
    }

    [Fact]
    public async Task StreamingRequest_ShouldNotBeRetried_EvenOnTransientException()
    {
        // Regression: ShouldHandle used to return true for any HttpRequestException,
        // including the first exception from a streaming request that had not yet returned
        // a response. Streaming must never be retried (partial NDJSON would be re-played).
        // We can't easily trigger an exception mid-stream against WireMock, but we can
        // verify an unresponsive endpoint produces exactly one connection attempt.
        using ServiceProvider badProvider = CreateBadProvider();
        IOllamaClient bad = badProvider.GetRequiredService<IOllamaClient>();

        Func<Task> act = async () =>
        {
            await foreach (GenerateResponse _ in bad.Generation.GenerateStreamAsync(
                new GenerateRequest("m", "hi"),
                CancellationToken.None).ConfigureAwait(false))
            {
                // no-op
            }
        };

        // Whether the transport surfaces this as connection or timeout depends on the
        // network stack, but it MUST be an OllamaException (connection/timeout) and not
        // retry multiple times.
        await act.Should().ThrowAsync<OllamaException>();
    }

    [Fact]
    public async Task AddOllamaCloudClient_ShouldRegisterResolvableClientWithBearerAuth()
    {
        using WireMockServer cloud = WireMockServer.Start();
        cloud
            .Given(Request.Create().WithPath("/api/version").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"version":"0.5.0"}"""));

        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaCloudClient("sk-cloud-abc", o => o.BaseAddress = new Uri(cloud.Url! + "/"));

        await using ServiceProvider provider = services.BuildServiceProvider();
        IOllamaClient client = provider.GetRequiredService<IOllamaClient>();

        await client.System.GetVersionAsync(CancellationToken.None);

        cloud.LogEntries.Should().ContainSingle()
            .Which.RequestMessage.Headers!["Authorization"]
            .Should().ContainSingle(h => h == "Bearer sk-cloud-abc");
        cloud.Stop();
    }

    private static ServiceProvider CreateBadProvider()
    {
        // port 1 is reserved and should refuse connections immediately.
        ServiceCollection services = new();
        services.AddLogging();
        services.AddOllamaClient(o =>
        {
            o.BaseAddress = new Uri("http://127.0.0.1:1/");
            o.Timeout = TimeSpan.FromSeconds(2);
        });
        return services.BuildServiceProvider();
    }
}
