using FloodAI.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Weather;

public sealed class WeatherService(FloodAiDbContext dbContext, OpenMeteoClient provider)
{
    private const string Source = "Open-Meteo";
    private const string SourceUrl = "https://open-meteo.com/";
    private const string DataOrigin = "WEATHER_PROVIDER";

    public async Task<CurrentWeather> GetCurrentAsync(long areaId, CancellationToken cancellationToken)
    {
        var (name, latitude, longitude) = await GetLocationAsync(areaId, cancellationToken);
        var snapshot = await provider.ReadCurrentAsync(latitude, longitude, cancellationToken);
        return new CurrentWeather(areaId, name, latitude, longitude, Source, SourceUrl,
            DataOrigin, OpenMeteoClient.TimeZone, DateTimeOffset.FromUnixTimeSeconds(snapshot.Time),
            DateTimeOffset.UtcNow, snapshot.Interval, snapshot.Temperature, snapshot.ApparentTemperature,
            snapshot.RelativeHumidity, snapshot.Precipitation, snapshot.WindSpeed, snapshot.WindDirection,
            snapshot.WeatherCode, snapshot.IsDay is int day ? day == 1 : null);
    }

    public async Task<WeatherForecast> GetForecastAsync(long areaId, CancellationToken cancellationToken)
    {
        var (name, latitude, longitude) = await GetLocationAsync(areaId, cancellationToken);
        var (hourly, daily) = await provider.ReadForecastAsync(latitude, longitude, cancellationToken);
        return new WeatherForecast(areaId, name, latitude, longitude, Source, SourceUrl, DataOrigin,
            OpenMeteoClient.TimeZone, DateTimeOffset.UtcNow, hourly, daily);
    }

    private async Task<(string Name, double Latitude, double Longitude)> GetLocationAsync(
        long areaId, CancellationToken cancellationToken)
    {
        var area = await dbContext.Areas.AsNoTracking()
            .Where(area => area.Id == areaId && area.IsActive)
            .Select(area => new { area.Name, area.WeatherLatitude, area.WeatherLongitude })
            .SingleOrDefaultAsync(cancellationToken) ??
            throw new WeatherException(WeatherError.AreaNotFound, "The active area was not found.");
        if (area.WeatherLatitude is not { } latitude || area.WeatherLongitude is not { } longitude)
        {
            throw new WeatherException(WeatherError.LocationNotConfigured,
                "The area does not have a configured weather location.");
        }
        return (area.Name, latitude, longitude);
    }
}
