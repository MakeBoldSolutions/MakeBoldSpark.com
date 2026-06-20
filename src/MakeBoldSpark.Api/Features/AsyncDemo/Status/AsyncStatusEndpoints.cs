using System.Reflection;
using MakeBoldSpark.Api.Features.AsyncDemo.Models;
using MakeBoldSpark.Api.Infrastructure.OpenApi;
using Microsoft.Extensions.Caching.Memory;

namespace MakeBoldSpark.Api.Features.AsyncDemo.Status;

public static class AsyncStatusEndpoints
{
    public static RouteGroupBuilder MapAsyncStatusApi(this RouteGroupBuilder group)
    {
        group.MapGet("/", (IMemoryCache cache, IConfiguration configuration) =>
        {
            const string cacheKey = "ApplicationStatus";
            if (cache.TryGetValue(cacheKey, out ApplicationStatus? cached) && cached is not null)
                return Results.Ok(cached);

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var buildDateAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            var buildVersion = version is not null
                ? new BuildVersion
                {
                    MajorVersion = version.Major,
                    MinorVersion = version.Minor,
                    Build = version.Build,
                    Revision = version.Revision
                }
                : new BuildVersion { MajorVersion = 1, MinorVersion = 0, Build = 0, Revision = 0 };

            var region = configuration["Region"]
                      ?? configuration["WEBSITE_SITE_NAME"]
                      ?? "local";

            var status = new ApplicationStatus
            {
                BuildDate = DateTime.UtcNow.Date,
                BuildVersion = buildVersion,
                Features = [],
                Messages = [],
                Region = region,
                Status = ServiceStatus.Online
            };

            cache.Set(cacheKey, status, TimeSpan.FromHours(24));
            return Results.Ok(status);
        })
        .WithName("GetAsyncDemoStatus")
        .WithTags(MakeBoldSparkOpenApiTags.AsyncMonitoringHealth)
        .WithSummary("Returns application status from assembly metadata (cached 24h)")
        .WithDescription("Builds an ApplicationStatus payload from the running assembly version and region configuration, then caches it for 24 hours. Useful as a machine-readable build manifest endpoint.")
        .Produces<ApplicationStatus>(200)
        .AllowAnonymous();

        group.MapGet("/appsettings", (IConfiguration configuration) =>
        {
            var testIds = ParseInts(configuration["Async:TestIds"]);
            var testId = int.TryParse(configuration["Async:TestId"], out var id) ? id : 0;
            var testNames = ParseStrings(configuration["Async:TestNames"]);
            var testName = configuration["Async:TestName"] ?? string.Empty;

            return Results.Ok(new
            {
                testIds,
                testId,
                testNames,
                testName
            });
        })
        .WithName("GetAsyncDemoAppSettings")
        .WithTags(MakeBoldSparkOpenApiTags.AsyncMonitoringHealth)
        .WithSummary("Tests IConfiguration CSV parsing helpers")
        .WithDescription("Reads Async:TestIds, Async:TestId, Async:TestNames, and Async:TestName from IConfiguration and returns parsed typed values. Useful for verifying configuration injection and CSV-to-list helpers in a live environment.")
        .Produces<AsyncAppSettingsResponse>(200)
        .AllowAnonymous();

        return group;
    }

    private static List<int> ParseInts(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToList();
    }

    private static List<string> ParseStrings(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
