namespace FloodAI.DataAccess.Entities;

public sealed class User
{
    public long Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public required string Role { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
