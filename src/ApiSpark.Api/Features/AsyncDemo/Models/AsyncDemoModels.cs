using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ApiSpark.Api.Features.AsyncDemo.Models;

/// <summary>Result payload returned by the remote mock simulation endpoint.</summary>
public class MockResults
{
    /// <summary>Number of loop iterations requested.</summary>
    [Range(1, 10000)]
    [DefaultValue(100)]
    [Description("Number of loop iterations requested.")]
    public int LoopCount { get; set; }
    /// <summary>Maximum allowed run time in milliseconds before a 408 timeout result is generated.</summary>
    [Range(1, 120000)]
    [DefaultValue(2000)]
    [Description("Maximum allowed run time in milliseconds before a simulated 408 timeout result is generated.")]
    public int MaxTimeMS { get; set; }
    /// <summary>Actual elapsed run time in milliseconds; null if not completed.</summary>
    [Description("Actual elapsed run time in milliseconds; null if the operation did not complete.")]
    public long? RunTimeMS { get; set; }
    /// <summary>Human-readable status message from the mock operation.</summary>
    [DefaultValue("Completed successfully")]
    [Description("Human-readable status message from the mock operation.")]
    public string? Message { get; set; }
    /// <summary>"200" on success or "408" when the simulated timeout was exceeded.</summary>
    [DefaultValue("200")]
    [Description("\"200\" on success or \"408\" when the simulated timeout is exceeded.")]
    public string? ResultValue { get; set; }
}

/// <summary>Current weather conditions fetched from the OpenWeatherMap API.</summary>
public class CurrentWeather
{
    /// <summary>Indicates whether the weather fetch succeeded.</summary>
    [DefaultValue(true)]
    [Description("Indicates whether the weather fetch succeeded.")]
    public bool Success { get; set; }
    /// <summary>Error message when <see cref="Success"/> is false; otherwise null.</summary>
    [Description("Error message when Success is false; otherwise null.")]
    public string? ErrorMessage { get; set; }
    /// <summary>Local time at which this payload was fetched.</summary>
    [Description("Local time at which this payload was fetched.")]
    public DateTime FetchTime { get; set; }
    /// <summary>Local time of the weather observation reported by the station.</summary>
    [Description("Local time of the weather observation reported by the station.")]
    public DateTime ObservationTime { get; set; }
    /// <summary>UTC time of the weather observation.</summary>
    [Description("UTC time of the weather observation.")]
    public DateTime ObservationTimeUtc { get; set; }
    /// <summary>Age of the observation relative to fetch time.</summary>
    [Description("Age of the observation relative to fetch time.")]
    public TimeSpan ObservationAge => DateTime.Now - FetchTime;
    /// <summary>Geographic location details for the queried city.</summary>
    [Description("Geographic location details for the queried city.")]
    public LocationData? Location { get; set; }
    /// <summary>Current atmospheric conditions at the location.</summary>
    [Description("Current atmospheric conditions at the location.")]
    public WeatherData? CurrentConditions { get; set; }

    /// <summary>Geographic coordinates and display name for the observed location.</summary>
    public class LocationData
    {
        /// <summary>City or station name returned by the weather provider.</summary>
        [DefaultValue("Dallas")]
        [Description("City or station name returned by the weather provider.")]
        public string? Name { get; set; }
        /// <summary>Latitude in decimal degrees.</summary>
        [Range(-90, 90)]
        [Description("Latitude in decimal degrees.")]
        public double Latitude { get; set; }
        /// <summary>Longitude in decimal degrees.</summary>
        [Range(-180, 180)]
        [Description("Longitude in decimal degrees.")]
        public double Longitude { get; set; }
    }

    /// <summary>Snapshot of atmospheric measurements at the observation time.</summary>
    public class WeatherData
    {
        /// <summary>Temperature in degrees Celsius.</summary>
        [Range(-100, 100)]
        [Description("Temperature in degrees Celsius.")]
        public double Temperature { get; set; }
        /// <summary>Relative humidity as a percentage (0–100).</summary>
        [Range(0, 100)]
        [Description("Relative humidity as a percentage from 0 to 100.")]
        public double Humidity { get; set; }
        /// <summary>Atmospheric pressure in hPa.</summary>
        [Range(800, 1200)]
        [Description("Atmospheric pressure in hPa.")]
        public double Pressure { get; set; }
        /// <summary>Wind speed in metres per second.</summary>
        [Range(0, 150)]
        [Description("Wind speed in metres per second.")]
        public double WindSpeed { get; set; }
        /// <summary>Cardinal wind direction (e.g. "NNE").</summary>
        [DefaultValue("NNE")]
        [Description("Cardinal wind direction, such as N, SW, or NNE.")]
        public string? WindDirection { get; set; }
        /// <summary>Wind direction in degrees clockwise from true north.</summary>
        [Range(0, 360)]
        [Description("Wind direction in degrees clockwise from true north.")]
        public double WindDirectionDegrees { get; set; }
        /// <summary>Short conditions label, e.g. "Clouds".</summary>
        [DefaultValue("Clouds")]
        [Description("Short conditions label, such as Clouds or Clear.")]
        public string? Conditions { get; set; }
        /// <summary>Longer conditions description, e.g. "overcast clouds".</summary>
        [DefaultValue("overcast clouds")]
        [Description("Longer weather conditions description.")]
        public string? ConditionsDescription { get; set; }
        /// <summary>Cloud cover as a percentage (0–100).</summary>
        [Range(0, 100)]
        [Description("Cloud cover as a percentage from 0 to 100.")]
        public double CloudCover { get; set; }
        /// <summary>Visibility in kilometres.</summary>
        [Range(0, 100)]
        [Description("Visibility in kilometres.")]
        public double Visibility { get; set; }
        /// <summary>Rainfall in the last hour in millimetres.</summary>
        [Range(0, 500)]
        [Description("Rainfall in the last hour in millimetres.")]
        public double RainfallOneHour { get; set; }
    }
}

