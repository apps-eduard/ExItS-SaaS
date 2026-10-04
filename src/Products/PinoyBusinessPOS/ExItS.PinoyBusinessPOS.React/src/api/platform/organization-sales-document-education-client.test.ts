import { beforeEach, describe, expect, it, vi } from "vitest";

const platformRequest = vi.fn();

vi.mock("@/api/platform/platform-http", async () => {
  const actual = await vi.importActual<typeof import("@/api/platform/platform-http")>(
    "@/api/platform/platform-http",
  );
  return { ...actual, platformRequest: (...args: unknown[]) => platformRequest(...args) };
});

const { acknowledgeSalesDocumentEducation, requestOrganizationComplianceReview } = await import(
  "@/api/platform/organization-sales-document-education-client"
).then(async (education) => {
  const compliance = await import("@/api/platform/organization-compliance-client");
  return {
    acknowledgeSalesDocumentEducation: education.acknowledgeSalesDocumentEducation,
    requestOrganizationComplianceReview: compliance.requestOrganizationComplianceReview,
  };
});

const ORG = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

describe("sales-document education requests", () => {
  beforeEach(() => {
    platformRequest.mockReset();
    platformRequest.mockResolvedValue({
      organizationId: ORG,
      currentVersion: "transaction-summary-v1",
      currentOwnerAcknowledged: true,
      requiresOwnerAction: false,
      documentMode: "TransactionSummary",
      complianceEligibilityStatus: "Requested",
    });
  });

  it("posts acknowledgment with an empty body and no actor id", async () => {
    await acknowledgeSalesDocumentEducation(ORG);
    const request = platformRequest.mock.calls[0]?.[0] as { method?: string; path?: string; body?: unknown };
    expect(request.method).toBe("POST");
    expect(request.path).toBe(
      `/api/v1/platform/organizations/${ORG}/sales-document-education/acknowledge`,
    );
    expect(request.body).toBeUndefined();
    expect(JSON.stringify(request)).not.toMatch(/userId|actor/i);
  });

  it("posts a compliance review request with an empty body", async () => {
    await requestOrganizationComplianceReview(ORG);
    const request = platformRequest.mock.calls[0]?.[0] as { method?: string; path?: string; body?: unknown };
    expect(request.method).toBe("POST");
    expect(request.path).toBe(`/api/v1/platform/organizations/${ORG}/compliance/request`);
    expect(request.body).toBeUndefined();
  });
});
