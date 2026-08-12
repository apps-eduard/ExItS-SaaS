using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>In-memory per-user offline grant/PIN store for unit tests.</summary>
internal sealed class MemoryOfflineGrantStore : IOfflineOperatingGrantStore
{
    private readonly Dictionary<Guid, OfflineOperatingGrant> _grants = new();
    private readonly Dictionary<Guid, OfflinePinVerifier> _pins = new();

    public Task EnsureMigratedAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<OfflineEnrolledUserSummary>> GetEnrolledUsersAsync(
        CancellationToken ct = default)
    {
        var list = _grants.Values
            .OrderBy(g => g.DisplayName ?? g.Username ?? g.UserId.ToString("D"), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                _pins.TryGetValue(g.UserId, out var pin);
                var hasPin = pin is not null
                    && !string.IsNullOrWhiteSpace(pin.HashBase64)
                    && !string.IsNullOrWhiteSpace(pin.SaltBase64);
                return new OfflineEnrolledUserSummary(
                    g.UserId,
                    string.IsNullOrWhiteSpace(g.DisplayName) ? (g.Username ?? g.UserId.ToString("D")) : g.DisplayName!,
                    g.Username,
                    g.ScopeKind,
                    g.OrganizationDisplayName,
                    g.ExpiresAtUtc,
                    hasPin);
            })
            .ToArray();
        return Task.FromResult<IReadOnlyList<OfflineEnrolledUserSummary>>(list);
    }

    public Task<OfflineOperatingGrant?> LoadGrantAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(_grants.TryGetValue(userId, out var g) ? g : null);

    public Task SaveGrantAsync(OfflineOperatingGrant grant, CancellationToken ct = default)
    {
        _grants[grant.UserId] = grant;
        return Task.CompletedTask;
    }

    public Task ClearGrantAsync(Guid userId, CancellationToken ct = default)
    {
        _grants.Remove(userId);
        return Task.CompletedTask;
    }

    public Task<OfflinePinVerifier?> LoadPinVerifierAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(_pins.TryGetValue(userId, out var p) ? p : null);

    public Task SavePinVerifierAsync(Guid userId, OfflinePinVerifier verifier, CancellationToken ct = default)
    {
        _pins[userId] = verifier.UserId is Guid id && id != Guid.Empty
            ? verifier
            : verifier with { UserId = userId };
        return Task.CompletedTask;
    }

    public Task ClearPinVerifierAsync(Guid userId, CancellationToken ct = default)
    {
        _pins.Remove(userId);
        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(Guid userId, CancellationToken ct = default)
    {
        _grants.Remove(userId);
        _pins.Remove(userId);
        return Task.CompletedTask;
    }
}
