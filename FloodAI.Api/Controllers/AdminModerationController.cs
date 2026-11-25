using System.ComponentModel.DataAnnotations;
using FloodAI.Api.Authentication;
using FloodAI.Api.Community;
using FloodAI.Business.Accounts;
using FloodAI.Business.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FloodAI.Api.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Admin)]
[Route("api/v1/admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminModerationController(CommunityModerationService moderation) : ControllerBase
{
    public sealed record DecisionRequest([StringLength(1000)] string? Reason);
    public sealed record ResolveRequest([Required, StringLength(16)] string? Resolution);

    public sealed class TargetQuery : CommunityPageQuery
    {
        [Range(typeof(long), "1", "9223372036854775807")]
        public long? PostId { get; init; }

        [Range(typeof(long), "1", "9223372036854775807")]
        public long? CommentId { get; init; }
    }

    [HttpGet("moderation/queue")]
    public async Task<ActionResult<CommunityPage<ModerationQueueItem>>> GetQueue(
        [FromQuery] CommunityPageQuery query, CancellationToken cancellationToken, [FromQuery] string? kind = null)
    {
        var normalized = (kind ?? "post").Trim().ToLowerInvariant();
        if (normalized is not ("post" or "comment"))
        {
            ModelState.AddModelError("kind", "Use post or comment.");
            return ValidationProblem(ModelState);
        }
        return await moderation.GetQueueAsync(normalized == "comment", query.Page, query.PageSize, cancellationToken);
    }

    [HttpGet("moderation/history")]
    public async Task<ActionResult<CommunityPage<ModerationHistoryItem>>> GetHistory(
        [FromQuery] TargetQuery query, CancellationToken cancellationToken)
    {
        if (!TargetIsValid(query))
        {
            return ValidationProblem(ModelState);
        }
        return await moderation.GetHistoryAsync(query.PostId, query.CommentId, query.Page, query.PageSize,
            cancellationToken);
    }

    [HttpPost("posts/{id:long}/{decision}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> DecidePost(long id, string decision,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DecisionRequest? request,
        CancellationToken cancellationToken) =>
        DecideAsync(id, decision, request, comments: false, cancellationToken);

    [HttpPost("comments/{id:long}/{decision}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> DecideComment(long id, string decision,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DecisionRequest? request,
        CancellationToken cancellationToken) =>
        DecideAsync(id, decision, request, comments: true, cancellationToken);

    [HttpGet("content-reports")]
    public async Task<ActionResult<CommunityPage<ModerationReportItem>>> GetReports(
        [FromQuery] TargetQuery query, CancellationToken cancellationToken)
    {
        if (!TargetIsValid(query))
        {
            return ValidationProblem(ModelState);
        }
        return await moderation.GetReportsAsync(query.PostId, query.CommentId, query.Page, query.PageSize,
            cancellationToken);
    }

    [HttpPost("content-reports/{id:long}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResolveReport(long id, ResolveRequest request, CancellationToken cancellationToken)
    {
        var status = request.Resolution?.Trim().ToUpperInvariant();
        if (status is not ("RESOLVED" or "DISMISSED"))
        {
            ModelState.AddModelError("resolution", "Use RESOLVED or DISMISSED.");
            return ValidationProblem(ModelState);
        }

        return ToResponse(await moderation.ResolveReportAsync(AccountBearerHandler.GetUserId(User), id,
            status == "DISMISSED", cancellationToken));
    }

    private async Task<IActionResult> DecideAsync(
        long id, string decision, DecisionRequest? request, bool comments, CancellationToken cancellationToken)
    {
        var parsed = decision.ToLowerInvariant() switch
        {
            "approve" => ModerationDecision.Approve,
            "reject" => ModerationDecision.Reject,
            "hide" => ModerationDecision.Hide,
            _ => (ModerationDecision?)null
        };
        if (parsed is null)
        {
            ModelState.AddModelError("decision", "Use approve, reject or hide.");
            return ValidationProblem(ModelState);
        }

        var reason = request?.Reason?.Trim();
        if (parsed != ModerationDecision.Approve && string.IsNullOrWhiteSpace(reason))
        {
            ModelState.AddModelError("reason", "Provide a reason to reject or hide content.");
            return ValidationProblem(ModelState);
        }

        var adminId = AccountBearerHandler.GetUserId(User);
        var result = comments
            ? await moderation.DecideCommentAsync(adminId, id, parsed.Value, reason, cancellationToken)
            : await moderation.DecidePostAsync(adminId, id, parsed.Value, reason, cancellationToken);
        return ToResponse(result);
    }

    private bool TargetIsValid(TargetQuery query)
    {
        if (query.PostId.HasValue && query.CommentId.HasValue)
        {
            ModelState.AddModelError("$", "Filter by either postId or commentId.");
            return false;
        }
        return true;
    }

    private IActionResult ToResponse(ModerationResult result) => result switch
    {
        ModerationResult.Applied => NoContent(),
        ModerationResult.NotFound => NotFound(),
        _ => Problem(detail: "The current state does not allow this action.", statusCode: StatusCodes.Status409Conflict)
    };
}
