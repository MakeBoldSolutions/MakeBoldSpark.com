using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace MakeBoldSpark.Api.Features.FamilyMemories;

public class FamilyMemory
{
    public int Id { get; set; }
    public string PersonName { get; set; } = "";
    public string PersonPronouns { get; set; } = "";
    public string RelationshipToJared { get; set; } = "";
    public string Body { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public record FamilyMemoryWrite
{
    [JsonRequired, StringLength(200, MinimumLength = 1)] public string PersonName { get; init; } = "";
    [JsonRequired, StringLength(100)] public string PersonPronouns { get; init; } = "";
    [JsonRequired, StringLength(300, MinimumLength = 1)] public string RelationshipToJared { get; init; } = "";
    [JsonRequired, StringLength(100000, MinimumLength = 1)] public string Body { get; init; } = "";
    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }
    public bool IsPublished { get; init; }

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        Check("personName", PersonName, 200);
        Check("personPronouns", PersonPronouns, 100, allowEmpty: true);
        Check("relationshipToJared", RelationshipToJared, 300);
        Check("body", Body, 100_000);
        if (SortOrder < 0) errors["sortOrder"] = ["Must be an integer from 0 to 2147483647."];
        return errors;

        void Check(string name, string? value, int max, bool allowEmpty = false)
        {
            if (value is null || (!allowEmpty && string.IsNullOrWhiteSpace(value)) || value.Length > max)
                errors[name] = [$"Must be a {(allowEmpty ? "non-null" : "non-whitespace")} string of at most {max} UTF-16 code units."];
        }
    }
}

public record PublishedMemory(
    [property: JsonPropertyName("personName")] string PersonName,
    [property: JsonPropertyName("personPronouns")] string PersonPronouns,
    [property: JsonPropertyName("relationshipToJared")] string RelationshipToJared,
    [property: JsonPropertyName("body")] string Body);
public record PublishedMemoriesData([property: JsonPropertyName("memories")] IReadOnlyList<PublishedMemory> Memories);
public record PublishedMemoriesResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] PublishedMemoriesData Data);
