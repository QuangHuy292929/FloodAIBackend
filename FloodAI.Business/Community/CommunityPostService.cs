using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Community;

public sealed class CommunityPostService(FloodAiDbContext dbContext)
{
    public Task<List<CommunityArea>> GetAreasAsync(CancellationToken cancellationToken) =>
        dbContext.Areas.AsNoTracking().Where(area => area.IsActive)
            .OrderBy(area => area.Name).ThenBy(area => area.Id)
            .Select(area => new CommunityArea(area.Id, area.ParentId, area.Code, area.Name, area.Level))
            .ToListAsync(cancellationToken);

    public async Task<CommunityPage<CommunityPostView>?> GetFeedAsync(
        long areaId, string? type, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var visibleAreaIds = await GetVisibleAreaIdsAsync([areaId], cancellationToken);
        if (visibleAreaIds.Count == 0)
        {
            return null;
        }

        var query = CommunityQueries.PublishedPosts(dbContext).Where(post => visibleAreaIds.Contains(post.AreaId));
        if (type is not null)
        {
            query = query.Where(post => post.PostType == type);
        }
        if (from is not null)
        {
            query = query.Where(post => post.PublishedAt >= from);
        }
        if (to is not null)
        {
            query = query.Where(post => post.PublishedAt < to);
        }

        return await ReadFeedPageAsync(query, page, pageSize, cancellationToken);
    }

    internal async Task<CommunityPage<CommunityPostView>> GetFollowedFeedAsync(
        IReadOnlyCollection<long> areaIds, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (areaIds.Count == 0)
        {
            return new CommunityPage<CommunityPostView>(page, pageSize, false, []);
        }

        var visibleAreaIds = await GetVisibleAreaIdsAsync(areaIds, cancellationToken);
        var query = CommunityQueries.PublishedPosts(dbContext).Where(post => visibleAreaIds.Contains(post.AreaId));
        return await ReadFeedPageAsync(query, page, pageSize, cancellationToken);
    }

    private async Task<HashSet<long>> GetVisibleAreaIdsAsync(
        IReadOnlyCollection<long> rootIds, CancellationToken cancellationToken)
    {
        var links = await dbContext.Areas.AsNoTracking().Where(area => area.IsActive)
            .Select(area => new { area.Id, area.ParentId }).ToListAsync(cancellationToken);

        var children = links.ToLookup(area => area.ParentId);
        var visible = links.Where(area => rootIds.Contains(area.Id)).Select(area => area.Id).ToHashSet();
        var queue = new Queue<long>(visible);
        while (queue.TryDequeue(out var parentId))
        {
            foreach (var child in children[parentId])
            {
                if (visible.Add(child.Id))
                {
                    queue.Enqueue(child.Id);
                }
            }
        }
        return visible;
    }

    private Task<CommunityPage<CommunityPostView>> ReadFeedPageAsync(
        IQueryable<CommunityPost> query, int page, int pageSize, CancellationToken cancellationToken) =>
        Views(query.OrderByDescending(post => post.PublishedAt).ThenByDescending(post => post.Id))
            .PageAsync(page, pageSize, cancellationToken);

