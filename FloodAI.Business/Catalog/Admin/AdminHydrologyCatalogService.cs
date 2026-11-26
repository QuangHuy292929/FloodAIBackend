using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Catalog.Admin;

public sealed class AdminHydrologyCatalogService(FloodAiDbContext dbContext)
{
    public Task<List<CatalogBasin>> GetBasinsAsync(CancellationToken cancellationToken) =>
        dbContext.Basins.AsNoTracking().OrderBy(basin => basin.Id)
            .Select(basin => new CatalogBasin(basin.Id, basin.Code, basin.Name, basin.Description, basin.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<CatalogBasin> SaveBasinAsync(long adminId, long? id, BasinInput input,
        CancellationToken cancellationToken)
    {
        var code = input.Code.ToLowerInvariant();
        var basin = id is { } existingId
            ? await dbContext.Basins.FindAsync([existingId], cancellationToken) ??
              throw new CatalogException(CatalogError.NotFound, "Basin not found.")
            : new Basin { Code = code, Name = "" };
        if (!basin.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
        {
            throw new CatalogException(CatalogError.Conflict, "The basin code is an immutable catalog key.");
        }
        basin.Name = input.Name.Trim();
        basin.Description = input.Description?.Trim();
        basin.IsActive = input.IsActive;
        if (id is null)
        {
            dbContext.Basins.Add(basin);
        }
        await SaveAsync(adminId, id, "BASIN", basin.Code, cancellationToken);
        return new CatalogBasin(basin.Id, basin.Code, basin.Name, basin.Description, basin.IsActive);
    }

    public Task<List<CatalogArea>> GetAreasAsync(CancellationToken cancellationToken) =>
        dbContext.Areas.AsNoTracking().OrderBy(area => area.Id)
            .Select(area => new CatalogArea(area.Id, area.ParentId, area.Code, area.Name, area.Level,
                area.IsActive, area.WeatherLatitude, area.WeatherLongitude))
            .ToListAsync(cancellationToken);

    public async Task<CatalogArea> SaveAreaAsync(long adminId, long? id, AreaInput input,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(hashtextextended('floodai:catalog:areas', 0));", cancellationToken);

        var code = input.Code.ToLowerInvariant();
        var area = id is { } existingId
            ? await dbContext.Areas.FindAsync([existingId], cancellationToken) ??
              throw new CatalogException(CatalogError.NotFound, "Area not found.")
            : new Area { Code = code, Name = "", Level = "" };
        if (!area.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
        {
            throw new CatalogException(CatalogError.Conflict, "The area code is an immutable catalog key.");
        }
        if (input.ParentId is { } parentId)
        {
            await EnsureValidParentAsync(id, parentId, cancellationToken);
        }

        area.ParentId = input.ParentId;
        area.Name = input.Name.Trim();
        area.Level = input.Level.Trim();
        area.IsActive = input.IsActive;
        if (id is null)
        {
            dbContext.Areas.Add(area);
        }
        await SaveAsync(adminId, id, "AREA", area.Code, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToCatalogArea(area);
    }

    public async Task<CatalogArea> SetWeatherLocationAsync(long adminId, long id, double? latitude,
        double? longitude, CancellationToken cancellationToken)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            throw new CatalogException(CatalogError.Invalid, "Provide both coordinates or set both to null.");
        }

        var area = await dbContext.Areas.FindAsync([id], cancellationToken) ??
                   throw new CatalogException(CatalogError.NotFound, "Area not found.");
        area.WeatherLatitude = latitude;
        area.WeatherLongitude = longitude;
        CatalogAudit.Record(dbContext, adminId, "UPDATE_WEATHER_LOCATION", "AREA", area.Code);
        await CatalogAudit.SaveAsync(dbContext, cancellationToken);
        return ToCatalogArea(area);
    }

    private static CatalogArea ToCatalogArea(Area area) =>
        new(area.Id, area.ParentId, area.Code, area.Name, area.Level, area.IsActive,
            area.WeatherLatitude, area.WeatherLongitude);

    public Task<List<CatalogStation>> GetStationsAsync(CancellationToken cancellationToken) =>
        dbContext.Stations.AsNoTracking().OrderBy(station => station.Id)
            .Select(station => new CatalogStation(station.Id, station.BasinId, station.AreaId, station.Code,
                station.Name, station.Type, station.Latitude, station.Longitude))
            .ToListAsync(cancellationToken);

    public async Task<CatalogStation> SaveStationAsync(long adminId, long? id, StationInput input,
        CancellationToken cancellationToken)
    {
        var code = input.Code.ToLowerInvariant();
        var station = id is { } existingId
            ? await dbContext.Stations.FindAsync([existingId], cancellationToken) ??
              throw new CatalogException(CatalogError.NotFound, "Station not found.")
            : new Station { Code = code, BasinId = input.BasinId, Name = "", Type = "" };
        if (!station.Code.Equals(code, StringComparison.OrdinalIgnoreCase) || station.BasinId != input.BasinId)
        {
            throw new CatalogException(CatalogError.Conflict, "The station cannot change its code or basin.");
        }
        if (!await dbContext.Basins.AnyAsync(basin => basin.Id == input.BasinId, cancellationToken) ||
            !await dbContext.Areas.AnyAsync(area => area.Id == input.AreaId, cancellationToken))
        {
            throw new CatalogException(CatalogError.Invalid, "The station needs an existing basin and area.");
        }
        station.AreaId = input.AreaId;
        station.Name = input.Name.Trim();
        station.Type = input.Type.Trim();
        station.Latitude = input.Latitude;
        station.Longitude = input.Longitude;
        if (id is null)
        {
            dbContext.Stations.Add(station);
        }

        await SaveAsync(adminId, id, "STATION", FormattableString.Invariant($"{station.BasinId}:{station.Code}"),
            cancellationToken);
        return new CatalogStation(station.Id, station.BasinId, station.AreaId, station.Code,
            station.Name, station.Type, station.Latitude, station.Longitude);
    }

    private async Task EnsureValidParentAsync(long? areaId, long parentId, CancellationToken cancellationToken)
    {
        var links = await dbContext.Areas.AsNoTracking()
            .ToDictionaryAsync(area => area.Id, area => area.ParentId, cancellationToken);
        var visited = new HashSet<long>();
        long? current = parentId;
        while (current is { } id)
        {
            if (id == areaId || !visited.Add(id))
            {
                throw new CatalogException(CatalogError.Conflict, "The selected parent would create an area cycle.");
            }
            if (!links.TryGetValue(id, out current))
            {
                throw new CatalogException(CatalogError.Invalid, "The parent area does not exist.");
            }
        }
    }

    private Task SaveAsync(long adminId, long? id, string type, string key, CancellationToken cancellationToken)
    {
        CatalogAudit.Record(dbContext, adminId, id is null ? "CREATE" : "UPDATE", type, key);
        return CatalogAudit.SaveAsync(dbContext, cancellationToken);
    }
}
