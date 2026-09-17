import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import {
  isCheckoutBusiness,
  isCheckoutBusinessDirectoryRow,
  isCheckoutPerson,
} from "@/features/checkout/checkout-customer-option";
import type { StatusChipTone } from "@/components/exits/StatusChip";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";
import {
  resolveCustomerListConnectionBadge,
  type CustomerListConnectionOverlay,
} from "@/features/customers/customer-list-connection";

/** User-facing directory credit status (backend enum unchanged). */
export function checkoutCreditStatusLabelKey(
  status: string | null | undefined,
): MessageKey {
  switch ((status ?? "").trim()) {
    case "Approved":
      return "checkout.directoryCredit.status.Approved";
    case "PendingApproval":
      return "checkout.directoryCredit.status.PendingApproval";
    case "Disabled":
      return "checkout.directoryCredit.status.Disabled";
    case "NotConfigured":
    default:
      return "checkout.directoryCredit.status.NotConfigured";
  }
}

export function checkoutCreditStatusTone(
  status: string | null | undefined,
): StatusChipTone {
  switch ((status ?? "").trim()) {
    case "Approved":
      return "success";
    case "PendingApproval":
      return "warning";
    case "Disabled":
      return "warning";
    case "NotConfigured":
    default:
      return "neutral";
  }
}

/** Canonical connection status for the Utang Connection column (not credit). */
export type CheckoutConnectionDisplay =
  | { kind: "none" }
  | { kind: "chip"; statusKey: "Pending" | "Connected" | "Declined" | "Disconnected"; raw: string }
  | { kind: "raw"; raw: string };

export function resolveCheckoutConnectionDisplay(
  customer: CheckoutCustomerOption,
  overlay?: CustomerListConnectionOverlay | null,
): CheckoutConnectionDisplay {
  if (isCheckoutBusiness(customer)) {
    const raw = (customer.status ?? "").trim();
    const normalized = raw.toLowerCase();
    if (normalized === "pending") {
      return { kind: "chip", statusKey: "Pending", raw };
    }
    if (normalized === "active") {
      return { kind: "chip", statusKey: "Connected", raw };
    }
    if (normalized === "declined") {
      return { kind: "chip", statusKey: "Declined", raw };
    }
    if (normalized === "disconnected") {
      return { kind: "chip", statusKey: "Disconnected", raw };
    }
    if (!raw) {
      return { kind: "none" };
    }
    return { kind: "raw", raw };
  }

  if (isCheckoutPerson(customer) && !isCheckoutBusinessDirectoryRow(customer)) {
    const badge = resolveCustomerListConnectionBadge(customer, overlay);
    if (badge === "connected") {
      return { kind: "chip", statusKey: "Connected", raw: "Connected" };
    }
    if (badge === "pending") {
      return { kind: "chip", statusKey: "Pending", raw: "Pending" };
    }
    return { kind: "none" };
  }

  return { kind: "none" };
}

export function checkoutConnectionStatusLabelKey(
  statusKey: "Pending" | "Connected" | "Declined" | "Disconnected",
): MessageKey {
  switch (statusKey) {
    case "Pending":
      return "checkout.directoryConnection.status.Pending";
    case "Connected":
      return "checkout.directoryConnection.status.Connected";
    case "Declined":
      return "checkout.directoryConnection.status.Declined";
    case "Disconnected":
      return "checkout.directoryConnection.status.Disconnected";
  }
}

export function checkoutConnectionStatusTone(
  statusKey: "Pending" | "Connected" | "Declined" | "Disconnected",
): StatusChipTone {
  switch (statusKey) {
    case "Connected":
      return "success";
    case "Pending":
      return "warning";
    case "Declined":
    case "Disconnected":
      return "neutral";
  }
}

export type UtangDirectorySelectBlockReason =
  | "pending_approval"
  | "not_configured"
  | "disabled"
  | "over_limit"
  | "inactive"
  | "connection_pending";

