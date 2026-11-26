using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FloodAI.Api.Authentication;
using FloodAI.Business.Accounts;
using FloodAI.Business.Catalog.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/admin/catalog")]
public sealed class AdminCatalogController(AdminHydrologyCatalogService hydrology, AdminDataCatalogService data)
    : ControllerBase
{
    private long AdminId => AccountBearerHandler.GetUserId(User);

    [HttpGet("basins")]
    public Task<List<CatalogBasin>> GetBasins(CancellationToken cancellationToken) =>
        hydrology.GetBasinsAsync(cancellationToken);

    [HttpPost("basins")]
    [ProducesResponseType<CatalogBasin>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogBasin>> CreateBasin(BasinInput input, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await hydrology.SaveBasinAsync(AdminId, null, input, cancellationToken));

    [HttpPut("basins/{id:long}")]
    public Task<CatalogBasin> UpdateBasin(long id, BasinInput input, CancellationToken cancellationToken) =>
        hydrology.SaveBasinAsync(AdminId, id, input, cancellationToken);

    [HttpGet("areas")]
    public Task<List<CatalogArea>> GetAreas(CancellationToken cancellationToken) =>
        hydrology.GetAreasAsync(cancellationToken);

    [HttpPost("areas")]
    [ProducesResponseType<CatalogArea>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogArea>> CreateArea(AreaInput input, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await hydrology.SaveAreaAsync(AdminId, null, input, cancellationToken));

    [HttpPut("areas/{id:long}")]
    public Task<CatalogArea> UpdateArea(long id, AreaInput input, CancellationToken cancellationToken) =>
        hydrology.SaveAreaAsync(AdminId, id, input, cancellationToken);

    [HttpPut("areas/{id:long}/weather-location")]
    public Task<CatalogArea> SetWeatherLocation(long id, AreaWeatherLocationInput input,
        CancellationToken cancellationToken) =>
        hydrology.SetWeatherLocationAsync(AdminId, id, input.Latitude, input.Longitude, cancellationToken);

    [HttpGet("stations")]
    public Task<List<CatalogStation>> GetStations(CancellationToken cancellationToken) =>
        hydrology.GetStationsAsync(cancellationToken);

    [HttpPost("stations")]
    [ProducesResponseType<CatalogStation>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogStation>> CreateStation(StationInput input, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await hydrology.SaveStationAsync(AdminId, null, input, cancellationToken));

    [HttpPut("stations/{id:long}")]
    public Task<CatalogStation> UpdateStation(long id, StationInput input, CancellationToken cancellationToken) =>
        hydrology.SaveStationAsync(AdminId, id, input, cancellationToken);

    [HttpGet("sources")]
    public Task<List<CatalogDataSource>> GetSources(CancellationToken cancellationToken) =>
        data.GetSourcesAsync(cancellationToken);

    [HttpPost("sources")]
    [ProducesResponseType<CatalogDataSource>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogDataSource>> CreateSource(DataSourceInput input, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await data.SaveSourceAsync(AdminId, null, input, cancellationToken));

    [HttpPut("sources/{id:long}")]
    public Task<CatalogDataSource> UpdateSource(long id, DataSourceInput input, CancellationToken cancellationToken) =>
        data.SaveSourceAsync(AdminId, id, input, cancellationToken);

    [HttpGet("metrics")]
    public Task<List<CatalogMetric>> GetMetrics(CancellationToken cancellationToken) =>
        data.GetMetricsAsync(cancellationToken);

    [HttpPost("metrics")]
    [ProducesResponseType<CatalogMetric>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogMetric>> CreateMetric(MetricInput input, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created,
            await data.SaveMetricAsync(AdminId, null, input, cancellationToken));

    [HttpPut("metrics/{id:long}")]
    public Task<CatalogMetric> UpdateMetric(long id, MetricInput input, CancellationToken cancellationToken) =>
        data.SaveMetricAsync(AdminId, id, input, cancellationToken);

    [HttpGet("observations")]
    public Task<CatalogPage<CatalogObservation>> GetObservations(
        [FromQuery] CatalogPageQuery query, long? stationId, string? qualityStatus, CancellationToken cancellationToken) =>
        data.GetObservationsAsync(stationId, qualityStatus, query.Page, query.PageSize, cancellationToken);

    [HttpPut("observations/{id:long}/metadata")]
    public async Task<ActionResult<CatalogObservation>> UpdateObservationMetadata(
        long id, ObservationMetadataInput input, CancellationToken cancellationToken)
    {
        DateTimeOffset? availableAt = null;
        if (input.AvailableAt is { } timestamp)
        {
            if (!DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                ModelState.AddModelError(nameof(input.AvailableAt), "Use a valid ISO timestamp with an offset.");
                return ValidationProblem(ModelState);
            }
            availableAt = parsed;
        }
        return await data.SetObservationMetadataAsync(AdminId, id, input.QualityStatus, availableAt, cancellationToken);
    }

    [HttpGet("actions")]
    public Task<CatalogPage<CatalogActionView>> GetActions(
        [FromQuery] CatalogPageQuery query, string? resourceType, CancellationToken cancellationToken) =>
        data.GetActionsAsync(resourceType, query.Page, query.PageSize, cancellationToken);
}

public sealed class AreaWeatherLocationInput
{
    [Range(-90d, 90d)]
    public required double? Latitude { get; init; }

    [Range(-180d, 180d)]
    public required double? Longitude { get; init; }
}

public sealed class CatalogPageQuery
{
    [Range(1, 100000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record ObservationMetadataInput(
    [Required, StringLength(32)] string QualityStatus,
    [RegularExpression(@"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\.[0-9]{1,7})?(?:Z|[+-][0-9]{2}:[0-9]{2})\z",
        ErrorMessage = "Use an ISO timestamp with an offset.")] string? AvailableAt = null);
