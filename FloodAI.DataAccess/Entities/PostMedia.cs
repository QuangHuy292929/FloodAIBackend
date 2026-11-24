namespace FloodAI.DataAccess.Entities;

public sealed class PostMedia
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public required string StorageKey { get; set; }
    public required string MediaType { get; set; }
    public int SortOrder { get; set; }

    public CommunityPost Post { get; set; } = null!;
}
