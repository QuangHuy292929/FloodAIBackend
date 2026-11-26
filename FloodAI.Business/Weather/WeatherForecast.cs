namespace FloodAI.Business.Weather;

public sealed record WeatherForecast(
    long AreaId, string AreaName, double Latitude, double Longitude,
    string Source, string SourceUrl, string DataOrigin, string TimeZone,
    DateTimeOffset FetchedAt, IReadOnlyList<HourlyWeatherForecast> Hourly,
    IReadOnlyList<DailyWeatherForecast> Daily, bool IsStale = false);

public sealed record HourlyWeatherForecast(
    DateTimeOffset ValidAt, double? TemperatureC, double? RelativeHumidityPercent,
    double? PrecipitationMm, double? PrecipitationProbabilityPercent, double? WindSpeedKmh, int? WeatherCode);

public sealed record DailyWeatherForecast(
    DateOnly Date, double? MinTemperatureC, double? MaxTemperatureC,
    double? PrecipitationMm, double? MaxPrecipitationProbabilityPercent, int? WeatherCode);
