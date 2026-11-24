namespace FloodAI.DataAccess.Entities;

public sealed class CommunityPost
{
    public long Id { get; set; }
    public long AuthorId { get; set; }
    public long AreaId { get; set; }
    public long? StationId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string PostType { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public string ModerationStatus { get; set; } = "PENDING";
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User Author { get; set; } = null!;
    public Area Area { get; set; } = null!;
    public Station? Station { get; set; }
    public ICollection<PostMedia> Media { get; } = new List<PostMedia>();
    public ICollection<PostComment> Comments { get; } = new List<PostComment>();
}
