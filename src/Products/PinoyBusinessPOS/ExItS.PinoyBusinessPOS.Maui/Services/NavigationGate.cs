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
    IUtangCapabilityEvaluator capabilities)
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

        if (accessPolicy.RequiresReconnectToVerifyAccess)
        {
            syncStatus.SetReconnectRequired(true);
            return "/reconnect";
        }

        syncStatus.SetReconnectRequired(false);

        if (!restore.Succeeded || currentUser.Session is null)
        {
            return "/signin";
        }

        if (currentUser.Session.OrganizationId is null || !currentUser.HasPosAccess)
        {
            return "/organization-select";
        }

        if (!accessPolicy.CanEnterProtectedShell)
        {
            syncStatus.SetReconnectRequired(true);
            return "/reconnect";
        }

        if (capabilities.IsAllowed(UtangCapability.ManageOperationalSetup))
        {
            var setupResult = await operationalSetup.GetAsync(ct).ConfigureAwait(false);
            if (setupResult.IsSuccess && setupResult.Data is { IsCompleted: false })
            {
                return "/setup";
            }
        }

        return "/home";
    }

    public bool CanEnterProtectedShell => accessPolicy.CanEnterProtectedShell;
}
