namespace FloodAI.DataAccess.Entities;

public sealed class UserStationFollow
{
    public long UserId { get; set; }
    public long StationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Station Station { get; set; } = null!;
}
