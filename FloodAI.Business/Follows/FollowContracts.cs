namespace FloodAI.Business.Follows;

public sealed record FollowedArea(
    long Id, long? ParentId, string Code, string Name, string Level, DateTimeOffset FollowedAt);

public sealed record FollowedStation(
    long Id, long BasinId, string Code, string Name, string Type,
    double? Latitude, double? Longitude, DateTimeOffset FollowedAt);
