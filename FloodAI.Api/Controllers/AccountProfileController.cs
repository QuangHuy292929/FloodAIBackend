using System.ComponentModel.DataAnnotations;
using FloodAI.Api.Authentication;
using FloodAI.Business.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountProfileController(AccountService accounts) : ControllerBase
{
    public sealed record ProfileRequest([Required, StringLength(100)] string DisplayName);

    [HttpGet]
    [ProducesResponseType<AccountProfile>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountProfile>> Get(CancellationToken cancellationToken)
    {
        var profile = await accounts.GetProfileAsync(AccountBearerHandler.GetUserId(User), cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }
        return profile;
    }

    [HttpPatch]
    [ProducesResponseType<AccountProfile>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountProfile>> Update(ProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await accounts.UpdateProfileAsync(AccountBearerHandler.GetUserId(User), request.DisplayName,
            cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }
        return profile;
    }
}
