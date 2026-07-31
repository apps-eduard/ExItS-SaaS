using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformExternalLoginRepository
{
    Task<PlatformExternalLogin?> FindByProviderSubjectAsync(
        string provider,
        string providerSubject,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformExternalLogin login, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformExternalLogin login, CancellationToken cancellationToken = default);
}

public sealed class PlatformExternalAuthOptions
{
    public const string SectionName = "PlatformAuthentication:External";

    public PlatformExternalProviderOptions Google { get; set; } = new();
    public PlatformExternalProviderOptions Facebook { get; set; } = new();

    /// <summary>
    /// Dev/Testing-only simulated external login completion. Forbidden in Production.
    /// </summary>
    public bool TestingEndpointEnabled { get; set; }
}

public sealed class PlatformExternalProviderOptions
{
    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public sealed record ExternalLoginIdentity(
    string Provider,
    string ProviderSubject,
    string Email,
    bool EmailVerified,
    string? DisplayName);
