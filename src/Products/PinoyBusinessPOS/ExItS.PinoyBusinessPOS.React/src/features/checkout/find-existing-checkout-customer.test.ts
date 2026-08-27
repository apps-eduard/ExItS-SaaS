import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  findCustomerByLinkedPersonalPublicUserId,
  searchCheckoutCustomers,
} from "@/api/pos/pos-customers-client";
import { findExistingCheckoutCustomerForPersonalId } from "@/features/checkout/find-existing-checkout-customer";

vi.mock("@/api/pos/pos-customers-client", () => ({
  findCustomerByLinkedPersonalPublicUserId: vi.fn(),
  searchCheckoutCustomers: vi.fn(),
}));

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const existing = {
  customerId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
  displayName: "Rosa Santos",
  mobileNumber: "09171234567",
  status: "Active",
};

describe("findExistingCheckoutCustomerForPersonalId", () => {
  beforeEach(() => {
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockReset();
    vi.mocked(searchCheckoutCustomers).mockReset();
  });
  it("returns the linked POS customer without searching", async () => {
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue(existing);

    await expect(
      findExistingCheckoutCustomerForPersonalId(workspace, "EX-4827-1936"),
    ).resolves.toEqual(existing);
    expect(searchCheckoutCustomers).not.toHaveBeenCalled();
  });

  it("falls back to checkout search when the link column is empty", async () => {
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue(null);
    vi.mocked(searchCheckoutCustomers).mockResolvedValue({
      items: [existing],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    await expect(
      findExistingCheckoutCustomerForPersonalId(workspace, "EX-4827-1936"),
    ).resolves.toEqual(existing);
  });

  it("returns null when the Personal ID is not in contacts", async () => {
    vi.mocked(findCustomerByLinkedPersonalPublicUserId).mockResolvedValue(null);
    vi.mocked(searchCheckoutCustomers).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });

    await expect(
      findExistingCheckoutCustomerForPersonalId(workspace, "EX-9999-9999"),
    ).resolves.toBeNull();
  });
});
