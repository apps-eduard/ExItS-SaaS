import type {
  InventoryTransferDamagedCustodyDecisionCode,
  InventoryTransferDto,
  InventoryTransferLineDto,
} from "@/api/pos/pos-inventory-transfer-client";
import type { ReceivedQuantityParse } from "@/features/inventory/inventory-transfer-labels";
import { parseNonNegativeQty } from "@/features/purchasing/receive-math";
import type {
  TransferDamagedOtherFollowUp,
  TransferMissingFollowUp,
} from "@/features/inventory/transfer-receive-follow-up";

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
  actualReceivedProductId: string | null;
  actualReceivedProductName: string | null;
  remarksText: string;
  missingFollowUp: TransferMissingFollowUp | null;
  damagedFollowUp: TransferDamagedOtherFollowUp | null;
  otherFollowUp: TransferDamagedOtherFollowUp | null;
  damagedCustodyDecision: InventoryTransferDamagedCustodyDecisionCode | null;
  otherCustodyDecision: InventoryTransferDamagedCustodyDecisionCode | null;
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

/** Damaged qty received for a transfer line (from immutable receipt classification). */
export function lineDamagedQty(
  transfer: Pick<InventoryTransferDto, "receipts">,
  lineId: string,
): number {
  let total = 0;
  for (const receipt of transfer.receipts ?? []) {
    for (const rl of receipt.lines ?? []) {
      if (rl.lineId === lineId) {
        total += rl.quantityDamaged ?? 0;
      }
    }
  }
  return total;
}

/**
 * Remaining fulfillment obligation for this line on the transfer family / stock request:
 * MAX(0, Sent − Good − InTransit − Waived). Damaged itself never reduces this.
 */
export function lineNeedsFulfillmentQty(
  line: Pick<
    InventoryTransferLineDto,
    "sentQty" | "receivedQty" | "closedQty" | "outstandingQty" | "waivedQty"
  >,
): number {
  const good = line.receivedQty;
  const inTransit = lineOutstandingQty(line);
  const waived = line.waivedQty ?? 0;
  return Math.max(0, line.sentQty - good - inTransit - waived);
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

export function buildTransferReceiveLineEdits(
  transfer: InventoryTransferDto,
  defaults?: {
    missingFollowUp?: TransferMissingFollowUp | null;
    damagedFollowUp?: TransferDamagedOtherFollowUp | null;
    otherFollowUp?: TransferDamagedOtherFollowUp | null;
  },
): TransferReceiveLineEdit[] {
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
      actualReceivedProductId: null,
      actualReceivedProductName: null,
      remarksText: "",
      missingFollowUp: defaults?.missingFollowUp ?? null,
      damagedFollowUp: defaults?.damagedFollowUp ?? null,
      otherFollowUp: defaults?.otherFollowUp ?? null,
      damagedCustodyDecision: "KeepAtDestination",
      otherCustodyDecision: null,
    };
  });
}
