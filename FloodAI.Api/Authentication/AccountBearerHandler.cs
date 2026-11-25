using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FloodAI.Business.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FloodAI.Api.Authentication;

public sealed class AccountBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AccountService accounts) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Bearer";
    internal const string RateLimitPolicy = "account-credentials";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header) ||
            !string.Equals(header.Scheme, SchemeName, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }
        var token = header.Parameter;
        if (token is not { Length: 43 } ||
            token.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            return AuthenticateResult.Fail("Invalid session.");
        }

        var account = await accounts.AuthenticateAsync(token, Context.RequestAborted);
        if (account is null)
        {
            return AuthenticateResult.Fail("Invalid or expired session.");
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, account.UserId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Sid, account.SessionId.ToString("D")),
            new Claim(ClaimTypes.Role, account.Role)
        ], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = SchemeName;
        return base.HandleChallengeAsync(properties);
    }

    internal static long GetUserId(ClaimsPrincipal user) =>
        long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);

    internal static Guid GetSessionId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.Sid)!);
}
