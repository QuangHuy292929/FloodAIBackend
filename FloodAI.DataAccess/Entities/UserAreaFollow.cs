namespace FloodAI.DataAccess.Entities;

public sealed class UserAreaFollow
{
    public long UserId { get; set; }
    public long AreaId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Area Area { get; set; } = null!;
}
