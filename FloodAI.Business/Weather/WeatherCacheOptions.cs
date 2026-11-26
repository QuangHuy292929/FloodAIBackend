namespace FloodAI.Business.Weather;

public sealed class WeatherCacheOptions
{
    public TimeSpan CurrentFreshFor { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan CurrentMaxAge { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ForecastFreshFor { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ForecastMaxAge { get; set; } = TimeSpan.FromHours(6);
    public TimeSpan RetryAfterFailure { get; set; } = TimeSpan.FromMinutes(1);

    public bool IsValid() =>
        CurrentFreshFor > TimeSpan.Zero && CurrentMaxAge > CurrentFreshFor &&
        ForecastFreshFor > TimeSpan.Zero && ForecastMaxAge > ForecastFreshFor &&
        CurrentMaxAge <= TimeSpan.FromDays(1) && ForecastMaxAge <= TimeSpan.FromDays(1) &&
        RetryAfterFailure > TimeSpan.Zero && RetryAfterFailure <= TimeSpan.FromHours(1);
}
