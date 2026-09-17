import { afterEach, describe, expect, it } from "vitest";
import {
  clearPendingSubscriptionCheckout,
  hasPendingSubscriptionCheckout,
  pendingSubscriptionCheckoutForOrganization,
  readPendingSubscriptionCheckout,
  shouldBlockOnboardingForPendingCheckout,
  shouldSkipOnboardingResume,
  writePendingSubscriptionCheckout,
} from "@/features/subscription-checkout/pending-subscription-checkout";

const ORG_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const ORG_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

describe("pending-subscription-checkout", () => {
  afterEach(() => {
    clearPendingSubscriptionCheckout();
  });

  it("stores payment-only pending checkout without organization", () => {
    expect(hasPendingSubscriptionCheckout()).toBe(false);
    writePendingSubscriptionCheckout({
      paymentId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      planKey: "pro",
      billingCycle: "Monthly",
    });
    expect(hasPendingSubscriptionCheckout()).toBe(true);
    expect(readPendingSubscriptionCheckout()?.paymentId).toBe(
      "cccccccc-cccc-cccc-cccc-cccccccccccc",
    );
    expect(readPendingSubscriptionCheckout()?.organizationId).toBeNull();
    expect(
      pendingSubscriptionCheckoutForOrganization(null)?.paymentId,
    ).toBe("cccccccc-cccc-cccc-cccc-cccccccccccc");
    expect(shouldBlockOnboardingForPendingCheckout(null)).toBe(true);
    clearPendingSubscriptionCheckout();
    expect(hasPendingSubscriptionCheckout()).toBe(false);
  });

  it("does not treat pre-org pending as matching a concrete organization", () => {
    writePendingSubscriptionCheckout({
      paymentId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      planKey: "pro",
      billingCycle: "Monthly",
    });

    expect(pendingSubscriptionCheckoutForOrganization(ORG_A)).toBeNull();
    expect(shouldBlockOnboardingForPendingCheckout(ORG_A)).toBe(false);
    expect(pendingSubscriptionCheckoutForOrganization(ORG_B)).toBeNull();
  });

  it("matches only the pending organization when organizationId is set", () => {
    writePendingSubscriptionCheckout({
      paymentId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      organizationId: ORG_A,
      planKey: "growth",
      billingCycle: "Monthly",
    });

    expect(pendingSubscriptionCheckoutForOrganization(ORG_A)?.paymentId).toBe(
      "dddddddd-dddd-dddd-dddd-dddddddddddd",
    );
    expect(shouldBlockOnboardingForPendingCheckout(ORG_A)).toBe(true);
    expect(pendingSubscriptionCheckoutForOrganization(ORG_B)).toBeNull();
    expect(shouldBlockOnboardingForPendingCheckout(ORG_B)).toBe(false);
    expect(pendingSubscriptionCheckoutForOrganization(null)).toBeNull();
  });

  it("skips onboarding resume on subscription-checkout paths", () => {
    expect(shouldSkipOnboardingResume("/subscription-checkout/pay-1")).toBe(true);
    expect(shouldSkipOnboardingResume("/subscription-checkout/pay-1/gcash")).toBe(true);
    expect(shouldSkipOnboardingResume("/subscription-checkout/pay-1/result")).toBe(true);
    expect(shouldSkipOnboardingResume("/onboarding")).toBe(true);
    expect(shouldSkipOnboardingResume("/personal/explore-pos")).toBe(true);
    expect(shouldSkipOnboardingResume("/org")).toBe(false);
    expect(shouldSkipOnboardingResume("/role/manager")).toBe(false);
  });
});
