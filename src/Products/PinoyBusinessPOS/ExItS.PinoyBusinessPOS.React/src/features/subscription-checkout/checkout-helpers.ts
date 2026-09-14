import type { SubscriptionPaymentTransactionDto } from "@/api/platform/subscription-payment-client";

export function formatPaymentMoney(amount: number, currency: string): string {
  return `${amount.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 })} ${currency}`;
}

export function normalizePaymentStatus(status: string): string {
  return status.trim().toLowerCase();
}

export function isPaidStatus(status: string): boolean {
  return normalizePaymentStatus(status) === "paid";
}

export function isFailedStatus(status: string): boolean {
  return normalizePaymentStatus(status) === "failed";
}

export function isProcessingStatus(status: string): boolean {
  return normalizePaymentStatus(status) === "processing";
}

export function isPendingStatus(status: string): boolean {
  return normalizePaymentStatus(status) === "pending";
}

export function isCancelledStatus(status: string): boolean {
  return normalizePaymentStatus(status) === "cancelled";
}

export function isExpiredStatus(status: string): boolean {
  return normalizePaymentStatus(status) === "expired";
}

export function isTestEnvironment(environment: string): boolean {
  return environment.localeCompare("Test", undefined, { sensitivity: "accent" }) === 0;
}

export function paymentResultPath(paymentId: string): string {
  return `/subscription-checkout/${paymentId}`;
}

export function paymentChannelPath(
  paymentId: string,
  channel: "gcash" | "maya" | "card",
): string {
  return `/subscription-checkout/${paymentId}/${channel}`;
}

export function checkoutTitleKey(payment: SubscriptionPaymentTransactionDto): string {
  const status = normalizePaymentStatus(payment.status);
  if (status === "paid") return "subscriptionCheckout.state.paidTitle";
  if (status === "processing") return "subscriptionCheckout.state.processingTitle";
  if (status === "failed") return "subscriptionCheckout.state.failedTitle";
  if (status === "cancelled") return "subscriptionCheckout.state.cancelledTitle";
  if (status === "expired") return "subscriptionCheckout.state.expiredTitle";
  if (payment.channel) return "subscriptionCheckout.state.completeTitle";
  return "subscriptionCheckout.state.chooseMethodTitle";
}

export function summaryCardTitleKey(payment: SubscriptionPaymentTransactionDto): string {
  return isPaidStatus(payment.status)
    ? "subscriptionCheckout.receiptTitle"
    : "subscriptionCheckout.paymentDetailsTitle";
}

export function buildTimelineEntries(payment: SubscriptionPaymentTransactionDto) {
  if (payment.activities.length > 0) {
    return payment.activities.map((a) => ({
      id: `${a.eventType}-${a.occurredAtUtc}`,
      label: a.message || a.eventType,
      at: a.occurredAtUtc,
    }));
  }

  const entries: { id: string; label: string; at: string }[] = [
    { id: "created", label: "Payment created", at: payment.createdAtUtc },
  ];
  if (payment.processingAtUtc) {
    entries.push({ id: "processing", label: "Processing started", at: payment.processingAtUtc });
  }
  if (payment.paidAtUtc) {
    entries.push({ id: "paid", label: "Payment confirmed", at: payment.paidAtUtc });
  }
  if (payment.failedAtUtc) {
    entries.push({ id: "failed", label: "Payment failed", at: payment.failedAtUtc });
  }
  if (payment.cancelledAtUtc) {
    entries.push({ id: "cancelled", label: "Payment cancelled", at: payment.cancelledAtUtc });
  }
  if (payment.expiredAtUtc) {
    entries.push({ id: "expired", label: "Payment expired", at: payment.expiredAtUtc });
  }
  return entries;
}

export function startBusinessAfterPaidPath(payment: SubscriptionPaymentTransactionDto): string {
  const params = new URLSearchParams({
    planKey: payment.planKey,
    billing: payment.billingCycle,
    paymentId: payment.id,
    paid: "1",
    trial: "0",
    payNow: "0",
  });
  return `/personal/start-business?${params.toString()}`;
}
