import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import type {
  InventoryTransferDto,
  InventoryTransferExceptionCustodyDto,
} from "@/api/pos/pos-inventory-transfer-client";
import type { MessageKey } from "@/i18n/messages";
import {
  buildReceivingDecisionView,
  pickReceivingDecisionNote,
  type ReceivingDecisionView,
} from "@/features/inventory/inventory-transfer-summary-presentation";
import { isTransferExceptionMovement } from "@/features/inventory/inventory-movement-transfer-ref";
import { requiresActualProduct } from "@/features/inventory/transfer-exception-custody-policy";
import { inventoryMovementTypeLabelKey } from "@/features/purchasing/purchase-cost-display";

function eqId(a: string | null | undefined, b: string | null | undefined): boolean {
  return Boolean(a && b && a.toLowerCase() === b.toLowerCase());
}

/**
 * Resolve the exception custody that owns this movement.
 * Prefer sourceId (receiptLineId or custodyId), then product role, never blind [0]
 * when multiple exception custodies exist.
 */
export function matchExceptionCustodyForMovement(
  transfer: Pick<InventoryTransferDto, "transferId" | "exceptionCustodies">,
  movement: Pick<PosStockMovementDto, "movementType" | "sourceId" | "productId">,
): InventoryTransferExceptionCustodyDto | null {
  if (!isTransferExceptionMovement(movement.movementType)) {
    return null;
  }

  const custodies = (transfer.exceptionCustodies ?? []).filter((c) =>
    eqId(c.transferId, transfer.transferId),
  );
  if (custodies.length === 0) {
    return null;
  }

  const sourceId = movement.sourceId?.trim() || null;
  if (sourceId) {
    const byReceiptLine = custodies.find((c) => eqId(c.receiptLineId, sourceId));
    if (byReceiptLine) {
      return byReceiptLine;
    }
    const byCustodyId = custodies.find((c) => eqId(c.custodyId, sourceId));
    if (byCustodyId) {
      return byCustodyId;
    }
  }

  const productId = movement.productId?.trim() || null;
  if (productId) {
    if (movement.movementType === "TransferExceptionExpectedRestore") {
      const byExpected = custodies.filter((c) => eqId(c.expectedProductId, productId));
      if (byExpected.length === 1) {
        return byExpected[0]!;
      }
    } else {
      const byActual = custodies.filter((c) => eqId(c.actualProductId, productId));
      if (byActual.length === 1) {
        return byActual[0]!;
      }
    }
  }

  return custodies.length === 1 ? custodies[0]! : null;
}

/** Reason-aware friendly label for exception movements (never raw enums). */
export function exceptionMovementTypeLabelKey(
  movementType: string,
  reasonCode: string | null | undefined,
): MessageKey {
  const reason = reasonCode?.trim() ?? "";
  const wrongVariant = reason === "WrongVariant";

  switch (movementType) {
    case "TransferExceptionActualOut":
      return wrongVariant
        ? "inventory.movementType.transferExceptionActualOutWrongVariant"
        : "inventory.movementType.transferExceptionActualOutWrongItem";
    case "TransferExceptionHold":
      return wrongVariant
        ? "inventory.movementType.transferExceptionHoldWrongVariant"
        : "inventory.movementType.transferExceptionHoldWrongItem";
    case "TransferExceptionExpectedRestore":
      return "inventory.movementType.transferExceptionExpectedRestore";
    case "TransferExceptionReturnOut":
      return wrongVariant
        ? "inventory.movementType.transferExceptionReturnOutWrongVariant"
        : "inventory.movementType.transferExceptionReturnOutWrongItem";
    case "TransferExceptionReturnIn":
    case "TransferExceptionReturnRestock":
      return wrongVariant
        ? "inventory.movementType.transferExceptionReturnRestockWrongVariant"
        : "inventory.movementType.transferExceptionReturnRestock";
    default:
      return inventoryMovementTypeLabelKey(movementType);
  }
}

export type ExceptionMovementRoute = {
  fromBranchName: string | null;
  toBranchName: string | null;
  /** True when physical flow is destination → source (return). */
  isReturnRoute: boolean;
};

/**
 * Physical route for this exception movement (not a generic original ship route).
 * ActualOut / ExpectedRestore / Hold: source → destination.
 * ReturnOut / ReturnRestock / ReturnIn: destination → source.
 */
export function resolveExceptionMovementRoute(
  movementType: string,
  transfer: Pick<
    InventoryTransferDto,
    "sourceBranchName" | "destinationBranchName" | "sourceBranchId" | "destinationBranchId"
  >,
): ExceptionMovementRoute | null {
  if (!isTransferExceptionMovement(movementType)) {
    return null;
  }

  const sourceName = transfer.sourceBranchName?.trim() || null;
  const destName = transfer.destinationBranchName?.trim() || null;
  const isReturn =
    movementType === "TransferExceptionReturnOut" ||
    movementType === "TransferExceptionReturnIn" ||
    movementType === "TransferExceptionReturnRestock" ||
    movementType === "TransferExceptionRecovery" ||
    movementType === "TransferExceptionWriteOff";

  if (isReturn) {
    return {
      fromBranchName: destName,
      toBranchName: sourceName,
      isReturnRoute: true,
    };
  }

  return {
    fromBranchName: sourceName,
    toBranchName: destName,
    isReturnRoute: false,
  };
}

/**
 * Receiving-decision rows scoped to one exception custody (multi-exception safe).
 */
export function buildExceptionCustodyReceivingDecisionView(
  transfer: Pick<
    InventoryTransferDto,
    "transferId" | "receipts" | "damageCustodies" | "exceptionCustodies" | "lines"
  >,
  custody: InventoryTransferExceptionCustodyDto,
): ReceivingDecisionView {
  let otherReasonNote: string | null = null;
  let receiptLineNote: string | null = null;
  for (const receipt of transfer.receipts ?? []) {
    for (const line of receipt.lines ?? []) {
      if (!eqId(line.receiptLineId, custody.receiptLineId)) {
        continue;
      }
      if ((line.quantityOther ?? 0) > 1e-9) {
        otherReasonNote = line.otherReasonNote?.trim() || null;
        receiptLineNote = line.note?.trim() || null;
      }
    }
  }

  const lineDiscrepancyNote =
    transfer.lines
      ?.find((l) => eqId(l.productId, custody.expectedProductId))
      ?.discrepancyNote?.trim() || null;

  const note = pickReceivingDecisionNote(
    otherReasonNote,
    receiptLineNote,
    lineDiscrepancyNote,
  );

  const base = buildReceivingDecisionView({
    ...transfer,
    exceptionCustodies: [custody],
    damageCustodies: [],
  });

  return {
    ...base,
    damagedQty: 0,
    damagedFollowUp: null,
    custodyDecision: null,
    custodyStatus: null,
    missingQty: 0,
    missingDisposition: null,
    otherQty: custody.quantity,
    otherReasonCode: custody.reasonCode,
    otherReasonNote: note,
    otherFollowUp: custody.followUpIntent,
    otherCustodyDecision: custody.decision,
    otherCustodyStatus: custody.status,
    expectedProductId: custody.expectedProductId,
    expectedProductName: custody.expectedProductName?.trim() || null,
    actualReceivedProductId: custody.actualProductId,
    actualReceivedProductName: custody.actualProductName?.trim() || null,
    showExpectedAndActualItems: requiresActualProduct(custody.reasonCode),
    confirmedNonSellableQty: custody.confirmedNonSellableQty,
    hasDiscrepancy: custody.quantity > 1e-9,
  };
}
