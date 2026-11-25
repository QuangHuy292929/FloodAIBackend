using FloodAI.Business.Forecasting;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/stations/{id:long}/models")]
public sealed class StationModelsController(HistoricalForecastService forecasts) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ForecastModelSummary>>> GetModels(long id, CancellationToken cancellationToken)
    {
        var models = await forecasts.GetModelsAsync(id, cancellationToken);
        if (models is null)
        {
            return NotFound();
        }
        return models;
    }
}