function normalizeCreditStatus(raw: string | null | undefined): string {
  return (raw ?? "").trim();
}

export function resolveUtangDirectorySelectBlock(args: {
  customer: CheckoutCustomerOption;
  thisSaleAmount: number;
  overlay?: CustomerListConnectionOverlay | null;
}): { reason: UtangDirectorySelectBlockReason; availableCredit?: number } | null {
  if (isCheckoutBusiness(args.customer)) {
    const relationship = (args.customer.status ?? "").trim().toLowerCase();
    if (relationship === "pending") {
      return { reason: "connection_pending" };
    }
    if (relationship !== "active") {
      return { reason: "inactive" };
    }
    const status = normalizeCreditStatus(args.customer.creditStatus);
    if (status === "PendingApproval") {
      return { reason: "pending_approval" };
    }
    if (status === "Disabled") {
      return { reason: "disabled" };
    }
    if (status !== "Approved") {
      return { reason: "not_configured" };
    }
    const available = args.customer.availableCredit ?? 0;
    if (args.thisSaleAmount > available + 1e-9) {
      return { reason: "over_limit", availableCredit: available };
    }
    return null;
  }
  if (!isCheckoutPerson(args.customer)) {
    return { reason: "not_configured" };
  }

  if (!isCheckoutBusinessDirectoryRow(args.customer)) {
    const badge = resolveCustomerListConnectionBadge(args.customer, args.overlay);
    if (badge === "pending") {
      return { reason: "connection_pending" };
    }
  }

  const statusRaw = args.customer.creditStatus;
  if (statusRaw == null || String(statusRaw).trim() === "") {
    // Directory projection missing — allow select; live policy query remains authoritative.
    return null;
  }
  const status = String(statusRaw).trim();
  if (status === "PendingApproval") {
    return { reason: "pending_approval" };
  }
  if (status === "Disabled") {
    return { reason: "disabled" };
  }
  if (status !== "Approved") {
    return { reason: "not_configured" };
  }

  const available = args.customer.availableCredit ?? 0;
  if (args.thisSaleAmount > available + 1e-9) {
    return { reason: "over_limit", availableCredit: available };
  }
  return null;
}

/** Pending Personal overlay or Pending B2B relationship — blocks debt methods only. */
export function isPendingRelationshipCustomer(
  customer: CheckoutCustomerOption | null | undefined,
  overlay?: CustomerListConnectionOverlay | null,
): boolean {
  if (!customer) {
    return false;
  }
  if (isCheckoutBusiness(customer)) {
    return (customer.status ?? "").trim().toLowerCase() === "pending";
  }
  if (!isCheckoutPerson(customer) || isCheckoutBusinessDirectoryRow(customer)) {
    return false;
  }
  return resolveCustomerListConnectionBadge(customer, overlay) === "pending";
}

export function utangDirectorySelectToastMessage(
  block: { reason: UtangDirectorySelectBlockReason; availableCredit?: number },
  t: (key: MessageKey) => string,
): string {
  switch (block.reason) {
    case "pending_approval":
      return t("checkout.utangSelect.pendingApproval");
    case "not_configured":
      return t("checkout.utangSelect.notConfigured");
    case "disabled":
      return t("checkout.utangSelect.disabled");
    case "inactive":
      return t("checkout.utangSelect.businessInactive");
    case "connection_pending":
      return t("checkout.utangSelect.connectionPending");
    case "over_limit":
      return t("checkout.utangSelect.overLimit").replace(
        "{amount}",
        formatPeso(block.availableCredit ?? 0),
      );
    default:
      return t("checkout.utangSelect.notConfigured");
  }
}

/** Format YYYY-MM-DD as "Oct 10, 2026". */
export function formatCreditDueDateLabel(
  isoDate: string,
  locale = "en-PH",
): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate.trim());
  if (!match) {
    return isoDate;
  }
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  return new Intl.DateTimeFormat(locale, {
    month: "short",
    day: "numeric",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, day)));
}
