using FloodAI.Business.Weather;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Middleware;

public sealed class WeatherExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not WeatherException weather)
        {
            return false;
        }

        var status = weather.Error switch
        {
            WeatherError.AreaNotFound => StatusCodes.Status404NotFound,
            WeatherError.LocationNotConfigured => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status503ServiceUnavailable
        };
        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Detail = weather.Message };
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
