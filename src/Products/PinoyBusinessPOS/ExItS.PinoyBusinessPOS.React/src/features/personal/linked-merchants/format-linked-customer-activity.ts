import type { LinkedCustomerActivityItem } from "@/api/pos/pos-linked-customers-client";
import { formatMoneyAmountInput } from "@/lib/money-input";

const customerOrderNumberPattern = /^SO-\d+$/i;

function formatSignedMoney(amount: number, sign: "+" | "−" | "" = ""): string {
  return `${sign}${formatMoneyAmountInput(Math.abs(amount))}`;
}

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
      return `${label} · ${reference} · ${formatSignedMoney(item.chargeAmount, "+")}`;
    }
    return `${label} · ${reference}`;
  }
  if (item.chargeAmount != null) {
    return `${reference} · ${formatSignedMoney(item.chargeAmount, "+")}`;
  }
  if (item.paymentAmount != null) {
    return `${reference} · ${formatSignedMoney(item.paymentAmount, "−")}`;
  }
  if (item.adjustmentAmount != null) {
    const sign = item.adjustmentAmount < 0 ? "−" : item.adjustmentAmount > 0 ? "+" : "";
    return `${reference} · ${formatSignedMoney(item.adjustmentAmount, sign)}`;
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
    return { text: formatSignedMoney(item.chargeAmount, "+"), kind: "charge" };
  }
  if (item.paymentAmount != null) {
    return { text: formatSignedMoney(item.paymentAmount, "−"), kind: "payment" };
  }
  if (item.adjustmentAmount != null) {
    const prefix = item.adjustmentAmount >= 0 ? "+" : "−";
    return {
      text: formatSignedMoney(item.adjustmentAmount, prefix),
      kind: "neutral",
    };
  }
  return null;
}
