using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FloodAI.Business.Weather;

public sealed class OpenMeteoClient(HttpClient httpClient)
{
    internal const string TimeZone = "Asia/Ho_Chi_Minh";
    private const string CurrentVariables = "temperature_2m,apparent_temperature,relative_humidity_2m," +
        "precipitation,wind_speed_10m,wind_direction_10m,weather_code,is_day";

    internal async Task<OpenMeteoCurrent> ReadCurrentAsync(
        double latitude, double longitude, CancellationToken cancellationToken)
    {
        var location = FormattableString.Invariant($"latitude={latitude:R}&longitude={longitude:R}");
        var path = $"v1/forecast?{location}&current={CurrentVariables}&timezone={Uri.EscapeDataString(TimeZone)}" +
            "&timeformat=unixtime&temperature_unit=celsius&wind_speed_unit=kmh&precipitation_unit=mm";
        try
        {
            var response = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(path, cancellationToken);
            if (response?.Current is not { Time: > 0, Interval: > 0 } current ||
                current.Time > DateTimeOffset.MaxValue.ToUnixTimeSeconds() ||
                current.IsDay is not (null or 0 or 1))
            {
                throw new WeatherException(WeatherError.ProviderUnavailable,
                    "Open-Meteo did not return a usable current weather snapshot.");
            }
            return current;
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WeatherException(WeatherError.ProviderUnavailable, "The weather provider timed out.", error);
        }
        catch (Exception error) when (error is HttpRequestException or JsonException or NotSupportedException or IOException)
        {
            throw new WeatherException(WeatherError.ProviderUnavailable, "Open-Meteo weather is temporarily unavailable.", error);
        }
    }

    private sealed record OpenMeteoResponse(OpenMeteoCurrent? Current);

    internal sealed record OpenMeteoCurrent(
        long Time,
        int Interval,
        [property: JsonPropertyName("temperature_2m")] double? Temperature,
        [property: JsonPropertyName("apparent_temperature")] double? ApparentTemperature,
        [property: JsonPropertyName("relative_humidity_2m")] double? RelativeHumidity,
        double? Precipitation,
        [property: JsonPropertyName("wind_speed_10m")] double? WindSpeed,
        [property: JsonPropertyName("wind_direction_10m")] double? WindDirection,
        [property: JsonPropertyName("weather_code")] int? WeatherCode,
        [property: JsonPropertyName("is_day")] int? IsDay);
}
