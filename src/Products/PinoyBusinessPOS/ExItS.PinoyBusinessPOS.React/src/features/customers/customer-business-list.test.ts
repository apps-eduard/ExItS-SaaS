import { describe, expect, it } from "vitest";
import {
  buildBusinessListRows,
  isBusinessPosCustomer,
  isPersonPosCustomer,
} from "@/features/customers/customer-business-list";
import type { PosCustomerListItem } from "@/api/pos/pos-customers-client";
import type { BusinessCustomer } from "@/api/pos/pos-connected-suppliers-client";

function posCustomer(
  overrides: Partial<PosCustomerListItem> & Pick<PosCustomerListItem, "customerId" | "displayName">,
): PosCustomerListItem {
  return {
    organizationId: "11111111-1111-1111-1111-111111111111",
    mobileNumber: null,
    address: null,
    notes: null,
    status: "Active",
    platformBusinessCustomerId: null,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    linkedPersonalPublicUserId: null,
    linkedBuyerOrganizationId: null,
    linkedBuyerPublicOrganizationId: null,
    partyKind: "Person",
    ...overrides,
  };
}

function connection(
  overrides: Partial<BusinessCustomer> &
    Pick<BusinessCustomer, "connectionId" | "buyerOrganizationId" | "organizationDisplayName">,
): BusinessCustomer {
  return {
    supplierOrganizationId: "11111111-1111-1111-1111-111111111111",
    organizationPublicId: "ORG000001",
    relationshipStatus: "Active",
    catalogSharingMode: "SelectedOnly",
    customerDiscountPercent: null,
    eligibleCount: 0,
    sharedCount: 0,
    excludedCount: 0,
    overrideCount: 0,
    connectedSinceUtc: "2026-01-01T00:00:00Z",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    displayNameIsLive: false,
    ...overrides,
  };
}

describe("customer-business-list", () => {
  it("keeps people and businesses separate", () => {
    expect(
      isPersonPosCustomer(posCustomer({ customerId: "a", displayName: "Ana", partyKind: "Person" })),
    ).toBe(true);
    expect(
      isBusinessPosCustomer(
        posCustomer({ customerId: "b", displayName: "Store", partyKind: "Business" }),
      ),
    ).toBe(true);
    expect(
      isPersonPosCustomer(
        posCustomer({
          customerId: "c",
          displayName: "Org",
          partyKind: "Person",
          linkedBuyerOrganizationId: "22222222-2222-2222-2222-222222222222",
        }),
      ),
    ).toBe(false);
  });

  it("dedupes POS org customer when connection already exists and marks also-supplier", () => {
    const buyerId = "22222222-2222-2222-2222-222222222222";
    const rows = buildBusinessListRows({
      connections: [
        connection({
          connectionId: "33333333-3333-3333-3333-333333333333",
          buyerOrganizationId: buyerId,
          organizationDisplayName: "Buyer Co",
        }),
      ],
      posBusinessCustomers: [
        posCustomer({
          customerId: "44444444-4444-4444-4444-444444444444",
          displayName: "Buyer Co",
          partyKind: "Business",
          linkedBuyerOrganizationId: buyerId,
          linkedBuyerPublicOrganizationId: "ORG000001",
        }),
        posCustomer({
          customerId: "55555555-5555-5555-5555-555555555555",
          displayName: "Local Mart",
          partyKind: "Business",
        }),
      ],
      activeSupplierOrganizationIds: new Set([buyerId.toLowerCase()]),
    });

    expect(rows).toHaveLength(2);
    expect(rows[0]?.source).toBe("connection");
    expect(rows[0]?.alsoSupplier).toBe(true);
    expect(rows[0]?.badges).toEqual(["connected", "exitsOrganization"]);
    expect(rows[1]?.source).toBe("pos");
    expect(rows[1]?.badges).toEqual(["local"]);
    expect(rows[1]?.alsoSupplier).toBe(false);
  });
});
