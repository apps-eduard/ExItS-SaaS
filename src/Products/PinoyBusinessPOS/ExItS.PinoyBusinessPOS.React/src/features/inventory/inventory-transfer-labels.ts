import type { MessageKey } from "@/i18n/messages";
import type { InventoryTransferDiscrepancyReasonCode } from "@/api/pos/pos-inventory-transfer-client";
import { INVENTORY_TRANSFER_DISCREPANCY_REASONS } from "@/api/pos/pos-inventory-transfer-client";

export function inventoryTransferStatusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Draft":
      return "transfer.status.draft";
    case "InTransit":
      return "transfer.status.inTransit";
    case "PartiallyReceived":
      return "transfer.status.partiallyReceived";
    case "Received":
      return "transfer.status.received";
    case "Cancelled":
      return "transfer.status.cancelled";
    default:
      return "transfer.status.draft";
  }
}

export function inventoryTransferStatusTone(
  status: string,
): "info" | "success" | "warning" | "danger" {
  switch (status) {
    case "Received":
      return "success";
    case "InTransit":
    case "PartiallyReceived":
      return "warning";
    case "Cancelled":
      return "danger";
    case "Draft":
    default:
      return "info";
  }
}

export function inventoryTransferDiscrepancyLabelKey(reason: string): MessageKey {
  switch (reason) {
    case "ShortShipment":
      return "transfer.discrepancy.shortShipment";
    case "Damaged":
      return "transfer.discrepancy.damaged";
    case "LostInTransit":
      return "transfer.discrepancy.lostInTransit";
    case "WrongItem":
      return "transfer.discrepancy.wrongItem";
    case "Other":
    default:
      return "transfer.discrepancy.other";
  }
}

export function isInventoryTransferDiscrepancyReason(
  value: string,
): value is InventoryTransferDiscrepancyReasonCode {
  return (INVENTORY_TRANSFER_DISCREPANCY_REASONS as readonly string[]).includes(value);
}

export function formatTransferTimestamp(utc: string | null | undefined): string {
  if (!utc) {
    return "—";
  }
  const parsed = new Date(utc);
  if (Number.isNaN(parsed.getTime())) {
    return utc;
  }
  return parsed.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

export function formatTransferQty(value: number): string {
  if (!Number.isFinite(value)) {
    return "—";
  }
  return Number.isInteger(value) ? String(value) : String(value);
}

export function branchDisplayName(
  name: string | null | undefined,
  fallbackId: string,
): string {
  const trimmed = name?.trim();
  return trimmed || fallbackId;
}

export function parseTransferQuantity(text: string): number | "invalid" | "empty" {
  const trimmed = text.trim();
  if (trimmed === "") {
    return "empty";
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value <= 0) {
    return "invalid";
  }
  return value;
}

export function parseReceivedQuantity(
  text: string,
  sentQty: number,
): number | "invalid" | "empty" {
  const trimmed = text.trim();
  if (trimmed === "") {
    return "empty";
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value < 0 || value > sentQty) {
    return "invalid";
  }
  return value;
}
