using System.ComponentModel.DataAnnotations;

namespace FloodAI.Business.Catalog.Admin;

public sealed record CatalogBasin(
    long Id, string Code, string Name, string? Description, bool IsActive);

public sealed record CatalogArea(
    long Id, long? ParentId, string Code, string Name, string Level, bool IsActive,
    double? WeatherLatitude, double? WeatherLongitude);

public sealed record CatalogStation(
    long Id, long BasinId, long AreaId, string Code, string Name, string Type, double? Latitude, double? Longitude,
    long? RiverBranchId, long? UpdateSourceId);

public sealed record CatalogDataSource(
    long Id, string Name, string? Url, string SourceType, string Status, string TimeBasis, bool SupportsUpdates);

public sealed record CatalogMetric(long Id, string Code, string Name, string QuantityType);

public sealed record CatalogObservation(
    long Id, long StationId, long MetricId, long SourceId, DateTime TimestampLocal,
    double ValueRaw, string QualityStatus, DateTimeOffset? AvailableAt,
    string DataOrigin, string? Unit, DateTimeOffset? IngestedAt);

public sealed record CatalogActionView(
    long Id, long AdminId, string Action, string ResourceType, string ResourceKey, DateTimeOffset CreatedAt);

public sealed record CatalogPage<T>(int Page, int PageSize, bool HasNextPage, IReadOnlyList<T> Items);

public sealed record BasinInput(
    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$")] string Code,
    [Required, StringLength(160)] string Name,
    [StringLength(2000)] string? Description = null,
    bool IsActive = true);

public sealed record AreaInput(
    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$")] string Code,
    [Required, StringLength(160)] string Name,
    [Required, StringLength(64)] string Level,
    long? ParentId = null,
    bool IsActive = true);

public sealed record RiverBranchInput(
    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$")] string Code,
    [Required, StringLength(160)] string Name,
    [Range(typeof(long), "1", "9223372036854775807")] long BasinId);

public sealed record StationInput(
    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$")] string Code,
    [Required, StringLength(160)] string Name,
    [Required, StringLength(64)] string Type,
    [Range(typeof(long), "1", "9223372036854775807")] long BasinId,
    [Range(typeof(long), "1", "9223372036854775807")] long AreaId,
    [Range(-90, 90)] double? Latitude = null,
    [Range(-180, 180)] double? Longitude = null);

public sealed record DataSourceInput(
    [Required, StringLength(160)] string Name,
    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$")] string SourceType,
    [StringLength(2048)] string? Url = null,
    bool IsActive = true,
    [StringLength(32)] string? TimeBasis = null,
    bool? SupportsUpdates = null);

public sealed record MetricInput(
    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$")] string Code,
    [Required, StringLength(160)] string Name,
    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$")] string QuantityType);
