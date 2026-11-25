namespace FloodAI.Business.Community;

public sealed record CommunityArea(long Id, long? ParentId, string Code, string Name, string Level);

public sealed record CommunityAuthor(long Id, string DisplayName);

public sealed record CommunityMedia(long Id, string MediaType, int SortOrder);

public sealed record CommunityPostInput(
    long AreaId, long? StationId, string Title, string Content, string PostType, DateTimeOffset ObservedAt);

public sealed record CommunityPostChanges(
    long? AreaId, bool SetStationId, long? StationId, string? Title, string? Content,
    string? PostType, DateTimeOffset? ObservedAt);

public sealed record CommunityPostView(
    long Id, long AreaId, long? StationId, CommunityAuthor Author, string Title, string Content,
    string PostType, DateTimeOffset ObservedAt, string ModerationStatus, DateTimeOffset? PublishedAt,
    string? RejectionReason, IReadOnlyList<CommunityMedia> Media);

public sealed record CommunityCommentView(
    long Id, long PostId, CommunityAuthor Author, string Content, string ModerationStatus,
    DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt, string? RejectionReason);

public sealed record CommunityReportView(long Id, string Status);

public sealed record CommunityPage<T>(int Page, int PageSize, bool HasNextPage, IReadOnlyList<T> Items);
