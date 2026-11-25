using System.Globalization;
using System.Security.Cryptography;
using FloodAI.DataAccess.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace FloodAI.Business.Accounts;

public sealed class PasswordResetTokens(IDataProtectionProvider dataProtection)
{
    private readonly ITimeLimitedDataProtector protector = dataProtection
        .CreateProtector("FloodAI.Accounts.PasswordReset.v1").ToTimeLimitedDataProtector();

    internal string Create(User user) =>
        protector.Protect(FormattableString.Invariant($"{user.Id}:{AccountTokens.Hash(user.PasswordHash)}"),
            TimeSpan.FromMinutes(30));

    internal bool TryRead(string token, out long userId, out string passwordFingerprint)
    {
        userId = 0;
        passwordFingerprint = "";
        try
        {
            var payload = protector.Unprotect(token);
            var separator = payload.IndexOf(':');
            if (separator < 1 || !long.TryParse(payload.AsSpan(0, separator),
                NumberStyles.None, CultureInfo.InvariantCulture, out userId))
            {
                return false;
            }

            passwordFingerprint = payload[(separator + 1)..];
            return userId > 0 && passwordFingerprint.Length == 64;
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return false;
        }
    }
}
