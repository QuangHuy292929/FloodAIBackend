using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using static FloodAI.DataAccess.Observations.VanUcObservationDataset;

namespace FloodAI.DataAccess.Observations;

public sealed class VanUcObservationStore(FloodAiDbContext dbContext)
{
    public async Task<int> ImportAsync(
        IReadOnlyList<VanUcHourlyRow> rows,
        long trungTrangId,
        long honDauId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(hashtext('floodai:van_uc_hourly:2021-2022'));", cancellationToken);

        var stationCount = await dbContext.Stations.CountAsync(station =>
            (station.Id == trungTrangId || station.Id == honDauId) &&
            station.Basin.Code == BasinCode && station.Basin.IsActive, cancellationToken);
        if (stationCount != 2)
        {
            throw new InvalidOperationException(
                "Trung Trang and Hòn Dấu must be two registered stations in the active van_uc basin.");
        }

        var sourceId = await GetOrCreateSourceAsync(cancellationToken);
        var metricIds = await GetOrCreateMetricsAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TEMP TABLE van_uc_observation_import (
                station_id bigint NOT NULL,
                metric_id bigint NOT NULL,
                timestamp_local timestamp without time zone NOT NULL,
                value_raw double precision NOT NULL,
                PRIMARY KEY (station_id, metric_id, timestamp_local)
            ) ON COMMIT DROP;
            """, cancellationToken);

        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        await using (var writer = await connection.BeginBinaryImportAsync("""
            COPY pg_temp.van_uc_observation_import (station_id, metric_id, timestamp_local, value_raw)
            FROM STDIN (FORMAT BINARY)
            """, cancellationToken))
        {
            foreach (var row in rows)
            {
                await WriteObservationAsync(writer, trungTrangId, metricIds[WaterLevelCode],
                    row.TimestampLocal, row.TrungTrangWaterLevel, cancellationToken);
                await WriteObservationAsync(writer, honDauId, metricIds[WaterLevelCode],
                    row.TimestampLocal, row.HonDauWaterLevel, cancellationToken);
                await WriteObservationAsync(writer, trungTrangId, metricIds[DischargeCode],
                    row.TimestampLocal, row.TrungTrangDischarge, cancellationToken);
            }

            await writer.CompleteAsync(cancellationToken);
        }

        var inserted = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO observations (station_id, metric_id, source_id, timestamp_local, value_raw, quality_status)
            SELECT station_id, metric_id, {sourceId}, timestamp_local, value_raw, 'SOURCE_UNVERIFIED'
            FROM pg_temp.van_uc_observation_import
            ON CONFLICT (station_id, metric_id, source_id, timestamp_local) DO NOTHING;
            """, cancellationToken);

        var hasDifferentValues = await dbContext.Database.SqlQuery<int>($"""
            SELECT 1 AS "Value"
            FROM pg_temp.van_uc_observation_import AS incoming
            JOIN observations AS stored
              ON stored.station_id = incoming.station_id
             AND stored.metric_id = incoming.metric_id
             AND stored.timestamp_local = incoming.timestamp_local
             AND stored.source_id = {sourceId}
            WHERE stored.value_raw IS DISTINCT FROM incoming.value_raw
            """).AnyAsync(cancellationToken);
        if (hasDifferentValues)
        {
            throw new InvalidOperationException(
                "The database already contains different values for this source and hour. The import was rolled back.");
        }

        await transaction.CommitAsync(cancellationToken);
        return inserted;
    }

    private async Task<long> GetOrCreateSourceAsync(CancellationToken cancellationToken)
    {
        var sources = await dbContext.DataSources
            .Where(source => source.Name == SourceName && source.SourceType == SourceType)
            .OrderBy(source => source.Id)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (sources.Count > 1 || (sources.Count == 1 && sources[0].Status != "ACTIVE"))
        {
            throw new InvalidOperationException("The Văn Úc source has multiple entries or is inactive.");
        }

        if (sources.Count == 1)
        {
            return sources[0].Id;
        }

        var source = new DataSource { Name = SourceName, SourceType = SourceType, Status = "ACTIVE" };
        dbContext.DataSources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);
        return source.Id;
    }

    private async Task<Dictionary<string, long>> GetOrCreateMetricsAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO metrics (code, name, quantity_type)
            VALUES ({WaterLevelCode}, {"Mực nước"}, {WaterLevelCode}),
                   ({DischargeCode}, {"Lưu lượng"}, {DischargeCode})
            ON CONFLICT (code) DO NOTHING;
            """, cancellationToken);

        var metrics = await dbContext.Metrics.AsNoTracking()
            .Where(metric => metric.Code == WaterLevelCode || metric.Code == DischargeCode)
            .ToListAsync(cancellationToken);
        if (metrics.Count != 2 || metrics.Any(metric => metric.QuantityType != metric.Code))
        {
            throw new InvalidOperationException("The observation metric codes are assigned to different quantity types.");
        }

        return metrics.ToDictionary(metric => metric.Code, metric => metric.Id);
    }

    private static async Task WriteObservationAsync(
        NpgsqlBinaryImporter writer, long stationId, long metricId,
        DateTime timestamp, double value, CancellationToken cancellationToken)
    {
        await writer.StartRowAsync(cancellationToken);
        await writer.WriteAsync(stationId, NpgsqlDbType.Bigint, cancellationToken);
        await writer.WriteAsync(metricId, NpgsqlDbType.Bigint, cancellationToken);
        await writer.WriteAsync(timestamp, NpgsqlDbType.Timestamp, cancellationToken);
        await writer.WriteAsync(value, NpgsqlDbType.Double, cancellationToken);
    }
}
