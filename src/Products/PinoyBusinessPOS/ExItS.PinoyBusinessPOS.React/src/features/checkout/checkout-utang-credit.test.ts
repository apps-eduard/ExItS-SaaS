import { describe, expect, it } from "vitest";
import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import {
  checkoutCreditStatusLabelKey,
  formatCreditDueDateLabel,
  resolveCheckoutConnectionDisplay,
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

  it("blocks PendingApproval / NotConfigured / Disabled / over-limit; allows approved Business under limit", () => {
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

    const businessBase = {
      kind: "Business" as const,
      connectionId: "22222222-2222-2222-2222-222222222222",
      buyerOrganizationId: "33333333-3333-3333-3333-333333333333",
      buyerPublicOrganizationId: "ORG123",
      displayName: "Kizy Bakery",
      status: "Active",
    };
    expect(
      resolveUtangDirectorySelectBlock({
        customer: { ...businessBase, creditStatus: "PendingApproval" },
        thisSaleAmount: 10,
      })?.reason,
    ).toBe("pending_approval");
    expect(
      resolveUtangDirectorySelectBlock({
        customer: { ...businessBase, creditStatus: "NotConfigured" },
        thisSaleAmount: 10,
      })?.reason,
    ).toBe("not_configured");
    expect(
      resolveUtangDirectorySelectBlock({
        customer: { ...businessBase, creditStatus: "Disabled" },
        thisSaleAmount: 10,
      })?.reason,
    ).toBe("disabled");
    expect(
      resolveUtangDirectorySelectBlock({
        customer: { ...businessBase, creditStatus: "Approved", availableCredit: 50000 },
        thisSaleAmount: 10,
      }),
    ).toBeNull();
    expect(
      resolveUtangDirectorySelectBlock({
        customer: {
          ...businessBase,
          creditStatus: "Approved",
          availableCredit: 30000,
        },
        thisSaleAmount: 35000,
      }),
    ).toEqual({ reason: "over_limit", availableCredit: 30000 });
    expect(
      resolveUtangDirectorySelectBlock({
        customer: {
          ...businessBase,
          status: "Pending",
          creditStatus: "Approved",
          availableCredit: 50000,
        },
        thisSaleAmount: 10,
      })?.reason,
    ).toBe("connection_pending");
    expect(
      resolveUtangDirectorySelectBlock({
        customer: {
          ...businessBase,
          status: "Declined",
          creditStatus: "Approved",
          availableCredit: 50000,
        },
        thisSaleAmount: 10,
      })?.reason,
    ).toBe("inactive");
  });

  it("maps connection display separately from credit", () => {
    expect(
      resolveCheckoutConnectionDisplay({
        kind: "Customer",
        customerId: "11111111-1111-1111-1111-111111111111",
        displayName: "Juan",
        status: "Active",
      }),
    ).toEqual({ kind: "none" });
    expect(
      resolveCheckoutConnectionDisplay({
        kind: "Business",
        connectionId: "22222222-2222-2222-2222-222222222222",
        buyerOrganizationId: "33333333-3333-3333-3333-333333333333",
        buyerPublicOrganizationId: "ORG123",
        displayName: "Kizy",
        status: "Pending",
      }),
    ).toEqual({ kind: "chip", statusKey: "Pending", raw: "Pending" });
    expect(
      resolveCheckoutConnectionDisplay({
        kind: "Business",
        connectionId: "22222222-2222-2222-2222-222222222222",
        buyerOrganizationId: "33333333-3333-3333-3333-333333333333",
        buyerPublicOrganizationId: "ORG123",
        displayName: "Kizy",
        status: "Active",
      }),
    ).toEqual({ kind: "chip", statusKey: "Connected", raw: "Active" });

    const platformId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
    const person = {
      kind: "Customer" as const,
      customerId: "11111111-1111-1111-1111-111111111111",
      displayName: "Mica",
      status: "Active",
      linkedPersonalPublicUserId: "EX-1111-2222",
      platformBusinessCustomerId: platformId,
    };
    expect(
      resolveCheckoutConnectionDisplay(person, {
        connectedBusinessCustomerIds: new Set([platformId]),
        pendingBusinessCustomerIds: new Set(),
        loaded: true,
      }),
    ).toEqual({ kind: "chip", statusKey: "Connected", raw: "Connected" });
    expect(
      resolveCheckoutConnectionDisplay(person, {
        connectedBusinessCustomerIds: new Set(),
        pendingBusinessCustomerIds: new Set([platformId]),
        loaded: true,
      }),
    ).toEqual({ kind: "chip", statusKey: "Pending", raw: "Pending" });
    expect(
      resolveUtangDirectorySelectBlock({
        customer: { ...person, creditStatus: "Approved", availableCredit: 30000 },
        thisSaleAmount: 10,
        overlay: {
          connectedBusinessCustomerIds: new Set(),
          pendingBusinessCustomerIds: new Set([platformId]),
          loaded: true,
        },
      })?.reason,
    ).toBe("connection_pending");
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
      utangDirectorySelectToastMessage({ reason: "connection_pending" }, t),
    ).toBe("checkout.utangSelect.connectionPending");
    expect(
      utangDirectorySelectToastMessage({ reason: "over_limit", availableCredit: 12.5 }, (key) =>
        key === "checkout.utangSelect.overLimit" ? "Credit limit exceeded. Available credit is {amount}." : key,
      ),
    ).toContain("₱");
  });
});
