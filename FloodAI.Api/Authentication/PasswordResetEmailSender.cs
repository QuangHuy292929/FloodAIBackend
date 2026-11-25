using FloodAI.Business.Accounts;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FloodAI.Api.Authentication;

public sealed class PasswordResetEmailSender(IConfiguration configuration, ILogger<PasswordResetEmailSender> logger)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["AuthSmtp:Host"]) &&
        configuration["AuthSmtp:From"] is { } from &&
        MailboxAddress.TryParse(from, out _) &&
        (string.IsNullOrWhiteSpace(configuration["AuthSmtp:Username"]) ||
         !string.IsNullOrEmpty(configuration["AuthSmtp:Password"]));

    public async Task SendAsync(PasswordResetDelivery reset, CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(configuration["AuthSmtp:From"]!));
            message.To.Add(new MailboxAddress("", reset.Email));
            message.Subject = "FloodAI password reset";
            message.Body = new TextPart("plain")
            {
                Text = $"Use this token to reset your FloodAI password. It expires in 30 minutes.\n\n{reset.Token}"
            };

            using var client = new SmtpClient { Timeout = 10000 };
            var port = configuration.GetValue("AuthSmtp:Port", 587);
            var security = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(configuration["AuthSmtp:Host"]!, port, security, cancellationToken);
            if (configuration["AuthSmtp:Username"] is { } username && !string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(username, configuration["AuthSmtp:Password"]!, cancellationToken);
            }
            var result = await client.SendAsync(FormatOptions.Default, new SmtpSendRequest(message), cancellationToken);
            if (result.RejectedRecipients.Count > 0)
            {
                logger.LogWarning("SMTP rejected the password reset recipient.");
            }
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("Password reset email delivery failed ({ErrorType}).", error.GetType().Name);
        }
    }
}
