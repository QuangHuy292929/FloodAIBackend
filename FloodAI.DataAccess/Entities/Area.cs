namespace FloodAI.DataAccess.Entities;

public sealed class Area
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Level { get; set; }
    public bool IsActive { get; set; } = true;

    public Area? Parent { get; set; }
    public ICollection<Area> Children { get; } = new List<Area>();
    public ICollection<Station> Stations { get; } = new List<Station>();
}
