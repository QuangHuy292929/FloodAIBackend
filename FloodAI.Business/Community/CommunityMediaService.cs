using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Community;

public sealed class CommunityMediaService(
    FloodAiDbContext dbContext, CommunityPostService posts, ICommunityMediaStore storage)
{
    public async Task<(CommunityMedia? Media, bool LimitReached)> AddAsync(
        long userId, long postId, string mediaType, Stream content, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var post = await posts.LockOwnedPostAsync(userId, postId, cancellationToken);
        if (post is null)
        {
            return (null, false);
        }

        var slots = await dbContext.PostMedia.Where(media => media.PostId == postId)
            .Select(media => media.SortOrder).ToListAsync(cancellationToken);
        if (slots.Count >= CommunityRules.MaxImagesPerPost)
        {
            return (null, true);
        }

        var sortOrder = Enumerable.Range(0, CommunityRules.MaxImagesPerPost).First(slot => !slots.Contains(slot));
        var media = new PostMedia
        {
            PostId = post.Id,
            MediaType = mediaType,
            SortOrder = sortOrder,
            StorageKey = Guid.NewGuid().ToString("N")
        };
        await storage.SaveAsync(media.StorageKey, content, cancellationToken);
        dbContext.PostMedia.Add(media);
        CommunityRules.RequireReview(post);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            storage.Delete(media.StorageKey);
            throw;
        }

        await transaction.CommitAsync(cancellationToken);
        return (new CommunityMedia(media.Id, media.MediaType, media.SortOrder), false);
    }

    public async Task<(Stream Content, string MediaType)?> OpenAsync(
        long postId, long mediaId, long? viewerId, bool isAdmin, CancellationToken cancellationToken)
    {
        var media = await dbContext.PostMedia.AsNoTracking().Where(media =>
                media.Id == mediaId && media.PostId == postId && media.Post.DeletedAt == null &&
                (isAdmin || media.Post.AuthorId == viewerId ||
                 CommunityQueries.PublishedPosts(dbContext).Any(post => post.Id == media.PostId)))
            .Select(media => new { media.StorageKey, media.MediaType }).SingleOrDefaultAsync(cancellationToken);
        if (media is null || media.MediaType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            return null;
        }
        return storage.OpenRead(media.StorageKey) is { } content ? (content, media.MediaType) : null;
    }

    public async Task<bool> DeleteAsync(long userId, long postId, long mediaId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var post = await posts.LockOwnedPostAsync(userId, postId, cancellationToken);
        if (post is null)
        {
            return false;
        }

        var media = await dbContext.PostMedia.SingleOrDefaultAsync(media => media.PostId == postId && media.Id == mediaId,
            cancellationToken);
        if (media is null)
        {
            return false;
        }
        dbContext.PostMedia.Remove(media);
        CommunityRules.RequireReview(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        storage.Delete(media.StorageKey);
        return true;
    }
}
