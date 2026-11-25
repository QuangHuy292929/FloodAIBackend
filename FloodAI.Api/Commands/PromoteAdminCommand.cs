using FloodAI.Business.Accounts;

namespace FloodAI.Api.Commands;

internal static class PromoteAdminCommand
{
    public const string Name = "--promote-admin";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        if (args is not [var email] || string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine("Usage: --promote-admin <registered email>");
            return 2;
        }

        await using var scope = services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        if (!await accounts.PromoteAdminAsync(email, CancellationToken.None))
        {
            Console.Error.WriteLine("The account was not found or is not active.");
            return 1;
        }
        Console.WriteLine("The account has the ADMIN role. Sign in again to obtain a new session.");
        return 0;
    }
}
