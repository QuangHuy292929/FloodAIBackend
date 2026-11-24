namespace FloodAI.DataAccess.Entities;

public sealed class PostComment
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public long AuthorId { get; set; }
    public required string Content { get; set; }
    public string ModerationStatus { get; set; } = "PENDING";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public CommunityPost Post { get; set; } = null!;
    public User Author { get; set; } = null!;
}
