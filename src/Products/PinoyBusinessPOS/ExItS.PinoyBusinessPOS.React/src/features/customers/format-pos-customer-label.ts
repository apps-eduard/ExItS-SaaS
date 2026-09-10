import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import { stripLocalValidationRunStamp } from "@/lib/local-validation-run-stamp";

const WALK_IN_CORE = /^(local\s+)?walk[- ]?ins?$/i;
const GENERATED_LINKED_SUFFIX = /\s+linked(?:\s+\d+)?$/i;

export function isSeededWalkInCustomerName(displayName: string | null | undefined): boolean {
  const raw = displayName?.trim() ?? "";
  if (!raw) {
    return false;
  }
  return WALK_IN_CORE.test(stripLocalValidationRunStamp(raw));
}

export function posCustomerDisplayName(
  displayName: string | null | undefined,
  walkInLabel: string,
): string {
  const raw = displayName?.trim() ?? "";
  if (!raw) {
    return walkInLabel;
  }

  let cleaned = stripLocalValidationRunStamp(raw) || raw;
  if (GENERATED_LINKED_SUFFIX.test(cleaned)) {
    cleaned = cleaned.replace(GENERATED_LINKED_SUFFIX, "").trim() || cleaned;
  }

  if (WALK_IN_CORE.test(cleaned) || isSeededWalkInCustomerName(raw) || /\d{8,}/.test(cleaned)) {
    return walkInLabel;
  }

  return cleaned;
}

export function checkoutCustomerTitle(
  customer: { displayName: string; resolvedPersonalDisplayName?: string | null },
  walkInLabel: string,
): string {
  const resolved = customer.resolvedPersonalDisplayName?.trim();
  if (resolved) {
    return resolved;
  }
  return posCustomerDisplayName(customer.displayName, walkInLabel);
}

export function checkoutCustomerHasExItsCorrelation(customer: CheckoutCustomerOption): boolean {
  if (customer.kind === "Business") {
    return Boolean(customer.buyerPublicOrganizationId);
  }
  return Boolean(
    customer.linkedPersonalPublicUserId ||
      customer.resolvedPersonalDisplayName ||
      customer.linkedBuyerPublicOrganizationId,
  );
}

export function shouldShowCheckoutCustomerWhenIdle(customer: CheckoutCustomerOption): boolean {
  if (customer.kind === "Business") {
    return true;
  }
  if (customer.linkedPersonalPublicUserId) {
    return true;
  }
  return !isSeededWalkInCustomerName(customer.displayName);
}

export function visibleCheckoutCustomers(
  customers: CheckoutCustomerOption[],
  search: string,
): CheckoutCustomerOption[] {
  const trimmed = search.trim();
  const source = trimmed ? customers : customers.filter(shouldShowCheckoutCustomerWhenIdle);

  return [...source].sort((a, b) => {
    const aLinked = a.kind === "Customer" && Boolean(a.linkedPersonalPublicUserId);
    const bLinked = b.kind === "Customer" && Boolean(b.linkedPersonalPublicUserId);
    const linkedDelta = Number(bLinked) - Number(aLinked);
    if (linkedDelta !== 0) {
      return linkedDelta;
    }
    // Businesses before walk-in people when idle.
    if (a.kind !== b.kind) {
      return a.kind === "Business" ? -1 : 1;
    }
    return (
      Number(isSeededWalkInCustomerName(a.displayName)) -
      Number(isSeededWalkInCustomerName(b.displayName))
    );
  });
}
