namespace FloodAI.DataAccess.Entities;

public sealed class UserSession
{
    public Guid Id { get; set; }
    public long UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public User User { get; set; } = null!;
}
