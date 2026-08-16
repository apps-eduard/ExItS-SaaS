using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Platform;
using ExItS.PinoyBusinessPOS.Application.Support;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>Resolves the correct first route after boot based on onboarding and session state.</summary>
public sealed class NavigationGate(
    IAuthenticationService auth,
    ICurrentUserContext currentUser,
    IOnboardingPreferenceStore preferences,
    IProtectedShellAccessPolicy accessPolicy,
    IPosSyncStatusService syncStatus,
    IPosOperationalSetupClient operationalSetup,
    IPlatformAccessClient platformAccess,
    IOrganizationOwnerProbe organizationOwner,
    IUtangCapabilityEvaluator capabilities,
    RoleHomeResolver roleHome)
{
    public async Task<string> ResolveStartRouteAsync(CancellationToken ct = default)
    {
        await accessPolicy.InitializeAsync(ct).ConfigureAwait(false);
        await syncStatus.InitializeAsync(ct).ConfigureAwait(false);

        if (!await preferences.GetOnboardingCompletedAsync(ct).ConfigureAwait(false))
        {
            var step = await preferences.GetOnboardingStepAsync(ct).ConfigureAwait(false);
            // Skip Welcome / Language / Theme / Density / Dev preference wizard — defaults apply
            // (Compact density, system theme). Open Sign-In directly for a faster first launch.
            if (string.Equals(step, nameof(OnboardingStep.OrganizationSelect), StringComparison.Ordinal))
            {
                return "/organization-select";
            }

            if (string.Equals(step, nameof(OnboardingStep.AccessConfirm), StringComparison.Ordinal))
            {
                return "/onboarding/access-confirm";
            }

            if (!string.Equals(step, nameof(OnboardingStep.SignIn), StringComparison.Ordinal))
            {
                await preferences.SetOnboardingStepAsync(nameof(OnboardingStep.SignIn), ct)
                    .ConfigureAwait(false);
            }

            return "/signin";
        }

        var restore = await auth.RestoreSessionAsync(ct).ConfigureAwait(false);
        syncStatus.Refresh();

        // Cold-start offline with a valid grant: collect PIN before treating as reconnect wall.
        if (!restore.Succeeded
            && restore.FailureReason == AuthFailureReason.Offline
            && string.Equals(restore.SafeMessageKey, "Offline_PinRequired", StringComparison.Ordinal))
        {
            syncStatus.SetReconnectRequired(false);
            return "/offline-pin";
        }

        if (accessPolicy.RequiresReconnectToVerifyAccess)
        {
            // Still offer PIN unlock when a grant exists (e.g. restore partially succeeded).
            var offer = await auth.EvaluateOfflineColdStartOfferAsync(ct).ConfigureAwait(false);
            if (offer.CanOfferPinUnlock)
            {
                syncStatus.SetReconnectRequired(false);
                return "/offline-pin";
            }

            syncStatus.SetReconnectRequired(true);
            return "/reconnect";
        }

        syncStatus.SetReconnectRequired(false);

        if (!restore.Succeeded || currentUser.Session is null)
        {
            return "/signin";
        }

        // Personal default (AccountClass Personal, unlocked): never land on Organization essentials
        // because of a leftover device SelectedOrganizationId / forged OrganizationId.
        if (AuthSessionWorkspace.IsPersonalDefault(currentUser.Session)
            || currentUser.Session.OrganizationId is null)
        {
            // Establish Personal grant before PIN check so enrollment can persist the verifier.
            await auth.EnsurePersonalAccountProfileAsync(ct).ConfigureAwait(false);

            if (await RequiresOfflinePinSetupAsync(ct).ConfigureAwait(false))
            {
                return "/offline-pin-setup";
            }

            return RoleHomeResolver.PersonalHome;
        }

        if (!currentUser.HasPosAccess)
        {
            // Organization selected but POS entitlement/access not active — Org Owner essentials.
            return RoleHomeResolver.OrgEssentials;
        }

        if (!accessPolicy.CanEnterProtectedShell)
        {
            syncStatus.SetReconnectRequired(true);
            return "/reconnect";
        }

        // A POS grant is device-bound. Do this after online/PIN access has been validated but
        // before catalog/setup routes so an owner cannot enter selling workflows unregistered.
        if (currentUser.Session.PosDeviceId is null || currentUser.Session.BranchId is null)
        {
            // Optional additional Business Types (Growth/Pro) before device registration.
            if (currentUser.Session.OrganizationId is Guid pendingOrgId
                && await preferences.GetBusinessTypeActivationPromptPendingAsync(pendingOrgId, ct)
                    .ConfigureAwait(false))
            {
                return "/onboarding/business-types";
            }

            return "/devices/register";
        }

        // PIN before template / sell-critical setup so offline unlock is ready immediately after
        // Start Business, trial, or first POS-role entry on this device.
        await auth.EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
        if (!await auth.HasOfflinePinConfiguredAsync(ct).ConfigureAwait(false))
        {
            return "/offline-pin-setup";
        }

        // One-time skippable starter-products suggest (Start Business / trial). Do not require
        // ManageCatalog here — trial sessions may reach setup before feature codes hydrate;
        // CatalogImport still enforces manage rights for the actual import API.
        if (currentUser.Session.OrganizationId is Guid organizationId
            && await preferences.GetBusinessTemplatePromptPendingAsync(organizationId, ct).ConfigureAwait(false))
        {
            return "/catalog/import?onboarding=1";
        }

        // Education is a soft setup prompt for the exact current Organization Owner.
        // If the Platform status cannot be read, continue normally; selling and sync are never blocked.
        if (await RequiresSalesDocumentEducationAsync(ct).ConfigureAwait(false))
        {
            return "/sales-document-education";
        }

        if (capabilities.IsAllowed(UtangCapability.ManageOperationalSetup))
        {
            var setupResult = await operationalSetup.GetAsync(ct).ConfigureAwait(false);
            if (setupResult.IsSuccess && setupResult.Data is { IsCompleted: false })
            {
                return "/setup";
            }
        }

        return await roleHome.ResolvePosHomeAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Organization POS first-time setup still blocking (device → PIN → template → /setup).
    /// Used to hide primary bottom navigation and bounce out of selling routes.
    /// </summary>
    public async Task<bool> IsOrgPosFirstTimeSetupIncompleteAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session?.OrganizationId is null || !currentUser.HasPosAccess)
        {
            return false;
        }

        if (await HasEarlierOrgPosFirstTimeSetupStepAsync(session, ct).ConfigureAwait(false))
        {
            return true;
        }

        if (capabilities.IsAllowed(UtangCapability.ManageOperationalSetup))
        {
            var setupResult = await operationalSetup.GetAsync(ct).ConfigureAwait(false);
            if (setupResult.IsSuccess && setupResult.Data is { IsCompleted: false })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Device / PIN / starter-template steps that must finish before operational setup.
    /// Does not call Platform education or operational-setup APIs.
    /// </summary>
    public async Task<bool> HasEarlierOrgPosFirstTimeSetupStepAsync(CancellationToken ct = default)
    {
        var session = currentUser.Session;
        if (session?.OrganizationId is null || !currentUser.HasPosAccess)
        {
            return false;
        }

        return await HasEarlierOrgPosFirstTimeSetupStepAsync(session, ct).ConfigureAwait(false);
    }

    private async Task<bool> HasEarlierOrgPosFirstTimeSetupStepAsync(
        AuthSession session,
        CancellationToken ct)
    {
        if (session.OrganizationId is null)
        {
            return false;
        }

        if (session.PosDeviceId is null || session.BranchId is null)
        {
            return true;
        }

        await auth.EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
        if (!await auth.HasOfflinePinConfiguredAsync(ct).ConfigureAwait(false))
        {
            return true;
        }

        return await preferences
            .GetBusinessTemplatePromptPendingAsync(session.OrganizationId.Value, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// After the owner acknowledges sales-document education — skip re-querying education status
    /// and avoid a full cold <see cref="ResolveStartRouteAsync"/> (restore/session). Continues at
    /// operational setup when incomplete, otherwise the role home.
    /// </summary>
    public async Task<string> ResolveRouteAfterSalesDocumentEducationAsync(CancellationToken ct = default)
    {
        if (currentUser.Session?.OrganizationId is null || !currentUser.HasPosAccess)
        {
            return await ResolveStartRouteAsync(ct).ConfigureAwait(false);
        }

        if (await HasEarlierOrgPosFirstTimeSetupStepAsync(currentUser.Session, ct).ConfigureAwait(false))
        {
            return await ResolveStartRouteAsync(ct).ConfigureAwait(false);
        }

        if (capabilities.IsAllowed(UtangCapability.ManageOperationalSetup))
        {
            try
            {
                var setupResult = await operationalSetup.GetAsync(ct).ConfigureAwait(false);
                if (!setupResult.IsSuccess || setupResult.Data is { IsCompleted: false })
                {
                    return "/setup";
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Prefer setup so the owner can retry loading the form instead of hanging.
                return "/setup";
            }
        }

        return await roleHome.ResolvePosHomeAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Routes allowed while <see cref="IsOrgPosFirstTimeSetupIncompleteAsync"/> is true.
    /// Anything else should redirect through <see cref="ResolveStartRouteAsync"/>.
    /// </summary>
    public static bool IsOrgFirstTimeSetupRoute(string absoluteOrRelativeUri)
    {
        if (string.IsNullOrWhiteSpace(absoluteOrRelativeUri))
        {
            return false;
        }

        string path;
        string query;
        if (Uri.TryCreate(absoluteOrRelativeUri, UriKind.Absolute, out var absolute))
        {
            path = absolute.AbsolutePath;
            query = absolute.Query.TrimStart('?');
        }
        else
        {
            var trimmed = absoluteOrRelativeUri.Trim();
            var queryIndex = trimmed.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                path = trimmed[..queryIndex];
                query = trimmed[(queryIndex + 1)..];
            }
            else
            {
                path = trimmed.Split('#', 2)[0];
                query = string.Empty;
            }

            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }
        }

        path = path.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
        {
            path = "/";
        }

        if (path.Equals("/devices/register", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/offline-pin-setup", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/offline-pin", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/setup", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/sales-document-education", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/reconnect", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/signin", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/organization-select", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/org", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/onboarding/business-types", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/org/business-types", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/catalog/import/jobs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.Equals("/catalog/import", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length == 0)
                {
                    continue;
                }

                if (!string.Equals(Uri.UnescapeDataString(pair[0]), "onboarding", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : "1";
                if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Catalog import wizard is AuthShell-only; treat it as setup chrome even without the query.
            return true;
        }

        return false;
    }

    /// <summary>
    /// Mandatory offline PIN enrollment after online auth (Personal Utang and Organization POS).
    /// Without a PIN, cold-start cannot unlock the offline grant and the app is forced online.
    /// </summary>
    private async Task<bool> RequiresOfflinePinSetupAsync(CancellationToken ct)
    {
        await auth.EnsureOfflineOperateGrantAsync(ct).ConfigureAwait(false);
        if (await auth.HasOfflinePinConfiguredAsync(ct).ConfigureAwait(false))
        {
            return false;
        }

        // Online Personal/POS operate: always enroll when missing (do not depend only on cold-start offer).
        if (currentUser.Session is not null
            && (currentUser.HasPosAccess
                || AuthSessionWorkspace.IsPersonalDefault(currentUser.Session)
                || currentUser.Session.OrganizationId is null))
        {
            return true;
        }

        var offer = await auth.EvaluateOfflineColdStartOfferAsync(ct).ConfigureAwait(false);
        return offer.Grant is not null
               || string.Equals(offer.DenialReasonCode, "offline_pin_not_configured", StringComparison.Ordinal);
    }

    public bool CanEnterProtectedShell => accessPolicy.CanEnterProtectedShell;

    public async Task<string> ResolveOperationalSetupRouteAsync(CancellationToken ct = default) =>
        await RequiresSalesDocumentEducationAsync(ct).ConfigureAwait(false)
            ? "/sales-document-education"
            : "/setup";

    private async Task<bool> RequiresSalesDocumentEducationAsync(CancellationToken ct)
    {
        if (currentUser.Session?.OrganizationId is not Guid organizationId
            || !await organizationOwner
                .IsExactOrganizationOwnerAsync(currentUser.Session, organizationId, ct)
                .ConfigureAwait(false))
        {
            return false;
        }

        var education = await platformAccess
            .GetSalesDocumentEducationStatusAsync(organizationId, ct)
            .ConfigureAwait(false);
        return education.IsSuccess && education.Data?.RequiresOwnerAction == true;
    }
}
