namespace FloodAI.Business.Community;

public enum ModerationResult
{
    Applied,
    NotFound,
    Conflict
}

public enum ModerationDecision
{
    Approve,
    Reject,
    Hide
}

public sealed record ModerationQueueItem(
    string Kind, long Id, long? ParentPostId, long AreaId, CommunityAuthor Author,
    string? Title, string Content, IReadOnlyList<CommunityMedia> Media);

public sealed record ModerationHistoryItem(
    long Id, long AdminId, long? PostId, long? CommentId, string Action, string? Reason, DateTimeOffset CreatedAt);

public sealed record ModerationReportItem(
    long Id, long ReporterId, long? PostId, long? CommentId, long ParentPostId,
    string Content, string ContentStatus, bool ContentDeleted, string Reason, int PendingReporterCount);

public sealed record ModerationReportHistoryItem(
    long Id, long ReporterId, long? PostId, long? CommentId, long ParentPostId,
    string ReportReason, string Resolution, string? ResolutionReason, long? ResolvedById, DateTimeOffset? ResolvedAt);
