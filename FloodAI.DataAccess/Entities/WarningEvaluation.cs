namespace FloodAI.DataAccess.Entities;

public sealed class WarningEvaluation
{
    public long Id { get; set; }
    public long ForecastValueId { get; set; }
    public long ThresholdId { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }

    public ForecastValue ForecastValue { get; set; } = null!;
    public StationThreshold Threshold { get; set; } = null!;
}
