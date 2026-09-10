import { describe, expect, it } from "vitest";
import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import {
  checkoutCreditStatusLabelKey,
  formatCreditDueDateLabel,
  resolveUtangDirectorySelectBlock,
  utangDirectorySelectToastMessage,
} from "@/features/checkout/checkout-utang-credit";

describe("checkout-utang-credit helpers", () => {
  it("maps directory status wording keys", () => {
    expect(checkoutCreditStatusLabelKey("NotConfigured")).toBe(
      "checkout.directoryCredit.status.NotConfigured",
    );
    expect(checkoutCreditStatusLabelKey("Disabled")).toBe(
      "checkout.directoryCredit.status.Disabled",
    );
    expect(checkoutCreditStatusLabelKey("PendingApproval")).toBe(
      "checkout.directoryCredit.status.PendingApproval",
    );
    expect(checkoutCreditStatusLabelKey("Approved")).toBe(
      "checkout.directoryCredit.status.Approved",
    );
  });

  it("formats policy due dates for display", () => {
    expect(formatCreditDueDateLabel("2026-10-10")).toBe("Oct 10, 2026");
  });

  it("blocks PendingApproval / NotConfigured / Disabled / over-limit / B2B", () => {
    const pending: CheckoutCustomerOption = {
      kind: "Customer",
      customerId: "11111111-1111-1111-1111-111111111111",
      displayName: "Maria",
      status: "Active",
      creditStatus: "PendingApproval",
    };
    expect(resolveUtangDirectorySelectBlock({ customer: pending, thisSaleAmount: 10 })).toEqual({
      reason: "pending_approval",
    });

    const paused: CheckoutCustomerOption = {
      ...pending,
      creditStatus: "Disabled",
    };
    expect(resolveUtangDirectorySelectBlock({ customer: paused, thisSaleAmount: 10 })?.reason).toBe(
      "disabled",
    );

    const notEnabled: CheckoutCustomerOption = {
      ...pending,
      creditStatus: "NotConfigured",
    };
    expect(
      resolveUtangDirectorySelectBlock({ customer: notEnabled, thisSaleAmount: 10 })?.reason,
    ).toBe("not_configured");

    const over: CheckoutCustomerOption = {
      ...pending,
      creditStatus: "Approved",
      availableCredit: 50,
    };
    expect(
      resolveUtangDirectorySelectBlock({ customer: over, thisSaleAmount: 77 }),
    ).toEqual({ reason: "over_limit", availableCredit: 50 });

    const business: CheckoutCustomerOption = {
      kind: "Business",
      connectionId: "22222222-2222-2222-2222-222222222222",
      buyerOrganizationId: "33333333-3333-3333-3333-333333333333",
      buyerPublicOrganizationId: "ORG123",
      displayName: "Kizy Bakery",
      status: "Active",
    };
    expect(
      resolveUtangDirectorySelectBlock({ customer: business, thisSaleAmount: 10 })?.reason,
    ).toBe("b2b_not_available");
  });

  it("allows Approved under limit and unknown projection", () => {
    const approved: CheckoutCustomerOption = {
      kind: "Customer",
      customerId: "11111111-1111-1111-1111-111111111111",
      displayName: "Juan",
      status: "Active",
      creditStatus: "Approved",
      availableCredit: 5000,
    };
    expect(resolveUtangDirectorySelectBlock({ customer: approved, thisSaleAmount: 77 })).toBeNull();

    const unknown: CheckoutCustomerOption = {
      kind: "Customer",
      customerId: "11111111-1111-1111-1111-111111111111",
      displayName: "Walk-in",
      status: "Active",
    };
    expect(resolveUtangDirectorySelectBlock({ customer: unknown, thisSaleAmount: 77 })).toBeNull();
  });

  it("builds toast copy for blocked reasons", () => {
    const t = (key: string) => key;
    expect(
      utangDirectorySelectToastMessage({ reason: "pending_approval" }, t),
    ).toBe("checkout.utangSelect.pendingApproval");
    expect(
      utangDirectorySelectToastMessage({ reason: "over_limit", availableCredit: 12.5 }, (key) =>
        key === "checkout.utangSelect.overLimit" ? "Credit limit exceeded. Available credit is {amount}." : key,
      ),
    ).toContain("₱");
  });
});
