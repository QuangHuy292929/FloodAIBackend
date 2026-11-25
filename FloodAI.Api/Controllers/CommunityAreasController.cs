using FloodAI.Api.Community;
using FloodAI.Business.Community;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/areas")]
public sealed class CommunityAreasController(CommunityPostService posts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CommunityArea>>> GetAreas(CancellationToken cancellationToken) =>
        await posts.GetAreasAsync(cancellationToken);

    [HttpGet("{id:long}/posts")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommunityPage<CommunityPostView>>> GetFeed(
        long id, [FromQuery] CommunityFeedQuery query, CancellationToken cancellationToken)
    {
        var type = CommunityRequests.PostType(query.Type, "type", ModelState);
        var from = CommunityRequests.Timestamp(query.From, "from", ModelState);
        var to = CommunityRequests.Timestamp(query.To, "to", ModelState);
        if (from >= to)
        {
            ModelState.AddModelError("to", "The end must be later than the start.");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var feed = await posts.GetFeedAsync(id, type, from, to, query.Page, query.PageSize, cancellationToken);
        return feed is null ? NotFound() : Ok(feed);
    }
}
