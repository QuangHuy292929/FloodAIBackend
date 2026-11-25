using System.ComponentModel.DataAnnotations;
using FloodAI.Api.Authentication;
using FloodAI.Api.Community;
using FloodAI.Business.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class PostCommentsController(CommunityCommentsService comments) : ControllerBase
{
    public sealed record CommentRequest([Required, StringLength(2000)] string Content);

    [HttpGet("posts/{id:long}/comments")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityPage<CommunityCommentView>>> GetComments(
        long id, [FromQuery] CommunityPageQuery query, CancellationToken cancellationToken)
    {
        var page = await comments.GetPublishedAsync(id, query.Page, query.PageSize, cancellationToken);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpPost("posts/{id:long}/comments")]
    [Authorize]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [ProducesResponseType<CommunityCommentView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CommunityCommentView>> Create(long id, CommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await comments.CreateAsync(AccountBearerHandler.GetUserId(User), id, request.Content,
            cancellationToken);
        return comment is null ? NotFound() :
            CreatedAtAction(nameof(MyCommunityController.GetComment), "MyCommunity", new { id = comment.Id }, comment);
    }

    [HttpPatch("comments/{id:long}")]
    [Authorize]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Edit(long id, CommentRequest request, CancellationToken cancellationToken)
    {
        var updated = await comments.EditAsync(AccountBearerHandler.GetUserId(User), id, request.Content,
            cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("comments/{id:long}")]
    [Authorize]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var deleted = await comments.DeleteAsync(AccountBearerHandler.GetUserId(User), id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
