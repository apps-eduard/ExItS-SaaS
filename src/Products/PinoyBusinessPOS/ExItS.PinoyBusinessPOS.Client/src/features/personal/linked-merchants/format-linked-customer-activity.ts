import type { LinkedCustomerActivityItem } from "@/api/pos/pos-linked-customers-client";

const customerOrderNumberPattern = /^SO-\d+$/i;

export function formatLinkedCustomerActivityLabel(item: LinkedCustomerActivityItem): string | null {
  if (
    item.type === "UtangCharge"
    && customerOrderNumberPattern.test(item.referenceNumber.trim())
  ) {
    return "Online purchase";
  }
  return null;
}

export function formatLinkedCustomerActivityReference(item: LinkedCustomerActivityItem): string {
  if (
    item.type === "UtangCharge"
    && customerOrderNumberPattern.test(item.referenceNumber.trim())
  ) {
    return `Order ${item.referenceNumber.trim().toUpperCase()}`;
  }
  return item.referenceNumber;
}

export function formatLinkedCustomerActivityTitle(item: LinkedCustomerActivityItem): string {
  const label = formatLinkedCustomerActivityLabel(item);
  const reference = formatLinkedCustomerActivityReference(item);
  if (label) {
    if (item.chargeAmount != null) {
      return `${label} · ${reference} · +${item.chargeAmount.toFixed(2)}`;
    }
    return `${label} · ${reference}`;
  }
  if (item.chargeAmount != null) {
    return `${reference} · +${item.chargeAmount.toFixed(2)}`;
  }
  if (item.paymentAmount != null) {
    return `${reference} · −${item.paymentAmount.toFixed(2)}`;
  }
  if (item.adjustmentAmount != null) {
    return `${reference} · ${item.adjustmentAmount.toFixed(2)}`;
  }
  return reference;
}

export function formatLinkedCustomerActivityMeta(item: LinkedCustomerActivityItem): string {
  const typeLabel =
    item.type === "UtangCharge" && formatLinkedCustomerActivityLabel(item)
      ? "Open credit"
      : item.type;
  return `${new Date(item.occurredAtUtc).toLocaleString()} · ${typeLabel}`;
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
