using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// SecureStorage-backed per-user offline grants + PIN verifiers.
/// Survives logout. Migrates legacy single-slot keys when user identity is attributable.
/// </summary>
public sealed class OfflineOperatingGrantStore(ISecureTokenStore tokens) : IOfflineOperatingGrantStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly SemaphoreSlim _migrateGate = new(1, 1);
    private bool _migrated;

    public async Task EnsureMigratedAsync(CancellationToken ct = default)
    {
        if (_migrated)
        {
            return;
        }

        await _migrateGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_migrated)
            {
                return;
            }

            await MigrateLegacyAsync(ct).ConfigureAwait(false);
            _migrated = true;
        }
        finally
        {
            _migrateGate.Release();
        }
    }

    public async Task<IReadOnlyList<OfflineEnrolledUserSummary>> GetEnrolledUsersAsync(
        CancellationToken ct = default)
    {
        await EnsureMigratedAsync(ct).ConfigureAwait(false);
        var directory = await LoadDirectoryAsync(ct).ConfigureAwait(false);
        if (directory.Users.Count == 0)
        {
            return Array.Empty<OfflineEnrolledUserSummary>();
        }

        var list = new List<OfflineEnrolledUserSummary>(directory.Users.Count);
        foreach (var entry in directory.Users)
        {
            var pin = await LoadPinVerifierCoreAsync(entry.UserId, ct).ConfigureAwait(false);
            var hasPin = pin is not null
                && !string.IsNullOrWhiteSpace(pin.HashBase64)
                && !string.IsNullOrWhiteSpace(pin.SaltBase64);
            list.Add(new OfflineEnrolledUserSummary(
                entry.UserId,
                entry.DisplayName,
                entry.Username,
                entry.ScopeKind,
                entry.OrganizationDisplayName,
                entry.ExpiresAtUtc,
                hasPin));
        }

        return list;
    }

    public async Task<OfflineOperatingGrant?> LoadGrantAsync(Guid userId, CancellationToken ct = default)
    {
        await EnsureMigratedAsync(ct).ConfigureAwait(false);
        return await LoadGrantCoreAsync(userId, ct).ConfigureAwait(false);
    }

    public async Task SaveGrantAsync(OfflineOperatingGrant grant, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        if (grant.UserId == Guid.Empty)
        {
            throw new ArgumentException("Grant UserId is required.", nameof(grant));
        }

        await EnsureMigratedAsync(ct).ConfigureAwait(false);
        var json = JsonSerializer.Serialize(grant, JsonOptions);
        await tokens.SetAsync(SecureTokenKeys.OfflineOperatingGrantFor(grant.UserId), json, ct)
            .ConfigureAwait(false);
        await UpsertDirectoryEntryAsync(grant, ct).ConfigureAwait(false);
    }

    public async Task ClearGrantAsync(Guid userId, CancellationToken ct = default)
    {
        await EnsureMigratedAsync(ct).ConfigureAwait(false);
        await tokens.ClearAsync(SecureTokenKeys.OfflineOperatingGrantFor(userId), ct).ConfigureAwait(false);
        await RemoveDirectoryEntryAsync(userId, keepPin: true, ct).ConfigureAwait(false);
    }

    public async Task<OfflinePinVerifier?> LoadPinVerifierAsync(Guid userId, CancellationToken ct = default)
    {
        await EnsureMigratedAsync(ct).ConfigureAwait(false);
        return await LoadPinVerifierCoreAsync(userId, ct).ConfigureAwait(false);
    }

    public async Task SavePinVerifierAsync(
        Guid userId,
        OfflinePinVerifier verifier,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        await EnsureMigratedAsync(ct).ConfigureAwait(false);
        var bound = verifier.UserId is Guid existing && existing != Guid.Empty
            ? verifier
            : verifier with { UserId = userId };
        if (bound.UserId is Guid pinUser && pinUser != userId)
        {
            throw new InvalidOperationException("PIN verifier UserId must match storage slot.");
        }

        var json = JsonSerializer.Serialize(bound, JsonOptions);
        await tokens.SetAsync(SecureTokenKeys.OfflinePinVerifierFor(userId), json, ct)
            .ConfigureAwait(false);

        // Keep directory HasPinConfigured accurate without exposing secrets.
        var grant = await LoadGrantCoreAsync(userId, ct).ConfigureAwait(false);
        if (grant is not null)
        {
            await UpsertDirectoryEntryAsync(grant, ct).ConfigureAwait(false);
        }
    }

    public async Task ClearPinVerifierAsync(Guid userId, CancellationToken ct = default)
    {
        await EnsureMigratedAsync(ct).ConfigureAwait(false);
        await tokens.ClearAsync(SecureTokenKeys.OfflinePinVerifierFor(userId), ct).ConfigureAwait(false);
    }

    public async Task RemoveUserAsync(Guid userId, CancellationToken ct = default)
    {
        await EnsureMigratedAsync(ct).ConfigureAwait(false);
        await tokens.ClearAsync(SecureTokenKeys.OfflineOperatingGrantFor(userId), ct).ConfigureAwait(false);
        await tokens.ClearAsync(SecureTokenKeys.OfflinePinVerifierFor(userId), ct).ConfigureAwait(false);
        await RemoveDirectoryEntryAsync(userId, keepPin: false, ct).ConfigureAwait(false);
    }

    private async Task MigrateLegacyAsync(CancellationToken ct)
    {
        var legacyGrantJson = await tokens.GetAsync(SecureTokenKeys.OfflineOperatingGrant, ct)
            .ConfigureAwait(false);
        var legacyPinJson = await tokens.GetAsync(SecureTokenKeys.OfflinePinVerifier, ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(legacyGrantJson) && string.IsNullOrWhiteSpace(legacyPinJson))
        {
            return;
        }

        OfflineOperatingGrant? legacyGrant = null;
        if (!string.IsNullOrWhiteSpace(legacyGrantJson))
        {
            try
            {
                legacyGrant = JsonSerializer.Deserialize<OfflineOperatingGrant>(legacyGrantJson, JsonOptions);
            }
            catch (JsonException)
            {
                // Corrupt legacy grant — fail closed: leave keys; do not invent a user.
                return;
            }
        }

        OfflinePinVerifier? legacyPin = null;
        if (!string.IsNullOrWhiteSpace(legacyPinJson))
        {
            try
            {
                legacyPin = JsonSerializer.Deserialize<OfflinePinVerifier>(legacyPinJson, JsonOptions);
            }
            catch (JsonException)
            {
                // Corrupt PIN alone: if we can migrate grant, still do; leave corrupt PIN key.
                legacyPin = null;
            }
        }

        Guid? attributableUserId = null;
        if (legacyGrant is not null && legacyGrant.UserId != Guid.Empty)
        {
            attributableUserId = legacyGrant.UserId;
        }
        else if (legacyPin?.UserId is Guid pinUser && pinUser != Guid.Empty)
        {
            attributableUserId = pinUser;
        }

        if (attributableUserId is null)
        {
            // Ambiguous / unbound — fail securely; do not guess. Leave legacy keys.
            return;
        }

        var userId = attributableUserId.Value;
        var existingGrant = await LoadGrantCoreAsync(userId, ct).ConfigureAwait(false);
        if (existingGrant is null && legacyGrant is not null)
        {
            var json = JsonSerializer.Serialize(legacyGrant, JsonOptions);
            await tokens.SetAsync(SecureTokenKeys.OfflineOperatingGrantFor(userId), json, ct)
                .ConfigureAwait(false);
            await UpsertDirectoryEntryAsync(legacyGrant, ct).ConfigureAwait(false);
        }

        var existingPin = await LoadPinVerifierCoreAsync(userId, ct).ConfigureAwait(false);
        if (existingPin is null && legacyPin is not null)
        {
            // Only migrate PIN when attributable to this user (bound or unbound with known grant owner).
            if (legacyPin.UserId is Guid pinOwner && pinOwner != Guid.Empty && pinOwner != userId)
            {
                // PIN belongs to someone else without a clear grant owner match — leave legacy.
                return;
            }

            var bound = legacyPin with { UserId = userId };
            var pinJson = JsonSerializer.Serialize(bound, JsonOptions);
            await tokens.SetAsync(SecureTokenKeys.OfflinePinVerifierFor(userId), pinJson, ct)
                .ConfigureAwait(false);
        }

        // Remove legacy only after successful attributable migration of grant and/or pin into slots.
        if (await LoadGrantCoreAsync(userId, ct).ConfigureAwait(false) is not null
            || await LoadPinVerifierCoreAsync(userId, ct).ConfigureAwait(false) is not null)
        {
            await tokens.ClearAsync(SecureTokenKeys.OfflineOperatingGrant, ct).ConfigureAwait(false);
            await tokens.ClearAsync(SecureTokenKeys.OfflinePinVerifier, ct).ConfigureAwait(false);
        }
    }

    private async Task<OfflineOperatingGrant?> LoadGrantCoreAsync(Guid userId, CancellationToken ct)
    {
        var json = await tokens.GetAsync(SecureTokenKeys.OfflineOperatingGrantFor(userId), ct)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OfflineOperatingGrant>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<OfflinePinVerifier?> LoadPinVerifierCoreAsync(Guid userId, CancellationToken ct)
    {
        var json = await tokens.GetAsync(SecureTokenKeys.OfflinePinVerifierFor(userId), ct)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OfflinePinVerifier>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<OfflineEnrolledUsersDirectory> LoadDirectoryAsync(CancellationToken ct)
    {
        var json = await tokens.GetAsync(SecureTokenKeys.OfflineEnrolledUsers, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new OfflineEnrolledUsersDirectory(
                OfflineEnrolledUsersDirectory.CurrentSchemaVersion,
                Array.Empty<OfflineEnrolledUserDirectoryEntry>());
        }

        try
        {
            var directory = JsonSerializer.Deserialize<OfflineEnrolledUsersDirectory>(json, JsonOptions);
            if (directory is null)
            {
                return new OfflineEnrolledUsersDirectory(
                    OfflineEnrolledUsersDirectory.CurrentSchemaVersion,
                    Array.Empty<OfflineEnrolledUserDirectoryEntry>());
            }

            return directory;
        }
        catch (JsonException)
        {
            return new OfflineEnrolledUsersDirectory(
                OfflineEnrolledUsersDirectory.CurrentSchemaVersion,
                Array.Empty<OfflineEnrolledUserDirectoryEntry>());
        }
    }

    private async Task UpsertDirectoryEntryAsync(OfflineOperatingGrant grant, CancellationToken ct)
    {
        var directory = await LoadDirectoryAsync(ct).ConfigureAwait(false);
        var display = string.IsNullOrWhiteSpace(grant.DisplayName)
            ? (grant.Username ?? grant.UserId.ToString("D"))
            : grant.DisplayName!;
        var entry = new OfflineEnrolledUserDirectoryEntry(
            grant.UserId,
            display,
            grant.Username,
            grant.ScopeKind,
            grant.OrganizationDisplayName,
            grant.ExpiresAtUtc);

        var users = directory.Users
            .Where(u => u.UserId != grant.UserId)
            .Append(entry)
            .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var updated = new OfflineEnrolledUsersDirectory(
            OfflineEnrolledUsersDirectory.CurrentSchemaVersion,
            users);
        var json = JsonSerializer.Serialize(updated, JsonOptions);
        await tokens.SetAsync(SecureTokenKeys.OfflineEnrolledUsers, json, ct).ConfigureAwait(false);
    }

    private async Task RemoveDirectoryEntryAsync(Guid userId, bool keepPin, CancellationToken ct)
    {
        var directory = await LoadDirectoryAsync(ct).ConfigureAwait(false);
        var pinStillPresent = keepPin
            && await LoadPinVerifierCoreAsync(userId, ct).ConfigureAwait(false) is not null;
        // If grant cleared but PIN remains, drop directory entry so unlock list only shows grant holders.
        _ = pinStillPresent;
        var users = directory.Users.Where(u => u.UserId != userId).ToArray();
        if (users.Length == directory.Users.Count)
        {
            return;
        }

        var updated = new OfflineEnrolledUsersDirectory(
            OfflineEnrolledUsersDirectory.CurrentSchemaVersion,
            users);
        if (users.Length == 0)
        {
            await tokens.ClearAsync(SecureTokenKeys.OfflineEnrolledUsers, ct).ConfigureAwait(false);
            return;
        }

        var json = JsonSerializer.Serialize(updated, JsonOptions);
        await tokens.SetAsync(SecureTokenKeys.OfflineEnrolledUsers, json, ct).ConfigureAwait(false);
    }
}
