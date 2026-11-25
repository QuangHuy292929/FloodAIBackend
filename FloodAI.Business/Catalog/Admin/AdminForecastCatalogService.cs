using FloodAI.Business.Forecasting;
using FloodAI.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Catalog.Admin;

public sealed record CatalogForecastModel(
    long Id, string Version, int HorizonHours, string ArtifactPath, string ManifestHash, bool IsActive);

public sealed record CatalogThreshold(
    long Id, string ThresholdType, double ValueRaw, string Source, string UnitStatus, bool IsActive);

public sealed class AdminForecastCatalogService(FloodAiDbContext dbContext)
{
    public async Task<List<CatalogForecastModel>> GetModelsAsync(long stationId, CancellationToken cancellationToken)
    {
        await EnsureStationExistsAsync(stationId, cancellationToken);
        return await dbContext.ForecastModels.AsNoTracking().Where(model => model.StationId == stationId)
            .OrderBy(model => model.Version).ThenBy(model => model.HorizonHours)
            .Select(model => new CatalogForecastModel(model.Id, model.Version, model.HorizonHours,
                model.ArtifactPath, model.ManifestHash, model.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CatalogThreshold>> GetThresholdsAsync(long stationId, CancellationToken cancellationToken)
    {
        await EnsureStationExistsAsync(stationId, cancellationToken);
        return await dbContext.StationThresholds.AsNoTracking().Where(threshold => threshold.StationId == stationId)
            .OrderBy(threshold => threshold.ThresholdType).ThenBy(threshold => threshold.Id)
            .Select(threshold => new CatalogThreshold(threshold.Id, threshold.ThresholdType,
                threshold.ValueRaw, threshold.Source, threshold.UnitStatus, threshold.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task SetBundleActiveAsync(
        long adminId, long stationId, string version, bool isActive, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await ForecastModelRegistration.LockStationAsync(dbContext, stationId, cancellationToken);

        var models = await dbContext.ForecastModels
            .Where(model => model.StationId == stationId && model.Version == version)
            .OrderBy(model => model.HorizonHours).ToListAsync(cancellationToken);
        if (models.Count == 0)
        {
            throw new CatalogException(CatalogError.NotFound, "Forecast model bundle not found.");
        }

        if (isActive && (version != VanUcForecastDefinition.Version ||
            !models.Select(model => model.HorizonHours).SequenceEqual(VanUcForecastDefinition.Horizons) ||
            models.Select(model => model.ManifestHash).Distinct().Count() != 1 ||
            models[0].ManifestHash is not { Length: 64 } || !models[0].ManifestHash.All(Uri.IsHexDigit) ||
            models.Any(model => model.ArtifactPath != $"xgboost_{model.HorizonHours}h.ubj")))
        {
            throw new CatalogException(CatalogError.Conflict, "The registered model bundle is incomplete or unsupported.");
        }

        foreach (var model in models)
        {
            model.IsActive = isActive;
        }
        await SaveStatusAsync(adminId, isActive, "FORECAST_BUNDLE", FormattableString.Invariant($"{stationId}:{version}"),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetThresholdActiveAsync(
        long adminId, long stationId, long thresholdId, bool isActive, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await ForecastModelRegistration.LockStationAsync(dbContext, stationId, cancellationToken);

        var threshold = await dbContext.StationThresholds.SingleOrDefaultAsync(candidate =>
            candidate.Id == thresholdId && candidate.StationId == stationId, cancellationToken);
        if (threshold is null)
        {
            throw new CatalogException(CatalogError.NotFound, "Station threshold not found.");
        }
        if (isActive && (!ForecastModelRegistration.IsResearchThreshold(threshold) ||
            await dbContext.StationThresholds.AnyAsync(candidate => candidate.StationId == stationId &&
                candidate.Id != thresholdId && candidate.ThresholdType == threshold.ThresholdType &&
                candidate.IsActive, cancellationToken)))
        {
            throw new CatalogException(CatalogError.Conflict,
                "Activation requires the supported Q95 threshold with no competing active threshold.");
        }

        threshold.IsActive = isActive;
        await SaveStatusAsync(adminId, isActive, "STATION_THRESHOLD",
            FormattableString.Invariant($"{stationId}:{threshold.Id}"), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task EnsureStationExistsAsync(long stationId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Stations.AnyAsync(station => station.Id == stationId, cancellationToken))
        {
            throw new CatalogException(CatalogError.NotFound, "Station not found.");
        }
    }

    private Task SaveStatusAsync(
        long adminId, bool isActive, string resourceType, string resourceKey, CancellationToken cancellationToken)
    {
        CatalogAudit.Record(dbContext, adminId, isActive ? "ACTIVATE" : "DEACTIVATE", resourceType, resourceKey);
        return CatalogAudit.SaveAsync(dbContext, cancellationToken);
    }
}
