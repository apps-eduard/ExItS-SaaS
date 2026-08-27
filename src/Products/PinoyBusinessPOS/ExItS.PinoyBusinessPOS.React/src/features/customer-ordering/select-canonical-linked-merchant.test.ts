import { describe, expect, it } from "vitest";
import type { LinkedMerchantDto } from "@/api/platform/linked-merchants-client";
import { selectCanonicalLinkedMerchantPerStore } from "@/features/customer-ordering/select-canonical-linked-merchant";

function link(
  organizationId: string,
  linkedCustomerId: string,
  linkedAtUtc: string,
  customerDisplayName: string,
): LinkedMerchantDto {
  return {
    linkedCustomerId,
    businessCustomerId: linkedCustomerId,
    organizationId,
    organizationDisplayName: "Kizy Store",
    customerDisplayName,
    linkStatus: "Linked",
    linkedAtUtc,
    canCustomerOrder: false,
    canCustomerDelivery: false,
  };
}

describe("selectCanonicalLinkedMerchantPerStore", () => {
  it("keeps one card per store and prefers the newest link", () => {
    const org = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    const other = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    const selected = selectCanonicalLinkedMerchantPerStore([
      link(org, "11111111-1111-4111-8111-111111111111", "2026-08-20T00:00:00Z", "Mica Linked 30121"),
      link(org, "22222222-2222-4222-8222-222222222222", "2026-08-27T12:00:00Z", "Mica Linked 30240"),
      link(org, "33333333-3333-4333-8333-333333333333", "2026-08-21T00:00:00Z", "Mica Linked 30148"),
      link(other, "44444444-4444-4444-8444-444444444444", "2026-08-01T00:00:00Z", "Other"),
    ]);

    expect(selected).toHaveLength(2);
    expect(selected.map((row) => row.organizationId).sort()).toEqual([org, other].sort());
    expect(selected.find((row) => row.organizationId === org)?.customerDisplayName).toBe(
      "Mica Linked 30240",
    );
  });

  it("returns empty for empty input", () => {
    expect(selectCanonicalLinkedMerchantPerStore([])).toEqual([]);
  });
});
