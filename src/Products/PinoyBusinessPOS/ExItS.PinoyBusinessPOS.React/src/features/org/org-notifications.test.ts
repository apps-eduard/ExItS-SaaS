import { describe, expect, it } from "vitest";
import { resolveOrganizationNotificationHref } from "@/features/org/org-notifications";

describe("resolveOrganizationNotificationHref", () => {
  it("routes supplier connection notifications", () => {
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "SupplierConnectionRequested",
        relatedId: "11111111-1111-1111-1111-111111111111",
      }),
    ).toBe("/suppliers/connected/requests");
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "SupplierConnectionAccepted",
        relatedId: null,
      }),
    ).toBe("/suppliers");
  });

  it("routes buyer-facing connected PO notifications", () => {
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "ConnectedPurchaseOrderAccepted",
        relatedId: "22222222-2222-2222-2222-222222222222",
      }),
    ).toBe("/purchasing/22222222-2222-2222-2222-222222222222");
  });

  it("routes supplier-facing connected PO submitted notifications to incoming orders", () => {
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "ConnectedPurchaseOrderSubmitted",
        relatedId: "44444444-4444-4444-4444-444444444444",
      }),
    ).toBe("/purchasing/incoming-orders/44444444-4444-4444-4444-444444444444");
  });

  it("routes customer-order notifications to seller order detail", () => {
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "CustomerOrderSubmitted",
        relatedId: "33333333-3333-3333-3333-333333333333",
      }),
    ).toBe("/orders/33333333-3333-3333-3333-333333333333");
  });

  it("routes stock-request notifications to warehouse request detail", () => {
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "StockRequestApproved",
        relatedId: "55555555-5555-5555-5555-555555555555",
      }),
    ).toBe("/warehouse/requests/55555555-5555-5555-5555-555555555555");
  });

  it("routes inventory-transfer notifications to transfer detail", () => {
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "InventoryTransferDispatched",
        relatedId: "66666666-6666-6666-6666-666666666666",
      }),
    ).toBe("/inventory/transfers/66666666-6666-6666-6666-666666666666");
  });

  it("returns null for unknown types", () => {
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "SomethingElse",
        relatedId: null,
      }),
    ).toBeNull();
  });
});
