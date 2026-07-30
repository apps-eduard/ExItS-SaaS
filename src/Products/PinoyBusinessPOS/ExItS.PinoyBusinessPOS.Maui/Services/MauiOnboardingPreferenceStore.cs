using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>Non-secret onboarding and organization preference persistence via MAUI Preferences.</summary>
public sealed class MauiOnboardingPreferenceStore : IOnboardingPreferenceStore
{
    public Task<bool> GetOnboardingCompletedAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Preferences.Default.Get(PreferenceKeys.OnboardingCompleted, false));
    }

    public Task SetOnboardingCompletedAsync(bool completed, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Preferences.Default.Set(PreferenceKeys.OnboardingCompleted, completed);
        return Task.CompletedTask;
    }

    public Task<string?> GetOnboardingStepAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(Preferences.Default.Get(PreferenceKeys.OnboardingStep, (string?)null));
    }

    public Task SetOnboardingStepAsync(string step, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Preferences.Default.Set(PreferenceKeys.OnboardingStep, step);
        return Task.CompletedTask;
    }

    public Task<Guid?> GetSelectedOrganizationIdAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var text = Preferences.Default.Get(PreferenceKeys.SelectedOrganizationId, (string?)null);
        return Task.FromResult(Guid.TryParse(text, out var id) ? id : (Guid?)null);
    }

    public Task SetSelectedOrganizationIdAsync(Guid? organizationId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (organizationId is null)
        {
            Preferences.Default.Remove(PreferenceKeys.SelectedOrganizationId);
        }
        else
        {
            Preferences.Default.Set(PreferenceKeys.SelectedOrganizationId, organizationId.Value.ToString("D"));
        }

        return Task.CompletedTask;
    }

    public Task<bool> GetDevEnvironmentConfirmedAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Preferences.Default.Get(PreferenceKeys.DevEnvironmentConfirmed, false));
    }

    public Task SetDevEnvironmentConfirmedAsync(bool confirmed, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Preferences.Default.Set(PreferenceKeys.DevEnvironmentConfirmed, confirmed);
        return Task.CompletedTask;
    }

    public Task ClearOrganizationPreferenceAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Preferences.Default.Remove(PreferenceKeys.SelectedOrganizationId);
        return Task.CompletedTask;
    }
}
