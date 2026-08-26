import { describe, expect, it } from "vitest";
import { billingIssueHref } from "@/api/billing/billing-operations-client";

describe("billing-operations-client", () => {
  it("links payment issues to payment detail", () => {
    expect(
      billingIssueHref({
        issueType: "pending-payment",
        severity: "warning",
        summary: "Pending",
        paymentId: "00000000-0000-0000-0000-000000000003",
      }),
    ).toBe("/admin/payments/00000000-0000-0000-0000-000000000003");
  });
});
