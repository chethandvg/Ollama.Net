namespace Krutaka.Ollama.Internal.Validation;

/// <summary>
/// Validates request parameters before sending to the API.
/// </summary>
internal static class RequestValidator
{
    /// <summary>
    /// Validates that a model name is provided.
    /// </summary>
    /// <param name="model">The model name.</param>
    /// <param name="parameterName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentException">Thrown when model is null or whitespace.</exception>
    public static void ValidateModel(string? model, string parameterName = "model")
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model name must be provided and cannot be empty.", parameterName);
        }
    }

    /// <summary>
    /// Validates that a messages array is not empty.
    /// </summary>
    /// <param name="messages">The messages array.</param>
    /// <param name="parameterName">The parameter name for error messages.</param>
    /// <exception cref="ArgumentException">Thrown when messages is null or empty.</exception>
    public static void ValidateMessages<T>(T[]? messages, string parameterName = "messages")
    {
        ArgumentNullException.ThrowIfNull(messages, parameterName);
        if (messages.Length == 0)
        {
            throw new ArgumentException("Messages array cannot be empty.", parameterName);
        }
    }

    /// <summary>
    /// Validates that stream mode matches the expected value.
    /// </summary>
    /// <param name="requestStream">The stream value in the request.</param>
    /// <param name="expectedStream">The expected stream value for the operation.</param>
    /// <param name="methodName">The calling method name (e.g. <c>nameof(ChatAsync)</c>).
    /// A trailing <c>Async</c> suffix is stripped so the suggested alternative reads as
    /// <c>ChatStreamAsync</c> rather than <c>ChatAsyncStreamAsync</c>.</param>
    /// <exception cref="InvalidOperationException">Thrown when stream mode mismatch.</exception>
    public static void ValidateStreamMode(bool? requestStream, bool expectedStream, string methodName)
    {
        if (requestStream.HasValue && requestStream.Value != expectedStream)
        {
            string baseName = StripAsyncSuffix(methodName);
            string suggestion = expectedStream
                ? $"Use {baseName}StreamAsync instead."
                : $"Use {baseName}Async (non-streaming) instead.";
            throw new InvalidOperationException(
                $"Stream mode mismatch: request has Stream={requestStream}, but this method expects Stream={expectedStream}. {suggestion}");
        }
    }

    private static string StripAsyncSuffix(string methodName)
    {
        const string AsyncSuffix = "Async";
        if (methodName.EndsWith(AsyncSuffix, StringComparison.Ordinal))
        {
            return methodName[..^AsyncSuffix.Length];
        }

        return methodName;
    }
}
