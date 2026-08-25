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

  it("returns null for unknown types", () => {
    expect(
      resolveOrganizationNotificationHref({
        relatedType: "SomethingElse",
        relatedId: null,
      }),
    ).toBeNull();
  });
});
