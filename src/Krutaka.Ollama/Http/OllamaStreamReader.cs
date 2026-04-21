using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Krutaka.Ollama.Exceptions;
using Krutaka.Ollama.Internal.Diagnostics;
using Krutaka.Ollama.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Krutaka.Ollama.Http;

/// <summary>
/// Reads and deserializes NDJSON streams from the Ollama API using a source-generated
/// <see cref="JsonTypeInfo{T}"/> so the path remains AOT/trim-safe.
/// </summary>
internal static class OllamaStreamReader
{
    /// <summary>
    /// Reads a newline-delimited JSON stream and yields deserialized objects.
    /// </summary>
    public static async IAsyncEnumerable<T> ReadNdjsonAsync<T>(
        Stream stream,
        JsonTypeInfo<T> typeInfo,
        string endpoint,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(typeInfo);

        OllamaLog.StartingStream(logger, endpoint);

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        int chunkIndex = 0;
        T? lastChunk = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (IOException ioEx)
            {
                OllamaLog.StreamError(logger, ioEx, chunkIndex, endpoint);
                throw new OllamaStreamException(
                    $"The Ollama stream was interrupted after {chunkIndex} chunks: {ioEx.Message}",
                    chunkIndex,
                    ioEx)
                { Endpoint = endpoint };
            }

            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            T chunk;
            try
            {
                chunk = JsonSerializer.Deserialize(line, typeInfo)
                    ?? throw new OllamaStreamException(
                        $"Malformed NDJSON in stream from '{endpoint}' at chunk {chunkIndex}: deserialized to null.",
                        chunkIndex)
                    { Endpoint = endpoint };
            }
            catch (JsonException jsonEx)
            {
                OllamaLog.DeserializationError(logger, jsonEx, endpoint);
                throw new OllamaStreamException(
                    $"Malformed NDJSON in stream from '{endpoint}' at chunk {chunkIndex}.",
                    chunkIndex,
                    jsonEx)
                { Endpoint = endpoint };
            }

            lastChunk = chunk;
            chunkIndex++;
            yield return chunk;
        }

        if (ShouldCheckForTruncation<T>() && lastChunk is not null && !IsTerminalChunk(lastChunk))
        {
            OllamaLog.StreamTruncated(logger, endpoint, chunkIndex);
            throw new OllamaStreamException(
                $"Ollama stream ended before completion (no final 'done' marker) from '{endpoint}'.",
                chunkIndex)
            {
                Endpoint = endpoint,
                IsTruncated = true
            };
        }
    }

    private static bool ShouldCheckForTruncation<T>()
    {
        Type type = typeof(T);
        return type == typeof(GenerateResponse) || type == typeof(ChatResponse);
    }

    private static bool IsTerminalChunk<T>(T chunk)
    {
        return chunk switch
        {
            GenerateResponse gr => gr.Done,
            ChatResponse cr => cr.Done,
            _ => true
        };
    }
}
