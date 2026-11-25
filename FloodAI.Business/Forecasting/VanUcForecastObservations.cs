using FloodAI.DataAccess;
using FloodAI.DataAccess.Observations;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Forecasting;

public sealed class VanUcForecastObservations(FloodAiDbContext dbContext)
{
    internal sealed record Series(long SourceId, string SourceName, long HonDauId, long WaterLevelId, long DischargeId);

    internal async Task<Series> ResolveSeriesAsync(long trungTrangId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Stations.AnyAsync(station => station.Id == trungTrangId &&
            station.Basin.IsActive && station.Basin.Code == VanUcObservationDataset.BasinCode, cancellationToken))
        {
            throw new ForecastException(ForecastError.NotFound, "The active Văn Úc station was not found.");
        }

        var sources = await dbContext.DataSources.AsNoTracking()
            .Where(source => source.Name == VanUcObservationDataset.SourceName &&
                source.SourceType == VanUcObservationDataset.SourceType && source.Status == "ACTIVE")
            .OrderBy(source => source.Id)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (sources.Count != 1)
        {
            throw new ForecastException(ForecastError.InsufficientData,
                "The Văn Úc 2021–2022 source must be imported exactly once.");
        }

        var source = sources[0];
        var metrics = await dbContext.Metrics.AsNoTracking()
            .Where(metric => metric.Code == VanUcObservationDataset.WaterLevelCode ||
                metric.Code == VanUcObservationDataset.DischargeCode)
            .ToDictionaryAsync(metric => metric.Code, metric => metric.Id, cancellationToken);
        if (!metrics.TryGetValue(VanUcObservationDataset.WaterLevelCode, out var waterLevelId) ||
            !metrics.TryGetValue(VanUcObservationDataset.DischargeCode, out var dischargeId))
        {
            throw new ForecastException(ForecastError.InsufficientData, "The imported observation metrics are missing.");
        }

        var sourceData = dbContext.Observations.Where(observation => observation.SourceId == source.Id &&
            observation.Station.Basin.IsActive &&
            observation.Station.Basin.Code == VanUcObservationDataset.BasinCode);
        var dischargeStations = await sourceData.Where(observation => observation.MetricId == dischargeId)
            .Select(observation => observation.StationId).Distinct().OrderBy(id => id)
            .Take(2).ToListAsync(cancellationToken);
        var waterStations = await sourceData.Where(observation => observation.MetricId == waterLevelId)
            .Select(observation => observation.StationId).Distinct().OrderBy(id => id)
            .Take(3).ToListAsync(cancellationToken);

        if (dischargeStations.Count != 1 || dischargeStations[0] != trungTrangId ||
            waterStations.Count != 2 || !waterStations.Contains(trungTrangId))
        {
            throw new ForecastException(ForecastError.InsufficientData,
                "The imported source must contain Trung Trang water level and discharge, and Hòn Dấu water level.");
        }

        return new Series(source.Id, source.Name, waterStations.Single(id => id != trungTrangId),
            waterLevelId, dischargeId);
    }

    internal async Task<IReadOnlyList<ForecastHour>> ReadHistoryAsync(
        long stationId, Series series, DateTime issueTime, CancellationToken cancellationToken)
    {
        var start = issueTime.AddHours(1 - VanUcForecastDefinition.HistoryHours);
        var values = await dbContext.Observations.AsNoTracking()
            .Where(observation => observation.SourceId == series.SourceId &&
                observation.TimestampLocal >= start && observation.TimestampLocal <= issueTime &&
                ((observation.StationId == stationId &&
                    (observation.MetricId == series.WaterLevelId || observation.MetricId == series.DischargeId)) ||
                    (observation.StationId == series.HonDauId && observation.MetricId == series.WaterLevelId)))
            .ToDictionaryAsync(
                observation => (observation.TimestampLocal, observation.StationId, observation.MetricId),
                observation => observation.ValueRaw, cancellationToken);

        var history = new List<ForecastHour>(VanUcForecastDefinition.HistoryHours);
        for (var index = 0; index < VanUcForecastDefinition.HistoryHours; index++)
        {
            var time = start.AddHours(index);
            if (!values.TryGetValue((time, stationId, series.WaterLevelId), out var trungTrangWater) ||
                !values.TryGetValue((time, series.HonDauId, series.WaterLevelId), out var honDauWater) ||
                !values.TryGetValue((time, stationId, series.DischargeId), out var discharge) ||
                !float.IsFinite((float)trungTrangWater) || !float.IsFinite((float)honDauWater) ||
                !float.IsFinite((float)discharge))
            {
                throw new ForecastException(ForecastError.InsufficientData,
                    $"A valid 48-hour window requires all three inputs. Check source hour {time:yyyy-MM-dd HH:mm:ss}.");
            }

            history.Add(new ForecastHour(time, trungTrangWater, honDauWater, discharge));
        }

        return history;
    }

    internal Task<Dictionary<DateTime, double>> ReadObservedTargetsAsync(
        long stationId, Series series, DateTime issueTime, CancellationToken cancellationToken)
    {
        var targetTimes = VanUcForecastDefinition.Horizons.Select(horizon => issueTime.AddHours(horizon)).ToArray();
        return dbContext.Observations.AsNoTracking()
            .Where(observation => observation.SourceId == series.SourceId && observation.StationId == stationId &&
                observation.MetricId == series.WaterLevelId && targetTimes.Contains(observation.TimestampLocal))
            .ToDictionaryAsync(observation => observation.TimestampLocal,
                observation => observation.ValueRaw, cancellationToken);
    }
}
