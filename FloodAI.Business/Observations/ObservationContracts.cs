namespace FloodAI.Business.Observations;

public sealed record StationMetricSummary(string Code, string Name, string QuantityType, string? Unit);

public sealed record ObservationSourceSummary(long Id, string Name, string? Url);

public sealed record ObservationSummary(
    DateTime TimestampLocal,
    double ValueRaw,
    string QualityStatus,
    DateTimeOffset? AvailableAt,
    ObservationSourceSummary Source);

public sealed record ObservationPage(
    string Metric,
    DateTime From,
    DateTime To,
    string TimeBasis,
    int Page,
    int PageSize,
    bool HasNextPage,
    IReadOnlyList<ObservationSummary> Items);
