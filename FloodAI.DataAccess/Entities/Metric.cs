namespace FloodAI.DataAccess.Entities;

public sealed class Metric
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string QuantityType { get; set; }
}
