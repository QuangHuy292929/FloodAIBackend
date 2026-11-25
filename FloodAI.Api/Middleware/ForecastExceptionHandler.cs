using FloodAI.Business.Forecasting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Middleware;

public sealed class ForecastExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<ForecastExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ForecastException forecast)
        {
            return false;
        }

        var status = forecast.Error switch
        {
            ForecastError.NotFound => StatusCodes.Status404NotFound,
            ForecastError.InsufficientData => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status503ServiceUnavailable
        };
        if (status == StatusCodes.Status503ServiceUnavailable)
        {
            logger.LogWarning(forecast, "Historical forecast inference is unavailable.");
        }

        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Detail = forecast.Message };
        if (!await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details
        }))
        {
            await context.Response.WriteAsJsonAsync(details, options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
        }
        return true;
    }
}
