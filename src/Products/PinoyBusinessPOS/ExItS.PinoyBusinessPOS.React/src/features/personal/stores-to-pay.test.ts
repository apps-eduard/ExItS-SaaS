import { beforeEach, describe, expect, it, vi } from "vitest";
import * as linkedMerchants from "@/api/platform/linked-merchants-client";
import * as buyerToken from "@/api/platform/personal-buyer-token";
import * as linkedCustomers from "@/api/pos/pos-linked-customers-client";
import { loadStoresToPayPreview } from "@/features/personal/stores-to-pay";

vi.mock("@/api/platform/linked-merchants-client", async (importOriginal) => {
  const actual = await importOriginal<typeof linkedMerchants>();
  return {
    ...actual,
    listLinkedMerchants: vi.fn(),
  };
});

vi.mock("@/api/platform/personal-buyer-token", () => ({
  ensurePersonalBuyerPosToken: vi.fn(),
}));

vi.mock("@/api/pos/pos-linked-customers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof linkedCustomers>();
  return {
    ...actual,
    getLinkedCustomerStatement: vi.fn(),
  };
});

describe("loadStoresToPayPreview", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });
  it("returns empty preview when there are no linked merchants", async () => {
    vi.mocked(linkedMerchants.listLinkedMerchants).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });

    await expect(loadStoresToPayPreview()).resolves.toEqual({
      storeCount: 0,
      activeCount: 0,
      preview: [],
    });
    expect(buyerToken.ensurePersonalBuyerPosToken).not.toHaveBeenCalled();
  });

  it("ranks stores with outstanding balances and caps the preview", async () => {
    vi.mocked(linkedMerchants.listLinkedMerchants).mockResolvedValue({
      items: [
        {
          linkedCustomerId: "11111111-1111-1111-1111-111111111111",
          businessCustomerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          organizationDisplayName: "Store A",
          customerDisplayName: "Toto",
          linkStatus: "Active",
          linkedAtUtc: "2026-01-01T00:00:00Z",
          canCustomerOrder: true,
          canCustomerDelivery: false,
        },
        {
          linkedCustomerId: "22222222-2222-2222-2222-222222222222",
          businessCustomerId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          organizationId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          organizationDisplayName: "Store B",
          customerDisplayName: "Toto",
          linkStatus: "Active",
          linkedAtUtc: "2026-01-01T00:00:00Z",
          canCustomerOrder: true,
          canCustomerDelivery: false,
        },
        {
          linkedCustomerId: "33333333-3333-3333-3333-333333333333",
          businessCustomerId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          organizationId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
          organizationDisplayName: "Store C",
          customerDisplayName: "Toto",
          linkStatus: "Active",
          linkedAtUtc: "2026-01-01T00:00:00Z",
          canCustomerOrder: false,
          canCustomerDelivery: false,
        },
      ],
      totalCount: 3,
      page: 1,
      pageSize: 50,
    });
    vi.mocked(buyerToken.ensurePersonalBuyerPosToken).mockResolvedValue({ ok: true });
    vi.mocked(linkedCustomers.getLinkedCustomerStatement)
      .mockResolvedValueOnce({
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        platformBusinessCustomerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        posCustomerId: "99999999-9999-4999-8999-999999999999",
        linkedCustomerAppUserId: "88888888-8888-4888-8888-888888888888",
        merchantDisplayName: "Store A",
        customerDisplayName: "Toto",
        outstandingBalance: 2000,
        currency: "PHP",
        asOfUtc: "2026-08-22T00:00:00Z",
      })
      .mockResolvedValueOnce({
        organizationId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        platformBusinessCustomerId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        posCustomerId: "99999999-9999-4999-8999-999999999991",
        linkedCustomerAppUserId: "88888888-8888-4888-8888-888888888881",
        merchantDisplayName: "Store B",
        customerDisplayName: "Toto",
        outstandingBalance: 1500,
        currency: "PHP",
        asOfUtc: "2026-08-22T00:00:00Z",
      })
      .mockResolvedValueOnce({
        organizationId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
        platformBusinessCustomerId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        posCustomerId: "99999999-9999-4999-8999-999999999992",
        linkedCustomerAppUserId: "88888888-8888-4888-8888-888888888882",
        merchantDisplayName: "Store C",
        customerDisplayName: "Toto",
        outstandingBalance: 0,
        currency: "PHP",
        asOfUtc: "2026-08-22T00:00:00Z",
      });

    const result = await loadStoresToPayPreview();
    expect(result.storeCount).toBe(3);
    expect(result.activeCount).toBe(2);
    expect(result.preview).toHaveLength(2);
    expect(result.preview[0]?.displayName).toBe("Store A");
    expect(result.preview[0]?.outstandingBalance).toBe(2000);
    expect(result.preview[1]?.displayName).toBe("Store B");
    expect(result.preview[1]?.outstandingBalance).toBe(1500);
  });

  it("keeps store count when buyer token cannot be issued", async () => {
    vi.mocked(linkedMerchants.listLinkedMerchants).mockResolvedValue({
      items: [
        {
          linkedCustomerId: "11111111-1111-1111-1111-111111111111",
          businessCustomerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          organizationDisplayName: "Store A",
          customerDisplayName: "Toto",
          linkStatus: "Active",
          linkedAtUtc: "2026-01-01T00:00:00Z",
          canCustomerOrder: true,
          canCustomerDelivery: false,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });
    vi.mocked(buyerToken.ensurePersonalBuyerPosToken).mockResolvedValue({
      ok: false,
      detail: "token failed",
    });

    await expect(loadStoresToPayPreview()).resolves.toEqual({
      storeCount: 1,
      activeCount: 0,
      preview: [],
    });
    expect(linkedCustomers.getLinkedCustomerStatement).not.toHaveBeenCalled();
  });

  it("counts the same merchant once when multiple customer links exist", async () => {
    const org = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    vi.mocked(linkedMerchants.listLinkedMerchants).mockResolvedValue({
      items: [
        {
          linkedCustomerId: "11111111-1111-4111-8111-111111111111",
          businessCustomerId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          organizationId: org,
          organizationDisplayName: "Kizy Store",
          customerDisplayName: "Mica Linked 30121",
          linkStatus: "Linked",
          linkedAtUtc: "2026-08-20T00:00:00Z",
          canCustomerOrder: false,
          canCustomerDelivery: false,
        },
        {
          linkedCustomerId: "22222222-2222-4222-8222-222222222222",
          businessCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          organizationId: org,
          organizationDisplayName: "Kizy Store",
          customerDisplayName: "Mica Linked 30240",
          linkStatus: "Linked",
          linkedAtUtc: "2026-08-27T12:00:00Z",
          canCustomerOrder: false,
          canCustomerDelivery: false,
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 50,
    });
    vi.mocked(buyerToken.ensurePersonalBuyerPosToken).mockResolvedValue({ ok: true });
    vi.mocked(linkedCustomers.getLinkedCustomerStatement).mockResolvedValue({
      organizationId: org,
      platformBusinessCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      posCustomerId: "99999999-9999-4999-8999-999999999999",
      linkedCustomerAppUserId: "88888888-8888-4888-8888-888888888888",
      merchantDisplayName: "Kizy Store",
      customerDisplayName: "Mica Linked 30240",
      outstandingBalance: 100,
      currency: "PHP",
      asOfUtc: "2026-08-27T00:00:00Z",
    });

    const result = await loadStoresToPayPreview();
    expect(result.storeCount).toBe(1);
    expect(result.activeCount).toBe(1);
    expect(result.preview).toHaveLength(1);
    expect(linkedCustomers.getLinkedCustomerStatement).toHaveBeenCalledTimes(1);
    expect(result.preview[0]?.businessCustomerId).toBe("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
  });
});
