import { describe, expect, it, vi } from "vitest";
import { searchCheckoutCustomers } from "@/api/pos/pos-customers-client";

vi.mock("@/api/pos/pos-http", () => ({
  posRequest: vi.fn(),
}));

import { posRequest } from "@/api/pos/pos-http";

describe("POS-UTANG-CHECKOUT-CUSTOMER-DIRECTORY-FIX-02 client", () => {
  it("allows blank All search (shared Cash/GCash/Utang directory)", async () => {
    vi.mocked(posRequest).mockResolvedValue({
      items: [
        {
          kind: "Customer",
          customerId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
          displayName: "Juan Dela Cruz",
          status: "Active",
          creditStatus: "NotConfigured",
          creditLimit: null,
          outstandingAmount: 0,
          availableCredit: 0,
          defaultTermDays: null,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    const result = await searchCheckoutCustomers(
      { organizationId: "org", branchId: "branch" },
      { kind: "All", pageSize: 20 },
    );

    expect(posRequest).toHaveBeenCalled();
    const call = vi.mocked(posRequest).mock.calls[0]?.[0] as { path?: string };
    expect(call.path).toContain("/checkout-search");
    expect(call.path).toContain("kind=All");
    expect(result.items).toHaveLength(1);
    expect(result.items[0]?.creditStatus).toBe("NotConfigured");
  });
});
