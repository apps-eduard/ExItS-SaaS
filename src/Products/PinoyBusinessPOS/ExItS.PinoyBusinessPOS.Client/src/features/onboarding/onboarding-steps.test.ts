import { describe, expect, it } from "vitest";
import {
  resolveOnboardingWizardStep,
  shouldResumeOnboarding,
  shouldShowFinishSetupEntry,
} from "@/features/onboarding/onboarding-steps";
import type { OrganizationOnboardingProgressDto } from "@/api/pos/pos-onboarding-client";
import { resolveBusinessSetupPreset } from "@/features/onboarding/business-setup-presets";

function progress(
  partial: Partial<OrganizationOnboardingProgressDto>,
): OrganizationOnboardingProgressDto {
  return {
    organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    organizationSetupStatus: "NotStarted",
    businessSetupStatus: "NotStarted",
    productTemplateStatus: "NotStarted",
    overallStatus: "InProgress",
    primaryBusinessTypeId: null,
    updatedAtUtc: "2026-08-25T00:00:00Z",
    createdAtUtc: "2026-08-25T00:00:00Z",
    ...partial,
  };
}

describe("resolveOnboardingWizardStep", () => {
  it("starts at organization when all NotStarted", () => {
    expect(resolveOnboardingWizardStep(progress({}))).toBe("organization");
  });

  it("advances past completed/skipped organization setup", () => {
    expect(
      resolveOnboardingWizardStep(
        progress({ organizationSetupStatus: "Skipped", businessSetupStatus: "NotStarted" }),
      ),
    ).toBe("business");
  });

  it("lands on products after business step", () => {
    expect(
      resolveOnboardingWizardStep(
        progress({
          organizationSetupStatus: "Completed",
          businessSetupStatus: "Skipped",
          productTemplateStatus: "NotStarted",
        }),
      ),
    ).toBe("products");
  });

  it("lands on ready when all steps resolved", () => {
    expect(
      resolveOnboardingWizardStep(
        progress({
          organizationSetupStatus: "Skipped",
          businessSetupStatus: "Skipped",
          productTemplateStatus: "Skipped",
        }),
      ),
    ).toBe("ready");
  });

  it("shows ready when overall completed", () => {
    expect(resolveOnboardingWizardStep(progress({ overallStatus: "Completed" }))).toBe("ready");
  });
});

describe("shouldResumeOnboarding", () => {
  it("resumes only InProgress", () => {
    expect(shouldResumeOnboarding(progress({ overallStatus: "InProgress" }))).toBe(true);
    expect(shouldResumeOnboarding(progress({ overallStatus: "FinishedLater" }))).toBe(false);
    expect(shouldResumeOnboarding(progress({ overallStatus: "Completed" }))).toBe(false);
    expect(shouldResumeOnboarding(null)).toBe(false);
  });
});

describe("shouldShowFinishSetupEntry", () => {
  it("shows for FinishedLater and incomplete skips", () => {
    expect(shouldShowFinishSetupEntry(progress({ overallStatus: "FinishedLater" }))).toBe(true);
    expect(
      shouldShowFinishSetupEntry(
        progress({
          overallStatus: "FinishedLater",
          organizationSetupStatus: "Skipped",
          businessSetupStatus: "Skipped",
          productTemplateStatus: "Skipped",
        }),
      ),
    ).toBe(true);
    expect(shouldShowFinishSetupEntry(progress({ overallStatus: "Completed" }))).toBe(false);
    expect(shouldShowFinishSetupEntry(null)).toBe(false);
  });
});

describe("resolveBusinessSetupPreset", () => {
  it("maps known business types without inventing commercial rules", () => {
    expect(resolveBusinessSetupPreset("sari-sari").titleKey).toContain("sariSari");
    expect(resolveBusinessSetupPreset("Mini Grocery").titleKey).toContain("grocery");
    expect(resolveBusinessSetupPreset("unknown-xyz").titleKey).toContain("general");
  });
});
