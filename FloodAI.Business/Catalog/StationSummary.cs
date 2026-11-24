namespace FloodAI.Business.Catalog;

public sealed record StationSummary(
    long Id,
    string Code,
    string Name,
    string Type,
    double? Latitude,
    double? Longitude);
