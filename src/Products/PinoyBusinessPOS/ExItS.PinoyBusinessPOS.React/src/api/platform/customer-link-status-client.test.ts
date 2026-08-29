import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
import {
  getCustomerLinkStatus,
  listCustomerLinkRequestHistory,
} from "@/api/platform/customer-link-status-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const organizationId = "11111111-1111-4111-8111-111111111111";
const businessCustomerId = "22222222-2222-4222-8222-222222222222";
const userId = "33333333-3333-4333-8333-333333333333";
const requestId = "44444444-4444-4444-8444-444444444444";
const requestId2 = "55555555-5555-4555-8555-555555555555";

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
        return jsonResponse(200, {
            BusinessCustomerId: businessCustomerId,
            OrganizationId: organizationId,
            Status: "Pending",
            LinkedUserIdentityId: null,
            LatestLinkRequestId: requestId,
            LatestLinkRequestStatus: "Pending",
          });
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
      vi.fn(async () => (jsonResponse(200, {
          businessCustomerId,
          organizationId,
          status: "Linked",
          linkedUserIdentityId: userId,
          latestLinkRequestId: requestId,
          latestLinkRequestStatus: "Active",
        }))),
    );

    const status = await getCustomerLinkStatus(organizationId, businessCustomerId);
    expect(status.status).toBe("Linked");
    expect(status.linkedUserIdentityId).toBe(userId);
  });

  it("parses PascalCase link-request history payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        expect(String(input)).toContain(
          `/api/v1/organizations/${organizationId}/customers/${businessCustomerId}/link-requests`,
        );
        return jsonResponse(200, [
            {
              Id: requestId,
              Status: "Pending",
              CreatedAtUtc: "2026-08-20T10:00:00Z",
            },
            {
              Id: requestId2,
              Status: "Active",
              CreatedAtUtc: "2026-08-18T08:00:00Z",
              ResolvedAtUtc: "2026-08-19T12:00:00Z",
            },
          ]);
      }),
    );

    const history = await listCustomerLinkRequestHistory(organizationId, businessCustomerId);
    expect(history).toHaveLength(2);
    expect(history[0]).toEqual({
      id: requestId,
      status: "Pending",
      createdAtUtc: "2026-08-20T10:00:00Z",
      resolvedAtUtc: null,
    });
    expect(history[1]).toEqual({
      id: requestId2,
      status: "Active",
      createdAtUtc: "2026-08-18T08:00:00Z",
      resolvedAtUtc: "2026-08-19T12:00:00Z",
    });
  });
});
