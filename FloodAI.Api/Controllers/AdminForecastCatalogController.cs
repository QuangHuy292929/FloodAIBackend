using System.ComponentModel.DataAnnotations;
using FloodAI.Api.Authentication;
using FloodAI.Business.Accounts;
using FloodAI.Business.Catalog.Admin;
using FloodAI.Business.Forecasting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/admin/stations/{stationId:long}")]
public sealed class AdminForecastCatalogController(
    AdminForecastCatalogService catalog, ForecastModelRegistration registration) : ControllerBase
{
    private long AdminId => AccountBearerHandler.GetUserId(User);

    [HttpGet("models")]
    public Task<List<CatalogForecastModel>> GetModels(long stationId, CancellationToken cancellationToken) =>
        catalog.GetModelsAsync(stationId, cancellationToken);

    [HttpPost("models/register-van-uc")]
    public async Task<IActionResult> RegisterVanUc(long stationId, CancellationToken cancellationToken)
    {
        await registration.RegisterAsync(stationId, cancellationToken, AdminId);
        return NoContent();
    }

    [HttpPatch("models/{version}/status")]
    public async Task<IActionResult> SetModelStatus(
        long stationId, string version, CatalogActivationInput input, CancellationToken cancellationToken)
    {
        await catalog.SetBundleActiveAsync(AdminId, stationId, version, input.IsActive!.Value, cancellationToken);
        return NoContent();
    }

    [HttpGet("thresholds")]
    public Task<List<CatalogThreshold>> GetThresholds(long stationId, CancellationToken cancellationToken) =>
        catalog.GetThresholdsAsync(stationId, cancellationToken);

    [HttpPatch("thresholds/{thresholdId:long}/status")]
    public async Task<IActionResult> SetThresholdStatus(
        long stationId, long thresholdId, CatalogActivationInput input, CancellationToken cancellationToken)
    {
        await catalog.SetThresholdActiveAsync(AdminId, stationId, thresholdId, input.IsActive!.Value, cancellationToken);
        return NoContent();
    }
}

public sealed record CatalogActivationInput([Required] bool? IsActive);
