namespace ExItS.PinoyBusinessPOS.Domain.Onboarding;

/// <summary>Overall onboarding progress status stored as varchar matching these names.</summary>
public enum OnboardingOverallStatus
{
    InProgress = 0,
    Completed = 1,
    FinishedLater = 2
}