/// <summary>Machine-readable application status snapshot, cached for 24 hours.</summary>
public sealed class ApplicationStatus
{
    /// <summary>UTC date on which the running assembly was built.</summary>
    [Description("UTC date on which the running assembly was built.")]
    public DateTime BuildDate { get; init; }
    /// <summary>Structured version numbers parsed from the executing assembly.</summary>
    [Description("Structured version numbers parsed from the executing assembly.")]
    public BuildVersion? BuildVersion { get; init; }
    /// <summary>Named feature flags active in this deployment.</summary>
    [Description("Named feature flags active in this deployment.")]
    public Dictionary<string, string> Features { get; init; } = [];
    /// <summary>Informational messages about the current deployment.</summary>
    [Description("Informational messages about the current deployment.")]
    public List<string> Messages { get; init; } = [];
    /// <summary>Deployment region identifier (e.g. "eastus" or "local").</summary>
    [DefaultValue("local")]
    [Description("Deployment region identifier, such as eastus or local.")]
    public string? Region { get; init; }
    /// <summary>Aggregate service status.</summary>
    [DefaultValue(ServiceStatus.Online)]
    [Description("Aggregate service status.")]
    public ServiceStatus Status { get; init; }
}

/// <summary>Structured representation of an assembly version number.</summary>
public sealed class BuildVersion
{
    /// <summary>Major version component.</summary>
    [Range(0, 999)]
    [Description("Major version component.")]
    public int MajorVersion { get; set; }
    /// <summary>Minor version component.</summary>
    [Range(0, 999)]
    [Description("Minor version component.")]
    public int MinorVersion { get; set; }
    /// <summary>Build number component.</summary>
    [Range(0, int.MaxValue)]
    [Description("Build number component.")]
    public int Build { get; set; }
    [Range(0, int.MaxValue)]
    [Description("Revision number component.")]
    public int Revision { get; set; }
    public override string ToString() => $"{MajorVersion}.{MinorVersion}.{Build}.{Revision}";
}

public enum ServiceStatus { Degraded, Offline, Online }

/// <summary>Result for cancellation-pattern endpoints that execute a single loop.</summary>
public sealed class CancellationLoopResponse
{
    [Description("Numeric result returned by the CPU-bound loop simulation.")]
    public decimal Result { get; set; }
    [Range(1, 10000)]
    [DefaultValue(100)]
    [Description("Number of iterations requested.")]
    public int Iterations { get; set; }
    [Description("Total elapsed server-side time in milliseconds.")]
    public long ElapsedMilliseconds { get; set; }
    [Description("Whether the operation observed a cancellation token.")]
    public bool Cancellable { get; set; }
}

/// <summary>Result for the cancellation cleanup example.</summary>
public sealed class CancellationCleanupResponse
{
    [Description("Numeric result returned by the CPU-bound loop simulation.")]
    public decimal Result { get; set; }
    [Description("Total elapsed server-side time in milliseconds.")]
    public long ElapsedMilliseconds { get; set; }
    [DefaultValue(true)]
    [Description("Indicates whether the finally-block cleanup path ran.")]
    public bool CleanupRan { get; set; }
    [Description("Time spent in cleanup in milliseconds.")]
    public long CleanupElapsedMilliseconds { get; set; }
}

/// <summary>One operation result inside a concurrency-pattern response.</summary>
public sealed class ConcurrencyOperationResult
{
    [Range(1, 50)]
    [Description("One-based operation identifier.")]
    public int OperationId { get; set; }
    [Description("Elapsed time for the operation in milliseconds.")]
    public long ElapsedMilliseconds { get; set; }
    [Description("Numeric result returned by the CPU-bound loop simulation.")]
    public decimal Result { get; set; }
    [Description("Execution wave assigned by the throttled concurrency example.")]
    public int? Wave { get; set; }
}

