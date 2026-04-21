using System.Text;
using FluentAssertions;
using Krutaka.Ollama.Exceptions;
using Krutaka.Ollama.Http;
using Krutaka.Ollama.Internal.Json;
using Krutaka.Ollama.Models.Responses;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Krutaka.Ollama.Tests.Http;

public sealed class OllamaStreamReaderTests
{
    private static MemoryStream ToStream(string payload)
        => new(Encoding.UTF8.GetBytes(payload));

    [Fact]
    public async Task ReadNdjsonAsync_WithValidStream_ShouldYieldAllChunks()
    {
        string ndjson =
            """{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"Hello","done":false}""" + "\n" +
            """{"model":"m","created_at":"2025-01-01T00:00:01Z","response":" world","done":false}""" + "\n" +
            """{"model":"m","created_at":"2025-01-01T00:00:02Z","response":"!","done":true,"done_reason":"stop"}""" + "\n";

        List<GenerateResponse> chunks = [];
        await foreach (GenerateResponse chunk in OllamaStreamReader.ReadNdjsonAsync(
            ToStream(ndjson),
            OllamaJsonContext.Default.GenerateResponse,
            "/api/generate",
            NullLogger.Instance,
            CancellationToken.None).ConfigureAwait(true))
        {
            chunks.Add(chunk);
        }

        chunks.Should().HaveCount(3);
        chunks[0].Response.Should().Be("Hello");
        chunks[2].Done.Should().BeTrue();
    }

    [Fact]
    public async Task ReadNdjsonAsync_WithBlankLines_ShouldSkipThem()
    {
        string ndjson =
            """{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"X","done":true}""" + "\n" +
            "\n   \n\t\n";

        int count = 0;
        await foreach (GenerateResponse _ in OllamaStreamReader.ReadNdjsonAsync(
            ToStream(ndjson),
            OllamaJsonContext.Default.GenerateResponse,
            "/api/generate",
            NullLogger.Instance,
            CancellationToken.None).ConfigureAwait(true))
        {
            count++;
        }

        count.Should().Be(1);
    }

    [Fact]
    public async Task ReadNdjsonAsync_WithMalformedJson_ShouldThrowStreamException()
    {
        string ndjson =
            """{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"ok","done":false}""" + "\n" +
            "{not valid json}\n";

        List<GenerateResponse> produced = [];
        Func<Task> act = async () =>
        {
            await foreach (GenerateResponse c in OllamaStreamReader.ReadNdjsonAsync(
                ToStream(ndjson),
                OllamaJsonContext.Default.GenerateResponse,
                "/api/generate",
                NullLogger.Instance,
                CancellationToken.None).ConfigureAwait(true))
            {
                produced.Add(c);
            }
        };

        OllamaStreamException ex = (await act.Should().ThrowAsync<OllamaStreamException>()).Which;
        ex.Endpoint.Should().Be("/api/generate");
        ex.ChunksProcessed.Should().Be(1);
        ex.InnerException.Should().BeAssignableTo<System.Text.Json.JsonException>();
        produced.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReadNdjsonAsync_TruncatedBeforeDoneMarker_ShouldThrowWithIsTruncated()
    {
        // Two chunks, neither terminal (done=false) — the stream ended early.
        string ndjson =
            """{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"one","done":false}""" + "\n" +
            """{"model":"m","created_at":"2025-01-01T00:00:01Z","response":"two","done":false}""" + "\n";

        int yielded = 0;
        Func<Task> act = async () =>
        {
            await foreach (GenerateResponse _ in OllamaStreamReader.ReadNdjsonAsync(
                ToStream(ndjson),
                OllamaJsonContext.Default.GenerateResponse,
                "/api/generate",
                NullLogger.Instance,
                CancellationToken.None).ConfigureAwait(true))
            {
                yielded++;
            }
        };

        OllamaStreamException ex = (await act.Should().ThrowAsync<OllamaStreamException>()).Which;
        ex.IsTruncated.Should().BeTrue();
        ex.ChunksProcessed.Should().Be(2);
        yielded.Should().Be(2);
    }

    [Fact]
    public async Task ReadNdjsonAsync_WithCancellation_ShouldHonorToken()
    {
        // A slow stream: use a pipe so we can keep the reader waiting then cancel.
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        string ndjson =
            """{"model":"m","created_at":"2025-01-01T00:00:00Z","response":"X","done":true}""" + "\n";

        Func<Task> act = async () =>
        {
            await foreach (GenerateResponse _ in OllamaStreamReader.ReadNdjsonAsync(
                ToStream(ndjson),
                OllamaJsonContext.Default.GenerateResponse,
                "/api/generate",
                NullLogger.Instance,
                cts.Token).ConfigureAwait(true))
            {
                // no-op
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReadNdjsonAsync_NonTerminalTypeWithoutDoneCheck_ShouldNotFlagTruncation()
    {
        // ProgressResponse has no 'done' property — truncation detection should be disabled.
        string ndjson =
            """{"status":"downloading","total":1024,"completed":100}""" + "\n" +
            """{"status":"downloading","total":1024,"completed":1024}""" + "\n";

        int count = 0;
        await foreach (ProgressResponse _ in OllamaStreamReader.ReadNdjsonAsync(
            ToStream(ndjson),
            OllamaJsonContext.Default.ProgressResponse,
            "/api/pull",
            NullLogger.Instance,
            CancellationToken.None).ConfigureAwait(true))
        {
            count++;
        }

        count.Should().Be(2);
    }
}
