using System.ComponentModel.DataAnnotations;
using FloodAI.Api.Authentication;
using FloodAI.Business.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(AccountService accounts, PasswordResetEmailSender emailSender) : ControllerBase
{
    public sealed record RegisterRequest(
        [Required, EmailAddress, StringLength(254)] string Email,
        [Required, StringLength(128, MinimumLength = 12)] string Password,
        [Required, StringLength(100)] string DisplayName);

    public sealed record LoginRequest(
        [Required, EmailAddress, StringLength(254)] string Email,
        [Required, StringLength(128)] string Password);

    public sealed record ChangePasswordRequest(
        [Required, StringLength(128)] string CurrentPassword,
        [Required, StringLength(128, MinimumLength = 12)] string NewPassword);

    public sealed record ForgotPasswordRequest([Required, EmailAddress, StringLength(254)] string Email);

    public sealed record ResetPasswordRequest(
        [Required, StringLength(2048)] string Token,
        [Required, StringLength(128, MinimumLength = 12)] string NewPassword);

    [HttpPost("register")]
    [EnableRateLimiting(AccountBearerHandler.RateLimitPolicy)]
    [ProducesResponseType<AccountProfile>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountProfile>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var profile = await accounts.RegisterAsync(request.Email, request.Password, request.DisplayName, cancellationToken);
        if (profile is null)
        {
            return Problem(detail: "An account with that email already exists.", statusCode: StatusCodes.Status409Conflict);
        }
        return StatusCode(StatusCodes.Status201Created, profile);
    }

    [HttpPost("login")]
    [EnableRateLimiting(AccountBearerHandler.RateLimitPolicy)]
    [ProducesResponseType<LoginSession>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginSession>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var login = await accounts.LoginAsync(request.Email, request.Password, cancellationToken);
        if (login is null)
        {
            return Problem(detail: "Invalid credentials or account unavailable.", statusCode: StatusCodes.Status401Unauthorized);
        }
        return login;
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await accounts.LogoutAsync(AccountBearerHandler.GetUserId(User), AccountBearerHandler.GetSessionId(User),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting(AccountBearerHandler.RateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var changed = await accounts.ChangePasswordAsync(AccountBearerHandler.GetUserId(User),
            request.CurrentPassword, request.NewPassword, cancellationToken);
        return changed ? NoContent() : Problem(detail: "The current password is incorrect.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(AccountBearerHandler.RateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured)
        {
            return Problem(detail: "Password reset email is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        var reset = await accounts.PreparePasswordResetAsync(request.Email, cancellationToken);
        if (reset is not null)
        {
            await emailSender.SendAsync(reset, cancellationToken);
        }
        return Accepted();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(AccountBearerHandler.RateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var changed = await accounts.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);
        return changed ? NoContent() : Problem(detail: "The reset token is invalid or expired.",
            statusCode: StatusCodes.Status400BadRequest);
    }
}
