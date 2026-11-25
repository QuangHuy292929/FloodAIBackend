using FloodAI.Api.Authentication;
using FloodAI.Api.Community;
using FloodAI.Business.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MyCommunityController(CommunityPostService posts, CommunityCommentsService comments) : ControllerBase
{
    [HttpGet("posts")]
    public Task<CommunityPage<CommunityPostView>> GetPosts(
        [FromQuery] CommunityPageQuery query, CancellationToken cancellationToken) =>
        posts.ListMineAsync(AccountBearerHandler.GetUserId(User), query.Page, query.PageSize, cancellationToken);

    [HttpGet("posts/{id:long}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityPostView>> GetPost(long id, CancellationToken cancellationToken)
    {
        var post = await posts.GetMineAsync(AccountBearerHandler.GetUserId(User), id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpGet("comments")]
    public Task<CommunityPage<CommunityCommentView>> GetComments(
        [FromQuery] CommunityPageQuery query, CancellationToken cancellationToken) =>
        comments.GetMineAsync(AccountBearerHandler.GetUserId(User), query.Page, query.PageSize, cancellationToken);

    [HttpGet("comments/{id:long}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityCommentView>> GetComment(long id, CancellationToken cancellationToken)
    {
        var comment = await comments.GetMineAsync(AccountBearerHandler.GetUserId(User), id, cancellationToken);
        return comment is null ? NotFound() : Ok(comment);
    }
}
