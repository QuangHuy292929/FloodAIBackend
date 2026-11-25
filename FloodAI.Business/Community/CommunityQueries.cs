using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Community;

public static class CommunityRules
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Hidden = "HIDDEN";
    public const int MaxImagesPerPost = 5;

    internal const string RejectionAction = "REJECT";
    internal const string HideAction = "HIDE";

    public static bool IsPostType(string? type) =>
        type is "FLOOD_REPORT" or "WEATHER_UPDATE" or "DISCUSSION" or "OTHER";

    internal static void RequireReview(CommunityPost post)
    {
        post.ModerationStatus = Pending;
        post.PublishedAt = null;
    }
}

internal static class CommunityQueries
{
    internal static IQueryable<CommunityPost> PublishedPosts(FloodAiDbContext dbContext) =>
        dbContext.CommunityPosts.AsNoTracking().Where(post => post.DeletedAt == null &&
            post.ModerationStatus == CommunityRules.Approved && post.PublishedAt != null && post.Area.IsActive);

    internal static IQueryable<PostComment> PublishedComments(FloodAiDbContext dbContext) =>
        dbContext.PostComments.AsNoTracking().Where(comment => comment.DeletedAt == null &&
            comment.ModerationStatus == CommunityRules.Approved && comment.PublishedAt != null &&
            PublishedPosts(dbContext).Any(post => post.Id == comment.PostId));

    internal static async Task<CommunityPage<T>> PageAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasNext = items.Count > pageSize;
        if (hasNext)
        {
            items.RemoveAt(pageSize);
        }
        return new CommunityPage<T>(page, pageSize, hasNext, items);
    }
}
