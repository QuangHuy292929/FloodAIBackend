namespace FloodAI.DataAccess.Entities;

public sealed class CatalogAction
{
    public long Id { get; set; }
    public long AdminId { get; set; }
    public required string Action { get; set; }
    public required string ResourceType { get; set; }
    public required string ResourceKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User Admin { get; set; } = null!;
}
