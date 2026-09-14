/**
 * Marks an unpaid PayNow subscription checkout so onboarding resume / auto-nav
 * cannot override the payment flow until Paid (+ user continues).
 * Pre-org payments may omit organizationId until Start Business.
 */
export const PENDING_SUBSCRIPTION_CHECKOUT_STORAGE_KEY = "exits.pendingSubscriptionCheckout";

export type PendingSubscriptionCheckout = {
  paymentId: string;
  organizationId?: string | null;
  planKey?: string | null;
  billingCycle?: string | null;
};

export function readPendingSubscriptionCheckout(): PendingSubscriptionCheckout | null {
  try {
    const raw = sessionStorage.getItem(PENDING_SUBSCRIPTION_CHECKOUT_STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const pending = JSON.parse(raw) as PendingSubscriptionCheckout;
    if (!pending?.paymentId?.trim()) {
      return null;
    }
    return {
      paymentId: pending.paymentId.trim(),
      organizationId: pending.organizationId?.trim() || null,
      planKey: pending.planKey?.trim() || null,
      billingCycle: pending.billingCycle?.trim() || null,
    };
  } catch {
    return null;
  }
}

export function writePendingSubscriptionCheckout(pending: PendingSubscriptionCheckout): void {
  sessionStorage.setItem(
    PENDING_SUBSCRIPTION_CHECKOUT_STORAGE_KEY,
    JSON.stringify({
      paymentId: pending.paymentId.trim(),
      organizationId: pending.organizationId?.trim() || null,
      planKey: pending.planKey?.trim() || null,
      billingCycle: pending.billingCycle?.trim() || null,
    }),
  );
}

export function clearPendingSubscriptionCheckout(): void {
  sessionStorage.removeItem(PENDING_SUBSCRIPTION_CHECKOUT_STORAGE_KEY);
}

export function hasPendingSubscriptionCheckout(): boolean {
  return readPendingSubscriptionCheckout() !== null;
}

export function pendingSubscriptionCheckoutForOrganization(
  organizationId: string | null | undefined,
): PendingSubscriptionCheckout | null {
  const pending = readPendingSubscriptionCheckout();
  if (!pending) {
    return null;
  }
  if (!organizationId?.trim()) {
    // Pre-org pending checkout still blocks onboarding routes.
    return pending.organizationId ? null : pending;
  }
  if (!pending.organizationId) {
    return pending;
  }
  if (pending.organizationId.toLowerCase() !== organizationId.trim().toLowerCase()) {
    return null;
  }
  return pending;
}

/** Paths where onboarding resume must never steal navigation. */
export function shouldSkipOnboardingResume(pathname: string): boolean {
  return (
    pathname.startsWith("/onboarding") ||
    pathname.startsWith("/personal") ||
    pathname.startsWith("/subscription-checkout")
  );
}

/** Active unpaid checkout blocks onboarding resume even off the checkout route. */
export function shouldBlockOnboardingForPendingCheckout(
  organizationId: string | null | undefined,
): boolean {
  return pendingSubscriptionCheckoutForOrganization(organizationId) !== null
    || hasPendingSubscriptionCheckout();
}
