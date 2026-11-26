using FloodAI.DataAccess.Observations;

namespace FloodAI.DataAccess.Entities;

public sealed class DataSource
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Url { get; set; }
    public required string SourceType { get; set; }
    public required string Status { get; set; }
    public string TimeBasis { get; set; } = ObservationMetadata.SourceLocalUnverified;
    public bool SupportsUpdates { get; set; }
}
