namespace FloodAI.DataAccess.Entities;

public sealed class ForecastRun
{
    public long Id { get; set; }
    public long StationId { get; set; }
    public DateTime IssueTimeLocal { get; set; }
    public required string ModelBundleHash { get; set; }
    public required string InputHash { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Station Station { get; set; } = null!;
    public ICollection<ForecastValue> Values { get; } = new List<ForecastValue>();
}
