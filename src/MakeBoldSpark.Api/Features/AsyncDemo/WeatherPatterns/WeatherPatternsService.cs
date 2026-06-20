using System.Text.Json;
using System.Text.Json.Serialization;
using MakeBoldSpark.Api.Features.AsyncDemo.Models;
using Microsoft.Extensions.Caching.Memory;

namespace MakeBoldSpark.Api.Features.AsyncDemo.WeatherPatterns;

public interface IAsyncDemoWeatherService
{
    Task<CurrentWeather> GetCurrentWeatherAsync(string location, CancellationToken ct = default);
}

public class OpenWeatherMapWeatherService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IConfiguration configuration,
    ILogger<OpenWeatherMapWeatherService> logger) : IAsyncDemoWeatherService
{
    private static readonly string[] CompassPoints =
    [
        "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
        "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
    ];

    private static string DegreesToCompass(double degrees)
    {
        var index = (int)Math.Round(degrees / 22.5) % 16;
        return CompassPoints[index];
    }

    public async Task<CurrentWeather> GetCurrentWeatherAsync(string location, CancellationToken ct = default)
    {
        var cacheKey = $"weather_{location.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out CurrentWeather? cached) && cached is not null)
            return cached;

        var apiKey = configuration["OpenWeatherMapApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "KEYMISSING")
        {
            logger.LogWarning("OpenWeatherMapApiKey is missing or invalid.");
            return new CurrentWeather { Success = false, ErrorMessage = "Weather API key is not configured." };
        }

        try
        {
            var client = httpClientFactory.CreateClient("weather");
            var url = $"/data/2.5/weather?q={Uri.EscapeDataString(location)}&units=imperial&appid={apiKey}";
            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var owm = JsonSerializer.Deserialize<OwmResponse>(json, OwmSerializerOptions);

            if (owm is null)
                return new CurrentWeather { Success = false, ErrorMessage = "Failed to parse weather response." };

            var fetchTime = DateTime.Now;
            var observationTimeUtc = DateTimeOffset.FromUnixTimeSeconds(owm.Dt).UtcDateTime;

            var result = new CurrentWeather
            {
                Success = true,
                FetchTime = fetchTime,
                ObservationTime = observationTimeUtc.ToLocalTime(),
                ObservationTimeUtc = observationTimeUtc,
                Location = new CurrentWeather.LocationData
                {
                    Name = owm.Name,
                    Latitude = owm.Coord?.Lat ?? 0,
                    Longitude = owm.Coord?.Lon ?? 0
                },
                CurrentConditions = new CurrentWeather.WeatherData
                {
                    Temperature = owm.Main?.Temp ?? 0,
                    Humidity = owm.Main?.Humidity ?? 0,
                    Pressure = owm.Main?.Pressure ?? 0,
                    WindSpeed = owm.Wind?.Speed ?? 0,
                    WindDirectionDegrees = owm.Wind?.Deg ?? 0,
                    WindDirection = DegreesToCompass(owm.Wind?.Deg ?? 0),
                    Conditions = owm.Weather?.FirstOrDefault()?.Main,
                    ConditionsDescription = owm.Weather?.FirstOrDefault()?.Description,
                    CloudCover = owm.Clouds?.All ?? 0,
                    Visibility = owm.Visibility,
                    RainfallOneHour = owm.Rain?.OneHour ?? 0
                }
            };

            cache.Set(cacheKey, result, TimeSpan.FromMinutes(90));
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to fetch weather for {Location}", location);
            return new CurrentWeather { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static readonly JsonSerializerOptions OwmSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // OpenWeatherMap response shape
    private sealed class OwmResponse
    {
        public CoordObj? Coord { get; set; }
        public List<WeatherObj>? Weather { get; set; }
        public MainObj? Main { get; set; }
        public double Visibility { get; set; }
        public WindObj? Wind { get; set; }
        public CloudsObj? Clouds { get; set; }
        public RainObj? Rain { get; set; }
        public long Dt { get; set; }
        public string? Name { get; set; }
    }

    private sealed class CoordObj { public double Lat { get; set; } public double Lon { get; set; } }
    private sealed class WeatherObj { public string? Main { get; set; } public string? Description { get; set; } }
    private sealed class MainObj { public double Temp { get; set; } public double Humidity { get; set; } public double Pressure { get; set; } }
    private sealed class WindObj { public double Speed { get; set; } public double Deg { get; set; } }
    private sealed class CloudsObj { public double All { get; set; } }

    private sealed class RainObj
    {
        [JsonPropertyName("1h")]
        public double OneHour { get; set; }
    }
}
