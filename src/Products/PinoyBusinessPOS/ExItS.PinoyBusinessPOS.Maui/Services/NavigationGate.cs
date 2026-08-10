using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>Resolves the correct first route after boot based on onboarding and session state.</summary>
public sealed class NavigationGate(
    IAuthenticationService auth,
    ICurrentUserContext currentUser,
    IOnboardingPreferenceStore preferences,
    IProtectedShellAccessPolicy accessPolicy,
    IPosSyncStatusService syncStatus,
    IPosOperationalSetupClient operationalSetup,
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
            return step switch
            {
                nameof(OnboardingStep.Language) => "/onboarding/language",
                nameof(OnboardingStep.Theme) => "/onboarding/theme",
                nameof(OnboardingStep.Density) => "/onboarding/density",
                nameof(OnboardingStep.DevEnvironment) => "/onboarding/dev-confirm",
                nameof(OnboardingStep.SignIn) => "/signin",
                nameof(OnboardingStep.OrganizationSelect) => "/organization-select",
                nameof(OnboardingStep.AccessConfirm) => "/onboarding/access-confirm",
                _ => "/welcome"
            };
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
}
