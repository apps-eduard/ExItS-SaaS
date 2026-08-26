import { describe, expect, it } from "vitest";
import type { PosCustomerListItem } from "@/api/pos/pos-customers-client";

/**
 * Mirrors CustomersListPage customerMeta: POS EX-ID fields must not advertise
 * Platform "Linked" status on the list (no N+1).
 */
function customerMeta(customer: PosCustomerListItem, exItsIdHint: string): string {
  const parts = [customer.mobileNumber].filter(Boolean);
  if (customer.linkedPersonalPublicUserId || customer.linkedBuyerPublicOrganizationId) {
    parts.push(exItsIdHint);
  }
  return parts.join(" · ");
}

describe("CustomersListPage link meta", () => {
  it("uses ExItS ID hint instead of Linked for POS-local public ids", () => {
    const customer = {
      customerId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
      organizationId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      displayName: "Ana",
      mobileNumber: "0917",
      status: "Active",
      createdAtUtc: "2026-08-20T00:00:00Z",
      updatedAtUtc: "2026-08-20T00:00:00Z",
      linkedPersonalPublicUserId: "EX-1234-5678",
    } as PosCustomerListItem;

    const meta = customerMeta(customer, "ExItS ID");
    expect(meta).toContain("ExItS ID");
    expect(meta).not.toMatch(/Linked/i);
  });
});
