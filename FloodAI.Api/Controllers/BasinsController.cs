using FloodAI.Business.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/basins")]
public sealed class BasinsController(HydrologyCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BasinSummary>>> GetBasins(CancellationToken cancellationToken)
    {
        return await catalog.GetBasinsAsync(cancellationToken);
    }

    [HttpGet("{id:long}/stations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<StationSummary>>> GetStations(long id, CancellationToken cancellationToken)
    {
        var stations = await catalog.GetStationsAsync(id, cancellationToken);
        if (stations is null)
        {
            return NotFound();
        }

        return stations;
    }
}
