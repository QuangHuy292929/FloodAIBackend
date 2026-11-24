namespace FloodAI.DataAccess.Entities;

public sealed class Station
{
    public long Id { get; set; }
    public long BasinId { get; set; }
    public long AreaId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public Basin Basin { get; set; } = null!;
    public Area Area { get; set; } = null!;
}