    public Task<CommunityPostView?> GetPublishedAsync(long id, CancellationToken cancellationToken) =>
        Views(CommunityQueries.PublishedPosts(dbContext).Where(post => post.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CommunityPostView?> GetMineAsync(long userId, long postId, CancellationToken cancellationToken) =>
        Views(OwnPosts(userId).Where(post => post.Id == postId)).SingleOrDefaultAsync(cancellationToken);

    public Task<CommunityPage<CommunityPostView>> ListMineAsync(long userId, int page, int pageSize,
        CancellationToken cancellationToken) =>
        Views(OwnPosts(userId).OrderByDescending(post => post.Id)).PageAsync(page, pageSize, cancellationToken);

    public async Task<string?> ValidateLocationAsync(long? areaId, long? stationId, CancellationToken cancellationToken)
    {
        if (areaId is { } selectedArea && !await dbContext.Areas.AnyAsync(
            area => area.Id == selectedArea && area.IsActive, cancellationToken))
        {
            return "areaId";
        }
        if (stationId is { } id && !await dbContext.Stations.AnyAsync(station =>
            station.Id == id && station.Basin.IsActive, cancellationToken))
        {
            return "stationId";
        }
        return null;
    }

    public async Task<CommunityPostView> CreateAsync(long userId, CommunityPostInput input,
        CancellationToken cancellationToken)
    {
        var post = new CommunityPost { AuthorId = userId, Title = input.Title, Content = input.Content, PostType = input.PostType };
        SetFields(post, input);
        CommunityRules.RequireReview(post);
        dbContext.CommunityPosts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetMineAsync(userId, post.Id, cancellationToken))!;
    }

    public async Task<CommunityPostView?> EditAsync(long userId, long postId, CommunityPostChanges changes,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var post = await LockOwnedPostAsync(userId, postId, cancellationToken);
        if (post is null)
        {
            return null;
        }
        post.AreaId = changes.AreaId ?? post.AreaId;
        if (changes.SetStationId)
        {
            post.StationId = changes.StationId;
        }
        post.Title = changes.Title?.Trim() ?? post.Title;
        post.Content = changes.Content?.Trim() ?? post.Content;
        post.PostType = changes.PostType ?? post.PostType;
        post.ObservedAt = changes.ObservedAt ?? post.ObservedAt;
        CommunityRules.RequireReview(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await GetMineAsync(userId, post.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> DeleteAsync(long userId, long postId, CancellationToken cancellationToken) =>
        await dbContext.CommunityPosts.Where(post => post.Id == postId && post.AuthorId == userId && post.DeletedAt == null)
            .ExecuteUpdateAsync(updates => updates.SetProperty(post => post.DeletedAt, DateTimeOffset.UtcNow),
                cancellationToken) == 1;

    internal Task<CommunityPost?> LockOwnedPostAsync(long userId, long postId, CancellationToken cancellationToken) =>
        dbContext.CommunityPosts.FromSqlInterpolated($"""
            SELECT * FROM community_posts WHERE id = {postId} AND author_id = {userId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);

    private IQueryable<CommunityPost> OwnPosts(long userId) =>
        dbContext.CommunityPosts.AsNoTracking().Where(post => post.AuthorId == userId && post.DeletedAt == null);

    private IQueryable<CommunityPostView> Views(IQueryable<CommunityPost> posts) =>
        posts.Select(post => new CommunityPostView(
            post.Id, post.AreaId, post.StationId, new CommunityAuthor(post.AuthorId, post.Author.DisplayName),
            post.Title, post.Content, post.PostType, post.ObservedAt, post.ModerationStatus, post.PublishedAt,
            post.ModerationStatus == CommunityRules.Rejected || post.ModerationStatus == CommunityRules.Hidden
                ? dbContext.ModerationActions.Where(action => action.PostId == post.Id &&
                    action.Action == (post.ModerationStatus == CommunityRules.Hidden
                        ? CommunityRules.HideAction : CommunityRules.RejectionAction))
                    .OrderByDescending(action => action.CreatedAt)
                    .ThenByDescending(action => action.Id).Select(action => action.Reason).FirstOrDefault()
                : null,
            post.Media.OrderBy(media => media.SortOrder).ThenBy(media => media.Id)
                .Select(media => new CommunityMedia(media.Id, media.MediaType, media.SortOrder)).ToList()));

    private static void SetFields(CommunityPost post, CommunityPostInput input)
    {
        post.AreaId = input.AreaId;
        post.StationId = input.StationId;
        post.Title = input.Title.Trim();
        post.Content = input.Content.Trim();
        post.PostType = input.PostType;
        post.ObservedAt = input.ObservedAt;
    }
}
