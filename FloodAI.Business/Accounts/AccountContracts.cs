namespace FloodAI.Business.Accounts;

public static class AccountRoles
{
    public const string User = "USER";
    public const string Admin = "ADMIN";
}

public static class AccountStatuses
{
    public const string Active = "ACTIVE";
    public const string Locked = "LOCKED";
}

public sealed record AccountProfile(
    long Id, string Email, string DisplayName, string Role, string Status, DateTimeOffset CreatedAt);

public sealed record LoginSession(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, AccountProfile User);

public sealed record AccountIdentity(Guid SessionId, long UserId, string Role);

public sealed record PasswordResetDelivery(string Email, string Token);
