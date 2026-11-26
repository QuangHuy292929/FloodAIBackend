using System.ComponentModel.DataAnnotations;

namespace FloodAI.Api.Controllers;

public sealed class ObservationQuery
{
    [Required]
    public string Metric { get; init; } = "";

    [Required]
    public string From { get; init; } = "";

    [Required]
    public string To { get; init; } = "";

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? SourceId { get; init; }

    [StringLength(32)]
    public string? DataOrigin { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 1000)]
    public int PageSize { get; init; } = 250;
}
