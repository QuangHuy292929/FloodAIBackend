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

    public Task<List<RiverBranchSummary>?> GetBranchesAsync(long basinId, CancellationToken cancellationToken) =>
        dbContext.Basins.AsNoTracking().Where(basin => basin.Id == basinId && basin.IsActive)
            .Select(basin => basin.RiverBranches.OrderBy(branch => branch.Name).ThenBy(branch => branch.Id)
                .Select(branch => new RiverBranchSummary(branch.Id, branch.BasinId, branch.Code, branch.Name)).ToList())
            .SingleOrDefaultAsync(cancellationToken);

    public Task<List<StationSummary>?> GetStationsAsync(long basinId, long? branchId, CancellationToken cancellationToken)
    {
        return dbContext.Basins
            .AsNoTracking()
            .Where(basin => basin.Id == basinId && basin.IsActive &&
                (branchId == null || basin.RiverBranches.Any(branch => branch.Id == branchId)))
            .Select(basin => basin.Stations
                .Where(station => branchId == null || station.RiverBranchId == branchId)
                .OrderBy(station => station.Name)
                .ThenBy(station => station.Id)
                .Select(station => new StationSummary(
                    station.Id, station.Code, station.Name, station.Type, station.Latitude, station.Longitude,
                    station.RiverBranchId,
                    station.UpdateSource != null && station.UpdateSource.SupportsUpdates &&
                    station.UpdateSource.Status == "ACTIVE" ? "CONFIGURED" : "NOT_CONNECTED"))
                .ToList())
            .SingleOrDefaultAsync(cancellationToken);
    }
}
