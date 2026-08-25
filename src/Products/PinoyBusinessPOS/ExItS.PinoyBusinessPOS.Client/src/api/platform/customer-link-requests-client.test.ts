import { afterEach, describe, expect, it, vi } from "vitest";
import {
  acceptCustomerLinkRequest,
  declineCustomerLinkRequest,
  listPendingCustomerLinkRequests,
} from "@/api/platform/customer-link-requests-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const requestId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const orgId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const customerId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

describe("customer-link-requests-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("parses PascalCase pending link request payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => [
          {
            Id: requestId,
            OrganizationId: orgId,
            OrganizationDisplayName: "Sari-Sari Ni Ana",
            BusinessCustomerId: customerId,
            Status: "Pending",
            CreatedAtUtc: "2026-08-20T00:00:00Z",
            ExpiresAtUtc: "2026-08-27T00:00:00Z",
            TargetPublicUserId: "EXITS-ANA",
          },
        ],
        text: async () => "",
      })),
    );

    const items = await listPendingCustomerLinkRequests();
    expect(items).toHaveLength(1);
    expect(items[0]?.id).toBe(requestId);
    expect(items[0]?.organizationDisplayName).toBe("Sari-Sari Ni Ana");
    expect(items[0]?.status).toBe("Pending");
    expect(items[0]?.targetPublicUserId).toBe("EXITS-ANA");
  });

  it("posts accept and decline by request id", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/antiforgery/token")) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
          text: async () => "",
        };
      }
      expect(init?.method).toBe("POST");
      expect(url).toMatch(
        /\/customer-link-requests\/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\/(accept|decline)/,
      );
      return {
        ok: true,
        status: 200,
        json: async () => ({}),
        text: async () => "",
      };
    });
    vi.stubGlobal("fetch", fetchMock);

    await acceptCustomerLinkRequest(requestId);
    await declineCustomerLinkRequest(requestId);
    const mutationCalls = fetchMock.mock.calls.filter(([input]) =>
      String(input).includes("/customer-link-requests/"),
    );
    expect(mutationCalls).toHaveLength(2);
  });
});
