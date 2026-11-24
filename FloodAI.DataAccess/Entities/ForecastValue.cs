namespace FloodAI.DataAccess.Entities;

public sealed class ForecastValue
{
    public long Id { get; set; }
    public long RunId { get; set; }
    public long ModelId { get; set; }
    public DateTime TargetTimeLocal { get; set; }
    public int HorizonHours { get; set; }
    public double PredictedValueRaw { get; set; }

    public ForecastRun Run { get; set; } = null!;
    public ForecastModel Model { get; set; } = null!;
}
