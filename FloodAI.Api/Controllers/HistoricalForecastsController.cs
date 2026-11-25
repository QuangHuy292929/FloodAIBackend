using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FloodAI.Api.OpenApi;
using FloodAI.Business.Forecasting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/forecasts")]
public sealed class HistoricalForecastsController(HistoricalForecastService forecasts) : ControllerBase
{
    public sealed record ForecastRequest([Range(1, long.MaxValue)] long StationId, [Required] string IssueTimeLocal);

    [HttpPost("historical")]
    [EnableRateLimiting(HistoricalForecastApiConvention.RateLimitPolicy)]
    [ApiConventionMethod(typeof(HistoricalForecastApiConvention), nameof(HistoricalForecastApiConvention.Predict))]
    public async Task<ActionResult<HistoricalForecast>> Predict(
        ForecastRequest request, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(request.IssueTimeLocal, "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var issueTime) ||
            issueTime.Minute != 0 || issueTime.Second != 0)
        {
            ModelState.AddModelError("issueTimeLocal", "Use a source-local whole hour such as 2022-10-15T12:00:00.");
            return ValidationProblem(ModelState);
        }

        return await forecasts.PredictAsync(request.StationId, issueTime, cancellationToken);
    }
}
