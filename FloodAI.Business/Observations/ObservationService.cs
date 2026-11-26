using FloodAI.DataAccess;
using FloodAI.DataAccess.Observations;
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

        var rows = await dbContext.Observations.AsNoTracking()
            .Where(observation => observation.StationId == stationId &&
                observation.DataOrigin != ObservationMetadata.Synthetic)
            .Select(observation => new
            {
                observation.Metric.Code, observation.Metric.Name, observation.Metric.QuantityType, observation.Unit
            }).Distinct().OrderBy(metric => metric.Name).ThenBy(metric => metric.Code)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(metric => new { metric.Code, metric.Name, metric.QuantityType })
            .Select(group => new StationMetricSummary(group.Key.Code, group.Key.Name, group.Key.QuantityType,
                group.Count() == 1 ? group.First().Unit : null)).ToList();
    }

    public async Task<ObservationPage?> GetObservationsAsync(
        long stationId,
        string metric,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        long? sourceId,
        string? dataOrigin,
        CancellationToken cancellationToken)
    {
        if (!await StationIsVisibleAsync(stationId, cancellationToken))
        {
            return null;
        }

        var query = dbContext.Observations.AsNoTracking().Where(observation =>
            observation.StationId == stationId && observation.Metric.Code == metric &&
            (sourceId == null || observation.SourceId == sourceId) &&
            (dataOrigin == null ? observation.DataOrigin != ObservationMetadata.Synthetic :
                observation.DataOrigin == dataOrigin) &&
            observation.TimestampLocal >= from && observation.TimestampLocal < to);
        var timeBases = await query.Select(observation => observation.Source.TimeBasis)
            .Distinct().Take(2).ToListAsync(cancellationToken);
        var timeBasis = timeBases.Count > 1 ? ObservationMetadata.PerSource :
            timeBases.SingleOrDefault() ?? ObservationMetadata.SourceLocalUnverified;

        var observations = await query.OrderBy(observation => observation.TimestampLocal)
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
                    observation.Source.Url,
                    observation.Source.SourceType,
                    observation.Source.TimeBasis),
                observation.DataOrigin,
                observation.Unit,
                observation.Unit == null ? ObservationMetadata.UnverifiedUnit : ObservationMetadata.VerifiedUnit,
                observation.IngestedAt))
            .ToListAsync(cancellationToken);

        var hasNextPage = observations.Count > pageSize;
        if (hasNextPage)
        {
            observations.RemoveAt(pageSize);
        }

        return new ObservationPage(metric, from, to, timeBasis,
            page, pageSize, hasNextPage, observations);
    }

    private Task<bool> StationIsVisibleAsync(long stationId, CancellationToken cancellationToken)
    {
        return dbContext.Stations.AnyAsync(station => station.Id == stationId && station.Basin.IsActive,
            cancellationToken);
    }
}
