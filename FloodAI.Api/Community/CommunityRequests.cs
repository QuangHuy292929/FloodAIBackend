using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using FloodAI.Business.Community;

namespace FloodAI.Api.Community;

public class CommunityPageQuery
{
    [Range(1, 100000)]
    public int Page { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 20;
}

public sealed class CommunityFeedQuery : CommunityPageQuery
{
    public string? Type { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
}

public sealed record CommunityPostRequest(
    [Range(typeof(long), "1", "9223372036854775807")] long AreaId,
    [Range(typeof(long), "1", "9223372036854775807")] long? StationId,
    [Required, StringLength(160)] string Title,
    [Required, StringLength(5000)] string Content,
    [Required, StringLength(32)] string PostType,
    [Required, StringLength(40)] string ObservedAt);

public sealed class CommunityPostPatchRequest
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long? AreaId { get; init; }

    public JsonElement StationId { get; init; }

    [StringLength(160)]
    public string? Title { get; init; }

    [StringLength(5000)]
    public string? Content { get; init; }

    [StringLength(32)]
    public string? PostType { get; init; }

    [StringLength(40)]
    public string? ObservedAt { get; init; }
}

internal static class CommunityRequests
{
    internal const string WritePolicy = "community-write";

    internal static string? PostType(string? input, string field, ModelStateDictionary state)
    {
        var type = input?.Trim().ToUpperInvariant();
        if (type is not null && !CommunityRules.IsPostType(type))
        {
            state.AddModelError(field, "Use FLOOD_REPORT, WEATHER_UPDATE, DISCUSSION or OTHER.");
        }
        return type;
    }

    internal static DateTimeOffset? Timestamp(string? input, string field, ModelStateDictionary state)
    {
        if (input is null)
        {
            return null;
        }
        var hasOffset = input.EndsWith('Z') || (input.Length >= 25 &&
            input[^6] is '+' or '-' && input[^3] == ':');
        if (input.Length < 20 || input[10] != 'T' || !hasOffset ||
            !DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            state.AddModelError(field, "Use an ISO timestamp with an offset, for example 2022-10-15T12:00:00+07:00.");
            return null;
        }
        return time.ToUniversalTime();
    }

    internal static long? StationId(JsonElement station, ModelStateDictionary state)
    {
        if (station.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }
        if (station.ValueKind == JsonValueKind.Number && station.TryGetInt64(out var id) && id > 0)
        {
            return id;
        }
        state.AddModelError("stationId", "Use a positive station ID or null.");
        return null;
    }
}
