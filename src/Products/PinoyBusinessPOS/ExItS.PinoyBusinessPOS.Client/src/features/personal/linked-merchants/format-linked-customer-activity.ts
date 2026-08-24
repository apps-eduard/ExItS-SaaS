import type { LinkedCustomerActivityItem } from "@/api/pos/pos-linked-customers-client";

export function formatLinkedCustomerActivityTitle(item: LinkedCustomerActivityItem): string {
  if (item.chargeAmount != null) {
    return `${item.referenceNumber} · +${item.chargeAmount.toFixed(2)}`;
  }
  if (item.paymentAmount != null) {
    return `${item.referenceNumber} · −${item.paymentAmount.toFixed(2)}`;
  }
  if (item.adjustmentAmount != null) {
    return `${item.referenceNumber} · ${item.adjustmentAmount.toFixed(2)}`;
  }
  return item.referenceNumber;
}

export function formatLinkedCustomerActivityMeta(item: LinkedCustomerActivityItem): string {
  return `${new Date(item.occurredAtUtc).toLocaleString()} · ${item.type}`;
}

export type LinkedCustomerActivityAmountKind = "charge" | "payment" | "neutral";

export function formatLinkedCustomerActivityAmount(
  item: LinkedCustomerActivityItem,
): { text: string; kind: LinkedCustomerActivityAmountKind } | null {
  if (item.chargeAmount != null) {
    return { text: `+${item.chargeAmount.toFixed(2)}`, kind: "charge" };
  }
  if (item.paymentAmount != null) {
    return { text: `−${item.paymentAmount.toFixed(2)}`, kind: "payment" };
  }
  if (item.adjustmentAmount != null) {
    const prefix = item.adjustmentAmount >= 0 ? "+" : "−";
    return {
      text: `${prefix}${Math.abs(item.adjustmentAmount).toFixed(2)}`,
      kind: "neutral",
    };
  }
  return null;
}
