import { describe, expect, it } from "vitest";
import type { PosCustomerCreditPolicy } from "@/api/pos/pos-credit-policy-client";
import {
  computeCreditPolicyDueDate,
  creditPolicyCheckoutBlockMessageKey,
  creditPolicyStatusLabelKey,
  creditPolicyStatusTone,
  outstandingExceedsNewLimit,
  resolveUtangCreditPolicyBlock,
  termDaysHelperLabelKey,
} from "@/features/customers/credit-policy";

function policy(partial: Partial<PosCustomerCreditPolicy>): PosCustomerCreditPolicy {
  return {
    customerId: "11111111-1111-1111-1111-111111111111",
    status: "NotConfigured",
    creditLimit: null,
    defaultTermDays: null,
    outstandingAmount: 0,
    availableCredit: 0,
    configuredByUserId: null,
    configuredAtUtc: null,
    approvedByUserId: null,
    approvedAtUtc: null,
    updatedByUserId: null,
    updatedAtUtc: null,
    expectedUpdatedAtUtc: null,
    ...partial,
  };
}

describe("credit-policy helpers", () => {
  it("maps status labels and tones", () => {
    expect(creditPolicyStatusLabelKey("NotConfigured")).toBe(
      "customers.creditPolicy.status.NotConfigured",
    );
    expect(creditPolicyStatusLabelKey("PendingApproval")).toBe(
      "customers.creditPolicy.status.PendingApproval",
    );
    expect(creditPolicyStatusLabelKey("Approved")).toBe(
      "customers.creditPolicy.status.Approved",
    );
    expect(creditPolicyStatusLabelKey("Disabled")).toBe(
      "customers.creditPolicy.status.Disabled",
    );
    expect(creditPolicyStatusTone("Approved")).toBe("success");
    expect(creditPolicyStatusTone("PendingApproval")).toBe("warning");
    expect(creditPolicyStatusTone("Disabled")).toBe("danger");
    expect(creditPolicyStatusTone("NotConfigured")).toBe("neutral");
  });

  it("flags 90-day term helper", () => {
    expect(termDaysHelperLabelKey(90)).toBe("customers.creditPolicy.termAbout3Months");
    expect(termDaysHelperLabelKey(30)).toBeNull();
  });

  it("computes due date from term days", () => {
    expect(computeCreditPolicyDueDate(new Date("2026-09-10T12:00:00Z"), 30)).toBe(
      "2026-10-10",
    );
  });

  it("warns when outstanding exceeds new limit", () => {
    expect(outstandingExceedsNewLimit(500, 400)).toBe(true);
    expect(outstandingExceedsNewLimit(500, 500)).toBe(false);
  });

  it("blocks checkout when policy is not approved or over limit", () => {
    expect(
      resolveUtangCreditPolicyBlock({
        paymentIsUtang: true,
        personCustomerSelected: true,
        policy: policy({ status: "NotConfigured" }),
        policyLoading: false,
        policyError: false,
        thisSaleAmount: 100,
      }),
    ).toBe("not_configured");

    expect(
      resolveUtangCreditPolicyBlock({
        paymentIsUtang: true,
        personCustomerSelected: true,
        policy: policy({ status: "PendingApproval", creditLimit: 1000 }),
        policyLoading: false,
        policyError: false,
        thisSaleAmount: 100,
      }),
    ).toBe("pending_approval");

    expect(
      resolveUtangCreditPolicyBlock({
        paymentIsUtang: true,
        personCustomerSelected: true,
        policy: policy({ status: "Disabled", creditLimit: 1000 }),
        policyLoading: false,
        policyError: false,
        thisSaleAmount: 100,
      }),
    ).toBe("disabled");

    expect(
      resolveUtangCreditPolicyBlock({
        paymentIsUtang: true,
        personCustomerSelected: true,
        policy: policy({
          status: "Approved",
          creditLimit: 1000,
          availableCredit: 50,
          outstandingAmount: 950,
        }),
        policyLoading: false,
        policyError: false,
        thisSaleAmount: 100,
      }),
    ).toBe("over_limit");

    expect(
      resolveUtangCreditPolicyBlock({
        paymentIsUtang: true,
        personCustomerSelected: true,
        policy: policy({
          status: "Approved",
          creditLimit: 1000,
          availableCredit: 200,
          outstandingAmount: 800,
        }),
        policyLoading: false,
        policyError: false,
        thisSaleAmount: 100,
      }),
    ).toBeNull();

    expect(creditPolicyCheckoutBlockMessageKey("pending_approval")).toBe(
      "checkout.creditPolicy.pendingApproval",
    );
    expect(creditPolicyCheckoutBlockMessageKey("not_configured")).toBe(
      "checkout.creditPolicy.notConfigured",
    );
    expect(creditPolicyCheckoutBlockMessageKey("disabled")).toBe(
      "checkout.creditPolicy.disabled",
    );
    expect(creditPolicyCheckoutBlockMessageKey("over_limit")).toBe(
      "checkout.creditPolicy.overLimit",
    );
  });
});
