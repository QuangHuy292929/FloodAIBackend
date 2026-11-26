using FloodAI.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Weather;

public sealed class WeatherService(FloodAiDbContext dbContext, OpenMeteoClient provider)
{
    public async Task<CurrentWeather> GetCurrentAsync(long areaId, CancellationToken cancellationToken)
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

        var snapshot = await provider.ReadCurrentAsync(latitude, longitude, cancellationToken);
        return new CurrentWeather(areaId, area.Name, latitude, longitude, "Open-Meteo", "https://open-meteo.com/",
            "WEATHER_PROVIDER", OpenMeteoClient.TimeZone, DateTimeOffset.FromUnixTimeSeconds(snapshot.Time),
            DateTimeOffset.UtcNow, snapshot.Interval, snapshot.Temperature, snapshot.ApparentTemperature,
            snapshot.RelativeHumidity, snapshot.Precipitation, snapshot.WindSpeed, snapshot.WindDirection,
            snapshot.WeatherCode, snapshot.IsDay is int day ? day == 1 : null);
    }
}
