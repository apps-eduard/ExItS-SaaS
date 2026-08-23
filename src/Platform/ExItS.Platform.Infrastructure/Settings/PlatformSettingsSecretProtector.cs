using ExItS.Platform.Application.Settings;
using Microsoft.AspNetCore.DataProtection;

namespace ExItS.Platform.Infrastructure.Settings;

internal sealed class PlatformSettingsSecretProtector(IDataProtectionProvider dataProtectionProvider)
    : IPlatformSettingsSecretProtector
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("ExItS.Platform.Settings.SmtpPassword");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
