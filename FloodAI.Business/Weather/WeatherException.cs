namespace FloodAI.Business.Weather;

public enum WeatherError
{
    AreaNotFound,
    LocationNotConfigured,
    ProviderUnavailable
}

public sealed class WeatherException(WeatherError error, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public WeatherError Error { get; } = error;
}
