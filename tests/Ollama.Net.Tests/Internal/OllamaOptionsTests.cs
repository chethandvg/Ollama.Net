using System.Text.Json;
using FluentAssertions;
using Ollama.Net.Internal.Json;
using Ollama.Net.Models.Common;
using Ollama.Net.Models.Requests;
using Xunit;

namespace Ollama.Net.Tests.Internal;

/// <summary>Unit tests for <see cref="OllamaOptions"/> serialisation.</summary>
public sealed class OllamaOptionsTests
{
    private static string SerialiseOptions(OllamaOptions opts) =>
        JsonSerializer.Serialize(opts, OllamaJsonContext.Default.OllamaOptions);

    [Fact]
    public void Serialise_OnlyIncludesNonNullFields()
    {
        string json = SerialiseOptions(new OllamaOptions(Temperature: 0.5, TopP: 0.9));

        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("temperature", "top_p");
        doc.RootElement.GetProperty("temperature").GetDouble().Should().Be(0.5);
        doc.RootElement.GetProperty("top_p").GetDouble().Should().Be(0.9);
    }

    [Fact]
    public void Serialise_AllNewFields_UseSnakeCase()
    {
        OllamaOptions opts = new(
            MinP: 0.05,
            TypicalP: 0.7,
            NumKeep: 5,
            RepeatLastN: 64,
            PenalizeNewline: true,
            NumBatch: 2,
            MainGpu: 0,
            UseMmap: true,
            Numa: false);

        using JsonDocument doc = JsonDocument.Parse(SerialiseOptions(opts));
        JsonElement root = doc.RootElement;

        root.GetProperty("min_p").GetDouble().Should().Be(0.05);
        root.GetProperty("typical_p").GetDouble().Should().Be(0.7);
        root.GetProperty("num_keep").GetInt32().Should().Be(5);
        root.GetProperty("repeat_last_n").GetInt32().Should().Be(64);
        root.GetProperty("penalize_newline").GetBoolean().Should().BeTrue();
        root.GetProperty("num_batch").GetInt32().Should().Be(2);
        root.GetProperty("main_gpu").GetInt32().Should().Be(0);
        root.GetProperty("use_mmap").GetBoolean().Should().BeTrue();
        root.GetProperty("numa").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void Serialise_Extra_IsFlattenedIntoOptionsObject()
    {
        using JsonDocument payload = JsonDocument.Parse("""{"hypothetical_future_knob": 42}""");
        OllamaOptions opts = new(
            Temperature: 0.1,
            Extra: new Dictionary<string, JsonElement>
            {
                ["custom_string"] = JsonDocument.Parse("\"hi\"").RootElement.Clone(),
                ["custom_number"] = payload.RootElement.GetProperty("hypothetical_future_knob").Clone(),
            });

        using JsonDocument doc = JsonDocument.Parse(SerialiseOptions(opts));
        JsonElement root = doc.RootElement;

        root.GetProperty("temperature").GetDouble().Should().Be(0.1);
        root.GetProperty("custom_string").GetString().Should().Be("hi");
        root.GetProperty("custom_number").GetInt32().Should().Be(42);
    }

    [Fact]
    public void Serialise_Extra_EmptyBag_DoesNotEmitKeys()
    {
        OllamaOptions opts = new(Temperature: 0.1, Extra: new Dictionary<string, JsonElement>());

        using JsonDocument doc = JsonDocument.Parse(SerialiseOptions(opts));
        doc.RootElement.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("temperature");
    }

    [Fact]
    public void Serialise_Extra_WithEmptyKey_Throws()
    {
        OllamaOptions opts = new(
            Extra: new Dictionary<string, JsonElement>
            {
                [""] = JsonDocument.Parse("1").RootElement.Clone(),
            });

        Action act = () => SerialiseOptions(opts);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Serialise_StopArray_IsEmittedAsArray()
    {
        string json = SerialiseOptions(new OllamaOptions(Stop: ["\n", "user:"]));
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement stop = doc.RootElement.GetProperty("stop");
        stop.ValueKind.Should().Be(JsonValueKind.Array);
        stop[0].GetString().Should().Be("\n");
        stop[1].GetString().Should().Be("user:");
    }

    [Fact]
    public void Serialise_EmptyStopArray_IsOmitted()
    {
        string json = SerialiseOptions(new OllamaOptions(Stop: []));
        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("stop", out _).Should().BeFalse();
    }

    [Fact]
    public void Serialise_MirostatMappedTo_mirostat_Key()
    {
        string json = SerialiseOptions(new OllamaOptions(MirostatMode: 2, MirostatTau: 5.0, MirostatEta: 0.1));
        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("mirostat").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("mirostat_tau").GetDouble().Should().Be(5.0);
        doc.RootElement.GetProperty("mirostat_eta").GetDouble().Should().Be(0.1);
    }

    [Fact]
    public void RoundTrip_KnownFields_Preserved()
    {
        OllamaOptions original = new(
            Temperature: 0.8,
            TopK: 40,
            MinP: 0.05,
            Seed: 42,
            PenalizeNewline: true,
            Stop: ["END"]);

        string json = SerialiseOptions(original);
        OllamaOptions? round = JsonSerializer.Deserialize(json, OllamaJsonContext.Default.OllamaOptions);

        round.Should().NotBeNull();
        round!.Temperature.Should().Be(0.8);
        round.TopK.Should().Be(40);
        round.MinP.Should().Be(0.05);
        round.Seed.Should().Be(42);
        round.PenalizeNewline.Should().BeTrue();
        round.Stop.Should().Equal("END");
    }

    [Fact]
    public void RoundTrip_UnknownFields_LandInExtra()
    {
        const string json = """
            {
              "temperature": 0.7,
              "totally_new_knob": 123,
              "another": "hello"
            }
            """;

        OllamaOptions? opts = JsonSerializer.Deserialize(json, OllamaJsonContext.Default.OllamaOptions);
        opts.Should().NotBeNull();
        opts!.Temperature.Should().Be(0.7);
        opts.Extra.Should().NotBeNull().And.HaveCount(2);
        opts.Extra!["totally_new_knob"].GetInt32().Should().Be(123);
        opts.Extra!["another"].GetString().Should().Be("hello");
    }

    [Fact]
    public void Deserialise_Null_ReturnsNull()
    {
        OllamaOptions? opts = JsonSerializer.Deserialize("null", OllamaJsonContext.Default.OllamaOptions);
        opts.Should().BeNull();
    }

    [Fact]
    public void Deserialise_NonObject_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize("[1,2,3]", OllamaJsonContext.Default.OllamaOptions));
    }

    [Fact]
    public void Deserialise_NullableFieldAsNull_IsAccepted()
    {
        OllamaOptions? opts = JsonSerializer.Deserialize(
            """{"temperature": null, "top_k": null, "stop": null}""",
            OllamaJsonContext.Default.OllamaOptions);

        opts.Should().NotBeNull();
        opts!.Temperature.Should().BeNull();
        opts.TopK.Should().BeNull();
        opts.Stop.Should().BeNull();
    }

    [Fact]
    public void GenerateRequest_OptionsBag_IsEmittedUnderOptionsKey()
    {
        var req = new GenerateRequest(
            "llama3",
            "hi",
            Options: new OllamaOptions(Temperature: 0.2, MinP: 0.01));

        string json = JsonSerializer.Serialize(req, OllamaJsonContext.Default.GenerateRequest);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement opts = doc.RootElement.GetProperty("options");
        opts.GetProperty("temperature").GetDouble().Should().Be(0.2);
        opts.GetProperty("min_p").GetDouble().Should().Be(0.01);
    }
}
