using Ollama.Net.Models.Requests;
using Ollama.Net.Models.Responses;

namespace Ollama.Net.Abstractions;

/// <summary>
/// Client for model management operations.
/// </summary>
public interface IOllamaModelsClient
{
    /// <summary>
    /// Lists all available models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of model information.</returns>
    Task<ModelList> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows detailed information about a model.
    /// </summary>
    /// <param name="request">The show model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed model information.</returns>
    Task<ShowModelResponse> ShowModelAsync(ShowModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls a model from the Ollama registry (non-streaming, returns final result).
    /// </summary>
    /// <param name="request">The pull model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final progress response.</returns>
    Task<ProgressResponse> PullModelAsync(PullModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls a model from the Ollama registry with streaming progress updates.
    /// </summary>
    /// <param name="request">The pull model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of progress updates.</returns>
    IAsyncEnumerable<ProgressResponse> PullModelStreamAsync(PullModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes a model to the Ollama registry (non-streaming, returns final result).
    /// </summary>
    /// <param name="request">The push model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final progress response.</returns>
    Task<ProgressResponse> PushModelAsync(PushModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes a model to the Ollama registry with streaming progress updates.
    /// </summary>
    /// <param name="request">The push model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of progress updates.</returns>
    IAsyncEnumerable<ProgressResponse> PushModelStreamAsync(PushModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new model from a Modelfile (non-streaming, returns final result).
    /// </summary>
    /// <param name="request">The create model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The final progress response.</returns>
    Task<ProgressResponse> CreateModelAsync(CreateModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new model from a Modelfile with streaming progress updates.
    /// </summary>
    /// <param name="request">The create model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of progress updates.</returns>
    IAsyncEnumerable<ProgressResponse> CreateModelStreamAsync(CreateModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a model.
    /// </summary>
    /// <param name="request">The delete model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteModelAsync(DeleteModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a model.
    /// </summary>
    /// <param name="request">The copy model request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CopyModelAsync(CopyModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists currently running models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of running models.</returns>
    Task<RunningModelList> ListRunningModelsAsync(CancellationToken cancellationToken = default);
}
