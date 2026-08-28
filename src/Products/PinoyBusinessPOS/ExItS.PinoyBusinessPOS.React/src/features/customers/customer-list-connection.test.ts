import { describe, expect, it } from "vitest";
import {
  customerHasExItsId,
  resolveCustomerListConnectionBadge,
  type CustomerListConnectionOverlay,
} from "@/features/customers/customer-list-connection";

const platformId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

function overlay(partial?: Partial<CustomerListConnectionOverlay>): CustomerListConnectionOverlay {
  return {
    connectedBusinessCustomerIds: new Set(),
    pendingBusinessCustomerIds: new Set(),
    loaded: true,
    ...partial,
  };
}

describe("customerHasExItsId", () => {
  it("is false for a local customer with no public id", () => {
    expect(customerHasExItsId({})).toBe(false);
    expect(customerHasExItsId({ linkedPersonalPublicUserId: "  " })).toBe(false);
  });

  it("is true for POS-local Personal or buyer org ids", () => {
    expect(customerHasExItsId({ linkedPersonalPublicUserId: "EX-1234-5678" })).toBe(true);
    expect(customerHasExItsId({ linkedBuyerPublicOrganizationId: "ORG000001" })).toBe(true);
    expect(customerHasExItsId({ resolvedPersonalDisplayName: "Rosa Santos" })).toBe(true);
  });

  it("is true when the ExItS ID was only tagged in notes", () => {
    expect(customerHasExItsId({ notes: "Neighbor\nexits-id:EX-4827-1936" })).toBe(true);
  });
});

describe("resolveCustomerListConnectionBadge", () => {
  it("marks local customers as no ExItS ID", () => {
    expect(resolveCustomerListConnectionBadge({}, overlay())).toBe("no-exits");
  });

  it("does not advertise Connected from an ExItS ID alone", () => {
    expect(
      resolveCustomerListConnectionBadge(
        {
          linkedPersonalPublicUserId: "EX-1234-5678",
          platformBusinessCustomerId: platformId,
        },
        overlay({ loaded: false }),
      ),
    ).toBe("exits-id");
    expect(
      resolveCustomerListConnectionBadge(
        {
          linkedPersonalPublicUserId: "EX-1234-5678",
          platformBusinessCustomerId: platformId,
        },
        null,
      ),
    ).toBe("exits-id");
  });

  it("shows Connected only when the overlay lists an Active link", () => {
    expect(
      resolveCustomerListConnectionBadge(
        {
          linkedPersonalPublicUserId: "EX-1234-5678",
          platformBusinessCustomerId: platformId,
        },
        overlay({
          connectedBusinessCustomerIds: new Set([platformId]),
        }),
      ),
    ).toBe("connected");
  });

  it("shows Pending when a pending request exists and the row is not Connected", () => {
    expect(
      resolveCustomerListConnectionBadge(
        {
          linkedPersonalPublicUserId: "EX-1234-5678",
          platformBusinessCustomerId: platformId.toUpperCase(),
        },
        overlay({
          pendingBusinessCustomerIds: new Set([platformId]),
        }),
      ),
    ).toBe("pending");
  });

  it("prefers Connected over Pending", () => {
    expect(
      resolveCustomerListConnectionBadge(
        {
          linkedPersonalPublicUserId: "EX-1234-5678",
          platformBusinessCustomerId: platformId,
        },
        overlay({
          connectedBusinessCustomerIds: new Set([platformId]),
          pendingBusinessCustomerIds: new Set([platformId]),
        }),
      ),
    ).toBe("connected");
  });
});
