namespace FloodAI.DataAccess.Entities;

public sealed class ContentReport
{
    public long Id { get; set; }
    public long ReporterId { get; set; }
    public long? PostId { get; set; }
    public long? CommentId { get; set; }
    public required string Reason { get; set; }
    public required string Status { get; set; }
    public long? ResolvedById { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionReason { get; set; }

    public User Reporter { get; set; } = null!;
    public CommunityPost? Post { get; set; }
    public PostComment? Comment { get; set; }
    public User? ResolvedBy { get; set; }
}
