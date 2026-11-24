namespace FloodAI.DataAccess.Entities;

public sealed class Observation
{
    public long Id { get; set; }
    public long StationId { get; set; }
    public long MetricId { get; set; }
    public long SourceId { get; set; }
    public DateTime TimestampLocal { get; set; }
    public double ValueRaw { get; set; }
    public required string QualityStatus { get; set; }
    public DateTimeOffset? AvailableAt { get; set; }

    public Station Station { get; set; } = null!;
    public Metric Metric { get; set; } = null!;
    public DataSource Source { get; set; } = null!;
}
