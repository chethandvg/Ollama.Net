using System.Text.Json;
using System.Text.Json.Serialization;
using Ollama.Net.Internal.Json;

namespace Ollama.Net.Models.Common;

/// <summary>
/// Represents the value of the Ollama <c>format</c> field on <c>/api/generate</c> and
/// <c>/api/chat</c>, which may be either a short mode string (typically <c>"json"</c>)
/// or a full JSON-schema object that constrains the model's output.
/// </summary>
/// <remarks>
/// <para>
/// Ollama documents two shapes for this field:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <b>JSON mode</b> — pass the literal string <c>"json"</c>. The model is asked to
/// produce any valid JSON object.
/// </description></item>
/// <item><description>
/// <b>Structured outputs</b> — pass a JSON-schema object. The model is constrained
/// to produce output matching that schema.
/// </description></item>
/// </list>
/// <para>
/// This type is a tagged union over those two shapes. Implicit conversions from
/// <see cref="string"/> and <see cref="JsonElement"/> make it convenient at call
/// sites, e.g. <c>new GenerateRequest(..., Format: "json")</c> or
/// <c>new GenerateRequest(..., Format: schema)</c>.
/// </para>
/// </remarks>
[JsonConverter(typeof(OllamaFormatConverter))]
public readonly struct OllamaFormat : IEquatable<OllamaFormat>
{
    private readonly string? _mode;
    private readonly JsonElement _schema;
    private readonly bool _hasSchema;

    private OllamaFormat(string? mode, JsonElement schema, bool hasSchema)
    {
        _mode = mode;
        _schema = schema;
        _hasSchema = hasSchema;
    }

    /// <summary>
    /// Shortcut for the <c>"json"</c> JSON-mode value. The model is asked to
    /// produce a valid JSON object; the schema is not constrained.
    /// </summary>
    public static OllamaFormat Json { get; } = new("json", default, hasSchema: false);

    /// <summary>
    /// Creates a mode-string format (the wire value is serialized as a JSON string).
    /// </summary>
    /// <param name="mode">The mode string, e.g. <c>"json"</c>.</param>
    public static OllamaFormat FromString(string mode)
    {
        ArgumentException.ThrowIfNullOrEmpty(mode);
        return new OllamaFormat(mode, default, hasSchema: false);
    }

    /// <summary>
    /// Creates a structured-output format from a JSON-schema <see cref="JsonElement"/>.
    /// The element is cloned so the caller may dispose any backing
    /// <see cref="JsonDocument"/> safely.
    /// </summary>
    /// <param name="schema">The JSON-schema object (<c>{"type":"object",...}</c>).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="schema"/> is not a JSON object.
    /// </exception>
    public static OllamaFormat FromSchema(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                $"Structured-output schema must be a JSON object, got {schema.ValueKind}.",
                nameof(schema));
        }

        return new OllamaFormat(mode: null, schema.Clone(), hasSchema: true);
    }

    /// <summary>
    /// Creates a structured-output format from a <see cref="JsonDocument"/>.
    /// The document's root element is cloned so the caller may dispose the
    /// document immediately after this call.
    /// </summary>
    /// <param name="schema">A JSON document whose root is the schema object.</param>
    public static OllamaFormat FromSchema(JsonDocument schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return FromSchema(schema.RootElement);
    }

    /// <summary>Parses a JSON-schema string and wraps it as a structured-output format.</summary>
    /// <param name="schemaJson">A JSON string describing a schema object.</param>
    public static OllamaFormat FromSchema(string schemaJson)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaJson);
        using JsonDocument doc = JsonDocument.Parse(schemaJson);
        return FromSchema(doc.RootElement);
    }

    /// <summary><see langword="true"/> when this value holds a mode string (e.g. <c>"json"</c>).</summary>
    public bool IsMode => !_hasSchema;

    /// <summary><see langword="true"/> when this value holds a JSON-schema object.</summary>
    public bool IsSchema => _hasSchema;

    /// <summary>
    /// Returns the mode string, or <see langword="null"/> when <see cref="IsSchema"/> is <see langword="true"/>.
    /// </summary>
    public string? AsMode() => _hasSchema ? null : _mode;

    /// <summary>
    /// Returns the JSON-schema element when <see cref="IsSchema"/> is <see langword="true"/>;
    /// otherwise <see langword="default"/>.
    /// </summary>
    public JsonElement AsSchema() => _hasSchema ? _schema : default;

    /// <summary>
    /// Implicit conversion from a non-null mode string; equivalent to <see cref="FromString"/>.
    /// Throws <see cref="ArgumentException"/> on <see langword="null"/> or empty input — callers
    /// that may have a <see cref="string"/>? value should target <see cref="OllamaFormat"/>?
    /// instead (see the overload below), which maps <see langword="null"/>/empty to
    /// <see langword="null"/> so the field is omitted on the wire.
    /// </summary>
    public static implicit operator OllamaFormat(string mode) => FromString(mode);

    /// <summary>
    /// Null-tolerant implicit conversion for <see cref="string"/>?-typed values, used
    /// whenever the target is <see cref="OllamaFormat"/>?. This preserves the ergonomics
    /// of the legacy <c>string?</c>-based <c>Format</c> API: a <see langword="null"/>
    /// or empty mode string produces a <see langword="null"/> <see cref="OllamaFormat"/>?,
    /// so the <c>format</c> field is omitted on the wire instead of throwing or
    /// emitting an explicit JSON null.
    /// </summary>
    /// <param name="mode">
    /// The mode string, e.g. <c>"json"</c>. <see langword="null"/> and empty are mapped
    /// to <see langword="null"/>.
    /// </param>
    public static implicit operator OllamaFormat?(string? mode) => FromStringOrNull(mode);

    /// <summary>Implicit conversion from a schema object; equivalent to <see cref="FromJsonElement"/>.</summary>
    public static implicit operator OllamaFormat(JsonElement schema) => FromJsonElement(schema);

    /// <summary>
    /// CA2225-compliant named alternate for <c>implicit operator OllamaFormat(JsonElement)</c>.
    /// Equivalent to <see cref="FromSchema(JsonElement)"/>.
    /// </summary>
    /// <param name="schema">The JSON-schema object.</param>
    public static OllamaFormat FromJsonElement(JsonElement schema) => FromSchema(schema);

    /// <summary>
    /// CA2225-compliant named alternate for <c>implicit operator OllamaFormat?(string?)</c>.
    /// Returns <see langword="null"/> for <see langword="null"/> or empty input; otherwise
    /// delegates to <see cref="FromString"/>.
    /// </summary>
    /// <param name="mode">
    /// The mode string, e.g. <c>"json"</c>. <see langword="null"/> and empty yield
    /// <see langword="null"/>.
    /// </param>
    public static OllamaFormat? FromStringOrNull(string? mode)
        => string.IsNullOrEmpty(mode) ? (OllamaFormat?)null : FromString(mode);

    /// <inheritdoc />
    public bool Equals(OllamaFormat other)
    {
        if (_hasSchema != other._hasSchema)
        {
            return false;
        }

        if (_hasSchema)
        {
            // JsonElement has no structural equality; compare serialized forms.
            return _schema.GetRawText() == other._schema.GetRawText();
        }

        return string.Equals(_mode, other._mode, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is OllamaFormat other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _hasSchema
            ? HashCode.Combine(true, _schema.GetRawText())
            : HashCode.Combine(false, _mode);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(OllamaFormat left, OllamaFormat right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(OllamaFormat left, OllamaFormat right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => _hasSchema ? _schema.GetRawText() : _mode ?? string.Empty;
}
