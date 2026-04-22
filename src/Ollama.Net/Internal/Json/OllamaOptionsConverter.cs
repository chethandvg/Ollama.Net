using System.Text.Json;
using System.Text.Json.Serialization;
using Ollama.Net.Models.Common;

namespace Ollama.Net.Internal.Json;

/// <summary>
/// Hand-written, AOT-safe converter for <see cref="OllamaOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// Written manually (rather than relying on the source generator) so that the
/// forward-compatibility <see cref="OllamaOptions.Extra"/> bag can be flattened into the
/// serialised <c>options</c> object without a dynamic code path. This preserves
/// <c>IsAotCompatible = true</c>.
/// </para>
/// <para>
/// On read, unknown keys are bucketed into <see cref="OllamaOptions.Extra"/> so they
/// round-trip cleanly even if future Ollama versions add new option keys.
/// </para>
/// </remarks>
internal sealed class OllamaOptionsConverter : JsonConverter<OllamaOptions>
{
    public override OllamaOptions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject for OllamaOptions, got {reader.TokenType}.");
        }

        double? temperature = null, topP = null, minP = null, typicalP = null,
                repeatPenalty = null, mirostatTau = null, mirostatEta = null,
                presencePenalty = null, frequencyPenalty = null;
        int? topK = null, numCtx = null, numPredict = null, numKeep = null,
             repeatLastN = null, seed = null, mirostatMode = null,
             numGpu = null, mainGpu = null, numThread = null, numBatch = null;
        bool? penalizeNewline = null, useMmap = null, numa = null;
        string[]? stop = null;
        string? format = null;
        Dictionary<string, JsonElement>? extra = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
#pragma warning disable CS0618 // Obsolete Format parameter is intentionally populated for round-tripping.
                return new OllamaOptions(
                    Temperature: temperature,
                    TopP: topP,
                    TopK: topK,
                    MinP: minP,
                    TypicalP: typicalP,
                    NumCtx: numCtx,
                    NumPredict: numPredict,
                    NumKeep: numKeep,
                    RepeatLastN: repeatLastN,
                    Stop: stop,
                    Seed: seed,
                    RepeatPenalty: repeatPenalty,
                    PenalizeNewline: penalizeNewline,
                    MirostatTau: mirostatTau,
                    MirostatEta: mirostatEta,
                    MirostatMode: mirostatMode,
                    PresencePenalty: presencePenalty,
                    FrequencyPenalty: frequencyPenalty,
                    NumGpu: numGpu,
                    MainGpu: mainGpu,
                    NumThread: numThread,
                    NumBatch: numBatch,
                    UseMmap: useMmap,
                    Numa: numa,
                    Format: format,
                    Extra: extra);
