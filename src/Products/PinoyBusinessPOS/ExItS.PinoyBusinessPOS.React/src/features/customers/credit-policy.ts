import type { PosCustomerCreditPolicy } from "@/api/pos/pos-credit-policy-client";
import type { StatusChipTone } from "@/components/exits/StatusChip";
import type { MessageKey } from "@/i18n/messages";

export const CREDIT_TERM_PRESETS = [7, 15, 30, 60, 90] as const;

export type CreditPolicyCheckoutBlockReason =
  | "pending_approval"
  | "not_configured"
  | "disabled"
  | "not_approved"
  | "over_limit"
  | "loading"
  | "error"
  | null;

/** Mirrors domain CustomerCreditPolicy.ComputeDefaultDueDate (sale date + term days). */
export function computeCreditPolicyDueDate(
  saleDate: Date,
  defaultTermDays: number,
): string {
  const days = Math.max(1, Math.floor(defaultTermDays));
  const due = new Date(
    Date.UTC(saleDate.getUTCFullYear(), saleDate.getUTCMonth(), saleDate.getUTCDate()),
  );
  due.setUTCDate(due.getUTCDate() + days);
  return due.toISOString().slice(0, 10);
}

export function creditPolicyStatusTone(status: string | null | undefined): StatusChipTone {
  switch ((status ?? "").trim()) {
    case "Approved":
      return "success";
    case "PendingApproval":
      return "warning";
    case "Disabled":
      return "danger";
    case "NotConfigured":
    default:
      return "neutral";
  }
}

export function creditPolicyStatusLabelKey(status: string | null | undefined): MessageKey {
  switch ((status ?? "").trim()) {
    case "Approved":
      return "customers.creditPolicy.status.Approved";
    case "PendingApproval":
      return "customers.creditPolicy.status.PendingApproval";
    case "Disabled":
      return "customers.creditPolicy.status.Disabled";
    case "NotConfigured":
    default:
      return "customers.creditPolicy.status.NotConfigured";
  }
}

export function termDaysHelperLabelKey(days: number | null | undefined): MessageKey | null {
  if (days === 90) {
    return "customers.creditPolicy.termAbout3Months";
  }
  return null;
}

export function isCreditPolicyApproved(policy: PosCustomerCreditPolicy | null | undefined): boolean {
  return (policy?.status ?? "").trim() === "Approved";
}

/**
 * Client-side convenience gate for Utang checkout. Server still enforces policy.
 */
export function resolveUtangCreditPolicyBlock(args: {
  paymentIsUtang: boolean;
  personCustomerSelected: boolean;
  policy: PosCustomerCreditPolicy | null | undefined;
  policyLoading: boolean;
  policyError: boolean;
  thisSaleAmount: number;
}): CreditPolicyCheckoutBlockReason {
  if (!args.paymentIsUtang || !args.personCustomerSelected) {
    return null;
  }
  if (args.policyLoading) {
    return "loading";
  }
  if (args.policyError || !args.policy) {
    return "error";
  }
  const status = (args.policy.status ?? "").trim();
  if (status === "PendingApproval") {
    return "pending_approval";
  }
  if (status === "Disabled") {
    return "disabled";
  }
  if (status === "NotConfigured" || status !== "Approved") {
    return status === "NotConfigured" ? "not_configured" : "not_approved";
  }
  const available = args.policy.availableCredit ?? 0;
  if (args.thisSaleAmount > available + 1e-9) {
    return "over_limit";
  }
  return null;
}

export function creditPolicyCheckoutBlockMessageKey(
  reason: CreditPolicyCheckoutBlockReason,
): MessageKey | null {
  switch (reason) {
    case "pending_approval":
      return "checkout.creditPolicy.pendingApproval";
    case "not_configured":
      return "checkout.creditPolicy.notConfigured";
    case "disabled":
      return "checkout.creditPolicy.disabled";
    case "not_approved":
      return "checkout.creditPolicy.notApproved";
    case "over_limit":
      return "checkout.creditPolicy.overLimit";
    case "loading":
      return "checkout.creditPolicy.loading";
    case "error":
      return "checkout.creditPolicy.loadError";
    default:
      return null;
  }
}

export function outstandingExceedsNewLimit(
  outstanding: number,
  newLimit: number,
): boolean {
  return newLimit + 1e-9 < outstanding;
}
