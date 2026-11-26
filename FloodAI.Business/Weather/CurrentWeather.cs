namespace FloodAI.Business.Weather;

public sealed record CurrentWeather(
    long AreaId, string AreaName, double Latitude, double Longitude,
    string Source, string SourceUrl, string DataOrigin, string TimeZone,
    DateTimeOffset ValidAt, DateTimeOffset FetchedAt, int IntervalSeconds,
    double? TemperatureC, double? ApparentTemperatureC, double? RelativeHumidityPercent,
    double? PrecipitationMm, double? WindSpeedKmh, double? WindDirectionDegrees,
    int? WeatherCode, bool? IsDay);
