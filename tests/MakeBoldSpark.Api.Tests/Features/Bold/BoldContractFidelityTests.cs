using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakeBoldSpark.Api.Tests.Infrastructure;

namespace MakeBoldSpark.Api.Tests.Features.Bold;

/// <summary>
/// spec.md AC1 (contract fidelity): every path/operation in the committed contract snapshot
/// (except the deferred /embeddings execution) must exist in the app's generated OpenAPI document
/// for the Bold route group, at the mapped internal path (spec.md alignment decision 2:
/// /v1/* contract paths mount at /api/integrations/bold/v1/*), with the same HTTP methods and
/// declared response status codes — no breaking drift.
/// </summary>
[TestClass]
public class BoldContractFidelityTests
{
    private const string InternalPrefix = "/api/integrations/bold/v1";
    private static MakeBoldSparkWebApplicationFactory _factory = null!;
    private static JsonElement _contract;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _factory = new MakeBoldSparkWebApplicationFactory();
        await _factory.InitializeAsync();

        var contractPath = FindContractSnapshotPath();
        var contractJson = await File.ReadAllTextAsync(contractPath);
        _contract = JsonDocument.Parse(contractJson).RootElement.Clone();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _factory.DisposeAsync();
    }

    private static string FindContractSnapshotPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MakeBoldSpark.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
            throw new FileNotFoundException("Could not locate repository root to find the Bold contract snapshot.");

        var candidate = Path.Combine(dir.FullName, "bold-docs", "features", "0005-bold-api", "contract", "bold-api-openapi.json");
        if (!File.Exists(candidate))
            throw new FileNotFoundException($"Bold contract snapshot not found at {candidate}");

        return candidate;
    }

    [TestMethod]
    public async Task GeneratedOpenApiDocument_CoversEveryContractPathAndOperation()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var generated = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var generatedPaths = generated.GetProperty("paths");

        var missing = new List<string>();

        foreach (var pathProperty in _contract.GetProperty("paths").EnumerateObject())
        {
            var contractPath = pathProperty.Name; // e.g. "/health", "/runs/{runId}"
            var internalPath = InternalPrefix + contractPath;

            foreach (var operationProperty in pathProperty.Value.EnumerateObject())
            {
                var method = operationProperty.Name; // "get" | "post"
                var isEmbeddingsExecution = contractPath == "/embeddings"; // deferred execution (Out of scope)

                if (!generatedPaths.TryGetProperty(internalPath, out var generatedPathItem))
                {
                    missing.Add($"{method.ToUpperInvariant()} {internalPath} (contract: {contractPath}) — path missing from generated document");
                    continue;
                }

                if (!generatedPathItem.TryGetProperty(method, out var generatedOperation))
                {
                    missing.Add($"{method.ToUpperInvariant()} {internalPath} — operation missing from generated document");
                    continue;
                }

                if (isEmbeddingsExecution)
                {
                    // Contract-specified for Phase 4; this feature only guarantees the endpoint
                    // exists and is documented, not that its request/response shapes match the
                    // eventual real implementation (spec.md Out of scope).
                    continue;
                }

                // Every contract response status code must be represented in the generated doc.
                if (operationProperty.Value.TryGetProperty("responses", out var contractResponses) &&
                    generatedOperation.TryGetProperty("responses", out var generatedResponses))
                {
                    foreach (var statusProperty in contractResponses.EnumerateObject())
                    {
                        if (!generatedResponses.TryGetProperty(statusProperty.Name, out _))
                            missing.Add($"{method.ToUpperInvariant()} {internalPath} — response {statusProperty.Name} missing from generated document");
                    }
                }
            }
        }

        Assert.IsTrue(missing.Count == 0, "Contract fidelity gaps:\n" + string.Join("\n", missing));
    }

    [TestMethod]
    public async Task GeneratedOpenApiDocument_Health_IsAnonymousInBothDocuments()
    {
        var (client, _, _) = await _factory.CreateBoldClientAsync();
        var response = await client.GetAsync("/openapi/v1.json");
        var generated = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        var contractHealthSecurity = _contract.GetProperty("paths").GetProperty("/health").GetProperty("get").GetProperty("security");
        Assert.AreEqual(0, contractHealthSecurity.GetArrayLength(), "Contract declares /health as anonymous (empty security array).");

        // The generated document's per-operation security requirement is asserted indirectly via
        // BoldAuthTests.Health_WithoutToken_ReturnsOk — this test only pins the contract's own
        // shape so a future contract edit that silently adds auth to /health is caught here too.
        Assert.IsTrue(generated.GetProperty("paths").TryGetProperty($"{InternalPrefix}/health", out _));
    }
    [TestMethod]
    public async Task GeneratedSchemas_CoverRequestAndResponseFieldShapes()
    {
        var client = _factory.CreateClient();
        var generated = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        foreach (var path in _contract.GetProperty("paths").EnumerateObject())
        {
            if (path.Name == "/embeddings") continue; // Explicitly deferred execution.
            foreach (var method in path.Value.EnumerateObject())
            {
                var operation = generated.GetProperty("paths").GetProperty(InternalPrefix + path.Name).GetProperty(method.Name);
                if (method.Value.TryGetProperty("requestBody", out var body))
                {
                    Assert.IsTrue(operation.TryGetProperty("requestBody", out var actualBody), path.Name + " requestBody missing");
                    CompareShape(body.GetProperty("content").GetProperty("application/json").GetProperty("schema"),
                        actualBody.GetProperty("content").GetProperty("application/json").GetProperty("schema"), generated, path.Name + " request");
                }
                foreach (var response in method.Value.GetProperty("responses").EnumerateObject())
                {
                    var expected = Resolve(response.Value, _contract);
                    if (!expected.TryGetProperty("content", out var content)) continue;
                    var actual = operation.GetProperty("responses").GetProperty(response.Name);
                    Assert.IsTrue(actual.TryGetProperty("content", out var actualContent), path.Name + " response schema missing");
                    CompareShape(content.GetProperty("application/json").GetProperty("schema"),
                        actualContent.GetProperty("application/json").GetProperty("schema"), generated, path.Name + " " + response.Name);
                }
            }
        }
    }

    private static JsonElement Resolve(JsonElement schema, JsonElement document)
    {
        while (schema.TryGetProperty("$ref", out var reference))
        {
            schema = document;
            foreach (var segment in reference.GetString()![2..].Split('/')) schema = schema.GetProperty(segment);
        }
        foreach (var composition in new[] { "anyOf", "oneOf", "allOf" })
        {
            if (schema.TryGetProperty(composition, out var choices))
            {
                var nonNull = choices.EnumerateArray().Where(c => !c.TryGetProperty("type", out var t) || t.ValueKind != JsonValueKind.String || t.GetString() != "null").ToArray();
                if (nonNull.Length == 1) return Resolve(nonNull[0], document);
            }
        }
        return schema;
    }

    private static void CompareShape(JsonElement expected, JsonElement actual, JsonElement generated, string location)
    {
        expected = Resolve(expected, _contract);
        actual = Resolve(actual, generated);
        // JsonElement intentionally represents an arbitrary response_schema, not a fixed DTO.
        if (location.EndsWith(".response_schema")) return;
        if (expected.TryGetProperty("type", out var expectedType))
        {
            var wanted = expectedType.ValueKind == JsonValueKind.Array
                ? expectedType.EnumerateArray().Select(t => t.GetString()).Where(t => t != "null").ToArray()
                : new[] { expectedType.GetString() };
            Assert.IsTrue(actual.TryGetProperty("type", out var actualType), location + " type missing: " + actual.GetRawText());
            var types = actualType.ValueKind == JsonValueKind.Array
                ? actualType.EnumerateArray().Select(t => t.GetString()).ToArray()
                : new[] { actualType.GetString() };
            Assert.IsTrue(wanted.All(types.Contains), location + " type changed");
        }
        if (expected.TryGetProperty("properties", out var properties))
        {
            Assert.IsTrue(actual.TryGetProperty("properties", out var actualProperties), location + " properties missing");
            CollectionAssert.AreEquivalent(properties.EnumerateObject().Select(p => p.Name).ToArray(),
                actualProperties.EnumerateObject().Select(p => p.Name).ToArray(), location + " field names changed");
            foreach (var property in properties.EnumerateObject())
                CompareShape(property.Value, actualProperties.GetProperty(property.Name), generated, location + "." + property.Name);
        }
        if (expected.TryGetProperty("items", out var items))
            CompareShape(items, actual.GetProperty("items"), generated, location + "[]");
        if (expected.TryGetProperty("format", out var format))
            Assert.AreEqual(format.GetString(), actual.GetProperty("format").GetString(), location);
    }

}
