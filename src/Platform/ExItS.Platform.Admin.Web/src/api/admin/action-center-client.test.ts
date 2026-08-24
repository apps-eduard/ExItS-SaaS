import { describe, expect, it } from "vitest";
import { actionCenterItemHref } from "@/api/admin/action-center-client";

describe("action-center-client", () => {
  it("routes payment items to billing workspace", () => {
    expect(
      actionCenterItemHref({
        id: "payment-pending-1",
        category: "payment",
        severity: "warning",
        title: "Pending",
        reason: "Ref",
        paymentId: "00000000-0000-0000-0000-000000000001",
      }),
    ).toBe("/admin/payments/00000000-0000-0000-0000-000000000001");
  });

  it("routes usage warnings to usage limits", () => {
    expect(
      actionCenterItemHref({
        id: "usage-1",
        category: "usage",
        severity: "warning",
        title: "Usage",
        reason: "80%",
        organizationId: "00000000-0000-0000-0000-000000000002",
        productCode: "pinoy-business-pos",
      }),
    ).toBe(
      "/admin/usage?organizationId=00000000-0000-0000-0000-000000000002&productCode=pinoy-business-pos",
    );
  });
});
