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

        var (subject, body) = PlatformAuthOutboundEmailComposer.Compose(message, opts.AdminPublicBaseUrl!);
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
}
