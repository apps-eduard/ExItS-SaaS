import type { OrganizationOnboardingProgressDto } from "@/api/pos/pos-onboarding-client";

export type OnboardingWizardStep = "organization" | "business" | "products" | "ready";

export const ONBOARDING_WIZARD_STEPS: OnboardingWizardStep[] = [
  "organization",
  "business",
  "products",
  "ready",
];

export function resolveOnboardingWizardStep(
  progress: OrganizationOnboardingProgressDto | null | undefined,
): OnboardingWizardStep {
  if (!progress || progress.overallStatus === "Completed") {
    return "ready";
  }

  if (progress.organizationSetupStatus === "NotStarted") {
    return "organization";
  }
  if (progress.businessSetupStatus === "NotStarted") {
    return "business";
  }
  if (progress.productTemplateStatus === "NotStarted") {
    return "products";
  }
  return "ready";
}

export function shouldResumeOnboarding(
  progress: OrganizationOnboardingProgressDto | null | undefined,
): boolean {
  return progress?.overallStatus === "InProgress";
}

export function shouldShowFinishSetupEntry(
  progress: OrganizationOnboardingProgressDto | null | undefined,
): boolean {
  if (!progress) return false;
  if (progress.overallStatus === "Completed") return false;
  return (
    progress.overallStatus === "FinishedLater" ||
    progress.overallStatus === "InProgress" ||
    progress.organizationSetupStatus === "Skipped" ||
    progress.businessSetupStatus === "Skipped" ||
    progress.productTemplateStatus === "Skipped"
  );
}
