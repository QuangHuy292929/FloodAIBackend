namespace FloodAI.DataAccess.Entities;

public sealed class ModerationAction
{
    public long Id { get; set; }
    public long AdminId { get; set; }
    public long? PostId { get; set; }
    public long? CommentId { get; set; }
    public required string Action { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User Admin { get; set; } = null!;
    public CommunityPost? Post { get; set; }
    public PostComment? Comment { get; set; }
}
