using System.Net;
using System.Net.Mail;
using System.Text;
using ExItS.Platform.Application.Settings;
using ExItS.Platform.Domain.Settings;

namespace ExItS.Platform.Infrastructure.Settings;

internal sealed class PlatformEmailTestSender : IPlatformEmailTestSender
{
    public async Task SendTestEmailAsync(
        string recipientEmail,
        ResolvedPlatformEmailDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        if (!delivery.IsConfigured
            || string.IsNullOrWhiteSpace(delivery.SmtpHost)
            || delivery.SmtpPort is not > 0
            || string.IsNullOrWhiteSpace(delivery.FromAddress))
        {
            throw new InvalidOperationException("Platform email delivery is not configured.");
        }

        using var client = CreateClient(delivery);
        using var mail = new MailMessage
        {
            From = new MailAddress(delivery.FromAddress, delivery.FromDisplayName),
            Subject = "ExItS Platform test email",
            Body = "<p>This is a test message from Platform Settings.</p>",
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };
        mail.To.Add(recipientEmail);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or IOException)
        {
            throw new InvalidOperationException("SMTP test delivery failed.", ex);
        }
    }

    internal static SmtpClient CreateClient(ResolvedPlatformEmailDelivery delivery)
    {
        var client = new SmtpClient(delivery.SmtpHost!, delivery.SmtpPort!.Value)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            EnableSsl = delivery.SecurityMode == PlatformSmtpSecurityMode.Ssl,
        };

        if (!string.IsNullOrWhiteSpace(delivery.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(delivery.SmtpUsername, delivery.SmtpPassword ?? string.Empty);
        }

        return client;
    }
}
