using System.Text.Json;
using FluentAssertions;
using Ollama.Net.Internal.Json;
using Ollama.Net.Models.Common;
using Ollama.Net.Models.Requests;
using Xunit;

namespace Ollama.Net.Tests.Internal;

/// <summary>Unit tests for <see cref="OllamaFormat"/> and its converter.</summary>
public sealed class OllamaFormatTests
{
    private static readonly JsonSerializerOptions SerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void Json_Constant_IsModeNotSchema()
    {
        OllamaFormat f = OllamaFormat.Json;
        f.IsMode.Should().BeTrue();
        f.IsSchema.Should().BeFalse();
        f.AsMode().Should().Be("json");
    }

    [Fact]
    public void FromString_RejectsNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => OllamaFormat.FromString(null!));
        Assert.Throws<ArgumentException>(() => OllamaFormat.FromString(string.Empty));
    }

    [Fact]
    public void FromSchema_RejectsNonObject()
    {
        using JsonDocument doc = JsonDocument.Parse("[1,2,3]");
        Action act = () => OllamaFormat.FromSchema(doc.RootElement);
        act.Should().Throw<ArgumentException>().WithMessage("*JSON object*");
    }

    [Fact]
    public void FromSchema_ClonesSoCallerCanDispose()
    {
        OllamaFormat f;
        using (JsonDocument doc = JsonDocument.Parse("""{"type":"object"}"""))
        {
            f = OllamaFormat.FromSchema(doc.RootElement);
        }

        // Doc disposed, but schema still accessible because it was cloned.
        f.IsSchema.Should().BeTrue();
        f.AsSchema().GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public void FromSchema_String_Overload_Parses()
    {
        OllamaFormat f = OllamaFormat.FromSchema("""{"type":"object","properties":{"x":{"type":"integer"}}}""");
        f.IsSchema.Should().BeTrue();
        f.AsSchema().GetProperty("properties").GetProperty("x").GetProperty("type").GetString().Should().Be("integer");
    }

    [Fact]
    public void ImplicitConversion_FromString_Works()
    {
        OllamaFormat f = "json";
        f.Should().Be(OllamaFormat.Json);
    }

    [Fact]
    public void ImplicitConversion_FromJsonElement_Works()
    {
        using JsonDocument doc = JsonDocument.Parse("""{"type":"object"}""");
        OllamaFormat f = doc.RootElement;
        f.IsSchema.Should().BeTrue();
    }

    [Fact]
    public void Serialize_ModeString_WritesJsonString()
    {
        string json = JsonSerializer.Serialize(OllamaFormat.Json, SerOptions);
        json.Should().Be("\"json\"");
    }

    [Fact]
    public void Serialize_Schema_WritesInlineObject()
    {
        using JsonDocument schema = JsonDocument.Parse("""{"type":"object","required":["x"]}""");
        OllamaFormat f = OllamaFormat.FromSchema(schema.RootElement);

        string json = JsonSerializer.Serialize(f, SerOptions);

        // JsonElement.WriteTo preserves structure but not whitespace — compare as JSON tokens.
        using JsonDocument round = JsonDocument.Parse(json);
        round.RootElement.GetProperty("type").GetString().Should().Be("object");
        round.RootElement.GetProperty("required")[0].GetString().Should().Be("x");
    }

    [Fact]
    public void Deserialize_String_ReturnsMode()
    {
        OllamaFormat f = JsonSerializer.Deserialize<OllamaFormat>("\"json\"", SerOptions);
        f.Should().Be(OllamaFormat.Json);
    }

    [Fact]
    public void Deserialize_Object_ReturnsSchema()
    {
        OllamaFormat f = JsonSerializer.Deserialize<OllamaFormat>("""{"type":"object"}""", SerOptions);
        f.IsSchema.Should().BeTrue();
        f.AsSchema().GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public void Deserialize_Invalid_Throws()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<OllamaFormat>("123", SerOptions));
    }

    [Fact]
    public void Equals_TreatsIdenticalSchemasAsEqual()
    {
        OllamaFormat a = OllamaFormat.FromSchema("""{"type":"object"}""");
        OllamaFormat b = OllamaFormat.FromSchema("""{"type":"object"}""");
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DistinguishesSchemaAndMode()
    {
        OllamaFormat schema = OllamaFormat.FromSchema("""{"type":"object"}""");
        OllamaFormat mode = OllamaFormat.Json;
        (schema == mode).Should().BeFalse();
    }

    [Fact]
    public void GenerateRequest_WithFormat_SerialisesAsTopLevelField()
    {
        var req = new GenerateRequest("llama3", "hi", Format: OllamaFormat.Json);
        string json = JsonSerializer.Serialize(req, OllamaJsonContext.Default.GenerateRequest);

        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("format").GetString().Should().Be("json");
    }

    [Fact]
    public void GenerateRequest_WithSchemaFormat_SerialisesAsObject()
    {
        OllamaFormat schema = OllamaFormat.FromSchema("""{"type":"object","required":["age"]}""");
        var req = new GenerateRequest("llama3", "hi", Format: schema);
        string json = JsonSerializer.Serialize(req, OllamaJsonContext.Default.GenerateRequest);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement f = doc.RootElement.GetProperty("format");
        f.ValueKind.Should().Be(JsonValueKind.Object);
        f.GetProperty("required")[0].GetString().Should().Be("age");
    }

    [Fact]
    public void GenerateRequest_WithoutFormat_OmitsField()
    {
        var req = new GenerateRequest("llama3", "hi");
        string json = JsonSerializer.Serialize(req, OllamaJsonContext.Default.GenerateRequest);

        using JsonDocument doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("format", out _).Should().BeFalse();
    }
}
