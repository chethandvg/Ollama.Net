using System.Text.Json;
using FluentAssertions;
using Ollama.Net.Abstractions;
using Ollama.Net.DependencyInjection;
using Ollama.Net.Models.Common;
using Ollama.Net.Models.Requests;
using Ollama.Net.Models.Responses;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Ollama.Net.Tests.Http;

/// <summary>
/// End-to-end WireMock tests for the features added to close documented gaps:
/// structured outputs, thinking models, and the <see cref="OllamaOptions.Extra"/> bag.
/// </summary>
public sealed class OllamaClientGapFillEndToEndTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly ServiceProvider _provider;
    private readonly IOllamaClient _client;

    public OllamaClientGapFillEndToEndTests()
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

    // ---------------- Structured outputs ----------------

    [Fact]
    public async Task GenerateAsync_WithSchemaFormat_SendsSchemaObjectOnWire()
    {
        _server.Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"{\"age\":22}","done":true}"""));

        OllamaFormat schema = OllamaFormat.FromSchema("""
            {"type":"object","properties":{"age":{"type":"integer"}},"required":["age"]}
            """);

        await _client.Generation.GenerateAsync(
            new GenerateRequest("m", "give JSON", Format: schema),
            CancellationToken.None);

        string body = _server.LogEntries.Single().RequestMessage.Body!;
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement formatNode = doc.RootElement.GetProperty("format");
        formatNode.ValueKind.Should().Be(JsonValueKind.Object, "schema must be sent inline, not stringified");
        formatNode.GetProperty("required")[0].GetString().Should().Be("age");
    }

    [Fact]
    public async Task GenerateAsync_WithJsonMode_SendsLiteralString()
    {
        _server.Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"{}","done":true}"""));

        await _client.Generation.GenerateAsync(
            new GenerateRequest("m", "give JSON", Format: OllamaFormat.Json),
            CancellationToken.None);

        string body = _server.LogEntries.Single().RequestMessage.Body!;
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement f = doc.RootElement.GetProperty("format");
        f.ValueKind.Should().Be(JsonValueKind.String);
        f.GetString().Should().Be("json");
    }

    [Fact]
    public async Task ChatAsync_WithSchemaFormat_SendsSchemaObject()
    {
        _server.Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""
                    {"model":"m","created_at":"2025-01-01T00:00:00Z",
                     "message":{"role":"assistant","content":"{\"ok\":true}"},"done":true}
                    """));

        OllamaFormat schema = OllamaFormat.FromSchema("""{"type":"object","properties":{"ok":{"type":"boolean"}}}""");

        await _client.Generation.ChatAsync(
            new ChatRequest("m", [new OllamaMessage(OllamaRole.User, "hi")], Format: schema),
            CancellationToken.None);

        string body = _server.LogEntries.Single().RequestMessage.Body!;
        using JsonDocument doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("format").ValueKind.Should().Be(JsonValueKind.Object);
    }

    // ---------------- Thinking support ----------------

    [Fact]
    public async Task GenerateAsync_WithThinkTrue_WritesThinkField()
    {
        _server.Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"hi","done":true}"""));

        await _client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi", Think: true),
            CancellationToken.None);

        string body = _server.LogEntries.Single().RequestMessage.Body!;
        using JsonDocument doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("think").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_WithoutThink_OmitsField()
    {
        _server.Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"hi","done":true}"""));

        await _client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi"),
            CancellationToken.None);

        string body = _server.LogEntries.Single().RequestMessage.Body!;
        using JsonDocument doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("think", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ChatAsync_WithThink_RoundTripsThinkingField()
    {
        _server.Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""
                    {"model":"m","created_at":"2025-01-01T00:00:00Z",
                     "message":{"role":"assistant","content":"42","thinking":"Let me compute 6*7 ... that's 42."},
                     "done":true,"done_reason":"stop"}
                    """));

        ChatResponse resp = await _client.Generation.ChatAsync(
            new ChatRequest("m", [new OllamaMessage(OllamaRole.User, "6*7?")], Think: true),
            CancellationToken.None);

        // Request contains the think toggle.
        string body = _server.LogEntries.Single().RequestMessage.Body!;
        using JsonDocument doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("think").GetBoolean().Should().BeTrue();

        // Response surfaces message.thinking distinct from message.content.
        resp.Message.Content.Should().Be("42");
        resp.Message.Thinking.Should().Be("Let me compute 6*7 ... that's 42.");
    }

    [Fact]
    public async Task ChatStreamAsync_YieldsThinkingThenContentChunks()
    {
        // Ollama streams thinking chunks first (content empty) then content chunks.
        string stream =
            """{"model":"m","created_at":"2025-01-01T00:00:00Z","message":{"role":"assistant","content":"","thinking":"Hmm..."},"done":false}""" + "\n" +
            """{"model":"m","created_at":"2025-01-01T00:00:01Z","message":{"role":"assistant","content":"42"},"done":false}""" + "\n" +
            """{"model":"m","created_at":"2025-01-01T00:00:02Z","message":{"role":"assistant","content":""},"done":true,"done_reason":"stop"}""" + "\n";

        _server.Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/x-ndjson")
                .WithBody(stream));

        List<(string content, string? thinking)> chunks = [];
        await foreach (ChatResponse c in _client.Generation.ChatStreamAsync(
            new ChatRequest("m", [new OllamaMessage(OllamaRole.User, "hi")], Think: true),
            CancellationToken.None))
        {
            chunks.Add((c.Message.Content, c.Message.Thinking));
        }

        chunks.Should().HaveCount(3);
        chunks[0].thinking.Should().Be("Hmm...");
        chunks[1].content.Should().Be("42");
    }

    // ---------------- Options Extra bag end-to-end ----------------

    [Fact]
    public async Task GenerateAsync_WithOptionsExtraBag_FlattensIntoOptionsJson()
    {
        _server.Given(Request.Create().WithPath("/api/generate").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"x","done":true}"""));

        using JsonDocument val = JsonDocument.Parse("true");
        OllamaOptions opts = new(
            Temperature: 0.2,
            MinP: 0.05,
            Extra: new Dictionary<string, JsonElement>
            {
                ["future_knob"] = val.RootElement.Clone(),
            });

        await _client.Generation.GenerateAsync(
            new GenerateRequest("m", "hi", Options: opts),
            CancellationToken.None);

        string body = _server.LogEntries.Single().RequestMessage.Body!;
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement o = doc.RootElement.GetProperty("options");
        o.GetProperty("temperature").GetDouble().Should().Be(0.2);
        o.GetProperty("min_p").GetDouble().Should().Be(0.05);
        o.GetProperty("future_knob").GetBoolean().Should().BeTrue();
    }
}
