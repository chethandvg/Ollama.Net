using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ollama.Net.Exceptions;
using Ollama.Net.Internal.Json;

namespace Ollama.Net.Http;

/// <summary>
/// Translates non-success <see cref="HttpResponseMessage"/> instances into typed
/// <see cref="OllamaException"/> instances. This is the single point where
/// status codes and server error strings are mapped to specific exception types,
/// which keeps the behavior testable in isolation.
/// </summary>
internal static partial class OllamaErrorTranslator
{
    private const int MaxRawBodyPreviewChars = 200;

    /// <summary>Matches names embedded in server error messages such as
    /// <c>model 'llama3' not found</c>, <c>model "qwen2:7b" was not found</c>,
    /// or <c>model llama3 must be pulled</c>.</summary>
    [GeneratedRegex(
        """(?:model|name)\s+['"]?(?<name>[a-zA-Z0-9][\w./:-]*)['"]?""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 200)]
    private static partial Regex EmbeddedModelNameRegex();

    /// <summary>
    /// Translates an HTTP response into an appropriate exception.
    /// </summary>
    public static OllamaException Translate(
        HttpResponseMessage response,
        string endpoint,
        string? rawBody)
    {
        ArgumentNullException.ThrowIfNull(response);

        HttpStatusCode statusCode = response.StatusCode;
        string? requestId = response.Headers.TryGetValues("X-Request-Id", out IEnumerable<string>? values)
            ? values.FirstOrDefault()
            : null;

        string serverError = TryExtractErrorMessage(rawBody) ?? $"HTTP {(int)statusCode} {response.ReasonPhrase}";

        OllamaException exception = statusCode switch
        {
            HttpStatusCode.BadRequest =>
                new OllamaRequestValidationException($"Ollama rejected the request: {serverError}. Endpoint: {endpoint}"),

            HttpStatusCode.Unauthorized =>
                new OllamaAuthenticationException(
                    "Ollama authentication failed. Verify OllamaClientOptions.AuthorizationHeader " +
                    "or OllamaClientOptions.ApiKey (for Ollama Cloud, set your API key from https://ollama.com/settings)."),

            HttpStatusCode.PaymentRequired =>
                new OllamaQuotaExceededException(
                    $"Ollama Cloud quota exceeded for endpoint '{endpoint}': {serverError}. " +
                    "Check your subscription and usage at https://ollama.com/settings."),

            HttpStatusCode.Forbidden =>
                new OllamaAuthorizationException($"Ollama authorization failed for endpoint '{endpoint}': {serverError}."),

            HttpStatusCode.NotFound when ContainsPullRequired(rawBody) =>
                BuildPullRequiredException(rawBody),

            HttpStatusCode.NotFound when ContainsModelNotFound(rawBody) =>
                BuildModelNotFoundException(rawBody),

            HttpStatusCode.NotFound =>
                new OllamaApiException(
                    $"Ollama endpoint '{endpoint}' not found (HTTP 404). The server version may not support this API.",
                    HttpStatusCode.NotFound),

            HttpStatusCode.MethodNotAllowed =>
                new OllamaApiException($"Method not allowed on '{endpoint}'.", HttpStatusCode.MethodNotAllowed),

            HttpStatusCode.Conflict =>
                new OllamaApiException($"Ollama reported a conflict: {serverError}.", HttpStatusCode.Conflict),

            HttpStatusCode.RequestEntityTooLarge =>
                new OllamaPayloadTooLargeException(
                    "Prompt exceeds the model's context window. Reduce prompt size or increase num_ctx.",
                    HttpStatusCode.RequestEntityTooLarge),

            HttpStatusCode.UnprocessableEntity =>
                new OllamaRequestValidationException($"Ollama validation error: {serverError}."),

            HttpStatusCode.TooManyRequests =>
                new OllamaRateLimitedException(
                    BuildRateLimitMessage(serverError, ParseRetryAfter(response)),
                    ParseRetryAfter(response)),

            HttpStatusCode.InternalServerError when ContainsContextLengthError(rawBody) =>
                new OllamaPayloadTooLargeException(
                    "Prompt exceeds the model's context window. Reduce prompt size or increase num_ctx.",
                    HttpStatusCode.InternalServerError),

            HttpStatusCode.InternalServerError when ContainsOutOfMemoryError(rawBody) =>
                new OllamaServerException(
                    "Ollama ran out of memory. Try a smaller model or reduce num_ctx/num_gpu.")
                { IsOutOfMemory = true },

            HttpStatusCode.InternalServerError when ContainsDiskFullError(rawBody) =>
                new OllamaServerException("Ollama ran out of disk space while processing the request.")
                { IsDiskFull = true },

            HttpStatusCode.InternalServerError =>
                new OllamaServerException($"Ollama server error: {serverError}."),

            HttpStatusCode.NotImplemented =>
                new OllamaApiException(
                    $"Ollama endpoint '{endpoint}' is not implemented on this server version.",
                    HttpStatusCode.NotImplemented),

            HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout =>
                new OllamaServerException($"Ollama gateway error (HTTP {(int)statusCode}). Check your reverse proxy."),

            HttpStatusCode.ServiceUnavailable =>
                new OllamaServiceUnavailableException(
                    "Ollama service unavailable. The server may be loading a model."),

            _ => new OllamaApiException(
                $"Ollama request to '{endpoint}' failed with HTTP {(int)statusCode}: {serverError}",
                statusCode)
        };

        // Every arm of the switch above returns an OllamaApiException or subtype, but
        // we still want the Endpoint tag on the common base if that ever changes.
        OllamaApiException apiException = (OllamaApiException)exception;
        apiException.RawServerError = rawBody;
        apiException.RequestId = requestId;
        apiException.Endpoint = endpoint;

        return exception;
    }

    private static OllamaModelPullRequiredException BuildPullRequiredException(string? rawBody)
    {
        string? modelName = ExtractModelName(rawBody);
        return new OllamaModelPullRequiredException(
            $"Model '{modelName ?? "(unknown)"}' must be pulled before use. " +
            "Run 'ollama pull <model>' or call IOllamaModelsClient.PullModelAsync first.",
            modelName);
    }

    private static OllamaModelNotFoundException BuildModelNotFoundException(string? rawBody)
    {
        string? modelName = ExtractModelName(rawBody);
        return new OllamaModelNotFoundException(
            $"Model '{modelName ?? "(unknown)"}' was not found on the Ollama server. " +
            "Run 'ollama pull <model>' or call IOllamaModelsClient.PullModelAsync first.",
            modelName);
    }

    private static string BuildRateLimitMessage(string serverError, TimeSpan? retryAfter)
    {
        // Ollama Cloud returns 429 for both per-second and per-hour/day quota limits.
        // The server message usually contains the word "quota" or "limit" to distinguish.
        bool looksLikeQuota = serverError.Contains("quota", StringComparison.OrdinalIgnoreCase)
                              || serverError.Contains("hourly", StringComparison.OrdinalIgnoreCase)
                              || serverError.Contains("daily", StringComparison.OrdinalIgnoreCase);

        string prefix = looksLikeQuota
            ? $"Ollama quota limit reached ({serverError})."
            : $"Ollama is rate-limiting requests: {serverError}.";

        return $"{prefix} Retry after {FormatRetryAfter(retryAfter)}.";
    }

    private static string? TryExtractErrorMessage(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            var error = JsonSerializer.Deserialize(rawBody, OllamaJsonContext.Default.OllamaErrorPayload);
            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                return error.Error;
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return rawBody.Length > MaxRawBodyPreviewChars ? rawBody[..MaxRawBodyPreviewChars] : rawBody;
    }

    private static bool ContainsModelNotFound(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        bool hasModelKeyword = body.Contains("model", StringComparison.OrdinalIgnoreCase)
                              || body.Contains("\"name\"", StringComparison.OrdinalIgnoreCase);
        bool hasNotFound = body.Contains("not found", StringComparison.OrdinalIgnoreCase)
                          || body.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
        return hasModelKeyword && hasNotFound;
    }

    private static bool ContainsPullRequired(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.Contains("try pulling", StringComparison.OrdinalIgnoreCase)
            || body.Contains("ollama pull", StringComparison.OrdinalIgnoreCase)
            || body.Contains("must be pulled", StringComparison.OrdinalIgnoreCase)
            || body.Contains("needs to be pulled", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsContextLengthError(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        bool hasContext = body.Contains("context", StringComparison.OrdinalIgnoreCase);
        bool hasLengthOrSize = body.Contains("length", StringComparison.OrdinalIgnoreCase)
                              || body.Contains("too large", StringComparison.OrdinalIgnoreCase)
                              || body.Contains("exceed", StringComparison.OrdinalIgnoreCase)
                              || body.Contains("num_ctx", StringComparison.OrdinalIgnoreCase);
        return hasContext && hasLengthOrSize;
    }

    private static bool ContainsOutOfMemoryError(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.Contains("out of memory", StringComparison.OrdinalIgnoreCase)
            || body.Contains("cuda error", StringComparison.OrdinalIgnoreCase)
            || body.Contains("oom", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsDiskFullError(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.Contains("no space", StringComparison.OrdinalIgnoreCase)
            || body.Contains("disk full", StringComparison.OrdinalIgnoreCase)
            || body.Contains("enospc", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractModelName(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        // 1) Structured: top-level "model" field in the error JSON.
        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("model", out JsonElement modelElem)
                && modelElem.ValueKind == JsonValueKind.String)
            {
                return modelElem.GetString();
            }

            // 2) Ollama commonly puts the human message in "error" — scan it with the regex.
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("error", out JsonElement errorElem)
                && errorElem.ValueKind == JsonValueKind.String)
            {
                string? errorText = errorElem.GetString();
                if (!string.IsNullOrEmpty(errorText))
                {
                    Match m = EmbeddedModelNameRegex().Match(errorText);
                    if (m.Success)
                    {
                        return m.Groups["name"].Value;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // non-JSON body — try the regex on the whole body below.
        }

        // 3) Non-JSON bodies: last-ditch regex over the raw text.
        try
        {
            Match m = EmbeddedModelNameRegex().Match(body);
            if (m.Success)
            {
                return m.Groups["name"].Value;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // adversarial input — give up rather than block the thread.
        }

        return null;
    }

    private static TimeSpan? ParseRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter is null)
        {
            return null;
        }

        if (response.Headers.RetryAfter.Delta.HasValue)
        {
            return response.Headers.RetryAfter.Delta.Value;
        }

        if (response.Headers.RetryAfter.Date.HasValue)
        {
            TimeSpan delta = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
            return delta > TimeSpan.Zero ? delta : TimeSpan.FromSeconds(1);
        }

        return null;
    }

    private static string FormatRetryAfter(TimeSpan? retryAfter)
    {
        if (!retryAfter.HasValue)
        {
            return "(no Retry-After header)";
        }

        return $"{retryAfter.Value.TotalSeconds:F0}s";
    }
}
