using System.Text.Json;
using System.Text.Json.Serialization;
using Krutaka.Ollama.Models.Common;
using Krutaka.Ollama.Models.Errors;
using Krutaka.Ollama.Models.Requests;
using Krutaka.Ollama.Models.Responses;

namespace Krutaka.Ollama.Internal.Json;

/// <summary>
/// Source-generated JSON serialization context for Ollama DTOs.
/// </summary>
#pragma warning disable CS0618 // Legacy types are intentionally included for backward compatibility
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
)]
[JsonSerializable(typeof(GenerateRequest))]
[JsonSerializable(typeof(GenerateResponse))]
[JsonSerializable(typeof(ChatRequest))]
[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(EmbedRequest))]
[JsonSerializable(typeof(EmbedResponse))]
[JsonSerializable(typeof(LegacyEmbeddingRequest))]
[JsonSerializable(typeof(LegacyEmbeddingResponse))]
[JsonSerializable(typeof(ShowModelRequest))]
[JsonSerializable(typeof(ShowModelResponse))]
[JsonSerializable(typeof(PullModelRequest))]
[JsonSerializable(typeof(PushModelRequest))]
[JsonSerializable(typeof(CreateModelRequest))]
[JsonSerializable(typeof(DeleteModelRequest))]
[JsonSerializable(typeof(CopyModelRequest))]
[JsonSerializable(typeof(ModelList))]
[JsonSerializable(typeof(ModelInfo))]
[JsonSerializable(typeof(ProgressResponse))]
[JsonSerializable(typeof(RunningModel))]
[JsonSerializable(typeof(RunningModelList))]
[JsonSerializable(typeof(VersionResponse))]
[JsonSerializable(typeof(OllamaErrorPayload))]
[JsonSerializable(typeof(OllamaMessage))]
[JsonSerializable(typeof(OllamaOptions))]
[JsonSerializable(typeof(ToolDefinition))]
[JsonSerializable(typeof(FunctionDefinition))]
[JsonSerializable(typeof(ToolCall))]
[JsonSerializable(typeof(ToolCallFunction))]
[JsonSerializable(typeof(ModelDetails))]
[JsonSerializable(typeof(OllamaRole))]
internal sealed partial class OllamaJsonContext : JsonSerializerContext
{
}
#pragma warning restore CS0618
