namespace FloodAI.DataAccess.Entities;

public sealed class ForecastModel
{
    public long Id { get; set; }
    public long StationId { get; set; }
    public required string Version { get; set; }
    public int HorizonHours { get; set; }
    public required string ArtifactPath { get; set; }
    public required string ManifestHash { get; set; }
    public bool IsActive { get; set; }

    public Station Station { get; set; } = null!;
}
