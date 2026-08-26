import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import type { OrganizationPayment } from "@/api/organizations/billing-list-query";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import { isPinoyBusinessPosSubscription } from "@/features/organizations/subscription-lifecycle";

export const MANUAL_PAYMENT_METHODS = ["Cash", "BankTransfer", "GCash"] as const;
export type ManualPaymentMethod = (typeof MANUAL_PAYMENT_METHODS)[number];

export type BillingCycleChoice = "Monthly" | "Annual";

export type BillingUpgradeContext = {
  upgradeSubscriptionId: string;
  targetPlanId: string;
  billingCycle: BillingCycleChoice;
};

export type PaymentActionCapabilities = {
  confirm: boolean;
  reject: boolean;
  void: boolean;
  activateFromPayment: boolean;
  createPaidSubscription: boolean;
  completeUpgradeFromPayment: boolean;
};

export function supportedBillingCycles(plan: CatalogPlan): BillingCycleChoice[] {
  const cycles: BillingCycleChoice[] = [];
  if (typeof plan.monthlyPrice === "number" && plan.monthlyPrice > 0) {
    cycles.push("Monthly");
  }
  if (typeof plan.annualPrice === "number" && plan.annualPrice > 0) {
    cycles.push("Annual");
  }
  return cycles;
}

export function defaultBillingCycle(plan: CatalogPlan): BillingCycleChoice {
  return supportedBillingCycles(plan)[0] ?? "Monthly";
}

export function paymentActionCapabilities(
  payment: OrganizationPayment,
  options: {
    canManagePayments: boolean;
    canManageSubscriptions: boolean;
    subscriptions: OrganizationSubscription[];
    upgradeContext?: BillingUpgradeContext | null;
    targetPlan?: CatalogPlan | null;
    billingCycle?: BillingCycleChoice;
  },
): PaymentActionCapabilities {
  const {
    canManagePayments,
    canManageSubscriptions,
    subscriptions,
    upgradeContext,
    targetPlan,
    billingCycle,
  } = options;
  const pending = payment.status === "PendingConfirmation";
  const confirmed = payment.status === "Confirmed";
  const unused = !payment.subscriptionId;
  const matchingSubscription = findSubscriptionForPayment(payment, subscriptions);
  const hasProductSubscription = subscriptions.some(
    (item) => item.productCode === payment.productCode,
  );
  const cycle = billingCycle ?? (targetPlan ? defaultBillingCycle(targetPlan) : "Monthly");
  const canCompleteUpgrade =
    Boolean(upgradeContext) &&
    Boolean(targetPlan) &&
    paymentMatchesUpgradeTarget(payment, targetPlan!, cycle) &&
    subscriptions.some(
      (item) =>
        item.id === upgradeContext!.upgradeSubscriptionId &&
        item.status === "Active" &&
        item.productCode === payment.productCode,
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
      isSubscriptionEligibleForPaymentActivation(matchingSubscription) &&
      !upgradeContext,
    createPaidSubscription:
      canManagePayments &&
      canManageSubscriptions &&
      confirmed &&
      unused &&
      !hasProductSubscription &&
      !upgradeContext,
    completeUpgradeFromPayment:
      canManagePayments && canManageSubscriptions && confirmed && unused && canCompleteUpgrade,
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

export function computePaidPeriod(
  billingCycle: BillingCycleChoice,
  reference = new Date(),
): {
  periodStartUtc: string;
  periodEndUtc: string;
} {
  const start = new Date(
    Date.UTC(reference.getUTCFullYear(), reference.getUTCMonth(), reference.getUTCDate()),
  );
  const end = new Date(start);
  if (billingCycle === "Annual") {
    end.setUTCFullYear(end.getUTCFullYear() + 1);
  } else {
    end.setUTCMonth(end.getUTCMonth() + 1);
  }
  return { periodStartUtc: start.toISOString(), periodEndUtc: end.toISOString() };
}

/** @deprecated Use computePaidPeriod with an explicit billing cycle. */
export function computeMonthlyPaidPeriod(reference = new Date()) {
  return computePaidPeriod("Monthly", reference);
}

export function planPriceLabel(plan: CatalogPlan, billingCycle: BillingCycleChoice = "Monthly"): string {
  const amount = billingCycle === "Annual" ? plan.annualPrice : plan.monthlyPrice;
  if (amount == null || !plan.currencyCode) {
    return "—";
  }
  return `${amount} ${plan.currencyCode}`;
}

export function defaultPaymentAmountForPlan(
  plan: CatalogPlan,
  billingCycle: BillingCycleChoice = "Monthly",
): number | undefined {
  const amount = billingCycle === "Annual" ? plan.annualPrice : plan.monthlyPrice;
  return typeof amount === "number" && amount > 0 ? amount : undefined;
}

export function paymentMatchesUpgradeTarget(
  payment: OrganizationPayment,
  targetPlan: CatalogPlan,
  billingCycle: BillingCycleChoice,
): boolean {
  const requiredAmount = defaultPaymentAmountForPlan(targetPlan, billingCycle);
  if (requiredAmount == null) {
    return false;
  }
  return (
    payment.productCode === targetPlan.productCode &&
    payment.amount === requiredAmount &&
    payment.currencyCode === (targetPlan.currencyCode ?? "PHP")
  );
}

export function parseBillingUpgradeContext(params: URLSearchParams): BillingUpgradeContext | null {
  const upgradeSubscriptionId = params.get("upgradeSubscriptionId")?.trim() ?? "";
  const targetPlanId = params.get("targetPlanId")?.trim() ?? "";
  const billingCycleRaw = params.get("billingCycle")?.trim() ?? "Monthly";
  if (!upgradeSubscriptionId || !targetPlanId) {
    return null;
  }
  const billingCycle: BillingCycleChoice = billingCycleRaw === "Annual" ? "Annual" : "Monthly";
  return { upgradeSubscriptionId, targetPlanId, billingCycle };
}

export function buildBillingUpgradeSearchParams(context: BillingUpgradeContext): URLSearchParams {
  const params = new URLSearchParams();
  params.set("upgradeSubscriptionId", context.upgradeSubscriptionId);
  params.set("targetPlanId", context.targetPlanId);
  params.set("billingCycle", context.billingCycle);
  return params;
}

export function pinoyBusinessPosProductCode(): string {
  return "pinoy-business-pos";
}

export function primaryBillingProductCode(subscriptions: OrganizationSubscription[]): string {
  const pos = subscriptions.find(isPinoyBusinessPosSubscription);
  return pos?.productCode ?? pinoyBusinessPosProductCode();
}
