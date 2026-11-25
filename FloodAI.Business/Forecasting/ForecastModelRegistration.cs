using FloodAI.Business.Catalog.Admin;
using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Forecasting;

public sealed class ForecastModelRegistration(
    FloodAiDbContext dbContext, VanUcForecastObservations observations, ForecastInferenceClient inference)
{
    public async Task RegisterAsync(long stationId, CancellationToken cancellationToken, long? adminId = null)
    {
        await observations.ResolveSeriesAsync(stationId, cancellationToken);
        var bundle = await inference.GetBundleAsync(cancellationToken);
        if (bundle.Version != VanUcForecastDefinition.Version ||
            bundle.ThresholdValueRaw != VanUcForecastDefinition.ResearchThreshold ||
            bundle.ManifestHash is not { Length: 64 } || !bundle.ManifestHash.All(Uri.IsHexDigit) ||
            bundle.Models is null || !bundle.Models.Select(model => model.HorizonHours).Order()
                .SequenceEqual(VanUcForecastDefinition.Horizons) ||
            bundle.Models.Any(model => model.ArtifactPath != $"xgboost_{model.HorizonHours}h.ubj"))
        {
            throw new ForecastException(ForecastError.Unavailable, "The worker serves an unsupported model bundle.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LockStationAsync(dbContext, stationId, cancellationToken);

        var existingModels = await dbContext.ForecastModels.Where(model =>
            model.StationId == stationId && model.Version == bundle.Version)
            .ToDictionaryAsync(model => model.HorizonHours, cancellationToken);
        foreach (var artifact in bundle.Models)
        {
            if (existingModels.TryGetValue(artifact.HorizonHours, out var registered))
            {
                if (registered.ManifestHash != bundle.ManifestHash || registered.ArtifactPath != artifact.ArtifactPath)
                {
                    throw new ForecastException(ForecastError.Unavailable,
                        "A different artifact is already registered for this model version.");
                }

                continue;
            }

            dbContext.ForecastModels.Add(new ForecastModel
            {
                StationId = stationId,
                Version = bundle.Version,
                HorizonHours = artifact.HorizonHours,
                ArtifactPath = artifact.ArtifactPath,
                ManifestHash = bundle.ManifestHash,
                IsActive = true
            });
        }

        var thresholds = await dbContext.StationThresholds.Where(threshold =>
            threshold.StationId == stationId && threshold.ThresholdType == VanUcForecastDefinition.ThresholdType)
            .ToListAsync(cancellationToken);
        var activeThresholds = thresholds.Where(threshold => threshold.IsActive).ToList();
        if (activeThresholds.Count > 1 || activeThresholds.Any(threshold => !IsResearchThreshold(threshold)))
        {
            throw new ForecastException(ForecastError.Unavailable, "The station has a conflicting Q95 research threshold.");
        }

        if (!thresholds.Any(IsResearchThreshold))
        {
            dbContext.StationThresholds.Add(new StationThreshold
            {
                StationId = stationId,
                ThresholdType = VanUcForecastDefinition.ThresholdType,
                ValueRaw = bundle.ThresholdValueRaw,
                Source = VanUcForecastDefinition.ThresholdSource,
                UnitStatus = VanUcForecastDefinition.UnitStatus,
                IsActive = true
            });
        }

        if (adminId is { } actor)
        {
            CatalogAudit.Record(dbContext, actor, "REGISTER", "FORECAST_BUNDLE",
                FormattableString.Invariant($"{stationId}:{bundle.Version}"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    internal static bool IsResearchThreshold(StationThreshold threshold) =>
        threshold.ThresholdType == VanUcForecastDefinition.ThresholdType &&
        threshold.ValueRaw == VanUcForecastDefinition.ResearchThreshold &&
        threshold.Source == VanUcForecastDefinition.ThresholdSource &&
        threshold.UnitStatus == VanUcForecastDefinition.UnitStatus;

    internal static Task LockStationAsync(
        FloodAiDbContext dbContext, long stationId, CancellationToken cancellationToken)
    {
        var lockKey = FormattableString.Invariant($"floodai:van_uc_model_registration:{stationId}");
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));", cancellationToken);
    }
}
