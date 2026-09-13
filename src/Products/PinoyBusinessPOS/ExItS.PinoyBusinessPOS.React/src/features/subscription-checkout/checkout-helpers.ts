import type { SubscriptionPaymentTransactionDto } from "@/api/platform/subscription-payment-client";

export function formatPaymentMoney(amount: number, currency: string): string {
  return `${amount.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 })} ${currency}`;
}

export function isPaidStatus(status: string): boolean {
  return status.localeCompare("Paid", undefined, { sensitivity: "accent" }) === 0;
}

export function isFailedStatus(status: string): boolean {
  return status.localeCompare("Failed", undefined, { sensitivity: "accent" }) === 0;
}

export function isProcessingStatus(status: string): boolean {
  return status.localeCompare("Processing", undefined, { sensitivity: "accent" }) === 0;
}

export function isTestEnvironment(environment: string): boolean {
  return environment.localeCompare("Test", undefined, { sensitivity: "accent" }) === 0;
}

export function paymentResultPath(paymentId: string): string {
  return `/subscription-checkout/${paymentId}/result`;
}

export function paymentChannelPath(
  paymentId: string,
  channel: "gcash" | "maya" | "card",
): string {
  return `/subscription-checkout/${paymentId}/${channel}`;
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
    { id: "created", label: "Created", at: payment.createdAtUtc },
  ];
  if (payment.processingAtUtc) {
    entries.push({ id: "processing", label: "Processing", at: payment.processingAtUtc });
  }
  if (payment.paidAtUtc) {
    entries.push({ id: "paid", label: "Paid", at: payment.paidAtUtc });
  }
  if (payment.failedAtUtc) {
    entries.push({ id: "failed", label: "Failed", at: payment.failedAtUtc });
  }
  if (payment.cancelledAtUtc) {
    entries.push({ id: "cancelled", label: "Cancelled", at: payment.cancelledAtUtc });
  }
  if (payment.expiredAtUtc) {
    entries.push({ id: "expired", label: "Expired", at: payment.expiredAtUtc });
  }
  return entries;
}
