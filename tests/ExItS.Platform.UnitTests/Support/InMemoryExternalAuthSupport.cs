using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformUserCredentialRepository : IPlatformUserCredentialRepository
{
    private readonly Dictionary<Guid, PlatformUserCredential> _byUserId = new();

    public Task<PlatformUserCredential?> GetByUserIdAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byUserId.TryGetValue(userId.Value, out var credential) ? credential : null);

    public Task<PlatformUserId?> FindUserIdByVerifiedRecoveryEmailAsync(
        string normalizedRecoveryEmail,
        CancellationToken cancellationToken = default)
    {
        var email = normalizedRecoveryEmail.Trim().ToLowerInvariant();
        var match = _byUserId.Values.FirstOrDefault(c =>
            c.HasVerifiedRecoveryEmail
            && string.Equals(c.RecoveryNormalizedEmail, email, StringComparison.Ordinal));
        return Task.FromResult(match?.UserId);
    }

    public Task<bool> IsRecoveryEmailInUseAsync(
        string normalizedRecoveryEmail,
        PlatformUserId? excludingUserId,
        CancellationToken cancellationToken = default)
    {
        var email = normalizedRecoveryEmail.Trim().ToLowerInvariant();
        var inUse = _byUserId.Values.Any(c =>
            c.HasVerifiedRecoveryEmail
            && string.Equals(c.RecoveryNormalizedEmail, email, StringComparison.Ordinal)
            && (excludingUserId is null || c.UserId != excludingUserId));
        return Task.FromResult(inUse);
    }

    public Task AddAsync(PlatformUserCredential credential, CancellationToken cancellationToken = default)
    {
        _byUserId[credential.UserId.Value] = credential;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformUserCredential credential, CancellationToken cancellationToken = default)
    {
        _byUserId[credential.UserId.Value] = credential;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPlatformExternalLoginRepository : IPlatformExternalLoginRepository
{
    private readonly List<PlatformExternalLogin> _items = [];

    public Task<PlatformExternalLogin?> FindByProviderSubjectAsync(
        string provider,
        string providerSubject,
        CancellationToken cancellationToken = default)
    {
        var normalized = provider.Trim().ToLowerInvariant();
        var subject = providerSubject.Trim();
        return Task.FromResult(_items.FirstOrDefault(x =>
            x.Provider == normalized && x.ProviderSubject == subject));
    }

    public Task AddAsync(PlatformExternalLogin login, CancellationToken cancellationToken = default)
    {
        _items.Add(login);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformExternalLogin login, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(x => x.Id == login.Id);
        if (index >= 0)
        {
            _items[index] = login;
        }

        return Task.CompletedTask;
    }
}

internal sealed class StubSessionTokenService : IPlatformSessionTokenService
{
    public string CreateOpaqueToken() => Convert.ToHexString(Guid.NewGuid().ToByteArray()) + Convert.ToHexString(Guid.NewGuid().ToByteArray());

    public string HashToken(string opaqueToken) => "hash:" + opaqueToken;
}
