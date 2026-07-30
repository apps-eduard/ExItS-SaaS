using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>Resolves the correct first route after boot based on onboarding and session state.</summary>
public sealed class NavigationGate(
    IAuthenticationService auth,
    ICurrentUserContext currentUser,
    IOnboardingPreferenceStore preferences)
{
    public async Task<string> ResolveStartRouteAsync(CancellationToken ct = default)
    {
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
        if (!restore.Succeeded || currentUser.Session is null)
        {
            return "/signin";
        }

        if (currentUser.Session.OrganizationId is null || !currentUser.HasPosAccess)
        {
            return "/organization-select";
        }

        return "/home";
    }

    public bool CanEnterProtectedShell =>
        currentUser.IsAuthenticated
        && currentUser.Session?.OrganizationId is not null
        && currentUser.HasPosAccess;
}
