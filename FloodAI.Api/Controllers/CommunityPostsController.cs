using System.Text.Json;
using FloodAI.Api.Authentication;
using FloodAI.Api.Community;
using FloodAI.Business.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/posts")]
public sealed class CommunityPostsController(CommunityPostService posts) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityPostView>> Get(long id, CancellationToken cancellationToken)
    {
        var post = await posts.GetPublishedAsync(id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [ProducesResponseType<CommunityPostView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CommunityPostView>> Create(CommunityPostRequest request, CancellationToken cancellationToken)
    {
        var type = CommunityRequests.PostType(request.PostType, "postType", ModelState);
        var observedAt = CommunityRequests.Timestamp(request.ObservedAt, "observedAt", ModelState);
        if (!ModelState.IsValid || !await LocationIsValidAsync(request.AreaId, request.StationId, cancellationToken))
        {
            return ValidationProblem(ModelState);
        }

        var post = await posts.CreateAsync(AccountBearerHandler.GetUserId(User),
            new CommunityPostInput(request.AreaId, request.StationId, request.Title, request.Content, type!,
                observedAt!.Value), cancellationToken);
        return CreatedAtAction(nameof(MyCommunityController.GetPost), "MyCommunity", new { id = post.Id }, post);
    }

    [HttpPatch("{id:long}")]
    [Authorize]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityPostView>> Update(
        long id, CommunityPostPatchRequest request, CancellationToken cancellationToken)
    {
        var type = CommunityRequests.PostType(request.PostType, "postType", ModelState);
        var observedAt = CommunityRequests.Timestamp(request.ObservedAt, "observedAt", ModelState);
        var stationId = CommunityRequests.StationId(request.StationId, ModelState);
        var setStationId = request.StationId.ValueKind != JsonValueKind.Undefined;
        if (request.Title is not null && string.IsNullOrWhiteSpace(request.Title))
        {
            ModelState.AddModelError("title", "The title cannot be blank.");
        }
        if (request.Content is not null && string.IsNullOrWhiteSpace(request.Content))
        {
            ModelState.AddModelError("content", "The content cannot be blank.");
        }
        if (request.AreaId is null && !setStationId && request.Title is null && request.Content is null &&
            request.PostType is null && request.ObservedAt is null)
        {
            ModelState.AddModelError("$", "Provide at least one field to update.");
        }
        if (!ModelState.IsValid || !await LocationIsValidAsync(request.AreaId, stationId, cancellationToken))
        {
            return ValidationProblem(ModelState);
        }

        var post = await posts.EditAsync(AccountBearerHandler.GetUserId(User), id,
            new CommunityPostChanges(request.AreaId, setStationId, stationId, request.Title, request.Content,
                type, observedAt), cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpDelete("{id:long}")]
    [Authorize]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var deleted = await posts.DeleteAsync(AccountBearerHandler.GetUserId(User), id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<bool> LocationIsValidAsync(long? areaId, long? stationId, CancellationToken cancellationToken)
    {
        var invalidField = await posts.ValidateLocationAsync(areaId, stationId, cancellationToken);
        if (invalidField is null)
        {
            return true;
        }
        ModelState.AddModelError(invalidField, "The selected area or station is unavailable.");
        return false;
    }
}