#pragma warning restore CS0618
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName, got {reader.TokenType}.");
            }

            string name = reader.GetString()!;
            reader.Read();

            switch (name)
            {
                case "temperature": temperature = ReadNullableDouble(ref reader); break;
                case "top_p": topP = ReadNullableDouble(ref reader); break;
                case "top_k": topK = ReadNullableInt(ref reader); break;
                case "min_p": minP = ReadNullableDouble(ref reader); break;
                case "typical_p": typicalP = ReadNullableDouble(ref reader); break;
                case "num_ctx": numCtx = ReadNullableInt(ref reader); break;
                case "num_predict": numPredict = ReadNullableInt(ref reader); break;
                case "num_keep": numKeep = ReadNullableInt(ref reader); break;
                case "repeat_last_n": repeatLastN = ReadNullableInt(ref reader); break;
                case "stop": stop = ReadStringArray(ref reader); break;
                case "seed": seed = ReadNullableInt(ref reader); break;
                case "repeat_penalty": repeatPenalty = ReadNullableDouble(ref reader); break;
                case "penalize_newline": penalizeNewline = ReadNullableBool(ref reader); break;
                case "mirostat_tau": mirostatTau = ReadNullableDouble(ref reader); break;
                case "mirostat_eta": mirostatEta = ReadNullableDouble(ref reader); break;
                case "mirostat": mirostatMode = ReadNullableInt(ref reader); break;
                case "presence_penalty": presencePenalty = ReadNullableDouble(ref reader); break;
                case "frequency_penalty": frequencyPenalty = ReadNullableDouble(ref reader); break;
                case "num_gpu": numGpu = ReadNullableInt(ref reader); break;
                case "main_gpu": mainGpu = ReadNullableInt(ref reader); break;
                case "num_thread": numThread = ReadNullableInt(ref reader); break;
                case "num_batch": numBatch = ReadNullableInt(ref reader); break;
                case "use_mmap": useMmap = ReadNullableBool(ref reader); break;
                case "numa": numa = ReadNullableBool(ref reader); break;
                case "format":
                    format = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;
                default:
                    extra ??= [];
                    extra[name] = JsonElement.ParseValue(ref reader);
                    break;
            }
        }

        throw new JsonException("Unexpected end of stream while reading OllamaOptions.");
    }

    public override void Write(Utf8JsonWriter writer, OllamaOptions value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();

        WriteIfPresent(writer, "temperature", value.Temperature);
        WriteIfPresent(writer, "top_p", value.TopP);
        WriteIfPresent(writer, "top_k", value.TopK);
        WriteIfPresent(writer, "min_p", value.MinP);
        WriteIfPresent(writer, "typical_p", value.TypicalP);
        WriteIfPresent(writer, "num_ctx", value.NumCtx);
        WriteIfPresent(writer, "num_predict", value.NumPredict);
        WriteIfPresent(writer, "num_keep", value.NumKeep);
        WriteIfPresent(writer, "repeat_last_n", value.RepeatLastN);

        if (value.Stop is { Length: > 0 } stop)
        {
            writer.WritePropertyName("stop");
            writer.WriteStartArray();
            foreach (string s in stop)
            {
                writer.WriteStringValue(s);
            }
            writer.WriteEndArray();
        }

        WriteIfPresent(writer, "seed", value.Seed);
        WriteIfPresent(writer, "repeat_penalty", value.RepeatPenalty);
        WriteIfPresent(writer, "penalize_newline", value.PenalizeNewline);
        WriteIfPresent(writer, "mirostat_tau", value.MirostatTau);
        WriteIfPresent(writer, "mirostat_eta", value.MirostatEta);
        WriteIfPresent(writer, "mirostat", value.MirostatMode);
        WriteIfPresent(writer, "presence_penalty", value.PresencePenalty);
        WriteIfPresent(writer, "frequency_penalty", value.FrequencyPenalty);
        WriteIfPresent(writer, "num_gpu", value.NumGpu);
        WriteIfPresent(writer, "main_gpu", value.MainGpu);
        WriteIfPresent(writer, "num_thread", value.NumThread);
        WriteIfPresent(writer, "num_batch", value.NumBatch);
        WriteIfPresent(writer, "use_mmap", value.UseMmap);
        WriteIfPresent(writer, "numa", value.Numa);

#pragma warning disable CS0618 // Serialise the deprecated legacy 'format' field if callers set it.
        if (value.Format is not null)
        {
            writer.WriteString("format", value.Format);
        }
#pragma warning restore CS0618

        if (value.Extra is { Count: > 0 } extras)
        {
            foreach (KeyValuePair<string, JsonElement> kv in extras)
            {
                if (string.IsNullOrEmpty(kv.Key))
                {
                    throw new JsonException("OllamaOptions.Extra must not contain null or empty keys.");
                }

                writer.WritePropertyName(kv.Key);
                kv.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }

    private static void WriteIfPresent(Utf8JsonWriter writer, string name, double? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(name, value.Value);
        }
    }

    private static void WriteIfPresent(Utf8JsonWriter writer, string name, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(name, value.Value);
        }
    }

    private static void WriteIfPresent(Utf8JsonWriter writer, string name, bool? value)
    {
        if (value.HasValue)
        {
            writer.WriteBoolean(name, value.Value);
        }
    }

    private static double? ReadNullableDouble(ref Utf8JsonReader reader) =>
        reader.TokenType == JsonTokenType.Null ? null : reader.GetDouble();

    private static int? ReadNullableInt(ref Utf8JsonReader reader) =>
        reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();

    private static bool? ReadNullableBool(ref Utf8JsonReader reader) =>
        reader.TokenType == JsonTokenType.Null ? null : reader.GetBoolean();

    private static string[]? ReadStringArray(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected StartArray for 'stop', got {reader.TokenType}.");
        }

        List<string> items = [];
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("'stop' entries must be strings.");
            }

            items.Add(reader.GetString()!);
        }

        return [.. items];
    }
}
