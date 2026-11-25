using FloodAI.Business.Community;
using FloodAI.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Follows;

public sealed class FollowService(FloodAiDbContext dbContext, CommunityPostService communityPosts)
{
    public Task<List<FollowedArea>> GetAreasAsync(long userId, CancellationToken cancellationToken) =>
        dbContext.UserAreaFollows.AsNoTracking()
            .Where(follow => follow.UserId == userId && follow.Area.IsActive)
            .OrderByDescending(follow => follow.CreatedAt).ThenBy(follow => follow.AreaId)
            .Select(follow => new FollowedArea(follow.AreaId, follow.Area.ParentId,
                follow.Area.Code, follow.Area.Name, follow.Area.Level, follow.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<List<FollowedStation>> GetStationsAsync(long userId, CancellationToken cancellationToken) =>
        dbContext.UserStationFollows.AsNoTracking()
            .Where(follow => follow.UserId == userId && follow.Station.Basin.IsActive)
            .OrderByDescending(follow => follow.CreatedAt).ThenBy(follow => follow.StationId)
            .Select(follow => new FollowedStation(follow.StationId, follow.Station.BasinId,
                follow.Station.Code, follow.Station.Name, follow.Station.Type,
                follow.Station.Latitude, follow.Station.Longitude, follow.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<CommunityPage<CommunityPostView>> GetAreaFeedAsync(
        long userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var areaIds = await dbContext.UserAreaFollows.AsNoTracking()
            .Where(follow => follow.UserId == userId && follow.Area.IsActive)
            .Select(follow => follow.AreaId).ToListAsync(cancellationToken);

        return await communityPosts.GetFollowedFeedAsync(areaIds, page, pageSize, cancellationToken);
    }

    public Task<bool> FollowAreaAsync(long userId, long areaId, CancellationToken cancellationToken) =>
        InsertFollowAsync($"""
            WITH target AS (
                SELECT id FROM areas WHERE id = {areaId} AND is_active
            ), inserted AS (
                INSERT INTO user_area_follows (user_id, area_id)
                SELECT {userId}, id FROM target
                ON CONFLICT (user_id, area_id) DO NOTHING
            )
            SELECT EXISTS (SELECT 1 FROM target) AS "Value"
            """, cancellationToken);

    public Task<bool> FollowStationAsync(long userId, long stationId, CancellationToken cancellationToken) =>
        InsertFollowAsync($"""
            WITH target AS (
                SELECT s.id FROM stations AS s JOIN basins AS b ON b.id = s.basin_id
                WHERE s.id = {stationId} AND b.is_active
            ), inserted AS (
                INSERT INTO user_station_follows (user_id, station_id)
                SELECT {userId}, id FROM target
                ON CONFLICT (user_id, station_id) DO NOTHING
            )
            SELECT EXISTS (SELECT 1 FROM target) AS "Value"
            """, cancellationToken);

    public Task UnfollowAreaAsync(long userId, long areaId, CancellationToken cancellationToken) =>
        dbContext.UserAreaFollows.Where(follow => follow.UserId == userId && follow.AreaId == areaId)
            .ExecuteDeleteAsync(cancellationToken);

    public Task UnfollowStationAsync(long userId, long stationId, CancellationToken cancellationToken) =>
        dbContext.UserStationFollows.Where(follow => follow.UserId == userId && follow.StationId == stationId)
            .ExecuteDeleteAsync(cancellationToken);

    private async Task<bool> InsertFollowAsync(FormattableString sql, CancellationToken cancellationToken)
    {
        // Keep the data-modifying CTE at the top level by materializing before taking its scalar result.
        var result = await dbContext.Database.SqlQuery<bool>(sql).ToListAsync(cancellationToken);
        return result.Single();
    }
}
