using Krutaka.Ollama.Abstractions;
using Krutaka.Ollama.Http;
using Krutaka.Ollama.Internal.Json;
using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Clients;

/// <summary>Implementation of system-level operations. Routes all traffic through <see cref="OllamaHttpClient"/>.</summary>
internal sealed class OllamaSystemClient : IOllamaSystemClient
{
    private readonly OllamaHttpClient _httpClient;

    public OllamaSystemClient(OllamaHttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        return _httpClient.SendJsonAsync<object, VersionResponse>(
            HttpMethod.Get,
            "/api/version",
            body: null,
            OllamaJsonContext.Default.VersionResponse,
            requestTypeInfo: null,
            cancellationToken);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.HeadAsync("/", cancellationToken).ConfigureAwait(false);
        }
        catch (Exceptions.OllamaConnectionException)
        {
            return false;
        }
        catch (Exceptions.OllamaTimeoutException)
        {
            return false;
        }
    }

    public async Task<bool> BlobExistsAsync(string digest, CancellationToken cancellationToken = default)
    {
        ValidateDigest(digest);
        return await _httpClient
            .HeadAsync($"/api/blobs/{digest}", cancellationToken)
            .ConfigureAwait(false);
    }

    public Task PushBlobAsync(string digest, Stream content, CancellationToken cancellationToken = default)
    {
        ValidateDigest(digest);
        ArgumentNullException.ThrowIfNull(content);

        return _httpClient.SendStreamBodyAsync(
            HttpMethod.Post,
            $"/api/blobs/{digest}",
            content,
            cancellationToken);
    }

    private static void ValidateDigest(string digest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(digest);

        // Accept both "sha256:<hex>" and bare hex forms; Ollama's canonical form is "sha256:<64-hex-chars>".
        string hex = digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? digest[7..]
            : digest;

        if (hex.Length != 64 || !IsHex(hex))
        {
            throw new ArgumentException(
                "Blob digest must be a sha256 value: 'sha256:<64 hex chars>' or 64 hex characters.",
                nameof(digest));
        }
    }

    private static bool IsHex(ReadOnlySpan<char> s)
    {
        foreach (char c in s)
        {
            if (!((c >= '0' && c <= '9')
                || (c >= 'a' && c <= 'f')
                || (c >= 'A' && c <= 'F')))
            {
                return false;
            }
        }

        return true;
    }
}
