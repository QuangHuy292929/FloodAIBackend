using System.Security.Cryptography;
using System.Text.Json;
using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using FloodAI.DataAccess.Observations;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Forecasting;

public sealed class HistoricalForecastService(
    FloodAiDbContext dbContext, VanUcForecastObservations observations, ForecastInferenceClient inference)
{
    private const string CompletedStatus = "COMPLETED";

    public async Task<List<ForecastModelSummary>?> GetModelsAsync(long stationId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Stations.AnyAsync(station => station.Id == stationId && station.Basin.IsActive,
            cancellationToken))
        {
            return null;
        }
        return await ActiveModels(stationId).OrderBy(model => model.HorizonHours)
            .Select(model => new ForecastModelSummary(model.Version, model.HorizonHours))
            .ToListAsync(cancellationToken);
    }

    public async Task<HistoricalForecast> PredictAsync(long stationId, DateTime issueTime,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Stations.AnyAsync(station => station.Id == stationId && station.Basin.IsActive,
            cancellationToken))
        {
            throw new ForecastException(ForecastError.NotFound, "The active station was not found.");
        }

        var models = await ActiveModels(stationId).OrderBy(model => model.HorizonHours).ToListAsync(cancellationToken);
        if (!models.Select(model => model.HorizonHours).SequenceEqual(VanUcForecastDefinition.Horizons) ||
            models.Select(model => model.ManifestHash).Distinct().Count() != 1)
        {
            throw new ForecastException(ForecastError.InsufficientData,
                "The station needs a complete active model bundle for the 1h, 3h and 6h horizons.");
        }

        if (issueTime.Year is < 2021 or > 2022)
        {
            throw new ForecastException(ForecastError.InsufficientData, "The issue hour is outside the Văn Úc 2021–2022 dataset.");
        }

        var threshold = await GetThresholdAsync(stationId, cancellationToken);
        var series = await observations.ResolveSeriesAsync(stationId, cancellationToken);
        var history = await observations.ReadHistoryAsync(stationId, series, issueTime, cancellationToken);
        var bundleHash = models[0].ManifestHash;
        var inputHash = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new { stationId, series.SourceId, series.HonDauId, history })));

        var cached = await FindRunAsync(stationId, issueTime, bundleHash, inputHash, threshold.Id, cancellationToken);
        var run = cached ?? await RunAndSaveAsync(
            stationId, issueTime, bundleHash, inputHash, threshold, models, history, cancellationToken);
        var actuals = await observations.ReadObservedTargetsAsync(stationId, series, issueTime, cancellationToken);
        var points = run.Values.OrderBy(value => value.HorizonHours).Select(value => new HistoricalForecastPoint(
            value.HorizonHours, value.TargetTimeLocal, value.PredictedValueRaw,
            actuals.TryGetValue(value.TargetTimeLocal, out var actual) && double.IsFinite(actual) ? actual : null,
            WarningStatus(value.PredictedValueRaw, threshold.ValueRaw))).ToList();

        return new HistoricalForecast(run.Id, stationId, issueTime, VanUcObservationDataset.TimeBasis,
            VanUcForecastDefinition.Version, bundleHash, inputHash, series.SourceName,
            "VALID_HISTORICAL_INPUT", threshold.UnitStatus, threshold.ThresholdType, threshold.Source, threshold.ValueRaw, points);
    }

    private IQueryable<ForecastModel> ActiveModels(long stationId) =>
        dbContext.ForecastModels.AsNoTracking().Where(model => model.StationId == stationId &&
            model.Version == VanUcForecastDefinition.Version && model.IsActive);

    private async Task<StationThreshold> GetThresholdAsync(long stationId, CancellationToken cancellationToken)
    {
        var thresholds = await dbContext.StationThresholds.AsNoTracking().Where(threshold =>
            threshold.StationId == stationId && threshold.IsActive &&
            threshold.ThresholdType == VanUcForecastDefinition.ThresholdType)
            .OrderBy(threshold => threshold.Id).Take(2).ToListAsync(cancellationToken);
        if (thresholds.Count != 1 || thresholds[0].ValueRaw != VanUcForecastDefinition.ResearchThreshold ||
            thresholds[0].Source != VanUcForecastDefinition.ThresholdSource ||
            thresholds[0].UnitStatus != VanUcForecastDefinition.UnitStatus)
        {
            throw new ForecastException(ForecastError.InsufficientData, "The Q95 research threshold is missing or ambiguous.");
        }
        return thresholds[0];
    }

    private Task<ForecastRun?> FindRunAsync(long stationId, DateTime issueTime,
        string bundleHash, string inputHash, long thresholdId, CancellationToken cancellationToken) =>
        dbContext.ForecastRuns.AsNoTracking().Include(run => run.Values)
            .Where(run => run.StationId == stationId && run.IssueTimeLocal == issueTime &&
                run.ModelBundleHash == bundleHash && run.InputHash == inputHash && run.Status == CompletedStatus &&
                run.Values.Count == VanUcForecastDefinition.Horizons.Count &&
                run.Values.All(value => dbContext.WarningEvaluations.Any(warning =>
                    warning.ForecastValueId == value.Id && warning.ThresholdId == thresholdId)))
            .OrderBy(run => run.Id).FirstOrDefaultAsync(cancellationToken);

    private async Task<ForecastRun> RunAndSaveAsync(long stationId, DateTime issueTime, string bundleHash,
        string inputHash, StationThreshold threshold, IReadOnlyList<ForecastModel> models,
        IReadOnlyList<ForecastHour> history, CancellationToken cancellationToken)
    {
        var result = await inference.PredictAsync(issueTime, bundleHash, history, cancellationToken);
        if (result.Version != VanUcForecastDefinition.Version || result.ManifestHash != bundleHash ||
            result.Predictions is null ||
            !result.Predictions.Select(prediction => prediction.HorizonHours).Order()
                .SequenceEqual(VanUcForecastDefinition.Horizons) ||
            result.Predictions.Any(prediction => !double.IsFinite(prediction.PredictedWaterLevelRaw)))
        {
            throw new ForecastException(ForecastError.Unavailable,
                "The inference response does not match the registered model bundle.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = FormattableString.Invariant(
            $"floodai:forecast_run:{stationId}:{issueTime:O}:{bundleHash}:{inputHash}:{threshold.Id}");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));", cancellationToken);

        var cached = await FindRunAsync(stationId, issueTime, bundleHash, inputHash, threshold.Id, cancellationToken);
        if (cached is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return cached;
        }

        var run = new ForecastRun
        {
            StationId = stationId,
            IssueTimeLocal = issueTime,
            ModelBundleHash = bundleHash,
            InputHash = inputHash,
            Status = CompletedStatus
        };
        dbContext.ForecastRuns.Add(run);
        foreach (var prediction in result.Predictions)
        {
            var value = new ForecastValue
            {
                Run = run,
                ModelId = models.Single(model => model.HorizonHours == prediction.HorizonHours).Id,
                TargetTimeLocal = issueTime.AddHours(prediction.HorizonHours),
                HorizonHours = prediction.HorizonHours,
                PredictedValueRaw = prediction.PredictedWaterLevelRaw
            };
            dbContext.WarningEvaluations.Add(new WarningEvaluation
            {
                ForecastValue = value,
                ThresholdId = threshold.Id,
                Status = WarningStatus(value.PredictedValueRaw, threshold.ValueRaw)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return run;
    }

    private static string WarningStatus(double predicted, double threshold) =>
        predicted >= threshold ? "THRESHOLD_EXCEEDED" : "NORMAL";
}
