using Microsoft.Extensions.Logging;

namespace Krutaka.Ollama.Internal.Diagnostics;

/// <summary>
/// Strongly-typed logging for Ollama operations using source generators.
/// </summary>
internal static partial class OllamaLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Sending {Method} request to {Endpoint}")]
    public static partial void SendingRequest(
        ILogger logger, 
        string method, 
        string endpoint);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Received response from {Endpoint} with status {StatusCode} in {DurationMs}ms")]
    public static partial void ReceivedResponse(
        ILogger logger, 
        string endpoint, 
        int statusCode, 
        double durationMs);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Request to {Endpoint} failed with status {StatusCode}: {ErrorMessage}")]
    public static partial void RequestFailed(
        ILogger logger, 
        string endpoint, 
        int statusCode, 
        string errorMessage);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Connection error calling {Endpoint}")]
    public static partial void ConnectionError(
        ILogger logger, 
        Exception exception, 
        string endpoint);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Deserialization error for {Endpoint}")]
    public static partial void DeserializationError(
        ILogger logger, 
        Exception exception, 
        string endpoint);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Starting stream from {Endpoint}")]
    public static partial void StartingStream(
        ILogger logger, 
        string endpoint);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Stream from {Endpoint} was truncated after {ChunksProcessed} chunks")]
    public static partial void StreamTruncated(
        ILogger logger, 
        string endpoint, 
        int chunksProcessed);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Stream error at chunk {ChunkIndex} from {Endpoint}")]
    public static partial void StreamError(
        ILogger logger, 
        Exception exception, 
        int chunkIndex, 
        string endpoint);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Trace,
        Message = "Serialized request body: {Length} bytes")]
    public static partial void RequestBody(ILogger logger, int length);
}
