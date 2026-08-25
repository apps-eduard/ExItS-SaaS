namespace ExItS.PinoyBusinessPOS.Domain.Onboarding;

/// <summary>Per-step onboarding status stored as varchar matching these names.</summary>
public enum OnboardingStepStatus
{
    NotStarted = 0,
    Completed = 1,
    Skipped = 2
}
