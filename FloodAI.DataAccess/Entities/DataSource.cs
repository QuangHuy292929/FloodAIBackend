namespace FloodAI.DataAccess.Entities;

public sealed class DataSource
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Url { get; set; }
    public required string SourceType { get; set; }
    public required string Status { get; set; }
}
