using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FloodAI.Business.Weather;

public sealed class OpenMeteoClient(HttpClient httpClient)
{
    internal const string TimeZone = "Asia/Ho_Chi_Minh";
    private const int ForecastDays = 7;
    private const int ForecastHours = ForecastDays * 24;
    internal const int VietnamUtcOffsetSeconds = 7 * 60 * 60;
    private const string CurrentVariables = "temperature_2m,apparent_temperature,relative_humidity_2m," +
        "precipitation,wind_speed_10m,wind_direction_10m,weather_code,is_day";
    private const string HourlyVariables = "temperature_2m,relative_humidity_2m,precipitation," +
        "precipitation_probability,wind_speed_10m,weather_code";
    private const string DailyVariables = "temperature_2m_min,temperature_2m_max,precipitation_sum," +
        "precipitation_probability_max,weather_code";

    internal async Task<OpenMeteoCurrent> ReadCurrentAsync(
        double latitude, double longitude, CancellationToken cancellationToken)
    {
        var response = await ReadAsync<OpenMeteoResponse>(
            latitude, longitude, $"current={CurrentVariables}&timeformat=unixtime", cancellationToken);
        if (response?.Current is not { Time: > 0, Interval: > 0 } current ||
            current.Time > DateTimeOffset.MaxValue.ToUnixTimeSeconds() ||
            current.IsDay is not (null or 0 or 1))
        {
            throw new WeatherException(WeatherError.ProviderUnavailable,
                "Open-Meteo did not return a usable current weather snapshot.");
        }
        return current;
    }

    internal async Task<(IReadOnlyList<HourlyWeatherForecast> Hourly, IReadOnlyList<DailyWeatherForecast> Daily)>
        ReadForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var response = await ReadAsync<OpenMeteoForecast>(
            latitude, longitude, $"hourly={HourlyVariables}&daily={DailyVariables}&timeformat=iso8601&forecast_days={ForecastDays}",
            cancellationToken);
        if (response is not { UtcOffsetSeconds: VietnamUtcOffsetSeconds, Hourly: { } hourly, Daily: { } daily } ||
            !MatchLength(ForecastHours, hourly.Time, hourly.Temperature, hourly.RelativeHumidity,
                hourly.Precipitation, hourly.PrecipitationProbability, hourly.WindSpeed, hourly.WeatherCode) ||
            !MatchLength(ForecastDays, daily.Time, daily.MinTemperature, daily.MaxTemperature,
                daily.Precipitation, daily.MaxPrecipitationProbability, daily.WeatherCode) ||
            daily.Time[0].Year < 1970)
        {
            throw InvalidForecast();
        }

        var startDate = daily.Time[0];
        var startTime = startDate.ToDateTime(TimeOnly.MinValue);
        var offset = TimeSpan.FromSeconds(response.UtcOffsetSeconds);
        var dailyPoints = new DailyWeatherForecast[ForecastDays];
        for (var index = 0; index < dailyPoints.Length; index++)
        {
            if (daily.Time[index].DayNumber - startDate.DayNumber != index)
            {
                throw InvalidForecast();
            }

            dailyPoints[index] = new DailyWeatherForecast(daily.Time[index], daily.MinTemperature[index],
                daily.MaxTemperature[index], daily.Precipitation[index], daily.MaxPrecipitationProbability[index],
                daily.WeatherCode[index]);
        }

        var hourlyPoints = new HourlyWeatherForecast[ForecastHours];
        for (var index = 0; index < hourlyPoints.Length; index++)
        {
            if (!DateTime.TryParseExact(hourly.Time[index], "yyyy-MM-dd'T'HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ||
                time - startTime != TimeSpan.FromHours(index))
            {
                throw InvalidForecast();
            }

            hourlyPoints[index] = new HourlyWeatherForecast(new DateTimeOffset(time, offset), hourly.Temperature[index],
                hourly.RelativeHumidity[index], hourly.Precipitation[index], hourly.PrecipitationProbability[index],
                hourly.WindSpeed[index], hourly.WeatherCode[index]);
        }

        return (hourlyPoints, dailyPoints);
    }

    private async Task<T?> ReadAsync<T>(
        double latitude, double longitude, string parameters, CancellationToken cancellationToken) where T : class
    {
        var location = FormattableString.Invariant($"latitude={latitude:R}&longitude={longitude:R}");
        var path = $"v1/forecast?{location}&{parameters}&timezone={Uri.EscapeDataString(TimeZone)}" +
            "&temperature_unit=celsius&wind_speed_unit=kmh&precipitation_unit=mm";
        try
        {
            return await httpClient.GetFromJsonAsync<T>(path, cancellationToken);
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

    private static bool MatchLength(int expected, params Array?[] series) =>
        series.All(values => values?.Length == expected);

    private static WeatherException InvalidForecast() =>
        new(WeatherError.ProviderUnavailable, "Open-Meteo did not return a complete seven-day forecast timeline.");

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

    private sealed record OpenMeteoForecast(
        [property: JsonPropertyName("utc_offset_seconds")] int UtcOffsetSeconds,
        OpenMeteoHourly? Hourly, OpenMeteoDaily? Daily);

    private sealed record OpenMeteoHourly(
        string[] Time,
        [property: JsonPropertyName("temperature_2m")] double?[] Temperature,
        [property: JsonPropertyName("relative_humidity_2m")] double?[] RelativeHumidity,
        double?[] Precipitation,
        [property: JsonPropertyName("precipitation_probability")] double?[] PrecipitationProbability,
        [property: JsonPropertyName("wind_speed_10m")] double?[] WindSpeed,
        [property: JsonPropertyName("weather_code")] int?[] WeatherCode);

    private sealed record OpenMeteoDaily(
        DateOnly[] Time,
        [property: JsonPropertyName("temperature_2m_min")] double?[] MinTemperature,
        [property: JsonPropertyName("temperature_2m_max")] double?[] MaxTemperature,
        [property: JsonPropertyName("precipitation_sum")] double?[] Precipitation,
        [property: JsonPropertyName("precipitation_probability_max")] double?[] MaxPrecipitationProbability,
        [property: JsonPropertyName("weather_code")] int?[] WeatherCode);
}
