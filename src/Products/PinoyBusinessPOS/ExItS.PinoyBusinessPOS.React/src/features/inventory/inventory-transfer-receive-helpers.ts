import type {
  InventoryTransferDto,
  InventoryTransferLineDto,
} from "@/api/pos/pos-inventory-transfer-client";
import type { ReceivedQuantityParse } from "@/features/inventory/inventory-transfer-labels";
import { parseNonNegativeQty } from "@/features/purchasing/receive-math";
import {
  buildRemainingDecisionRows,
  type RemainingDecisionAction,
  type RemainingDecisionRow,
} from "@/features/purchasing/receive-remaining-decision";

export type TransferReceiveLineEdit = {
  lineId: string;
  productId: string;
  name: string;
  sku: string;
  uom: string;
  sentQty: number;
  receivedQty: number;
  outstandingQty: number;
  goodText: string;
  damagedText: string;
  notDeliveredText: string;
  otherText: string;
  otherReasonCode: string;
  otherReasonText: string;
  otherExpanded?: boolean;
  remarksText: string;
  remainingAction: RemainingDecisionAction | null;
};

export function lineClosedQty(line: InventoryTransferLineDto): number {
  return line.closedQty ?? 0;
}

export function lineOutstandingQty(
  line: Pick<InventoryTransferLineDto, "sentQty" | "receivedQty" | "closedQty" | "outstandingQty">,
): number {
  if (line.outstandingQty != null && Number.isFinite(line.outstandingQty)) {
    return line.outstandingQty;
  }
  return line.sentQty - line.receivedQty - lineClosedQty(line as InventoryTransferLineDto);
}

export function transferTotalOutstanding(transfer: InventoryTransferDto): number {
  if (transfer.totalOutstandingQty != null && Number.isFinite(transfer.totalOutstandingQty)) {
    return transfer.totalOutstandingQty;
  }
  return transfer.lines.reduce((sum, line) => sum + lineOutstandingQty(line), 0);
}

export function isTransferTerminalStatus(status: string): boolean {
  return status === "Received" || status === "ClosedWithDiscrepancy" || status === "Cancelled";
}

export function isTransferReceiveActionable(status: string): boolean {
  return status === "InTransit" || status === "PartiallyReceived";
}

export function canDestinationReceiveTransfer(transfer: InventoryTransferDto): boolean {
  return isTransferReceiveActionable(transfer.status) && transferTotalOutstanding(transfer) > 0;
}

export function canDestinationCloseRemainder(transfer: InventoryTransferDto): boolean {
  return transfer.status === "PartiallyReceived" && transferTotalOutstanding(transfer) > 0;
}

/** Receive-now qty for this wave: 0 … outstanding (inclusive). */
export function parseReceiveNowQuantity(text: string, outstandingQty: number): ReceivedQuantityParse {
  const trimmed = text.trim();
  if (trimmed === "") {
    return "empty";
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value < 0) {
    return "invalid";
  }
  if (value > outstandingQty) {
    return "exceeds";
  }
  return value;
}

export function isReceiveNowLineValid(receivedText: string, outstandingQty: number): boolean {
  const parsed = parseReceiveNowQuantity(receivedText, outstandingQty);
  return typeof parsed === "number";
}

/** At least one line must have receive-now &gt; 0; every line must be valid. */
export function isReceiveSubmissionReady(
  lines: ReadonlyArray<InventoryTransferLineDto>,
  receiveNowByLine: Record<string, string>,
): boolean {
  let anyPositive = false;
  for (const line of lines) {
    const outstanding = lineOutstandingQty(line);
    const parsed = parseReceiveNowQuantity(receiveNowByLine[line.lineId] ?? "", outstanding);
    if (parsed === "empty" || parsed === "invalid" || parsed === "exceeds") {
      return false;
    }
    if (typeof parsed === "number" && parsed > 0) {
      anyPositive = true;
    }
  }
  return anyPositive;
}

export function defaultReceiveNowByLine(transfer: InventoryTransferDto): Record<string, string> {
  const next: Record<string, string> = {};
  for (const line of transfer.lines) {
    const outstanding = lineOutstandingQty(line);
    next[line.lineId] = outstanding > 0 ? String(outstanding) : "0";
  }
  return next;
}

export function buildTransferReceiveLineEdits(transfer: InventoryTransferDto): TransferReceiveLineEdit[] {
  return transfer.lines.map((line) => {
    const outstanding = lineOutstandingQty(line);
    return {
      lineId: line.lineId,
      productId: line.productId,
      name: line.productName,
      sku: line.sku?.trim() ?? "",
      uom: line.unitOfMeasure,
      sentQty: line.sentQty,
      receivedQty: line.receivedQty,
      outstandingQty: outstanding,
      goodText: outstanding > 0 ? String(outstanding) : "0",
      damagedText: "0",
      notDeliveredText: "0",
      otherText: "0",
      otherReasonCode: "",
      otherReasonText: "",
      remarksText: "",
      remainingAction: null,
    };
  });
}

/** Remaining decisions match PO: any shortfall (outstanding − good) needs replace later / cancel. */
export function buildTransferRemainingDecisionRows(
  lines: readonly TransferReceiveLineEdit[],
  labels: {
    damaged: string;
    notDelivered: string;
    otherReasons?: Record<string, string>;
    otherFallback?: string;
  },
): RemainingDecisionRow[] {
  return buildRemainingDecisionRows(
    lines.map((line) => ({
      productId: line.productId,
      name: line.name,
      sku: line.sku,
      uom: line.uom,
      outstandingQty: line.outstandingQty,
      goodText: line.goodText,
      damagedText: line.damagedText,
      notDeliveredText: line.notDeliveredText,
      otherText: line.otherText,
      otherReasonCode: line.otherReasonCode,
      remarksText: line.remarksText,
      remainingAction: line.remainingAction,
    })),
    labels,
  );
}
