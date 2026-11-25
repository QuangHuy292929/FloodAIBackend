namespace FloodAI.Business.Forecasting;

public enum ForecastError
{
    NotFound,
    InsufficientData,
    Unavailable
}

public sealed class ForecastException(ForecastError error, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ForecastError Error { get; } = error;
}