/// <summary>Response for sequential and throttled concurrency examples.</summary>
public class ConcurrencyRunResponse
{
    [DefaultValue("Sequential")]
    [Description("Execution strategy used for this request.")]
    public string ExecutionMode { get; set; } = string.Empty;
    [Range(1, 50)]
    [Description("Number of operations requested.")]
    public int TotalOperations { get; set; }
    [Description("Total elapsed server-side time in milliseconds.")]
    public long TotalElapsedMilliseconds { get; set; }
    [Description("Average elapsed time per operation in milliseconds.")]
    public long? AverageMillisecondsPerOperation { get; set; }
    [Description("Maximum number of operations allowed to run simultaneously.")]
    public int? MaxConcurrency { get; set; }
    [Description("Per-operation results.")]
    public List<ConcurrencyOperationResult> Results { get; set; } = [];
}

/// <summary>Response for unbounded parallel concurrency examples.</summary>
public sealed class ParallelConcurrencyResponse : ConcurrencyRunResponse
{
    [Description("Estimated speedup compared to sequential execution.")]
    public double SpeedupFactor { get; set; }
}

/// <summary>Elapsed-time summary for one branch in the concurrency comparison endpoint.</summary>
public sealed class ConcurrencyTimingSummary
{
    [Description("Total elapsed server-side time in milliseconds.")]
    public long TotalElapsedMilliseconds { get; set; }
    [Description("Speedup compared to the sequential branch.")]
    public double? SpeedupVsSequential { get; set; }
}

/// <summary>Response comparing sequential, parallel, and throttled concurrency.</summary>
public sealed class ConcurrencyComparisonResponse
{
    [Range(1, 50)]
    [DefaultValue(5)]
    [Description("Number of operations executed in each comparison branch.")]
    public int OperationCount { get; set; }
    [Range(1, 10000)]
    [DefaultValue(50)]
    [Description("Number of CPU-bound loop iterations per operation.")]
    public int IterationsPerOperation { get; set; }
    [Range(1, 20)]
    [DefaultValue(2)]
    [Description("Maximum concurrency used by the throttled branch.")]
    public int MaxConcurrency { get; set; }
    [Description("Sequential execution timing.")]
    public ConcurrencyTimingSummary Sequential { get; set; } = new();
    [Description("Unbounded parallel execution timing.")]
    public ConcurrencyTimingSummary Parallel { get; set; } = new();
    [Description("SemaphoreSlim-throttled execution timing.")]
    public ConcurrencyTimingSummary Throttled { get; set; } = new();
}

/// <summary>Retry diagnostics captured by the Polly retry example.</summary>
public sealed class WeatherRetryInfo
{
    [Range(0, 10)]
    [Description("Number of attempts used by the request.")]
    public int AttemptsUsed { get; set; }
    [Range(0, 10)]
    [DefaultValue(3)]
    [Description("Maximum retry attempts configured for the request.")]
    public int MaxRetries { get; set; }
    [Description("Retry delay values in milliseconds.")]
    public List<double> RetryDelays { get; set; } = [];
}

/// <summary>Response for the weather retry endpoint.</summary>
public sealed class WeatherRetryResponse
{
    [Description("Weather payload returned after retry processing.")]
    public CurrentWeather? Weather { get; set; }
    [Description("Retry diagnostics captured by the Polly policy.")]
    public WeatherRetryInfo RetryInfo { get; set; } = new();
}

/// <summary>Summary for a multi-city weather request.</summary>
public sealed class MultiWeatherSummary
{
    [Range(1, 10)]
    [Description("Number of requested cities.")]
    public int TotalCities { get; set; }
    [Description("Number of weather requests that succeeded.")]
    public int SuccessCount { get; set; }
    [Description("Number of weather requests that failed.")]
    public int FailureCount { get; set; }
    [Description("Total elapsed server-side time in milliseconds.")]
    public long TotalElapsedMilliseconds { get; set; }
}

/// <summary>Response for parallel multi-city weather requests.</summary>
public sealed class MultiWeatherResponse
{
    [Description("Aggregate multi-city request summary.")]
    public MultiWeatherSummary Summary { get; set; } = new();
    [Description("Weather payload for each requested city.")]
    public CurrentWeather[] Results { get; set; } = [];
}

/// <summary>Parsed configuration values returned by the async demo appsettings endpoint.</summary>
public sealed class AsyncAppSettingsResponse
{
    [Description("Parsed integer list from Async:TestIds.")]
    public List<int> TestIds { get; set; } = [];
    [DefaultValue(0)]
    [Description("Parsed integer value from Async:TestId.")]
    public int TestId { get; set; }
    [Description("Parsed string list from Async:TestNames.")]
    public List<string> TestNames { get; set; } = [];
    [DefaultValue("local")]
    [Description("String value from Async:TestName.")]
    public string TestName { get; set; } = string.Empty;
}
