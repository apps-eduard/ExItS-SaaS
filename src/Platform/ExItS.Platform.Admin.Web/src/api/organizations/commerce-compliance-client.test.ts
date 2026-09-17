import { describe, expect, it } from "vitest";
import {
  birComplianceDisplayLabel,
  isBirComplianceTransitionAllowed,
  isOnlineSupplierPaymentsTransitionAllowed,
  mapOnlineSupplierPaymentsCapability,
  mapOrganizationComplianceStatus,
  onlineSupplierPaymentsDisplayLabel,
} from "@/api/organizations/commerce-compliance-client";

describe("commerce-compliance-client", () => {
  it("maps online supplier payments capability and display labels", () => {
    const mapped = mapOnlineSupplierPaymentsCapability({
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      status: "Available",
      updatedAtUtc: "2026-09-17T08:00:00Z",
      updatedByActorReference: "admin",
      reason: "ready",
    });
    expect(mapped.status).toBe("Available");
    expect(onlineSupplierPaymentsDisplayLabel(mapped.status)).toBe("Enabled");
    expect(onlineSupplierPaymentsDisplayLabel("Disabled")).toBe("Disabled");
    expect(onlineSupplierPaymentsDisplayLabel("Suspended")).toBe("Suspended");
  });

  it("gates online supplier payments transitions", () => {
    expect(isOnlineSupplierPaymentsTransitionAllowed("Disabled", "Available")).toBe(true);
    expect(isOnlineSupplierPaymentsTransitionAllowed("Disabled", "Suspended")).toBe(false);
    expect(isOnlineSupplierPaymentsTransitionAllowed("Available", "Suspended")).toBe(true);
    expect(isOnlineSupplierPaymentsTransitionAllowed("Suspended", "Available")).toBe(true);
  });

  it("maps BIR display labels and allowed admin transitions", () => {
    expect(birComplianceDisplayLabel("NotRequested")).toBe("Not approved");
    expect(birComplianceDisplayLabel("UnderReview")).toBe("Pending review");
    expect(birComplianceDisplayLabel("Approved")).toBe("Approved");
    expect(birComplianceDisplayLabel("Rejected")).toBe("Rejected");
    expect(birComplianceDisplayLabel("Suspended")).toBe("Suspended/Revoked");
    expect(isBirComplianceTransitionAllowed("UnderReview", "Approved")).toBe(true);
    expect(isBirComplianceTransitionAllowed("NotRequested", "Approved")).toBe(false);
    expect(isBirComplianceTransitionAllowed("Approved", "Suspended")).toBe(true);

    const mapped = mapOrganizationComplianceStatus({
      organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      complianceEligibilityStatus: "UnderReview",
      taxDocumentIssuanceEnabled: false,
      taxDocumentIssuanceStatus: "NotEnabled",
      taxConfigurationEnabled: false,
      taxConfigurationStatus: "NotEnabled",
      taxDocumentImplementationAvailable: false,
      currentOwnerEducationAcknowledged: true,
      educationVersion: "v1",
    });
    expect(mapped.complianceEligibilityStatus).toBe("UnderReview");
  });
});
