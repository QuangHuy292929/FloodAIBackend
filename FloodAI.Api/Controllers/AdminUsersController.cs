using System.ComponentModel.DataAnnotations;
using FloodAI.Business.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Authorize(Roles = AccountRoles.Admin)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController(AccountService accounts) : ControllerBase
{
    public sealed record ChangeStatusRequest([Required] bool? Locked);

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType<AccountProfile>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountProfile>> ChangeStatus(long id, ChangeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await accounts.SetUserLockAsync(id, request.Locked!.Value, cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }
        return profile;
    }
}
