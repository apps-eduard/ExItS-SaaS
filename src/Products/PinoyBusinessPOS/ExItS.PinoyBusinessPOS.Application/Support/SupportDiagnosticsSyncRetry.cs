using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Application.Support;

public interface IPersonalDiagnosticsSyncRetry
{
    Task RetryAsync(CancellationToken ct = default);
}

public interface IOrganizationDiagnosticsSyncRetry
{
    Task RetryAsync(CancellationToken ct = default);
}

public interface ISupportDiagnosticsRoleReader
{
    Task<string?> GetCurrentRoleLabelAsync(CancellationToken ct = default);
}

public sealed class PersonalDiagnosticsSyncRetry(IPersonalOfflineSyncService personalSync)
    : IPersonalDiagnosticsSyncRetry
{
    public Task RetryAsync(CancellationToken ct = default) =>
        personalSync.TrySyncPendingAsync(ct);
}

public sealed class OrganizationDiagnosticsSyncRetry(
    IConnectivityService connectivity,
    ICustomerCreditOfflineSyncService customerCreditSync,
    IOfflineQueueProcessor queueProcessor) : IOrganizationDiagnosticsSyncRetry
{
    public async Task RetryAsync(CancellationToken ct = default)
    {
        if (await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            await customerCreditSync.ReconcileOnReconnectAsync(ct).ConfigureAwait(false);
            return;
        }

        await queueProcessor.ProcessAvailableAsync(ct).ConfigureAwait(false);
    }
}

public sealed class PosEffectiveRoleReader(
    IConnectivityService connectivity,
    IPosPermissionClient permissions,
    IOfflineOperatingGrantService offlineGrant,
    ICurrentUserContext currentUser) : ISupportDiagnosticsRoleReader
{
    public async Task<string?> GetCurrentRoleLabelAsync(CancellationToken ct = default)
    {
        try
        {
            if (await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
            {
                var effective = await permissions.GetEffectiveAsync(ct).ConfigureAwait(false);
                if (effective.IsSuccess && !string.IsNullOrWhiteSpace(effective.Data?.Role))
                {
                    return effective.Data.Role;
                }
            }
        }
        catch
        {
            // Fall through to grant.
        }

        var session = currentUser.Session;
        var grant = offlineGrant.ActiveUnlockedGrant
            ?? await offlineGrant.PeekStoredGrantAsync(ct).ConfigureAwait(false);
        if (grant is not null
            && grant.IsOrganizationScope
            && session?.OrganizationId is Guid orgId
            && grant.OrganizationId == orgId
            && !string.IsNullOrWhiteSpace(grant.RoleCode))
        {
            return grant.RoleCode;
        }

        return null;
    }
}
