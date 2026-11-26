namespace FloodAI.DataAccess.Entities;

public sealed class RiverBranch
{
    public long Id { get; set; }
    public long BasinId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    public Basin Basin { get; set; } = null!;
    public ICollection<Station> Stations { get; } = new List<Station>();
}
