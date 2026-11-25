using FloodAI.Business.Forecasting;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.OpenApi;

public static class HistoricalForecastApiConvention
{
    public const string RateLimitPolicy = "historical-forecast";

    [ProducesResponseType<HistoricalForecast>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public static void Predict()
    {
    }
}
