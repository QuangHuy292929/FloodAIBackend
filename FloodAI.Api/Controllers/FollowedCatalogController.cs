using FloodAI.Api.Authentication;
using FloodAI.Api.Community;
using FloodAI.Business.Community;
using FloodAI.Business.Follows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FollowedCatalogController(FollowService follows) : ControllerBase
{
    [HttpGet("followed-areas")]
    public async Task<ActionResult<List<FollowedArea>>> GetAreas(CancellationToken cancellationToken) =>
        await follows.GetAreasAsync(AccountBearerHandler.GetUserId(User), cancellationToken);

    [HttpGet("followed-areas/posts")]
    public Task<CommunityPage<CommunityPostView>> GetAreaFeed(
        [FromQuery] CommunityPageQuery query, CancellationToken cancellationToken) =>
        follows.GetAreaFeedAsync(AccountBearerHandler.GetUserId(User), query.Page, query.PageSize, cancellationToken);

    [HttpPut("followed-areas/{areaId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FollowArea(long areaId, CancellationToken cancellationToken) =>
        await follows.FollowAreaAsync(AccountBearerHandler.GetUserId(User), areaId, cancellationToken)
            ? NoContent() : NotFound();

    [HttpDelete("followed-areas/{areaId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnfollowArea(long areaId, CancellationToken cancellationToken)
    {
        await follows.UnfollowAreaAsync(AccountBearerHandler.GetUserId(User), areaId, cancellationToken);
        return NoContent();
    }

    [HttpGet("followed-stations")]
    public async Task<ActionResult<List<FollowedStation>>> GetStations(CancellationToken cancellationToken) =>
        await follows.GetStationsAsync(AccountBearerHandler.GetUserId(User), cancellationToken);

    [HttpPut("followed-stations/{stationId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FollowStation(long stationId, CancellationToken cancellationToken) =>
        await follows.FollowStationAsync(AccountBearerHandler.GetUserId(User), stationId, cancellationToken)
            ? NoContent() : NotFound();

    [HttpDelete("followed-stations/{stationId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnfollowStation(long stationId, CancellationToken cancellationToken)
    {
        await follows.UnfollowStationAsync(AccountBearerHandler.GetUserId(User), stationId, cancellationToken);
        return NoContent();
    }
}
