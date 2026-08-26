using System.Text;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Settings;
using ExItS.Platform.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.Identity;

/// <summary>
/// Delivers auth outbound messages over SMTP (Mailpit for Local Validation; real SMTP in Production).
/// Tokens remain application-owned; Mailpit is only a catcher during Local Validation.
/// </summary>
internal sealed class SmtpPlatformAuthOutboundMessageSink(
    IPlatformEmailDeliveryResolver deliveryResolver,
    IOptions<PlatformEmailDeliveryOptions> emailOptions,
    ILogger<SmtpPlatformAuthOutboundMessageSink> logger) : IPlatformAuthOutboundMessageSink
{
    public async Task PublishAsync(PlatformAuthOutboundMessage message, CancellationToken cancellationToken = default)
    {
        var delivery = await deliveryResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
        if (!delivery.IsConfigured
            || string.IsNullOrWhiteSpace(delivery.SmtpHost)
            || delivery.SmtpPort is not > 0
            || string.IsNullOrWhiteSpace(delivery.AdminPublicBaseUrl))
        {
            logger.LogWarning(
                "SMTP email delivery requested but Platform email settings are not configured. Kind={Kind} UserId={UserId}",
                message.Kind,
                message.UserId);
            return;
        }

        var opts = emailOptions.Value;
        var (subject, body) = PlatformAuthOutboundEmailComposer.Compose(
            message,
            delivery.AdminPublicBaseUrl!,
            opts.PinoyLoanManagerPublicBaseUrl,
            opts.AllowHttpLoopbackPublicUrls,
            opts.LinkGuidanceHtml);
        using var client = Settings.PlatformEmailTestSender.CreateClient(delivery);
        using var mail = new System.Net.Mail.MailMessage
        {
            From = new System.Net.Mail.MailAddress(delivery.FromAddress, delivery.FromDisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
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
                delivery.SmtpHost,
                delivery.SmtpPort);
        }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException or IOException)
        {
            logger.LogError(
                ex,
                "SMTP auth outbound delivery failed. Kind={Kind} UserId={UserId}",
                message.Kind,
                message.UserId);
        }
    }
}
