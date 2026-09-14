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

/**
 * Resolve a pending checkout that belongs to a specific organization context.
 *
 * Pre-org markers (organizationId null) apply only when organizationId is also
 * null/empty. They must NOT match a concrete Manage Business org — that caused
 * /onboarding → /subscription-checkout soft-locks for abandoned Explore checkouts.
 */
export function pendingSubscriptionCheckoutForOrganization(
  organizationId: string | null | undefined,
): PendingSubscriptionCheckout | null {
  const pending = readPendingSubscriptionCheckout();
  if (!pending) {
    return null;
  }

  const org = organizationId?.trim() || null;
  const pendingOrg = pending.organizationId?.trim() || null;

  if (!org) {
    return pendingOrg ? null : pending;
  }

  if (!pendingOrg) {
    return null;
  }

  if (pendingOrg.toLowerCase() !== org.toLowerCase()) {
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

/**
 * Block onboarding resume only when the pending checkout is scoped to this org.
 * Pre-org Explore checkout is already protected by shouldSkipOnboardingResume on
 * /personal and /subscription-checkout paths — do not sticky-block all orgs.
 */
export function shouldBlockOnboardingForPendingCheckout(
  organizationId: string | null | undefined,
): boolean {
  return pendingSubscriptionCheckoutForOrganization(organizationId) !== null;
}
