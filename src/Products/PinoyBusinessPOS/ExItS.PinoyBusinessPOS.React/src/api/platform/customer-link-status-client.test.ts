import { afterEach, describe, expect, it, vi } from "vitest";
import { getCustomerLinkStatus } from "@/api/platform/customer-link-status-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const organizationId = "11111111-1111-4111-8111-111111111111";
const businessCustomerId = "22222222-2222-4222-8222-222222222222";
const userId = "33333333-3333-4333-8333-333333333333";
const requestId = "44444444-4444-4444-8444-444444444444";

describe("customer-link-status-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("parses PascalCase Platform link-status payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        expect(String(input)).toContain(
          `/api/v1/organizations/${organizationId}/customers/${businessCustomerId}/link-status`,
        );
        return {
          ok: true,
          status: 200,
          json: async () => ({
            BusinessCustomerId: businessCustomerId,
            OrganizationId: organizationId,
            Status: "Pending",
            LinkedUserIdentityId: null,
            LatestLinkRequestId: requestId,
            LatestLinkRequestStatus: "Pending",
          }),
          text: async () => "",
        };
      }),
    );

    const status = await getCustomerLinkStatus(organizationId, businessCustomerId);
    expect(status.status).toBe("Pending");
    expect(status.businessCustomerId).toBe(businessCustomerId);
    expect(status.latestLinkRequestId).toBe(requestId);
    expect(status.linkedUserIdentityId).toBeNull();
  });

  it("parses Linked status with linked user identity", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({
          businessCustomerId,
          organizationId,
          status: "Linked",
          linkedUserIdentityId: userId,
          latestLinkRequestId: requestId,
          latestLinkRequestStatus: "Active",
        }),
        text: async () => "",
      })),
    );

    const status = await getCustomerLinkStatus(organizationId, businessCustomerId);
    expect(status.status).toBe("Linked");
    expect(status.linkedUserIdentityId).toBe(userId);
  });
});
