using FloodAI.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Catalog;

public sealed class HydrologyCatalogService(FloodAiDbContext dbContext)
{
    public Task<List<BasinSummary>> GetBasinsAsync(CancellationToken cancellationToken)
    {
        return dbContext.Basins
            .AsNoTracking()
            .Where(basin => basin.IsActive)
            .OrderBy(basin => basin.Name)
            .ThenBy(basin => basin.Id)
            .Select(basin => new BasinSummary(basin.Id, basin.Code, basin.Name, basin.Description))
            .ToListAsync(cancellationToken);
    }

    public Task<List<StationSummary>?> GetStationsAsync(long basinId, CancellationToken cancellationToken)
    {
        return dbContext.Basins
            .AsNoTracking()
            .Where(basin => basin.Id == basinId && basin.IsActive)
            .Select(basin => basin.Stations
                .OrderBy(station => station.Name)
                .ThenBy(station => station.Id)
                .Select(station => new StationSummary(
                    station.Id, station.Code, station.Name, station.Type, station.Latitude, station.Longitude))
                .ToList())
            .SingleOrDefaultAsync(cancellationToken);
    }
}
