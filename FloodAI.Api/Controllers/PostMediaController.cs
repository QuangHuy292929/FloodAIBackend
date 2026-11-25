using System.ComponentModel.DataAnnotations;
using FloodAI.Api.Authentication;
using FloodAI.Api.Community;
using FloodAI.Business.Accounts;
using FloodAI.Business.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/posts/{postId:long}/media")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PostMediaController(CommunityMediaService mediaService) : ControllerBase
{
    [HttpGet("{mediaId:long}")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(long postId, long mediaId, CancellationToken cancellationToken)
    {
        long? viewerId = User.Identity?.IsAuthenticated == true ? AccountBearerHandler.GetUserId(User) : null;
        var image = await mediaService.OpenAsync(postId, mediaId, viewerId,
            User.IsInRole(AccountRoles.Admin), cancellationToken);
        if (image is null)
        {
            return NotFound();
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Value.Content, image.Value.MediaType);
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CommunityImage.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CommunityImage.MaxRequestBytes)]
    [ProducesResponseType<CommunityMedia>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CommunityMedia>> Upload(long postId, [Required] IFormFile file,
        CancellationToken cancellationToken)
    {
        var type = await CommunityImage.ReadMediaTypeAsync(file, cancellationToken);
        if (type is null)
        {
            ModelState.AddModelError("file", "Upload a JPEG, PNG or WebP image of at most 5 MiB.");
            return ValidationProblem(ModelState);
        }

        await using var content = file.OpenReadStream();
        var result = await mediaService.AddAsync(AccountBearerHandler.GetUserId(User), postId, type, content,
            cancellationToken);
        if (result.LimitReached)
        {
            return Problem(detail: "A post can have at most five images.", statusCode: StatusCodes.Status409Conflict);
        }
        if (result.Media is not { } image)
        {
            return NotFound();
        }
        return CreatedAtAction(nameof(Get), new { postId, mediaId = image.Id }, image);
    }

    [HttpDelete("{mediaId:long}")]
    [Authorize]
    [EnableRateLimiting(CommunityRequests.WritePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long postId, long mediaId, CancellationToken cancellationToken)
    {
        var deleted = await mediaService.DeleteAsync(AccountBearerHandler.GetUserId(User), postId, mediaId,
            cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
