import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import {
  isCheckoutBusiness,
  isCheckoutPerson,
} from "@/features/checkout/checkout-customer-option";
import type { StatusChipTone } from "@/components/exits/StatusChip";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";

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

export type UtangDirectorySelectBlockReason =
  | "b2b_not_available"
  | "pending_approval"
  | "not_configured"
  | "disabled"
  | "over_limit";

export function resolveUtangDirectorySelectBlock(args: {
  customer: CheckoutCustomerOption;
  thisSaleAmount: number;
}): { reason: UtangDirectorySelectBlockReason; availableCredit?: number } | null {
  if (isCheckoutBusiness(args.customer)) {
    return { reason: "b2b_not_available" };
  }
  if (!isCheckoutPerson(args.customer)) {
    return { reason: "not_configured" };
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

export function utangDirectorySelectToastMessage(
  block: { reason: UtangDirectorySelectBlockReason; availableCredit?: number },
  t: (key: MessageKey) => string,
): string {
  switch (block.reason) {
    case "b2b_not_available":
      return t("checkout.utangSelect.b2bNotAvailable");
    case "pending_approval":
      return t("checkout.utangSelect.pendingApproval");
    case "not_configured":
      return t("checkout.utangSelect.notConfigured");
    case "disabled":
      return t("checkout.utangSelect.disabled");
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
