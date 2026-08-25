import { afterEach, describe, expect, it, vi } from "vitest";
import {
  getLinkedMerchantOrderingCapability,
  listLinkedMerchants,
} from "@/api/platform/linked-merchants-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const linkedCustomerId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const businessCustomerId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const organizationId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

describe("linked-merchants-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("parses PascalCase linked merchant page payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({
          Items: [
            {
              LinkedCustomerId: linkedCustomerId,
              BusinessCustomerId: businessCustomerId,
              OrganizationId: organizationId,
              OrganizationDisplayName: "Corner Store",
              CustomerDisplayName: "Ana Reyes",
              LinkStatus: "Active",
              LinkedAtUtc: "2026-08-10T00:00:00Z",
              CanCustomerOrder: true,
              CanCustomerDelivery: false,
            },
          ],
          TotalCount: 1,
          Page: 1,
          PageSize: 20,
        }),
        text: async () => "",
      })),
    );

    const page = await listLinkedMerchants(1, 20);
    expect(page.totalCount).toBe(1);
    expect(page.items[0]?.organizationDisplayName).toBe("Corner Store");
    expect(page.items[0]?.canCustomerOrder).toBe(true);
  });

  it("parses ordering capability payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({
          OrganizationId: organizationId,
          CanCustomerOrder: true,
          CanCustomerDelivery: true,
          OrganizationDisplayName: "Corner Store",
        }),
        text: async () => "",
      })),
    );

    const capability = await getLinkedMerchantOrderingCapability(organizationId);
    expect(capability.canCustomerOrder).toBe(true);
    expect(capability.canCustomerDelivery).toBe(true);
    expect(capability.organizationDisplayName).toBe("Corner Store");
  });
});
