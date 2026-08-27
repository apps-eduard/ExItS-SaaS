import { describe, expect, it } from "vitest";
import type { LinkedMerchantDto } from "@/api/platform/linked-merchants-client";
import {
  filterLinkedMerchantRows,
  type LinkedMerchantRow,
} from "@/features/customer-ordering/LinkedMerchantsListSection";

function merchant(name: string, customer = "Me"): LinkedMerchantDto {
  return {
    linkedCustomerId: `00000000-0000-4000-8000-${name.padEnd(12, "0").slice(0, 12)}`,
    businessCustomerId: "11111111-1111-4111-8111-111111111111",
    organizationId: "22222222-2222-4222-8222-222222222222",
    organizationDisplayName: name,
    customerDisplayName: customer,
    linkStatus: "Linked",
    linkedAtUtc: "2026-08-27T00:00:00.000Z",
    canCustomerOrder: false,
    canCustomerDelivery: false,
  };
}

function row(
  name: string,
  ordering: LinkedMerchantRow["ordering"],
  customer = "Me",
): LinkedMerchantRow {
  return { merchant: merchant(name, customer), ordering };
}

describe("filterLinkedMerchantRows", () => {
  const rows: LinkedMerchantRow[] = [
    row("Alpha Store", { canCustomerOrder: true, canCustomerDelivery: false, pending: false, resolved: true }),
    row("Beta Shop", { canCustomerOrder: false, canCustomerDelivery: false, pending: false, resolved: true }),
    row("Gamma Mart", { canCustomerOrder: false, canCustomerDelivery: false, pending: true, resolved: false }),
  ];

  it("filters by ordering availability", () => {
    expect(filterLinkedMerchantRows(rows, "can_order", "").map((r) => r.merchant.organizationDisplayName)).toEqual([
      "Alpha Store",
    ]);
    expect(filterLinkedMerchantRows(rows, "unavailable", "").map((r) => r.merchant.organizationDisplayName)).toEqual([
      "Beta Shop",
    ]);
  });

  it("searches store and linked customer names", () => {
    const searchable = [
      row("Kizy Store", { canCustomerOrder: false, canCustomerDelivery: false, pending: false, resolved: true }, "1111"),
    ];
    expect(filterLinkedMerchantRows(searchable, "all", "kizy")).toHaveLength(1);
    expect(filterLinkedMerchantRows(searchable, "all", "1111")).toHaveLength(1);
    expect(filterLinkedMerchantRows(searchable, "all", "missing")).toHaveLength(0);
  });

  it("matches a store after stripping the Local Validation run stamp", () => {
    const searchable = [
      row(
        "Kizy Store 20260826225642",
        { canCustomerOrder: false, canCustomerDelivery: false, pending: false, resolved: true },
        "Mica Linked 20260826230121",
      ),
    ];
    expect(filterLinkedMerchantRows(searchable, "all", "kizy store")).toHaveLength(1);
    expect(filterLinkedMerchantRows(searchable, "all", "20260826225642")).toHaveLength(1);
  });
});
