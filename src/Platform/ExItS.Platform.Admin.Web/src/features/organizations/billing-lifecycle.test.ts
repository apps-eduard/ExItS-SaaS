import { describe, expect, it } from "vitest";
import type { OrganizationPayment } from "@/api/organizations/billing-list-query";
import {
  computeMonthlyPaidPeriod,
  defaultPaymentAmountForPlan,
  findSubscriptionForPayment,
  paymentActionCapabilities,
} from "@/features/organizations/billing-lifecycle";

const payment = (overrides: Partial<OrganizationPayment>): OrganizationPayment => ({
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  productCode: "pinoy-business-pos",
  amount: 699,
  currencyCode: "PHP",
  method: "GCash",
  status: "PendingConfirmation",
  ...overrides,
});

describe("billing lifecycle helpers", () => {
  it("computes a one-month paid period", () => {
    const { periodStartUtc, periodEndUtc } = computeMonthlyPaidPeriod(new Date("2026-08-22T12:00:00Z"));
    expect(periodStartUtc).toBe("2026-08-22T00:00:00.000Z");
    expect(periodEndUtc).toBe("2026-09-22T00:00:00.000Z");
  });

  it("uses catalog monthly price for default payment amount", () => {
    expect(
      defaultPaymentAmountForPlan({
        id: "plan",
        productCode: "pinoy-business-pos",
        code: "growth",
        displayName: "Growth",
        status: "Active",
        monthlyPrice: 699,
        currencyCode: "PHP",
      }),
    ).toBe(699);
  });

  it("shows confirm and reject for pending payments with manage_manual_payments", () => {
    const caps = paymentActionCapabilities(payment({ status: "PendingConfirmation" }), {
      canManagePayments: true,
      canManageSubscriptions: true,
      subscriptions: [],
    });
    expect(caps.confirm).toBe(true);
    expect(caps.reject).toBe(true);
    expect(caps.void).toBe(false);
    expect(caps.activateFromPayment).toBe(false);
  });

  it("requires both permissions to activate from payment", () => {
    const subscriptions = [
      {
        id: "sub-1",
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        productCode: "pinoy-business-pos",
        planId: "plan-1",
        status: "Trialing",
      },
    ];
    const confirmed = payment({ status: "Confirmed" });
    expect(
      paymentActionCapabilities(confirmed, {
        canManagePayments: true,
        canManageSubscriptions: false,
        subscriptions,
      }).activateFromPayment,
    ).toBe(false);
    expect(
      paymentActionCapabilities(confirmed, {
        canManagePayments: true,
        canManageSubscriptions: true,
        subscriptions,
      }).activateFromPayment,
    ).toBe(true);
  });

  it("finds trialing subscription for product payment", () => {
    const subscriptions = [
      {
        id: "sub-1",
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        productCode: "pinoy-business-pos",
        planId: "plan-1",
        status: "Trialing",
      },
    ];
    expect(findSubscriptionForPayment(payment({ status: "Confirmed" }), subscriptions)?.id).toBe(
      "sub-1",
    );
  });
});
