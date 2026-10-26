import { describe, expect, it } from "vitest";
import { paymentStatusLabel } from "@/features/customer-ordering/personal-order-card";
import type { MessageKey } from "@/i18n/messages";

const labels: Partial<Record<MessageKey, string>> = {
  "orders.paymentPaid": "Paid",
  "orders.paymentOnCredit": "On Credit",
  "orders.paymentPartiallyPaid": "Partially Paid",
  "orders.paymentCreditPending": "Credit Pending",
  "orders.paymentCreditDeclined": "Credit Declined",
  "orders.paymentAwaitingVerification": "Awaiting verification",
  "orders.paymentUnpaid": "Unpaid",
};

function t(key: MessageKey): string {
  return labels[key] ?? key;
}

describe("paymentStatusLabel", () => {
  it("maps Utang payment statuses to credit lifecycle labels", () => {
    expect(paymentStatusLabel("Unpaid", t, "Utang")).toBe("On Credit");
    expect(paymentStatusLabel("PartiallyPaid", t, "Utang")).toBe("Partially Paid");
    expect(paymentStatusLabel("Paid", t, "Utang")).toBe("Paid");
    expect(paymentStatusLabel("Pending", t, "Utang")).toBe("Credit Pending");
    expect(paymentStatusLabel("CreditPending", t, "Utang")).toBe("Credit Pending");
    expect(paymentStatusLabel("Declined", t, "Utang")).toBe("Credit Declined");
    expect(paymentStatusLabel("CreditDeclined", t, "Utang")).toBe("Credit Declined");
  });

  it("keeps Cash / GCash labels unchanged", () => {
    expect(paymentStatusLabel("Paid", t, "Cash")).toBe("Paid");
    expect(paymentStatusLabel("Unpaid", t, "Cash")).toBe("Unpaid");
    expect(paymentStatusLabel("Pending", t, "ManualGCash")).toBe("Awaiting verification");
  });
});
