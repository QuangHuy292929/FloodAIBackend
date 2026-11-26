using System.Globalization;
using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using FloodAI.DataAccess.Observations;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Catalog.Admin;

public sealed class AdminDataCatalogService(FloodAiDbContext dbContext)
{
    public Task<List<CatalogDataSource>> GetSourcesAsync(CancellationToken cancellationToken) =>
        dbContext.DataSources.AsNoTracking().OrderBy(source => source.Id)
            .Select(source => new CatalogDataSource(source.Id, source.Name, source.Url, source.SourceType, source.Status,
                source.TimeBasis, source.SupportsUpdates))
            .ToListAsync(cancellationToken);

    public async Task<CatalogDataSource> SaveSourceAsync(long adminId, long? id, DataSourceInput input,
        CancellationToken cancellationToken)
    {
        var name = input.Name.Trim();
        var sourceType = input.SourceType.ToUpperInvariant();
        var url = input.Url?.Trim();
        if (!string.IsNullOrEmpty(url) && (!Uri.TryCreate(url, UriKind.Absolute, out var address) ||
            address.Scheme is not ("http" or "https") || string.IsNullOrEmpty(address.Host)))
        {
            throw new CatalogException(CatalogError.Invalid, "A data source URL must be an absolute HTTP or HTTPS URL.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({VanUcObservationDataset.ImportLockKey}));", cancellationToken);
        var source = id is { } existingId
            ? await dbContext.DataSources.FindAsync([existingId], cancellationToken) ??
              throw new CatalogException(CatalogError.NotFound, "Data source not found.")
            : new DataSource { Name = name, SourceType = sourceType, Status = "ACTIVE" };
        if (source.Name != name || !source.SourceType.Equals(sourceType, StringComparison.OrdinalIgnoreCase))
        {
            throw new CatalogException(CatalogError.Conflict, "The data source name and type identify the imported dataset.");
        }

        if (id is null)
        {
            if (await dbContext.DataSources.AnyAsync(existing =>
                existing.Name == name && existing.SourceType.ToUpper() == sourceType, cancellationToken))
            {
                throw new CatalogException(CatalogError.Conflict, "A data source with this name and type already exists.");
            }
            dbContext.DataSources.Add(source);
        }
        var timeBasis = input.TimeBasis?.Trim().ToUpperInvariant() ?? source.TimeBasis;
        var supportsUpdates = input.SupportsUpdates ?? source.SupportsUpdates;
        if (!ObservationMetadata.IsKnownTimeBasis(timeBasis) ||
            (name == VanUcObservationDataset.SourceName && sourceType == VanUcObservationDataset.SourceType &&
                timeBasis != VanUcObservationDataset.TimeBasis))
        {
            throw new CatalogException(CatalogError.Invalid, "The time basis is unsupported or would change the Văn Úc source clock.");
        }
        if (sourceType == VanUcObservationDataset.SourceType && supportsUpdates)
        {
            throw new CatalogException(CatalogError.Invalid, "Historical CSV is not an updating source.");
        }
        source.TimeBasis = timeBasis;
        source.SupportsUpdates = supportsUpdates;
        source.Url = string.IsNullOrEmpty(url) ? null : url;
        source.Status = input.IsActive ? "ACTIVE" : "INACTIVE";

        await SaveAsync(adminId, id is null ? "CREATE" : "UPDATE", "DATA_SOURCE", $"{source.SourceType}:{source.Name}",
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CatalogDataSource(source.Id, source.Name, source.Url, source.SourceType, source.Status,
            source.TimeBasis, source.SupportsUpdates);
    }

    public Task<List<CatalogMetric>> GetMetricsAsync(CancellationToken cancellationToken) =>
        dbContext.Metrics.AsNoTracking().OrderBy(metric => metric.Id)
            .Select(metric => new CatalogMetric(metric.Id, metric.Code, metric.Name, metric.QuantityType))
            .ToListAsync(cancellationToken);

    public async Task<CatalogMetric> SaveMetricAsync(long adminId, long? id, MetricInput input,
        CancellationToken cancellationToken)
    {
        var code = input.Code.ToLowerInvariant();
        var quantity = input.QuantityType.ToLowerInvariant();
        var metric = id is { } existingId
            ? await dbContext.Metrics.FindAsync([existingId], cancellationToken) ??
              throw new CatalogException(CatalogError.NotFound, "Metric not found.")
            : new Metric { Code = code, Name = "", QuantityType = quantity };
        if (!metric.Code.Equals(code, StringComparison.OrdinalIgnoreCase) ||
            !metric.QuantityType.Equals(quantity, StringComparison.OrdinalIgnoreCase))
        {
            throw new CatalogException(CatalogError.Conflict, "The metric code and quantity type cannot change.");
        }
        if (id is null &&
            ((code == VanUcObservationDataset.WaterLevelCode || code == VanUcObservationDataset.DischargeCode) &&
             quantity != code))
        {
            throw new CatalogException(CatalogError.Invalid, "The Văn Úc metric code must match its quantity type.");
        }

        metric.Name = input.Name.Trim();
        if (id is null)
        {
            dbContext.Metrics.Add(metric);
        }

        await SaveAsync(adminId, id is null ? "CREATE" : "UPDATE", "METRIC", metric.Code, cancellationToken);
        return new CatalogMetric(metric.Id, metric.Code, metric.Name, metric.QuantityType);
    }

    public Task<CatalogPage<CatalogObservation>> GetObservationsAsync(
        long? stationId, string? qualityStatus, int page, int pageSize, CancellationToken cancellationToken)
    {
        var status = qualityStatus?.Trim().ToUpperInvariant();
        if (status is not null && !ObservationQuality.IsKnown(status))
        {
            throw new CatalogException(CatalogError.Invalid, "Unknown observation quality status.");
        }
        return PageAsync(dbContext.Observations.AsNoTracking()
            .Where(observation => (stationId == null || observation.StationId == stationId) &&
                                  (status == null || observation.QualityStatus == status))
            .OrderByDescending(observation => observation.TimestampLocal).ThenByDescending(observation => observation.Id)
            .Select(observation => new CatalogObservation(observation.Id, observation.StationId, observation.MetricId,
                observation.SourceId, observation.TimestampLocal, observation.ValueRaw, observation.QualityStatus,
                observation.AvailableAt, observation.DataOrigin, observation.Unit, observation.IngestedAt)),
            page, pageSize, cancellationToken);
    }

    public async Task<CatalogObservation> SetObservationMetadataAsync(
        long adminId, long id, string qualityStatus, DateTimeOffset? availableAt, CancellationToken cancellationToken)
    {
        var status = qualityStatus.Trim().ToUpperInvariant();
        if (!ObservationQuality.IsKnown(status))
        {
            throw new CatalogException(CatalogError.Invalid, "Unknown observation quality status.");
        }
        var observation = await dbContext.Observations.FindAsync([id], cancellationToken) ??
                          throw new CatalogException(CatalogError.NotFound, "Observation not found.");
        observation.QualityStatus = status;
        observation.AvailableAt = availableAt?.ToUniversalTime();

        await SaveAsync(adminId, "UPDATE_METADATA", "OBSERVATION", id.ToString(CultureInfo.InvariantCulture),
            cancellationToken);
        return new CatalogObservation(observation.Id, observation.StationId, observation.MetricId,
            observation.SourceId, observation.TimestampLocal, observation.ValueRaw, observation.QualityStatus,
            observation.AvailableAt, observation.DataOrigin, observation.Unit, observation.IngestedAt);
    }

    public Task<CatalogPage<CatalogActionView>> GetActionsAsync(
        string? resourceType, int page, int pageSize, CancellationToken cancellationToken)
    {
        var filter = resourceType?.Trim().ToUpperInvariant();
        return PageAsync(dbContext.CatalogActions.AsNoTracking().Where(action =>
                filter == null || action.ResourceType == filter)
            .OrderByDescending(action => action.CreatedAt).ThenByDescending(action => action.Id)
            .Select(action => new CatalogActionView(action.Id, action.AdminId, action.Action, action.ResourceType,
                action.ResourceKey, action.CreatedAt)), page, pageSize, cancellationToken);
    }

    private Task SaveAsync(long adminId, string action, string type, string key, CancellationToken cancellationToken)
    {
        CatalogAudit.Record(dbContext, adminId, action, type, key);
        return CatalogAudit.SaveAsync(dbContext, cancellationToken);
    }

    private static async Task<CatalogPage<T>> PageAsync<T>(
        IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasNext = items.Count > pageSize;
        if (hasNext)
        {
            items.RemoveAt(pageSize);
        }
        return new CatalogPage<T>(page, pageSize, hasNext, items);
    }
}
