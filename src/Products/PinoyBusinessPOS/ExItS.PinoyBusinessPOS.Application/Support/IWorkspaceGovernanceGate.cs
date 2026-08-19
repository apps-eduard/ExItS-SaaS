using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.Support;

/// <summary>
/// Mobile workspace governance exposure (P28-WP15B). Lazy — does not preload on burger open unless queried.
/// </summary>
public interface IWorkspaceGovernanceGate
{
    /// <summary>Owner/Administrator on Primary/Main selected branch — may open Manage Business.</summary>
    Task<bool> CanAccessManageBusinessAsync(CancellationToken ct = default);

    /// <summary>Selected workspace branch is the organization primary branch.</summary>
    Task<bool> IsPrimaryWorkspaceAsync(CancellationToken ct = default);

    /// <summary>Current management branch id from session, if any.</summary>
    Guid? GetSelectedBranchId();
}

public sealed class WorkspaceGovernanceGate(
    ICurrentUserContext currentUser,
    IOrganizationOwnerProbe ownerProbe,
    IPlatformAccessClient platform) : IWorkspaceGovernanceGate
{
    private Guid? _cachedBranchId;
    private bool? _isPrimaryCached;
    private bool? _canManageCached;

    public Guid? GetSelectedBranchId()
    {
        var session = currentUser.Session;
        return session is null ? null : AuthSessionBranchContext.GetSelectedBranchId(session);
    }

    public async Task<bool> IsPrimaryWorkspaceAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session?.OrganizationId is not Guid orgId)
        {
            ClearCache();
            return false;
        }

        var selected = AuthSessionBranchContext.GetSelectedBranchId(session);
        if (selected is not Guid branchId)
        {
            ClearCache();
            return false;
        }

        if (_cachedBranchId == branchId && _isPrimaryCached is bool cached)
        {
            return cached;
        }

        try
        {
            var branches = await platform.GetBranchesAsync(orgId, ct).ConfigureAwait(false);
            if (!branches.IsSuccess || branches.Data is null)
            {
                ClearCache();
                return false;
            }

            var primary = branches.Data.FirstOrDefault(b => b.IsPrimary);
            _cachedBranchId = branchId;
            _isPrimaryCached = primary is not null && primary.Id == branchId;
            _canManageCached = null;
            return _isPrimaryCached.Value;
        }
        catch
        {
            ClearCache();
            return false;
        }
    }

    public async Task<bool> CanAccessManageBusinessAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session?.OrganizationId is not Guid orgId)
        {
            ClearCache();
            return false;
        }

        var selected = AuthSessionBranchContext.GetSelectedBranchId(session);
        if (_cachedBranchId == selected && _canManageCached is bool cached)
        {
            return cached;
        }

        if (!await ownerProbe.IsOwnerAsync(session, orgId, ct).ConfigureAwait(false))
        {
            _cachedBranchId = selected;
            _canManageCached = false;
            return false;
        }

        _canManageCached = await IsPrimaryWorkspaceAsync(ct).ConfigureAwait(false);
        return _canManageCached.Value;
    }

    private void ClearCache()
    {
        _cachedBranchId = null;
        _isPrimaryCached = null;
        _canManageCached = null;
    }
}
