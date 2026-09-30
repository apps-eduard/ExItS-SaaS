import { describe, expect, it } from "vitest";
import type { InventoryTransferDto } from "@/api/pos/pos-inventory-transfer-client";
import {
  buildExceptionCustodyReceivingDecisionView,
  exceptionMovementTypeLabelKey,
  matchExceptionCustodyForMovement,
  resolveExceptionMovementRoute,
} from "@/features/inventory/inventory-movement-exception-display";
import {
  isInventoryTransferMovement,
  isTransferExceptionMovement,
} from "@/features/inventory/inventory-movement-transfer-ref";

const transferId = "11111111-1111-1111-1111-111111111111";
const appleId = "33333333-3333-3333-3333-333333333333";
const bananaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const mangoId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const appleReceiptLine = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
const orangeReceiptLine = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef";
const bananaCustodyId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const mangoCustodyId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

function transferWithTwoExceptions(): InventoryTransferDto {
  return {
    transferId,
    organizationId: "88888888-8888-8888-8888-888888888888",
    transferNumber: "TR-260924-001",
    sourceBranchId: "77777777-7777-7777-7777-777777777777",
    sourceBranchName: "Main Branch",
    destinationBranchId: "99999999-9999-9999-9999-999999999999",
    destinationBranchName: "Branch 2",
    status: "ClosedWithDiscrepancy",
    notes: null,
    createdBy: "actor",
    createdAtUtc: "2026-09-24T10:00:00Z",
    updatedAtUtc: "2026-09-24T11:00:00Z",
    totalSentQty: 20,
    totalReceivedQty: 12,
    totalDifferenceQty: 8,
    lines: [],
    receipts: [
      {
        receiptId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
        sequence: 1,
        receivedAtUtc: "2026-09-24T11:00:00Z",
        receivedBy: "actor",
        lines: [
          {
            receiptLineId: appleReceiptLine,
            lineId: "22222222-2222-2222-2222-222222222222",
            productId: appleId,
            quantityReceived: 5,
            quantityOther: 5,
            otherReasonCode: "WrongItem",
            otherReasonNote: "Banana sent instead of Apple",
            otherFollowUp: "RequestReplacement",
            actualReceivedProductId: bananaId,
            otherCustodyDecision: "ReturnToSource",
          },
        ],
      },
    ],
    exceptionCustodies: [
      {
        custodyId: bananaCustodyId,
        transferId,
        rootTransferId: transferId,
        receiptLineId: appleReceiptLine,
        expectedProductId: appleId,
        actualProductId: bananaId,
        expectedProductName: "Apple",
        actualProductName: "Banana Lakatan",
        quantity: 5,
        reasonCode: "WrongItem",
        decision: "ReturnToSource",
        followUpIntent: "RequestReplacement",
        status: "AwaitingReturn",
        heldBranchId: "99999999-9999-9999-9999-999999999999",
        recoveredSellableQty: 0,
        confirmedNonSellableQty: 0,
        replacementDemandQty: 5,
        createdAtUtc: "2026-09-24T11:00:00Z",
        updatedAtUtc: "2026-09-24T11:00:00Z",
      },
      {
        custodyId: mangoCustodyId,
        transferId,
        rootTransferId: transferId,
        receiptLineId: orangeReceiptLine,
        expectedProductId: "44444444-4444-4444-4444-444444444444",
        actualProductId: mangoId,
        expectedProductName: "Orange",
        actualProductName: "Mango",
        quantity: 3,
        reasonCode: "WrongItem",
        decision: "ReturnToSource",
        followUpIntent: "RequestReplacement",
        status: "AwaitingReturn",
        heldBranchId: "99999999-9999-9999-9999-999999999999",
        recoveredSellableQty: 0,
        confirmedNonSellableQty: 0,
        replacementDemandQty: 3,
        createdAtUtc: "2026-09-24T11:00:00Z",
        updatedAtUtc: "2026-09-24T11:00:00Z",
      },
    ],
  };
}

describe("inventory-movement-exception-display", () => {
  it("recognizes all exception movement types as inventory transfer movements", () => {
    for (const movementType of [
      "TransferExceptionHold",
      "TransferExceptionExpectedRestore",
      "TransferExceptionActualOut",
      "TransferExceptionReturnOut",
      "TransferExceptionReturnIn",
      "TransferExceptionReturnRestock",
    ]) {
      expect(isTransferExceptionMovement(movementType)).toBe(true);
      expect(
        isInventoryTransferMovement({
          movementId: "m",
          productId: "p",
          inventoryAccountId: "a",
          movementType,
          quantityEffect: 1,
          reason: "x",
          sourceType: "InventoryTransfer",
          recordedAtUtc: "2026-09-24T10:00:00Z",
          recordedBy: "a",
        }),
      ).toBe(true);
    }
  });

  it("matches Banana custody by receiptLine sourceId, not first custody", () => {
    const matched = matchExceptionCustodyForMovement(transferWithTwoExceptions(), {
      movementType: "TransferExceptionActualOut",
      sourceId: appleReceiptLine,
      productId: bananaId,
    });
    expect(matched?.actualProductName).toBe("Banana Lakatan");
    expect(matched?.expectedProductName).toBe("Apple");
    expect(matched?.custodyId).toBe(bananaCustodyId);
  });

  it("matches ReturnOut by custodyId sourceId", () => {
    const matched = matchExceptionCustodyForMovement(transferWithTwoExceptions(), {
      movementType: "TransferExceptionReturnOut",
      sourceId: mangoCustodyId,
      productId: mangoId,
    });
    expect(matched?.actualProductName).toBe("Mango");
  });

  it("uses reason-aware labels for WrongItem vs WrongVariant", () => {
    expect(exceptionMovementTypeLabelKey("TransferExceptionActualOut", "WrongItem")).toBe(
      "inventory.movementType.transferExceptionActualOutWrongItem",
    );
    expect(exceptionMovementTypeLabelKey("TransferExceptionActualOut", "WrongVariant")).toBe(
      "inventory.movementType.transferExceptionActualOutWrongVariant",
    );
  });

  it("resolves ActualOut route source→dest and ReturnOut dest→source", () => {
    const t = transferWithTwoExceptions();
    expect(resolveExceptionMovementRoute("TransferExceptionActualOut", t)).toEqual({
      fromBranchName: "Main Branch",
      toBranchName: "Branch 2",
      isReturnRoute: false,
    });
    expect(resolveExceptionMovementRoute("TransferExceptionReturnOut", t)).toEqual({
      fromBranchName: "Branch 2",
      toBranchName: "Main Branch",
      isReturnRoute: true,
    });
  });

  it("scopes receiving decision note and products to matched custody", () => {
    const custody = transferWithTwoExceptions().exceptionCustodies![0]!;
    const view = buildExceptionCustodyReceivingDecisionView(
      transferWithTwoExceptions(),
      custody,
    );
    expect(view.expectedProductName).toBe("Apple");
    expect(view.actualReceivedProductName).toBe("Banana Lakatan");
    expect(view.otherReasonNote).toBe("Banana sent instead of Apple");
    expect(view.otherQty).toBe(5);
    expect(view.showExpectedAndActualItems).toBe(true);
  });
});
