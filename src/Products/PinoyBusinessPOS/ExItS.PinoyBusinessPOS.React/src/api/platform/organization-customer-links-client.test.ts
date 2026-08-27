import { afterEach, describe, expect, it, vi } from "vitest";
import {
  loadOrganizationCustomerLinkOverlay,
  overlayFromLinkPages,
} from "@/api/platform/organization-customer-links-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const organizationId = "11111111-1111-4111-8111-111111111111";
const connectedId = "22222222-2222-4222-8222-222222222222";
const pendingId = "33333333-3333-4333-8333-333333333333";
const revokedId = "44444444-4444-4444-8444-444444444444";

describe("overlayFromLinkPages", () => {
  it("keeps Active linked users and Pending requests, skipping revoked", () => {
    const overlay = overlayFromLinkPages({
      linkedUsers: [
        { BusinessCustomerId: connectedId, Status: "Active" },
        { businessCustomerId: revokedId, status: "Revoked" },
      ],
      pendingRequests: [
        { businessCustomerId: pendingId, status: "Pending" },
        { businessCustomerId: connectedId, status: "Pending" },
      ],
    });

    expect(overlay.loaded).toBe(true);
    expect(overlay.connectedBusinessCustomerIds.has(connectedId)).toBe(true);
    expect(overlay.connectedBusinessCustomerIds.has(revokedId)).toBe(false);
    expect(overlay.pendingBusinessCustomerIds.has(pendingId)).toBe(true);
    expect(overlay.pendingBusinessCustomerIds.has(connectedId)).toBe(false);
  });
});

describe("loadOrganizationCustomerLinkOverlay", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("loads linked users and pending requests without per-customer calls", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("linked-customer-app-users")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              Items: [{ BusinessCustomerId: connectedId, Status: "Active" }],
              TotalCount: 1,
              Page: 1,
              PageSize: 100,
            }),
            text: async (): Promise<string> => "",
          };
        }
        expect(url).toContain("customer-link-requests?status=Pending");
        return {
          ok: true,
          status: 200,
            json: async () => ({
            items: [{ businessCustomerId: pendingId, status: "Pending" }],
            totalCount: 1,
            page: 1,
            pageSize: 100,
          }),
          text: async (): Promise<string> => "",
        };
      }),
    );

    const overlay = await loadOrganizationCustomerLinkOverlay(organizationId);
    expect(overlay.loaded).toBe(true);
    expect(overlay.connectedBusinessCustomerIds.has(connectedId)).toBe(true);
    expect(overlay.pendingBusinessCustomerIds.has(pendingId)).toBe(true);
  });

  it("treats 403 as an empty loaded overlay so the list still renders", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: false,
        status: 403,
        json: async () => ({ title: "Forbidden", status: 403 }),
        text: async (): Promise<string> => "",
      })),
    );

    const overlay = await loadOrganizationCustomerLinkOverlay(organizationId);
    expect(overlay.loaded).toBe(true);
    expect(overlay.connectedBusinessCustomerIds.size).toBe(0);
    expect(overlay.pendingBusinessCustomerIds.size).toBe(0);
  });
});
