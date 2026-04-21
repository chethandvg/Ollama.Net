using System.Diagnostics.CodeAnalysis;

// CA1819 — public DTO record types intentionally expose arrays because they model JSON
// payloads from the Ollama API and are serialized directly. Replacing arrays with
// IReadOnlyList<T> would break System.Text.Json source-generated metadata and make
// the API less ergonomic for consumers that receive deserialized data.
[assembly: SuppressMessage(
    "Performance",
    "CA1819:Properties should not return arrays",
    Scope = "namespaceanddescendants",
    Target = "~N:Krutaka.Ollama.Models",
    Justification = "DTO records mirror the Ollama JSON schema; arrays are the natural representation.")]
