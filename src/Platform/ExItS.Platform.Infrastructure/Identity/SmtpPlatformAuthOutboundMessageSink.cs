using System.Net;
using System.Net.Mail;
using System.Text;
using ExItS.Platform.Application.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.Identity;

/// <summary>
/// Delivers auth outbound messages over SMTP (Mailpit for Local Validation; real SMTP in Production).
/// Tokens remain application-owned; Mailpit is only a catcher during Local Validation.
/// </summary>
internal sealed class SmtpPlatformAuthOutboundMessageSink(
    IOptions<PlatformEmailDeliveryOptions> options,
    ILogger<SmtpPlatformAuthOutboundMessageSink> logger) : IPlatformAuthOutboundMessageSink
{
    public async Task PublishAsync(PlatformAuthOutboundMessage message, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (!opts.IsConfigured)
        {
            logger.LogWarning(
                "SMTP email delivery requested but PlatformEmail is not configured. Kind={Kind} UserId={UserId}",
                message.Kind,
                message.UserId);
            return;
        }

        var (subject, body) = BuildMessage(opts, message);
        using var client = new SmtpClient(opts.SmtpHost!, opts.SmtpPort)
        {
            EnableSsl = opts.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(opts.FromAddress, opts.FromDisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        mail.To.Add(message.Email);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Auth outbound email delivered via SMTP. Kind={Kind} UserId={UserId} Host={Host}:{Port}",
                message.Kind,
                message.UserId,
                opts.SmtpHost,
                opts.SmtpPort);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or IOException)
        {
            logger.LogError(
                ex,
                "SMTP auth outbound delivery failed. Kind={Kind} UserId={UserId}",
                message.Kind,
                message.UserId);
            throw;
        }
    }

    private static (string Subject, string Body) BuildMessage(
        PlatformEmailDeliveryOptions opts,
        PlatformAuthOutboundMessage message)
    {
        var baseUrl = opts.AdminPublicBaseUrl!.TrimEnd('/');
        var encodedToken = WebUtility.UrlEncode(message.OpaqueToken);

        return message.Kind switch
        {
            PlatformAuthOutboundMessageKinds.EmailVerification => (
                "Verify your ExItS account",
                $"""
                 <p>Welcome to ExItS.</p>
                 <p>Confirm your email and create your password to activate your Personal Account.</p>
                 <p><a href="{baseUrl}/admin/activate-account?token={encodedToken}">Activate your account</a></p>
                 <p>This link expires at {message.ExpiresAtUtc:u} (UTC).</p>
                 <p>If you did not register, you can ignore this message.</p>
                 """),
            PlatformAuthOutboundMessageKinds.PasswordReset => (
                "Reset your ExItS password",
                $"""
                 <p>A password reset was requested for your ExItS account.</p>
                 <p><a href="{baseUrl}/admin/reset-password?token={encodedToken}">Reset password</a></p>
                 <p>This link expires at {message.ExpiresAtUtc:u} (UTC).</p>
                 """),
            PlatformAuthOutboundMessageKinds.RecoveryEmailVerification => (
                "Confirm your ExItS recovery email",
                $"""
                 <p>Confirm your recovery email for ExItS.</p>
                 <p><a href="{baseUrl}/admin/confirm-recovery-email?token={encodedToken}">Confirm recovery email</a></p>
                 <p>This link expires at {message.ExpiresAtUtc:u} (UTC).</p>
                 """),
            _ => (
                "ExItS account message",
                $"<p>ExItS account message ({WebUtility.HtmlEncode(message.Kind)}).</p><p>Token expires at {message.ExpiresAtUtc:u} (UTC).</p>")
        };
    }
}
