using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Community;

public sealed class CommunityCommentsService(FloodAiDbContext dbContext)
{
    public async Task<CommunityPage<CommunityCommentView>?> GetPublishedAsync(
        long postId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await CommunityQueries.PublishedPosts(dbContext).AnyAsync(post => post.Id == postId, cancellationToken))
        {
            return null;
        }
        var query = CommunityQueries.PublishedComments(dbContext).Where(comment => comment.PostId == postId);
        return await Views(query.OrderBy(comment => comment.CreatedAt).ThenBy(comment => comment.Id))
            .PageAsync(page, pageSize, cancellationToken);
    }

    public Task<CommunityPage<CommunityCommentView>> GetMineAsync(long userId, int page, int pageSize,
        CancellationToken cancellationToken) =>
        Views(OwnComments(userId).OrderByDescending(comment => comment.CreatedAt).ThenByDescending(comment => comment.Id))
            .PageAsync(page, pageSize, cancellationToken);

    public Task<CommunityCommentView?> GetMineAsync(long userId, long id, CancellationToken cancellationToken) =>
        Views(OwnComments(userId).Where(comment => comment.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<CommunityCommentView?> CreateAsync(long userId, long postId, string content,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var published = await dbContext.CommunityPosts.FromSqlInterpolated($"""
            SELECT * FROM community_posts WHERE id = {postId} FOR UPDATE
            """).AsNoTracking().AnyAsync(post => post.DeletedAt == null && post.PublishedAt != null &&
            post.ModerationStatus == CommunityRules.Approved && post.Area.IsActive, cancellationToken);
        if (!published)
        {
            return null;
        }

        var comment = new PostComment
        {
            PostId = postId,
            AuthorId = userId,
            Content = content.Trim(),
            ModerationStatus = CommunityRules.Pending
        };
        dbContext.PostComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        var view = await GetMineAsync(userId, comment.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return view;
    }

    public async Task<bool> EditAsync(long userId, long id, string content, CancellationToken cancellationToken) =>
        await OwnComments(userId).Where(comment => comment.Id == id).ExecuteUpdateAsync(updates => updates
            .SetProperty(comment => comment.Content, content.Trim())
            .SetProperty(comment => comment.ModerationStatus, CommunityRules.Pending)
            .SetProperty(comment => comment.PublishedAt, (DateTimeOffset?)null), cancellationToken) == 1;

    public async Task<bool> DeleteAsync(long userId, long id, CancellationToken cancellationToken) =>
        await OwnComments(userId).Where(comment => comment.Id == id).ExecuteUpdateAsync(updates => updates
            .SetProperty(comment => comment.DeletedAt, DateTimeOffset.UtcNow), cancellationToken) == 1;

    public async Task<CommunityReportView?> ReportAsync(long userId, long? postId, long? commentId, string reason,
        CancellationToken cancellationToken)
    {
        var isPublic = postId is { } id
            ? await CommunityQueries.PublishedPosts(dbContext).AnyAsync(post => post.Id == id, cancellationToken)
            : await CommunityQueries.PublishedComments(dbContext).AnyAsync(comment => comment.Id == commentId,
                cancellationToken);
        if (!isPublic)
        {
            return null;
        }

        var report = new ContentReport
        {
            ReporterId = userId,
            PostId = postId,
            CommentId = commentId,
            Reason = reason.Trim(),
            Status = CommunityRules.Pending
        };
        dbContext.ContentReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CommunityReportView(report.Id, report.Status);
    }

    private IQueryable<PostComment> OwnComments(long userId) =>
        dbContext.PostComments.AsNoTracking().Where(comment => comment.AuthorId == userId &&
            comment.DeletedAt == null && comment.Post.DeletedAt == null);

    private IQueryable<CommunityCommentView> Views(IQueryable<PostComment> comments) =>
        comments.Select(comment => new CommunityCommentView(
            comment.Id, comment.PostId, new CommunityAuthor(comment.AuthorId, comment.Author.DisplayName),
            comment.Content, comment.ModerationStatus, comment.CreatedAt, comment.PublishedAt,
            comment.ModerationStatus == CommunityRules.Rejected
                ? dbContext.ModerationActions.Where(action => action.CommentId == comment.Id &&
                    action.Action == CommunityRules.RejectionAction).OrderByDescending(action => action.CreatedAt)
                    .ThenByDescending(action => action.Id).Select(action => action.Reason).FirstOrDefault()
                : null));
}
