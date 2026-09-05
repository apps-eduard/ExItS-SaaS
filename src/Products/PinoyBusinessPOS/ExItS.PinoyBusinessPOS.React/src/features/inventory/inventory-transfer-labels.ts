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

export type ReceivedQuantityParse = number | "invalid" | "empty" | "exceeds";

export function parseReceivedQuantity(text: string, sentQty: number): ReceivedQuantityParse {
  const trimmed = text.trim();
  if (trimmed === "") {
    return "empty";
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value < 0) {
    return "invalid";
  }
  if (value > sentQty) {
    return "exceeds";
  }
  return value;
}

/** Receive line is ready when qty is 0–sent and short/missing lines have a discrepancy reason. */
export function isReceiveLineReady(
  receivedText: string,
  sentQty: number,
  discrepancyReason: string | undefined | null,
): boolean {
  const parsed = parseReceivedQuantity(receivedText, sentQty);
  if (parsed === "empty" || parsed === "invalid" || parsed === "exceeds") {
    return false;
  }
  if (parsed < sentQty && !discrepancyReason?.trim()) {
    return false;
  }
  return true;
}

/** Most recent executor for list cards, based on transfer status. */
export function inventoryTransferExecutor(item: {
  status: string;
  createdBy: string;
  dispatchedBy?: string | null;
  receivedBy?: string | null;
  cancelledBy?: string | null;
}): { actorId: string; labelKey: MessageKey } {
  switch (item.status) {
    case "Cancelled":
      return {
        actorId: item.cancelledBy || item.createdBy,
        labelKey: "transfer.byCancelled",
      };
    case "Received":
    case "PartiallyReceived":
      return {
        actorId: item.receivedBy || item.dispatchedBy || item.createdBy,
        labelKey: "transfer.byReceived",
      };
    case "InTransit":
      return {
        actorId: item.dispatchedBy || item.createdBy,
        labelKey: "transfer.byDispatched",
      };
    case "Draft":
    default:
      return {
        actorId: item.createdBy,
        labelKey: "transfer.byCreated",
      };
  }
}
