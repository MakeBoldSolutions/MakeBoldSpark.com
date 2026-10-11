using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MakeBoldSpark.Api.Features.ArrowSparkReactions;

/// <summary>Outcome of validating a request body. <see cref="Property"/> names only a contract property (API-32).</summary>
public sealed record ArrowSparkValidationResult(string? Property, string? ReactionType, byte[]? CanonicalPayload)
{
    public bool IsValid => Property is null;
}

/// <summary>
/// Strict schema validation for the ArrowSpark reaction contract (schemaVersion 1, API-10..API-19).
/// Format and range only; the canonical payload contains contract properties in contract order.
/// </summary>
public static class ArrowSparkReactionValidator
{
    public const int MaxBodyBytes = 4096;

    private enum Kind { Enum, Int, Comment }

    private sealed record Field(string Name, Kind Kind, string[]? Allowed = null, int Max = 0, int Min = 0);

    private static readonly Field[] GameFields =
    [
        new("readStoryFirst", Kind.Enum, ["yes", "no"]),
        new("finished", Kind.Enum, ["finished", "partway", "notStarted"]),
        new("satisfaction", Kind.Int, Min: 1, Max: 5),
        new("playAnother", Kind.Enum, ["yes", "maybe", "no"]),
        new("comment", Kind.Comment, Max: 1000),
    ];

    private static readonly Field[] StoryFields =
    [
        new("madeSense", Kind.Enum, ["yes", "partly", "no"]),
        new("changedView", Kind.Enum, ["moreInterested", "noChange", "lessInterested"]),
        new("wouldUse", Kind.Enum, ["yes", "maybe", "no"]),
        new("comment", Kind.Comment, Max: 1000),
    ];

    private static readonly Field[] AttemptFields =
    [
        new("puzzleId", Kind.Comment, Max: 64),
        new("puzzleVersion", Kind.Comment, Max: 64),
        new("mistakes", Kind.Int, Max: 10_000),
        new("openMoveAssists", Kind.Int, Max: 10_000),
        new("score", Kind.Int, Max: 10_000),
        new("elapsedSeconds", Kind.Int, Max: 86_400),
    ];

    private const string BodyProperty = "body";

    public static ArrowSparkValidationResult Validate(ReadOnlyMemory<byte> body)
    {
        try
        {
            using var document = JsonDocument.Parse(body, new JsonDocumentOptions { AllowDuplicateProperties = false });
            return ValidateRoot(document.RootElement);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            // Malformed JSON, invalid UTF-8, duplicate property, or a string that cannot be decoded.
            return Fail(BodyProperty);
        }
    }

    private static ArrowSparkValidationResult ValidateRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return Fail(BodyProperty);

        string? reactionType = null;
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name != "reactionType") continue;
            if (property.Value.ValueKind == JsonValueKind.String)
                reactionType = property.Value.GetString();
        }
        if (reactionType is not ("game" or "story")) return Fail("reactionType");

        var isGame = reactionType == "game";
        var fields = isGame ? GameFields : StoryFields;

        foreach (var property in root.EnumerateObject())
        {
            var known = property.Name == "reactionType" || fields.Any(f => f.Name == property.Name)
                || (isGame && property.Name == "attempt");
            if (known) continue;
            if (property.Name == "attempt") return Fail("attempt"); // not allowed on a story (API-16)
            return Fail(BodyProperty); // Never echo a visitor-supplied property name.
        }

        var buffer = new ArrayBufferWriter<byte>(512);
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteString("reactionType", reactionType);
            foreach (var field in fields)
            {
                if (!root.TryGetProperty(field.Name, out var value)) continue;
                if (!WriteField(writer, field, value, field.Name)) return Fail(field.Name);
            }
            if (isGame && root.TryGetProperty("attempt", out var attempt))
            {
                var failed = WriteAttempt(writer, attempt);
                if (failed is not null) return Fail(failed);
            }
            writer.WriteEndObject();
        }
        return new ArrowSparkValidationResult(null, reactionType, buffer.WrittenSpan.ToArray());
    }

    private static string? WriteAttempt(Utf8JsonWriter writer, JsonElement attempt)
    {
        if (attempt.ValueKind != JsonValueKind.Object) return "attempt";
        foreach (var property in attempt.EnumerateObject())
            if (!AttemptFields.Any(f => f.Name == property.Name)) return "attempt";

        writer.WriteStartObject("attempt");
        foreach (var field in AttemptFields)
        {
            if (!attempt.TryGetProperty(field.Name, out var value)) return "attempt." + field.Name;
            if (!WriteField(writer, field, value, field.Name, pattern: field.Kind == Kind.Comment))
                return "attempt." + field.Name;
        }
        writer.WriteEndObject();
        return null;
    }

    private static bool WriteField(Utf8JsonWriter writer, Field field, JsonElement value, string name, bool pattern = false)
    {
        switch (field.Kind)
        {
            case Kind.Enum:
            {
                if (value.ValueKind != JsonValueKind.String) return false;
                var text = value.GetString();
                if (text is null || Array.IndexOf(field.Allowed!, text) < 0) return false;
                writer.WriteString(name, text);
                return true;
            }
            case Kind.Int:
            {
                if (value.ValueKind != JsonValueKind.Number) return false;
                var raw = value.GetRawText();
                // Whole numbers only: no fraction, exponent or sign form (API-18).
                if (raw.Length == 0 || !raw.All(char.IsAsciiDigit)) return false;
                if (!int.TryParse(raw, out var number) || number < field.Min || number > field.Max) return false;
                writer.WriteNumber(name, number);
                return true;
            }
            default:
            {
                if (value.ValueKind != JsonValueKind.String) return false;
                var text = value.GetString();
                // UTF-16 code units, as JavaScript's String.length (API-19).
                if (text is null || text.Length < 1 || text.Length > field.Max) return false;
                if (pattern && !text.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-')) return false;
                writer.WriteString(name, text);
                return true;
            }
        }
    }

    private static ArrowSparkValidationResult Fail(string property) => new(property, null, null);
}
