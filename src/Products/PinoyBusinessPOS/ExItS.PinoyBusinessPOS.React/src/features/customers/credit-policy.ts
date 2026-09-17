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
    case "Active":
    case "Available":
      return "success";
    case "PendingApproval":
    case "NeedsSetup":
      return "warning";
    case "Disabled":
    case "Paused":
      return "danger";
    case "NotConfigured":
    case "Unavailable":
    default:
      return "neutral";
  }
}

/**
 * Seller display chip: Credit unavailable | Needs setup | Active | Paused.
 * Prefer server sellerDisplayStatus when present; otherwise derive from status + hasEverBeenApproved.
 */
export function resolveSellerCreditDisplayStatus(args: {
  status?: string | null;
  hasEverBeenApproved?: boolean | null;
  sellerDisplayStatus?: string | null;
}): string {
  const fromServer = (args.sellerDisplayStatus ?? "").trim();
  if (fromServer) {
    return fromServer;
  }
  const status = (args.status ?? "").trim();
  const ever = args.hasEverBeenApproved === true;
  if (status === "Approved") return "Active";
  if (status === "PendingApproval") return "NeedsSetup";
  if (status === "Disabled" && ever) return "Paused";
  return "Unavailable";
}

/**
 * Buyer display: Credit unavailable | Credit available | Credit paused.
 */
export function resolveBuyerCreditDisplayStatus(args: {
  status?: string | null;
  hasEverBeenApproved?: boolean | null;
  buyerDisplayStatus?: string | null;
}): string {
  const fromServer = (args.buyerDisplayStatus ?? "").trim();
  if (fromServer) {
    return fromServer;
  }
  const status = (args.status ?? "").trim();
  const ever = args.hasEverBeenApproved === true;
  if (status === "Approved") return "Available";
  if (status === "Disabled" && ever) return "Paused";
  return "Unavailable";
}

export function creditPolicyStatusLabelKey(status: string | null | undefined): MessageKey {
  switch ((status ?? "").trim()) {
    case "Approved":
    case "Active":
      return "customers.creditPolicy.status.Active";
    case "PendingApproval":
    case "NeedsSetup":
      return "customers.creditPolicy.status.NeedsSetup";
    case "Disabled":
    case "Paused":
      return "customers.creditPolicy.status.Paused";
    case "Available":
      return "customers.creditPolicy.buyerStatus.Available";
    case "NotConfigured":
    case "Unavailable":
    default:
      return "customers.creditPolicy.status.Unavailable";
  }
}

export function buyerCreditStatusLabelKey(displayStatus: string | null | undefined): MessageKey {
  switch ((displayStatus ?? "").trim()) {
    case "Available":
      return "customers.creditPolicy.buyerStatus.Available";
    case "Paused":
      return "customers.creditPolicy.buyerStatus.Paused";
    case "Unavailable":
    default:
      return "customers.creditPolicy.buyerStatus.Unavailable";
  }
}

/** Detail-page / dialog action label when opening the configure form. */
export function creditPolicyConfigureActionLabelKey(
  status: string | null | undefined,
): MessageKey {
  switch ((status ?? "").trim()) {
    case "PendingApproval":
      return "customers.creditPolicy.editProposedTerms";
    case "Approved":
      return "customers.creditPolicy.editCreditTerms";
    case "Disabled":
      return "customers.creditPolicy.setNewCreditTerms";
    case "NotConfigured":
    default:
      return "customers.creditPolicy.setCreditTerms";
  }
}

/** Primary submit label for the configure dialog. */
export function creditPolicyConfigureSubmitLabelKey(
  status: string | null | undefined,
): MessageKey {
  return (status ?? "").trim() === "PendingApproval"
    ? "customers.creditPolicy.updateProposedTerms"
    : "customers.creditPolicy.saveForApproval";
}

export function termDaysHelperLabelKey(days: number | null | undefined): MessageKey | null {
  if (days === 90) {
    return "customers.creditPolicy.termAbout3Months";
  }
  return null;
}

/** Format subject line under dialog title: "Name · PUBLICID". */
export function formatCreditPolicySubjectIdentity(
  name: string | null | undefined,
  publicId: string | null | undefined,
): string | null {
  const display = (name ?? "").trim();
  const id = (publicId ?? "").trim();
  if (!display && !id) {
    return null;
  }
  if (display && id) {
    return `${display} · ${id}`;
  }
  return display || id;
}

export function isCreditPolicyApproved(policy: PosCustomerCreditPolicy | null | undefined): boolean {
  return (policy?.status ?? "").trim() === "Approved";
}

/** Allow credit switch is ON for Approved (active) or PendingApproval (needs setup). */
export function isCreditAllowSwitchOn(status: string | null | undefined): boolean {
  const s = (status ?? "").trim();
  return s === "Approved" || s === "PendingApproval";
}

/** Terms exist and can be reused when re-enabling from Disabled. */
export function creditPolicyHasReusableTerms(policy: {
  status?: string | null;
  creditLimit?: number | null;
  defaultTermDays?: number | null;
} | null | undefined): boolean {
  if (!policy) return false;
  const status = (policy.status ?? "").trim();
  if (status === "NotConfigured") return false;
  return policy.creditLimit != null && policy.defaultTermDays != null;
}

export const CREDIT_ALLOW_ENABLE_REASON = "Allow credit turned on.";
export const CREDIT_ALLOW_DISABLE_REASON = "Allow credit turned off.";

/**
 * Client-side convenience gate for Utang checkout. Server still enforces policy.
 * Works for Person and Business policies (status + availableCredit).
 */
export function resolveUtangCreditPolicyBlock(args: {
  paymentIsUtang: boolean;
  /** @deprecated Prefer utangBuyerSelected */
  personCustomerSelected?: boolean;
  utangBuyerSelected?: boolean;
  policy: { status: string; availableCredit: number } | null | undefined;
  policyLoading: boolean;
  policyError: boolean;
  thisSaleAmount: number;
}): CreditPolicyCheckoutBlockReason {
  const buyerSelected = args.utangBuyerSelected ?? args.personCustomerSelected ?? false;
  if (!args.paymentIsUtang || !buyerSelected) {
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
