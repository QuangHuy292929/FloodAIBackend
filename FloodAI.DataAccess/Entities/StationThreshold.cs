namespace FloodAI.DataAccess.Entities;

public sealed class StationThreshold
{
    public long Id { get; set; }
    public long StationId { get; set; }
    public required string ThresholdType { get; set; }
    public double ValueRaw { get; set; }
    public required string Source { get; set; }
    public required string UnitStatus { get; set; }
    public bool IsActive { get; set; }

    public Station Station { get; set; } = null!;
}
