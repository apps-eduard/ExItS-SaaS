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
