using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ollama.Net.Configuration;
using Ollama.Net.Exceptions;
using Ollama.Net.Internal.Diagnostics;
using Ollama.Net.Internal.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ollama.Net.Http;

/// <summary>
/// Internal HTTP client that owns all Ollama REST traffic.
/// Handles user-agent, authorization header, request/response serialization via the
/// source-generated <see cref="OllamaJsonContext"/>, NDJSON streaming, and
/// translation of non-success responses into typed <see cref="OllamaException"/> instances.
/// </summary>
internal sealed class OllamaHttpClient
{
    private static readonly string LibraryVersion =
        typeof(OllamaHttpClient).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<OllamaClientOptions> _optionsMonitor;
    private readonly string _optionsName;
    private readonly ILogger<OllamaHttpClient> _logger;

    public OllamaHttpClient(
        HttpClient httpClient,
        IOptionsMonitor<OllamaClientOptions> optionsMonitor,
        ILogger<OllamaHttpClient> logger)
        : this(httpClient, optionsMonitor, Options.DefaultName, logger)
    {
    }

    public OllamaHttpClient(
        HttpClient httpClient,
        IOptionsMonitor<OllamaClientOptions> optionsMonitor,
        string optionsName,
        ILogger<OllamaHttpClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(optionsName);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _optionsMonitor = optionsMonitor;
        _optionsName = optionsName;
        _logger = logger;

        OllamaClientOptions initialOptions = _optionsMonitor.Get(_optionsName);

        if (_httpClient.BaseAddress is null && initialOptions.BaseAddress is not null)
        {
            _httpClient.BaseAddress = initialOptions.BaseAddress;
        }

        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    /// <summary>The current (live) options snapshot for this client.</summary>
    private OllamaClientOptions CurrentOptions => _optionsMonitor.Get(_optionsName);

    /// <summary>Exposed for testing; the effective base address.</summary>
    internal Uri? BaseAddress => _httpClient.BaseAddress ?? CurrentOptions.BaseAddress;

    /// <summary>
    /// Sends a request with an optional JSON body and deserializes the JSON response.
    /// </summary>
    public async Task<TResponse> SendJsonAsync<TRequest, TResponse>(
        HttpMethod method,
        string path,
        TRequest? body,
        JsonTypeInfo<TResponse> responseTypeInfo,
        JsonTypeInfo<TRequest>? requestTypeInfo,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(responseTypeInfo);

        // Snapshot live options once for the full lifetime of this request so
        // Timeout, headers, and error messages all agree even if another thread
        // rotates OllamaClientOptions mid-call. Subsequent requests pick up the
        // new values automatically via IOptionsMonitor.
        OllamaClientOptions options = CurrentOptions;

        using Activity? activity = StartActivity(method, path, streaming: false);
        Stopwatch stopwatch = Stopwatch.StartNew();

        using HttpRequestMessage request = CreateRequest(method, path, body, requestTypeInfo, isStreaming: false, options);

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.Timeout);
        CancellationToken effectiveCt = timeoutCts.Token;

        HttpResponseMessage response;
        try
        {
            OllamaLog.SendingRequest(_logger, method.Method, path);
            OllamaMetrics.RequestsTotal.Add(1, new KeyValuePair<string, object?>("endpoint", path));

            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseContentRead, effectiveCt)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException)
        {
            OllamaLog.ConnectionError(_logger, ex, path);
            OllamaMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("endpoint", path));
            throw TranslateTransportError(ex, path, options);
        }
        catch (OperationCanceledException) when (
            timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            OllamaMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("endpoint", path));
            throw new OllamaTimeoutException(
                $"The Ollama request to '{path}' timed out after {options.Timeout}. " +
                "Increase OllamaClientOptions.Timeout for large prompts or use the streaming API.",
                new TimeoutException())
            {
                Endpoint = path
            };
        }

        try
        {
            stopwatch.Stop();
            double durationMs = stopwatch.Elapsed.TotalMilliseconds;
            activity?.SetTag("ollama.status_code", (int)response.StatusCode);
            activity?.SetTag("ollama.duration_ms", durationMs);
            OllamaMetrics.RequestDuration.Record(durationMs, new KeyValuePair<string, object?>("endpoint", path));
            OllamaLog.ReceivedResponse(_logger, path, (int)response.StatusCode, durationMs);

            if (!response.IsSuccessStatusCode)
            {
                string? rawBody = await ReadBodySafelyAsync(response, cancellationToken).ConfigureAwait(false);
                OllamaLog.RequestFailed(_logger, path, (int)response.StatusCode, rawBody ?? "(no body)");
                OllamaMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("endpoint", path));
                throw OllamaErrorTranslator.Translate(response, path, rawBody);
            }

            ValidateContentType(response, path, expectNdjson: false);

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            CheckForEmbeddedError(responseBody, path);

            TResponse? result;
            try
            {
                result = JsonSerializer.Deserialize(responseBody, responseTypeInfo);
            }
            catch (JsonException jsonEx)
            {
                OllamaLog.DeserializationError(_logger, jsonEx, path);
                throw new OllamaDeserializationException(
                    $"Failed to deserialize response from '{path}'. Raw body length: {responseBody.Length}.",
                    jsonEx)
                {
                    Endpoint = path,
                    RawContent = responseBody
                };
            }

            if (result is null)
            {
                throw new OllamaDeserializationException(
                    $"Ollama response from '{path}' deserialized to null.",
                    new JsonException("Null deserialization result"))
                {
                    Endpoint = path,
                    RawContent = responseBody
                };
            }

            return result;
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Sends a request that returns no response body (e.g., delete, copy).
    /// </summary>
    public async Task SendJsonVoidAsync<TRequest>(
        HttpMethod method,
        string path,
        TRequest? body,
        JsonTypeInfo<TRequest>? requestTypeInfo,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        OllamaClientOptions options = CurrentOptions;

        using HttpRequestMessage request = CreateRequest(method, path, body, requestTypeInfo, isStreaming: false, options);
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.Timeout);

        HttpResponseMessage response;
        try
        {
            OllamaLog.SendingRequest(_logger, method.Method, path);
            OllamaMetrics.RequestsTotal.Add(1, new KeyValuePair<string, object?>("endpoint", path));
            response = await _httpClient.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException)
        {
            OllamaMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("endpoint", path));
            throw TranslateTransportError(ex, path, options);
        }
        catch (OperationCanceledException) when (
            timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            OllamaMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("endpoint", path));
            throw new OllamaTimeoutException(
                $"The Ollama request to '{path}' timed out after {options.Timeout}.",
                new TimeoutException())
            { Endpoint = path };
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string? rawBody = await ReadBodySafelyAsync(response, cancellationToken).ConfigureAwait(false);
                OllamaMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("endpoint", path));
                throw OllamaErrorTranslator.Translate(response, path, rawBody);
            }
        }
    }

    /// <summary>
    /// Sends a request with an optional JSON body and yields NDJSON chunks as they arrive.
    /// Retries are disabled for streaming (the <c>X-Ollama-Stream</c> header signals the resilience handler).
    /// </summary>
    public async IAsyncEnumerable<TResponse> SendStreamAsync<TRequest, TResponse>(
        HttpMethod method,
        string path,
        TRequest? body,
        JsonTypeInfo<TResponse> responseTypeInfo,
        JsonTypeInfo<TRequest>? requestTypeInfo,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(responseTypeInfo);

        OllamaClientOptions options = CurrentOptions;

        using Activity? activity = StartActivity(method, path, streaming: true);
        OllamaMetrics.RequestsTotal.Add(1, new KeyValuePair<string, object?>("endpoint", path));

        using HttpRequestMessage request = CreateRequest(method, path, body, requestTypeInfo, isStreaming: true, options);

        HttpResponseMessage response;
        try
        {
            OllamaLog.SendingRequest(_logger, method.Method, path);
            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException)
        {
            OllamaLog.ConnectionError(_logger, ex, path);
            OllamaMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("endpoint", path));
            throw TranslateTransportError(ex, path, options);
        }

        activity?.SetTag("ollama.status_code", (int)response.StatusCode);

        try
        {
            if (!response.IsSuccessStatusCode)
            {
                string? rawBody = await ReadBodySafelyAsync(response, cancellationToken).ConfigureAwait(false);
                OllamaLog.RequestFailed(_logger, path, (int)response.StatusCode, rawBody ?? "(no body)");
                OllamaMetrics.RequestsFailed.Add(1, new KeyValuePair<string, object?>("endpoint", path));
                throw OllamaErrorTranslator.Translate(response, path, rawBody);
            }

            ValidateContentType(response, path, expectNdjson: true);

            Stream responseStream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            await foreach (TResponse chunk in OllamaStreamReader.ReadNdjsonAsync(
                responseStream, responseTypeInfo, path, _logger, cancellationToken).ConfigureAwait(false))
            {
                yield return chunk;
            }
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <summary>Sends an HTTP HEAD request and returns whether the response was a success (2xx).</summary>
    public async Task<bool> HeadAsync(string path, CancellationToken cancellationToken)
    {
        OllamaClientOptions options = CurrentOptions;

        using HttpRequestMessage request = CreateRequest<object>(HttpMethod.Head, path, body: null, requestTypeInfo: null, isStreaming: false, options);
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.Timeout);

        try
        {
            OllamaLog.SendingRequest(_logger, request.Method.Method, path);
            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException)
        {
            throw TranslateTransportError(ex, path, options);
        }
        catch (OperationCanceledException) when (
            timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new OllamaTimeoutException(
                $"The Ollama HEAD request to '{path}' timed out after {options.Timeout}.",
                new TimeoutException())
            { Endpoint = path };
        }
    }

    /// <summary>Sends a request with a raw binary stream body (used for blob uploads).</summary>
    public async Task SendStreamBodyAsync(
        HttpMethod method,
        string path,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        OllamaClientOptions options = CurrentOptions;

        using HttpRequestMessage request = CreateRequestCore(method, path, isStreaming: false, options);
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.Timeout);

        HttpResponseMessage response;
        try
        {
            OllamaLog.SendingRequest(_logger, method.Method, path);
            response = await _httpClient.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException)
        {
            throw TranslateTransportError(ex, path, options);
        }
        catch (OperationCanceledException) when (
            timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new OllamaTimeoutException(
                $"The Ollama request to '{path}' timed out after {options.Timeout}.",
                new TimeoutException())
            { Endpoint = path };
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string? rawBody = await ReadBodySafelyAsync(response, cancellationToken).ConfigureAwait(false);
                throw OllamaErrorTranslator.Translate(response, path, rawBody);
            }
        }
    }

    private HttpRequestMessage CreateRequest<TRequest>(
        HttpMethod method,
        string path,
        TRequest? body,
        JsonTypeInfo<TRequest>? requestTypeInfo,
        bool isStreaming,
        OllamaClientOptions options)
        where TRequest : class
    {
        HttpRequestMessage request = CreateRequestCore(method, path, isStreaming, options);

        if (body is not null && requestTypeInfo is not null)
        {
            string json = JsonSerializer.Serialize(body, requestTypeInfo);
            OllamaLog.RequestBody(_logger, json.Length);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static HttpRequestMessage CreateRequestCore(HttpMethod method, string path, bool isStreaming, OllamaClientOptions options)
    {
        HttpRequestMessage request = new(method, path);

        request.Headers.UserAgent.ParseAdd($"{options.UserAgent}/{LibraryVersion}");

        if (!string.IsNullOrWhiteSpace(options.AuthorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", options.AuthorizationHeader);
        }
        else if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {options.ApiKey}");
        }

        if (isStreaming)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-ndjson"));
            request.Headers.TryAddWithoutValidation(OllamaRequestHeaders.Stream, "true");
        }
        else
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        return request;
    }

    private static Activity? StartActivity(HttpMethod method, string path, bool streaming)
    {
        Activity? activity = OllamaActivitySource.Instance.StartActivity($"{method.Method} {path}");
        activity?.SetTag("ollama.endpoint", path);
        activity?.SetTag("ollama.stream", streaming);
        activity?.SetTag("ollama.method", method.Method);
        return activity;
    }

    /// <summary>
    /// Maps a transport-level failure to the most specific typed Ollama exception.
    /// DNS-resolution failures (HostNotFound, NoData, TryAgain, NoRecovery) surface as
    /// <see cref="OllamaConfigurationException"/> — they almost always mean
    /// <see cref="OllamaClientOptions.BaseAddress"/> is wrong — while every other
    /// socket/transport failure becomes <see cref="OllamaConnectionException"/>. The
    /// guard also unwraps <see cref="HttpRequestException"/> → <see cref="SocketException"/>
    /// and handles raw <see cref="SocketException"/> (e.g. pre-request DNS lookups).
    /// </summary>
    private OllamaException TranslateTransportError(Exception ex, string path, OllamaClientOptions options)
    {
        // Also inspect OllamaConfigurationException thrown from inside our own
        // ConnectCallback (DisallowPrivateNetworks) — those are already typed
        // correctly; when wrapped in HttpRequestException we unwrap them so
        // callers see the direct cause rather than a generic connection error.
        if (ex is HttpRequestException { InnerException: OllamaConfigurationException inner })
        {
            return inner;
        }

        SocketException? socketEx = ex switch
        {
            SocketException s => s,
            HttpRequestException { InnerException: SocketException s } => s,
            _ => null,
        };

        Uri? baseAddress = _httpClient.BaseAddress ?? options.BaseAddress;

        if (IsDnsResolutionFailure(socketEx))
        {
            return new OllamaConfigurationException(
                $"DNS resolution failed for Ollama BaseAddress '{baseAddress}': {socketEx!.Message}. " +
                "Verify OllamaClientOptions.BaseAddress points at a reachable host.",
                ex)
            {
                Endpoint = path,
            };
        }

        return new OllamaConnectionException(
            $"Could not connect to Ollama at '{baseAddress}'. Is the Ollama server running? ({ex.Message})",
            ex)
        {
            Endpoint = path,
        };
    }

    private static bool IsDnsResolutionFailure(SocketException? socketEx)
    {
        if (socketEx is null)
        {
            return false;
        }

        return socketEx.SocketErrorCode is
            SocketError.HostNotFound or
            SocketError.NoData or
            SocketError.TryAgain or
            SocketError.NoRecovery;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Reading the error body is best-effort; any failure is reported via the surrounding error path.")]
    private static async Task<string?> ReadBodySafelyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void CheckForEmbeddedError(string responseBody, string path)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("error", out JsonElement errorElem) &&
                errorElem.ValueKind == JsonValueKind.String)
            {
                string? errorMessage = errorElem.GetString();
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    throw new OllamaApiException(
                        $"Ollama returned an error in a successful response: {errorMessage}",
                        System.Net.HttpStatusCode.OK)
                    {
                        Endpoint = path,
                        RawServerError = responseBody
                    };
                }
            }
        }
        catch (JsonException)
        {
            // Non-JSON bodies are allowed here; the caller will see a deserialization error if needed.
        }
    }

    private static void ValidateContentType(HttpResponseMessage response, string path, bool expectNdjson)
    {
        MediaTypeHeaderValue? ct = response.Content.Headers.ContentType;
        if (ct is null || string.IsNullOrEmpty(ct.MediaType))
        {
            // Ollama sometimes omits content-type on empty or plain responses; treat as permissive.
            return;
        }

        string media = ct.MediaType;

        bool ok = expectNdjson
            ? (media.Equals("application/x-ndjson", StringComparison.OrdinalIgnoreCase)
               || media.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            : media.Equals("application/json", StringComparison.OrdinalIgnoreCase);

        if (!ok)
        {
            throw new OllamaDeserializationException(
                $"Unexpected content-type '{media}' from '{path}'.",
                new JsonException($"Unexpected content-type '{media}'."))
            {
                Endpoint = path
            };
        }
    }
}

/// <summary>Names of custom HTTP request headers emitted by the client.</summary>
internal static class OllamaRequestHeaders
{
    /// <summary>
    /// Marker header set on streaming requests so the shared resilience handler can skip retries.
    /// Retrying a partially-consumed NDJSON stream would produce corrupt output.
    /// </summary>
    public const string Stream = "X-Ollama-Stream";
}
