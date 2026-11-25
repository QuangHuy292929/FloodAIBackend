using System.ComponentModel.DataAnnotations;
using FloodAI.Api.Authentication;
using FloodAI.Api.Community;
using FloodAI.Business.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/content-reports")]
public sealed class CommunityReportsController(CommunityCommentsService comments) : ControllerBase
{
    public sealed record ReportRequest(
        [Range(typeof(long), "1", "9223372036854775807")] long? PostId,
        [Range(typeof(long), "1", "9223372036854775807")] long? CommentId,
        [Required, StringLength(1000)] string Reason);

    [HttpPost]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [ProducesResponseType<CommunityReportView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CommunityReportView>> Create(ReportRequest request, CancellationToken cancellationToken)
    {
        if (request.PostId.HasValue == request.CommentId.HasValue)
        {
            ModelState.AddModelError("$", "Select exactly one post or comment to report.");
            return ValidationProblem(ModelState);
        }

        var report = await comments.ReportAsync(AccountBearerHandler.GetUserId(User), request.PostId,
            request.CommentId, request.Reason, cancellationToken);
        return report is null ? NotFound() : StatusCode(StatusCodes.Status201Created, report);
    }
}
