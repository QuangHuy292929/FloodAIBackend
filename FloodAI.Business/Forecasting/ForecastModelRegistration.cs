using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Forecasting;

public sealed class ForecastModelRegistration(
    FloodAiDbContext dbContext, VanUcForecastObservations observations, ForecastInferenceClient inference)
{
    public async Task RegisterAsync(long stationId, CancellationToken cancellationToken)
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
        var lockKey = FormattableString.Invariant($"floodai:van_uc_model_registration:{stationId}");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));", cancellationToken);

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
            threshold.StationId == stationId && threshold.ThresholdType == VanUcForecastDefinition.ThresholdType &&
            threshold.IsActive).OrderBy(threshold => threshold.Id).Take(2).ToListAsync(cancellationToken);
        if (thresholds.Count > 1 || thresholds.Any(threshold =>
            threshold.ValueRaw != bundle.ThresholdValueRaw || threshold.Source != VanUcForecastDefinition.ThresholdSource ||
            threshold.UnitStatus != VanUcForecastDefinition.UnitStatus))
        {
            throw new ForecastException(ForecastError.Unavailable, "The station has a conflicting Q95 research threshold.");
        }

        if (thresholds.Count == 0)
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

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
