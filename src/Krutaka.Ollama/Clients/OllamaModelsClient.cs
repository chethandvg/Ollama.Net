using System.Runtime.CompilerServices;
using Krutaka.Ollama.Abstractions;
using Krutaka.Ollama.Exceptions;
using Krutaka.Ollama.Http;
using Krutaka.Ollama.Internal.Json;
using Krutaka.Ollama.Internal.Validation;
using Krutaka.Ollama.Models.Requests;
using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Clients;

/// <summary>Implementation of model management operations.</summary>
internal sealed class OllamaModelsClient : IOllamaModelsClient
{
    private readonly OllamaHttpClient _httpClient;

    public OllamaModelsClient(OllamaHttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    public Task<ModelList> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        return _httpClient.SendJsonAsync<object, ModelList>(
            HttpMethod.Get,
            "/api/tags",
            body: null,
            OllamaJsonContext.Default.ModelList,
            requestTypeInfo: null,
            cancellationToken);
    }

    public Task<ShowModelResponse> ShowModelAsync(
        ShowModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        return _httpClient.SendJsonAsync(
            HttpMethod.Post,
            "/api/show",
            request,
            OllamaJsonContext.Default.ShowModelResponse,
            OllamaJsonContext.Default.ShowModelRequest,
            cancellationToken);
    }

    public async Task<ProgressResponse> PullModelAsync(
        PullModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        ProgressResponse? last = null;
        await foreach (ProgressResponse progress in PullModelStreamAsync(request, cancellationToken).ConfigureAwait(false))
        {
            last = progress;
        }

        return last ?? throw EmptyProgressStream("/api/pull", request.Model);
    }

    public async IAsyncEnumerable<ProgressResponse> PullModelStreamAsync(
        PullModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        PullModelRequest body = request with { Stream = true };

        await foreach (ProgressResponse progress in _httpClient.SendStreamAsync(
            HttpMethod.Post,
            "/api/pull",
            body,
            OllamaJsonContext.Default.ProgressResponse,
            OllamaJsonContext.Default.PullModelRequest,
            cancellationToken).ConfigureAwait(false))
        {
            yield return progress;
        }
    }

    public async Task<ProgressResponse> PushModelAsync(
        PushModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        ProgressResponse? last = null;
        await foreach (ProgressResponse progress in PushModelStreamAsync(request, cancellationToken).ConfigureAwait(false))
        {
            last = progress;
        }

        return last ?? throw EmptyProgressStream("/api/push", request.Model);
    }

    public async IAsyncEnumerable<ProgressResponse> PushModelStreamAsync(
        PushModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        PushModelRequest body = request with { Stream = true };

        await foreach (ProgressResponse progress in _httpClient.SendStreamAsync(
            HttpMethod.Post,
            "/api/push",
            body,
            OllamaJsonContext.Default.ProgressResponse,
            OllamaJsonContext.Default.PushModelRequest,
            cancellationToken).ConfigureAwait(false))
        {
            yield return progress;
        }
    }

    public async Task<ProgressResponse> CreateModelAsync(
        CreateModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        ProgressResponse? last = null;
        await foreach (ProgressResponse progress in CreateModelStreamAsync(request, cancellationToken).ConfigureAwait(false))
        {
            last = progress;
        }

        return last ?? throw EmptyProgressStream("/api/create", request.Model);
    }

    public async IAsyncEnumerable<ProgressResponse> CreateModelStreamAsync(
        CreateModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        CreateModelRequest body = request with { Stream = true };

        await foreach (ProgressResponse progress in _httpClient.SendStreamAsync(
            HttpMethod.Post,
            "/api/create",
            body,
            OllamaJsonContext.Default.ProgressResponse,
            OllamaJsonContext.Default.CreateModelRequest,
            cancellationToken).ConfigureAwait(false))
        {
            yield return progress;
        }
    }

    public Task DeleteModelAsync(
        DeleteModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.ValidateModel(request.Model);

        return _httpClient.SendJsonVoidAsync(
            HttpMethod.Delete,
            "/api/delete",
            request,
            OllamaJsonContext.Default.DeleteModelRequest,
            cancellationToken);
    }

    public Task CopyModelAsync(
        CopyModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            throw new ArgumentException("CopyModelRequest.Source is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Destination))
        {
            throw new ArgumentException("CopyModelRequest.Destination is required.", nameof(request));
        }

        return _httpClient.SendJsonVoidAsync(
            HttpMethod.Post,
            "/api/copy",
            request,
            OllamaJsonContext.Default.CopyModelRequest,
            cancellationToken);
    }

    public Task<RunningModelList> ListRunningModelsAsync(CancellationToken cancellationToken = default)
    {
        return _httpClient.SendJsonAsync<object, RunningModelList>(
            HttpMethod.Get,
            "/api/ps",
            body: null,
            OllamaJsonContext.Default.RunningModelList,
            requestTypeInfo: null,
            cancellationToken);
    }

    private static OllamaStreamException EmptyProgressStream(string endpoint, string model)
    {
        return new OllamaStreamException(
            $"Ollama returned no progress records from '{endpoint}' for model '{model}'. " +
            "This usually indicates a truncated or aborted server response; treat the operation as failed and retry.")
        {
            IsTruncated = true,
            Endpoint = endpoint,
        };
    }
}
