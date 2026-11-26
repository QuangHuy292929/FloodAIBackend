namespace FloodAI.DataAccess.Entities;

public sealed class Basin
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Station> Stations { get; } = new List<Station>();
    public ICollection<RiverBranch> RiverBranches { get; } = new List<RiverBranch>();
}
