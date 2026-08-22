import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import type { OrganizationPayment } from "@/api/organizations/billing-list-query";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import { isPinoyBusinessPosSubscription } from "@/features/organizations/subscription-lifecycle";

export const MANUAL_PAYMENT_METHODS = ["Cash", "BankTransfer", "GCash"] as const;
export type ManualPaymentMethod = (typeof MANUAL_PAYMENT_METHODS)[number];

export type PaymentActionCapabilities = {
  confirm: boolean;
  reject: boolean;
  void: boolean;
  activateFromPayment: boolean;
  createPaidSubscription: boolean;
};

export function paymentActionCapabilities(
  payment: OrganizationPayment,
  options: {
    canManagePayments: boolean;
    canManageSubscriptions: boolean;
    subscriptions: OrganizationSubscription[];
  },
): PaymentActionCapabilities {
  const { canManagePayments, canManageSubscriptions, subscriptions } = options;
  const pending = payment.status === "PendingConfirmation";
  const confirmed = payment.status === "Confirmed";
  const unused = !payment.subscriptionId;
  const matchingSubscription = findSubscriptionForPayment(payment, subscriptions);
  const hasProductSubscription = subscriptions.some(
    (item) => item.productCode === payment.productCode,
  );

  return {
    confirm: canManagePayments && pending,
    reject: canManagePayments && pending,
    void: canManagePayments && confirmed && unused,
    activateFromPayment:
      canManagePayments &&
      canManageSubscriptions &&
      confirmed &&
      unused &&
      matchingSubscription != null &&
      isSubscriptionEligibleForPaymentActivation(matchingSubscription),
    createPaidSubscription:
      canManagePayments &&
      canManageSubscriptions &&
      confirmed &&
      unused &&
      !hasProductSubscription,
  };
}

export function isSubscriptionEligibleForPaymentActivation(
  subscription: OrganizationSubscription,
): boolean {
  const status = subscription.status;
  return (
    status === "Trialing" ||
    status === "GracePeriod" ||
    status === "PastDue" ||
    status === "Suspended" ||
    status === "Expired"
  );
}

export function findSubscriptionForPayment(
  payment: OrganizationPayment,
  subscriptions: OrganizationSubscription[],
): OrganizationSubscription | undefined {
  return subscriptions.find(
    (item) =>
      item.productCode === payment.productCode &&
      isSubscriptionEligibleForPaymentActivation(item),
  );
}

export function computeMonthlyPaidPeriod(reference = new Date()): {
  periodStartUtc: string;
  periodEndUtc: string;
} {
  const start = new Date(
    Date.UTC(reference.getUTCFullYear(), reference.getUTCMonth(), reference.getUTCDate()),
  );
  const end = new Date(start);
  end.setUTCMonth(end.getUTCMonth() + 1);
  return { periodStartUtc: start.toISOString(), periodEndUtc: end.toISOString() };
}

export function planPriceLabel(plan: CatalogPlan, billingCycle: "Monthly" | "Annual" = "Monthly"): string {
  const amount = billingCycle === "Annual" ? plan.annualPrice : plan.monthlyPrice;
  if (amount == null || !plan.currencyCode) {
    return "—";
  }
  return `${amount} ${plan.currencyCode}`;
}

export function defaultPaymentAmountForPlan(
  plan: CatalogPlan,
  billingCycle: "Monthly" | "Annual" = "Monthly",
): number | undefined {
  const amount = billingCycle === "Annual" ? plan.annualPrice : plan.monthlyPrice;
  return typeof amount === "number" && amount > 0 ? amount : undefined;
}

export function pinoyBusinessPosProductCode(): string {
  return "pinoy-business-pos";
}

export function primaryBillingProductCode(subscriptions: OrganizationSubscription[]): string {
  const pos = subscriptions.find(isPinoyBusinessPosSubscription);
  return pos?.productCode ?? pinoyBusinessPosProductCode();
}
