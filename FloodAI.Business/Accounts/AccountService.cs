using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FloodAI.Business.Accounts;

public sealed class AccountService(
    FloodAiDbContext dbContext, IPasswordHasher<User> passwordHasher, PasswordResetTokens resetTokens)
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public async Task<AccountProfile?> RegisterAsync(
        string email, string password, string displayName, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = NormalizeEmail(email),
            DisplayName = displayName.Trim(),
            Role = AccountRoles.User,
            Status = AccountStatuses.Active,
            PasswordHash = ""
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        dbContext.Users.Add(user);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Profile(user);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_users_email" })
        {
            return null;
        }
    }

    public async Task<LoginSession?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = await LockUserByEmailAsync(email, cancellationToken);
        if (user is null || user.Status != AccountStatuses.Active || !VerifyPassword(user, password))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        await dbContext.UserSessions.Where(session => session.UserId == user.Id && session.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
        var token = AccountTokens.NewSessionToken();
        var expiresAt = now + SessionLifetime;
        dbContext.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = AccountTokens.Hash(token),
            ExpiresAt = expiresAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LoginSession(token, "Bearer", expiresAt, Profile(user));
    }

    public Task<AccountIdentity?> AuthenticateAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = AccountTokens.Hash(token);
        var now = DateTimeOffset.UtcNow;
        return dbContext.UserSessions.AsNoTracking().Where(session =>
                session.TokenHash == tokenHash && session.ExpiresAt > now &&
                session.User.Status == AccountStatuses.Active)
            .Select(session => new AccountIdentity(session.Id, session.UserId, session.User.Role))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task LogoutAsync(long userId, Guid sessionId, CancellationToken cancellationToken) =>
        dbContext.UserSessions.Where(session => session.Id == sessionId && session.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<AccountProfile?> GetProfileAsync(long userId, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new AccountProfile(user.Id, user.Email, user.DisplayName, user.Role, user.Status, user.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<AccountProfile?> UpdateProfileAsync(
        long userId, string displayName, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }
        user.DisplayName = displayName.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Profile(user);
    }

    public async Task<bool> ChangePasswordAsync(
        long userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = await LockUserAsync(userId, cancellationToken);
        if (user is null || user.Status != AccountStatuses.Active || !VerifyPassword(user, currentPassword))
        {
            return false;
        }

        await ReplacePasswordAsync(user, newPassword, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<PasswordResetDelivery?> PreparePasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(
            user => user.Email == normalizedEmail && user.Status == AccountStatuses.Active, cancellationToken);
        return user is null ? null : new PasswordResetDelivery(user.Email, resetTokens.Create(user));
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken)
    {
        if (!resetTokens.TryRead(token, out var userId, out var passwordFingerprint))
        {
            return false;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = await LockUserAsync(userId, cancellationToken);
        if (user is null || user.Status != AccountStatuses.Active ||
            AccountTokens.Hash(user.PasswordHash) != passwordFingerprint)
        {
            return false;
        }
        await ReplacePasswordAsync(user, newPassword, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<AccountProfile?> SetUserLockAsync(long userId, bool locked, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = await LockUserAsync(userId, cancellationToken);
        if (user is null || user.Role != AccountRoles.User)
        {
            return null;
        }

        user.Status = locked ? AccountStatuses.Locked : AccountStatuses.Active;
        if (locked)
        {
            await RevokeSessionsAsync(user.Id, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Profile(user);
    }

    public async Task<bool> PromoteAdminAsync(string email, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = await LockUserByEmailAsync(email, cancellationToken);
        if (user is null || user.Status != AccountStatuses.Active)
        {
            return false;
        }
        if (user.Role != AccountRoles.Admin)
        {
            user.Role = AccountRoles.Admin;
            await RevokeSessionsAsync(user.Id, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private Task<User?> LockUserAsync(long id, CancellationToken cancellationToken) =>
        dbContext.Users.FromSqlInterpolated($"SELECT * FROM users WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<User?> LockUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Users.FromSqlInterpolated($"SELECT * FROM users WHERE email = {NormalizeEmail(email)} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private bool VerifyPassword(User user, string password) =>
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;

    private async Task ReplacePasswordAsync(User user, string password, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        await RevokeSessionsAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task RevokeSessionsAsync(long userId, CancellationToken cancellationToken) =>
        dbContext.UserSessions.Where(session => session.UserId == userId).ExecuteDeleteAsync(cancellationToken);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static AccountProfile Profile(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role, user.Status, user.CreatedAt);
}
