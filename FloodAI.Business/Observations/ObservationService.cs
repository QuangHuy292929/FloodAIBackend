using FloodAI.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Observations;

public sealed class ObservationService(FloodAiDbContext dbContext)
{
    public async Task<List<StationMetricSummary>?> GetMetricsAsync(
        long stationId, CancellationToken cancellationToken)
    {
        if (!await StationIsVisibleAsync(stationId, cancellationToken))
        {
            return null;
        }

        return await dbContext.Metrics.AsNoTracking()
            .Where(metric => dbContext.Observations.Any(observation =>
                observation.StationId == stationId && observation.MetricId == metric.Id))
            .OrderBy(metric => metric.Name)
            .ThenBy(metric => metric.Code)
            .Select(metric => new StationMetricSummary(metric.Code, metric.Name, metric.QuantityType, null))
            .ToListAsync(cancellationToken);
    }

    public async Task<ObservationPage?> GetObservationsAsync(
        long stationId,
        string metric,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await StationIsVisibleAsync(stationId, cancellationToken))
        {
            return null;
        }

        var observations = await dbContext.Observations.AsNoTracking()
            .Where(observation =>
                observation.StationId == stationId &&
                observation.Metric.Code == metric &&
                observation.TimestampLocal >= from &&
                observation.TimestampLocal < to)
            .OrderBy(observation => observation.TimestampLocal)
            .ThenBy(observation => observation.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(observation => new ObservationSummary(
                observation.TimestampLocal,
                observation.ValueRaw,
                observation.QualityStatus,
                observation.AvailableAt,
                new ObservationSourceSummary(
                    observation.Source.Id,
                    observation.Source.Name,
                    observation.Source.Url)))
            .ToListAsync(cancellationToken);

        var hasNextPage = observations.Count > pageSize;
        if (hasNextPage)
        {
            observations.RemoveAt(pageSize);
        }

        return new ObservationPage(metric, from, to, "SOURCE_LOCAL_UNVERIFIED",
            page, pageSize, hasNextPage, observations);
    }

    private Task<bool> StationIsVisibleAsync(long stationId, CancellationToken cancellationToken)
    {
        return dbContext.Stations.AnyAsync(station => station.Id == stationId && station.Basin.IsActive,
            cancellationToken);
    }
}
