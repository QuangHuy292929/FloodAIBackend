using FloodAI.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Weather;

public sealed class WeatherService(
    FloodAiDbContext dbContext, OpenMeteoClient provider, WeatherCache cache, TimeProvider clock)
{
    private const string Source = "Open-Meteo";
    private const string SourceUrl = "https://open-meteo.com/";
    private const string DataOrigin = "WEATHER_PROVIDER";

    public async Task<CurrentWeather> GetCurrentAsync(long areaId, CancellationToken cancellationToken)
    {
        var (name, latitude, longitude) = await GetLocationAsync(areaId, cancellationToken);
        var (value, isStale) = await cache.GetCurrentAsync(areaId, latitude, longitude, async token =>
        {
            var snapshot = await provider.ReadCurrentAsync(latitude, longitude, token);
            return new CurrentWeather(areaId, name, latitude, longitude, Source, SourceUrl,
                DataOrigin, OpenMeteoClient.TimeZone, DateTimeOffset.FromUnixTimeSeconds(snapshot.Time),
                clock.GetUtcNow(), snapshot.Interval, snapshot.Temperature, snapshot.ApparentTemperature,
                snapshot.RelativeHumidity, snapshot.Precipitation, snapshot.WindSpeed, snapshot.WindDirection,
                snapshot.WeatherCode, snapshot.IsDay is int day ? day == 1 : null);
        }, cancellationToken);
        return value with { AreaName = name, IsStale = isStale };
    }

    public async Task<WeatherForecast> GetForecastAsync(long areaId, CancellationToken cancellationToken)
    {
        var (name, latitude, longitude) = await GetLocationAsync(areaId, cancellationToken);
        var (value, isStale) = await cache.GetForecastAsync(areaId, latitude, longitude, async token =>
        {
            var (hourly, daily) = await provider.ReadForecastAsync(latitude, longitude, token);
            return new WeatherForecast(areaId, name, latitude, longitude, Source, SourceUrl, DataOrigin,
                OpenMeteoClient.TimeZone, clock.GetUtcNow(), hourly, daily);
        }, cancellationToken);
        return value with { AreaName = name, IsStale = isStale };
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
