import { describe, expect, it } from "vitest";
import { isBirComplianceModuleUnlocked } from "@/api/platform/organization-sales-document-capability-client";

describe("isBirComplianceModuleUnlocked", () => {
  it("unlocks only when ComplianceEligibilityStatus is Approved", () => {
    expect(
      isBirComplianceModuleUnlocked({
        organizationId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        complianceEligibilityStatus: "Approved",
        taxDocumentIssuanceEnabled: false,
        taxConfigurationEnabled: false,
        taxDocumentImplementationAvailable: false,
      }),
    ).toBe(true);
    expect(
      isBirComplianceModuleUnlocked({
        organizationId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        complianceEligibilityStatus: "Requested",
        taxDocumentIssuanceEnabled: true,
        taxConfigurationEnabled: true,
        taxDocumentImplementationAvailable: false,
      }),
    ).toBe(false);
  });
});
