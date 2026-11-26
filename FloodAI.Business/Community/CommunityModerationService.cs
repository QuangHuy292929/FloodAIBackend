using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FloodAI.Business.Community;

public sealed class CommunityModerationService(FloodAiDbContext dbContext)
{
    public Task<CommunityPage<ModerationQueueItem>> GetQueueAsync(
        bool comments, int page, int pageSize, CancellationToken cancellationToken) =>
        (comments ? PendingComments() : PendingPosts()).PageAsync(page, pageSize, cancellationToken);

    public Task<CommunityPage<ModerationHistoryItem>> GetHistoryAsync(
        long? postId, long? commentId, int page, int pageSize, CancellationToken cancellationToken) =>
        dbContext.ModerationActions.AsNoTracking()
            .Where(action => (postId == null || action.PostId == postId) &&
                             (commentId == null || action.CommentId == commentId))
            .OrderByDescending(action => action.CreatedAt).ThenByDescending(action => action.Id)
            .Select(action => new ModerationHistoryItem(action.Id, action.AdminId, action.PostId, action.CommentId,
                action.Action, action.Reason, action.CreatedAt))
            .PageAsync(page, pageSize, cancellationToken);

    public async Task<ModerationResult> DecidePostAsync(long adminId, long postId, ModerationDecision decision,
        string? reason, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var post = await dbContext.CommunityPosts.FromSqlInterpolated($"""
            SELECT * FROM community_posts WHERE id = {postId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (post is null)
        {
            return ModerationResult.NotFound;
        }

        var state = NextState(post.ModerationStatus, decision);
        if (state is null || (decision == ModerationDecision.Approve &&
            !await dbContext.Areas.AnyAsync(area => area.Id == post.AreaId && area.IsActive, cancellationToken)))
        {
            return ModerationResult.Conflict;
        }

        post.ModerationStatus = state;
        post.PublishedAt = PublicationTime(state);
        await SaveDecisionAsync(adminId, post.Id, null, decision, reason, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ModerationResult.Applied;
    }

    public async Task<ModerationResult> DecideCommentAsync(long adminId, long commentId, ModerationDecision decision,
        string? reason, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var comment = await dbContext.PostComments.FromSqlInterpolated($"""
            SELECT * FROM post_comments WHERE id = {commentId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (comment is null || !await dbContext.CommunityPosts.AnyAsync(post =>
            post.Id == comment.PostId && post.DeletedAt == null, cancellationToken))
        {
            return ModerationResult.NotFound;
        }

        var state = NextState(comment.ModerationStatus, decision);
        if (state is null || (decision == ModerationDecision.Approve &&
            !await CommunityQueries.PublishedPosts(dbContext).AnyAsync(post => post.Id == comment.PostId, cancellationToken)))
        {
            return ModerationResult.Conflict;
        }

        comment.ModerationStatus = state;
        comment.PublishedAt = PublicationTime(state);
        await SaveDecisionAsync(adminId, null, comment.Id, decision, reason, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ModerationResult.Applied;
    }

    public Task<CommunityPage<ModerationReportItem>> GetReportsAsync(
        long? postId, long? commentId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var pending = dbContext.ContentReports.AsNoTracking().Where(report => report.Status == CommunityRules.Pending);
        var ranked = pending.Where(report => (postId == null || report.PostId == postId) &&
                (commentId == null || report.CommentId == commentId))
            .Select(report => new
            {
                Report = report,
                PendingReporterCount = pending.Where(other =>
                        other.PostId == report.PostId && other.CommentId == report.CommentId)
                    .Select(other => other.ReporterId).Distinct().Count()
            })
            .OrderByDescending(item => item.PendingReporterCount).ThenBy(item => item.Report.Id);

        return ranked.Select(item => new ModerationReportItem(
                item.Report.Id, item.Report.ReporterId, item.Report.PostId, item.Report.CommentId,
                item.Report.PostId ?? item.Report.Comment!.PostId,
                item.Report.PostId != null ? item.Report.Post!.Content : item.Report.Comment!.Content,
                item.Report.PostId != null ? item.Report.Post!.ModerationStatus : item.Report.Comment!.ModerationStatus,
                item.Report.PostId != null ? item.Report.Post!.DeletedAt != null :
                    item.Report.Comment!.DeletedAt != null || item.Report.Comment.Post.DeletedAt != null,
                item.Report.Reason, item.PendingReporterCount))
            .PageAsync(page, pageSize, cancellationToken);
    }

    public Task<CommunityPage<ModerationReportHistoryItem>> GetReportHistoryAsync(
        long? reportId, long? postId, long? commentId, string? resolution, int page, int pageSize,
        CancellationToken cancellationToken) =>
        dbContext.ContentReports.AsNoTracking()
            .Where(report => (report.Status == CommunityRules.ReportResolved ||
                report.Status == CommunityRules.ReportDismissed) &&
                (reportId == null || report.Id == reportId) &&
                (postId == null || report.PostId == postId) &&
                (commentId == null || report.CommentId == commentId) &&
                (resolution == null || report.Status == resolution))
            .OrderByDescending(report => report.ResolvedAt.HasValue)
            .ThenByDescending(report => report.ResolvedAt).ThenByDescending(report => report.Id)
            .Select(report => new ModerationReportHistoryItem(
                report.Id, report.ReporterId, report.PostId, report.CommentId,
                report.PostId ?? report.Comment!.PostId, report.Reason, report.Status,
                report.ResolutionReason, report.ResolvedById, report.ResolvedAt))
            .PageAsync(page, pageSize, cancellationToken);

    public async Task<ModerationResult> ResolveReportAsync(
        long adminId, long id, string resolution, string reason, CancellationToken cancellationToken)
    {
        var resolvedAt = DateTimeOffset.UtcNow;
        var changed = await dbContext.ContentReports.Where(report => report.Id == id &&
                report.Status == CommunityRules.Pending)
            .ExecuteUpdateAsync(updates => updates.SetProperty(report => report.Status, resolution)
                .SetProperty(report => report.ResolvedById, adminId)
                .SetProperty(report => report.ResolvedAt, resolvedAt)
                .SetProperty(report => report.ResolutionReason, reason), cancellationToken);
        if (changed == 1)
        {
            return ModerationResult.Applied;
        }
        return await dbContext.ContentReports.AnyAsync(report => report.Id == id, cancellationToken)
            ? ModerationResult.Conflict
            : ModerationResult.NotFound;
    }

    private IQueryable<ModerationQueueItem> PendingPosts() =>
        dbContext.CommunityPosts.AsNoTracking()
            .Where(post => post.DeletedAt == null && post.ModerationStatus == CommunityRules.Pending)
            .OrderBy(post => post.Id)
            .Select(post => new ModerationQueueItem("POST", post.Id, null, post.AreaId,
                new CommunityAuthor(post.AuthorId, post.Author.DisplayName), post.Title, post.Content,
                post.Media.OrderBy(media => media.SortOrder).ThenBy(media => media.Id)
                    .Select(media => new CommunityMedia(media.Id, media.MediaType, media.SortOrder)).ToList()));

    private IQueryable<ModerationQueueItem> PendingComments() =>
        dbContext.PostComments.AsNoTracking()
            .Where(comment => comment.DeletedAt == null && comment.ModerationStatus == CommunityRules.Pending &&
                CommunityQueries.PublishedPosts(dbContext).Any(post => post.Id == comment.PostId))
            .OrderBy(comment => comment.CreatedAt).ThenBy(comment => comment.Id)
            .Select(comment => new ModerationQueueItem("COMMENT", comment.Id, comment.PostId, comment.Post.AreaId,
                new CommunityAuthor(comment.AuthorId, comment.Author.DisplayName), null, comment.Content,
                Array.Empty<CommunityMedia>()));

    private async Task SaveDecisionAsync(long adminId, long? postId, long? commentId,
        ModerationDecision decision, string? reason, CancellationToken cancellationToken)
    {
        dbContext.ModerationActions.Add(new ModerationAction
        {
            AdminId = adminId,
            PostId = postId,
            CommentId = commentId,
            Action = decision switch
            {
                ModerationDecision.Approve => "APPROVE",
                ModerationDecision.Reject => CommunityRules.RejectionAction,
                _ => CommunityRules.HideAction
            },
            Reason = reason
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? NextState(string current, ModerationDecision decision) => (current, decision) switch
    {
        (CommunityRules.Pending, ModerationDecision.Approve) => CommunityRules.Approved,
        (CommunityRules.Pending, ModerationDecision.Reject) => CommunityRules.Rejected,
        (CommunityRules.Approved, ModerationDecision.Hide) => CommunityRules.Hidden,
        _ => null
    };

    private static DateTimeOffset? PublicationTime(string state) =>
        state == CommunityRules.Approved ? DateTimeOffset.UtcNow : null;
}
