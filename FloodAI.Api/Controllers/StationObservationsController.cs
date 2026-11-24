using System.Globalization;
using FloodAI.Business.Observations;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/stations/{id:long}")]
public sealed class StationObservationsController(ObservationService observations) : ControllerBase
{
    private static readonly string[] LocalTimeFormats =
        ["yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd"];

    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<StationMetricSummary>>> GetMetrics(
        long id, CancellationToken cancellationToken)
    {
        var metrics = await observations.GetMetricsAsync(id, cancellationToken);
        if (metrics is null)
        {
            return NotFound();
        }

        return metrics;
    }

    [HttpGet("observations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObservationPage>> GetObservations(
        long id, [FromQuery] ObservationQuery query, CancellationToken cancellationToken)
    {
        if (!TryParseLocalTime(query.From, out var from))
        {
            ModelState.AddModelError("from", "Use an ISO date or local date and time without a timezone offset.");
        }

        if (!TryParseLocalTime(query.To, out var to))
        {
            ModelState.AddModelError("to", "Use an ISO date or local date and time without a timezone offset.");
        }

        if (ModelState.IsValid && from >= to)
        {
            ModelState.AddModelError("to", "The end must be later than the start.");
        }

        if (((long)query.Page - 1) * query.PageSize > int.MaxValue)
        {
            ModelState.AddModelError("page", "The requested page is out of range.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var page = await observations.GetObservationsAsync(
            id, query.Metric.Trim(), from, to, query.Page, query.PageSize, cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        return page;
    }

    private static bool TryParseLocalTime(string value, out DateTime timestamp)
    {
        return DateTime.TryParseExact(value, LocalTimeFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out timestamp);
    }
}
