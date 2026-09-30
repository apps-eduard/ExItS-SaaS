import { describe, expect, it } from "vitest";
import type { InventoryTransferDto } from "@/api/pos/pos-inventory-transfer-client";
import {
  buildReceivingDecisionView,
  computeThisShipmentTotals,
  familyFulfillmentTargetQty,
  formatExceptionCustodyReturnStatusLabel,
  formatReturnToSourceCustodyLabel,
  lineFollowUpDisplay,
  lineOtherExceptionSecondaryText,
  pickReceivingDecisionNote,
  resolveExceptionCustodyExpectedItemLabel,
  resolveExceptionCustodyItemLabel,
  transferCustodyDecisionLabelKey,
  transferCustodyStatusLabelKey,
  transferDiscrepancyFollowUpLabelKey,
  transferMissingDispositionLabelKey,
} from "@/features/inventory/inventory-transfer-summary-presentation";

const transferId = "11111111-1111-1111-1111-111111111111";
const lineId = "22222222-2222-2222-2222-222222222222";
const productId = "33333333-3333-3333-3333-333333333333";
const receiptId = "44444444-4444-4444-4444-444444444444";
const receiptLineId = "55555555-5555-5555-5555-555555555555";
const custodyId = "66666666-6666-6666-6666-666666666666";
const branchId = "77777777-7777-7777-7777-777777777777";
const actualBananaId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const destinationBranchId = "99999999-9999-9999-9999-999999999999";

function baseTransfer(
  overrides: Partial<InventoryTransferDto> = {},
): InventoryTransferDto {
  return {
    transferId,
    organizationId: "88888888-8888-8888-8888-888888888888",
    transferNumber: "TR-260922-001",
    sourceBranchId: branchId,
    sourceBranchName: "Main Branch",
    destinationBranchId,
    destinationBranchName: "Branch 2",
    status: "ClosedWithDiscrepancy",
    notes: null,
    createdBy: branchId,
    createdAtUtc: "2026-09-22T10:00:00Z",
    updatedAtUtc: "2026-09-22T12:00:00Z",
    totalSentQty: 10,
    totalReceivedQty: 5,
    totalDifferenceQty: 5,
    lines: [
      {
        lineId,
        productId,
        productName: "Apple",
        unitOfMeasure: "kg",
        lineNumber: 1,
        sentQty: 10,
        receivedQty: 5,
        differenceQty: 5,
        lineStatus: "ClosedWithDiscrepancy",
      },
    ],
    receipts: [
      {
        receiptId,
        sequence: 1,
        receivedAtUtc: "2026-09-22T12:00:00Z",
        receivedBy: branchId,
        lines: [
          {
            receiptLineId,
            lineId,
            productId,
            quantityReceived: 5,
            quantityDamaged: 5,
            quantityMissing: 0,
            quantityOther: 0,
            damagedFollowUp: "RequestReplacement",
            missingDisposition: null,
            otherFollowUp: null,
          },
        ],
      },
    ],
    damageCustodies: [
      {
        custodyId,
        transferId,
        rootTransferId: transferId,
        receiptLineId,
        productId,
        quantity: 5,
        decision: "KeepAtDestination",
        followUpIntent: "RequestReplacement",
        status: "HeldAtDestination",
        heldBranchId: "99999999-9999-9999-9999-999999999999",
        recoveredSellableQty: 0,
        confirmedDamagedQty: 0,
        waivedQty: 0,
        destinationRecoveredSellableQty: 0,
        replacementDemandQty: 5,
        createdAtUtc: "2026-09-22T12:00:00Z",
        updatedAtUtc: "2026-09-22T12:00:00Z",
      },
    ],
    satisfiedAtDestinationQty: 5,
    openInTransitQty: 0,
    remainingToDispatchQty: 5,
    waivedQty: 0,
    ...overrides,
  } as InventoryTransferDto;
}

describe("computeThisShipmentTotals", () => {
  it("keeps original shipment 10/5/5 after family good total rises", () => {
    const transfer = baseTransfer({
      satisfiedAtDestinationQty: 10,
      remainingToDispatchQty: 0,
    });
    expect(computeThisShipmentTotals(transfer)).toEqual({
      sent: 10,
      goodReceived: 5,
      damaged: 5,
      missing: 0,
      other: 0,
    });
  });

  it("shows replacement shipment from its own receipts only", () => {
    const transfer = baseTransfer({
      transferId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      transferNumber: "TR-260922-001-R1",
      totalSentQty: 5,
      totalReceivedQty: 5,
      rootTransferId: transferId,
      receipts: [
        {
          receiptId,
          sequence: 1,
          receivedAtUtc: "2026-09-22T14:00:00Z",
          receivedBy: branchId,
          lines: [
            {
              receiptLineId,
              lineId,
              productId,
              quantityReceived: 5,
              quantityDamaged: 0,
              quantityMissing: 0,
              quantityOther: 0,
            },
          ],
        },
      ],
      damageCustodies: [],
      satisfiedAtDestinationQty: 10,
      remainingToDispatchQty: 0,
    });
    expect(computeThisShipmentTotals(transfer)).toEqual({
      sent: 5,
      goodReceived: 5,
      damaged: 0,
      missing: 0,
      other: 0,
    });
  });
});

describe("buildReceivingDecisionView", () => {
  it("surfaces RequestReplacement + KeepAtDestination from persisted custody", () => {
    const view = buildReceivingDecisionView(baseTransfer());
    expect(view.hasDiscrepancy).toBe(true);
    expect(view.damagedQty).toBe(5);
    expect(view.damagedFollowUp).toBe("RequestReplacement");
    expect(view.custodyDecision).toBe("KeepAtDestination");
    expect(view.custodyStatus).toBe("HeldAtDestination");
  });

  it("surfaces RequestReplacement + ReturnToSource", () => {
    const view = buildReceivingDecisionView(
      baseTransfer({
        damageCustodies: [
          {
            custodyId,
            transferId,
            rootTransferId: transferId,
            receiptLineId,
            productId,
            quantity: 5,
            decision: "ReturnToSource",
            followUpIntent: "RequestReplacement",
            status: "AwaitingReturn",
            heldBranchId: "99999999-9999-9999-9999-999999999999",
            recoveredSellableQty: 0,
            confirmedDamagedQty: 0,
            waivedQty: 0,
            destinationRecoveredSellableQty: 0,
            replacementDemandQty: 5,
            createdAtUtc: "2026-09-22T12:00:00Z",
            updatedAtUtc: "2026-09-22T12:00:00Z",
          },
        ],
      }),
    );
    expect(view.damagedFollowUp).toBe("RequestReplacement");
    expect(view.custodyDecision).toBe("ReturnToSource");
    expect(view.custodyStatus).toBe("AwaitingReturn");
  });

  it("surfaces AcceptShortage + KeepAtDestination", () => {
    const view = buildReceivingDecisionView(
      baseTransfer({
        remainingToDispatchQty: 0,
        receipts: [
          {
            receiptId,
            sequence: 1,
            receivedAtUtc: "2026-09-22T12:00:00Z",
            receivedBy: branchId,
            lines: [
              {
                receiptLineId,
                lineId,
                productId,
                quantityReceived: 5,
                quantityDamaged: 5,
                damagedFollowUp: "AcceptShortage",
              },
            ],
          },
        ],
        damageCustodies: [
          {
            custodyId,
            transferId,
            rootTransferId: transferId,
            receiptLineId,
            productId,
            quantity: 5,
            decision: "KeepAtDestination",
            followUpIntent: "AcceptShortage",
            status: "HeldAtDestination",
            heldBranchId: "99999999-9999-9999-9999-999999999999",
            recoveredSellableQty: 0,
            confirmedDamagedQty: 0,
            waivedQty: 0,
            destinationRecoveredSellableQty: 0,
            replacementDemandQty: 0,
            createdAtUtc: "2026-09-22T12:00:00Z",
            updatedAtUtc: "2026-09-22T12:00:00Z",
          },
        ],
      }),
    );
    expect(view.damagedFollowUp).toBe("AcceptShortage");
    expect(view.custodyDecision).toBe("KeepAtDestination");
  });

  it("surfaces AcceptShortage + ReturnToSource", () => {
    const view = buildReceivingDecisionView(
      baseTransfer({
        remainingToDispatchQty: 0,
        receipts: [
          {
            receiptId,
            sequence: 1,
            receivedAtUtc: "2026-09-22T12:00:00Z",
            receivedBy: branchId,
            lines: [
              {
                receiptLineId,
                lineId,
                productId,
                quantityReceived: 5,
                quantityDamaged: 5,
                damagedFollowUp: "AcceptShortage",
              },
            ],
          },
        ],
        damageCustodies: [
          {
            custodyId,
            transferId,
            rootTransferId: transferId,
            receiptLineId,
            productId,
            quantity: 5,
            decision: "ReturnToSource",
            followUpIntent: "AcceptShortage",
            status: "AwaitingReturn",
            heldBranchId: "99999999-9999-9999-9999-999999999999",
            recoveredSellableQty: 0,
            confirmedDamagedQty: 0,
            waivedQty: 0,
            destinationRecoveredSellableQty: 0,
            replacementDemandQty: 0,
            createdAtUtc: "2026-09-22T12:00:00Z",
            updatedAtUtc: "2026-09-22T12:00:00Z",
          },
        ],
      }),
    );
    expect(view.damagedFollowUp).toBe("AcceptShortage");
    expect(view.custodyDecision).toBe("ReturnToSource");
  });

  it("surfaces other exception custody from exceptionCustodies", () => {
    const actualProductId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const view = buildReceivingDecisionView(
      baseTransfer({
        totalReceivedQty: 8,
        damageCustodies: [],
        receipts: [
          {
            receiptId,
            sequence: 1,
            receivedAtUtc: "2026-09-22T12:00:00Z",
            receivedBy: branchId,
            lines: [
              {
                receiptLineId,
                lineId,
                productId,
                quantityReceived: 8,
                quantityOther: 2,
                otherReasonCode: "WrongVariant",
                otherFollowUp: "RequestReplacement",
                actualReceivedProductId: actualProductId,
                otherCustodyDecision: "ReturnToSource",
              },
            ],
          },
        ],
        exceptionCustodies: [
          {
            custodyId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            transferId,
            rootTransferId: transferId,
            receiptLineId,
            expectedProductId: productId,
            actualProductId,
            expectedProductName: "Coke 1.5L",
            actualProductName: "Coke 500ml",
            quantity: 2,
            reasonCode: "WrongVariant",
            decision: "ReturnToSource",
            followUpIntent: "RequestReplacement",
            status: "AwaitingReturn",
            heldBranchId: "99999999-9999-9999-9999-999999999999",
            recoveredSellableQty: 0,
            confirmedNonSellableQty: 0,
            replacementDemandQty: 2,
            createdAtUtc: "2026-09-22T12:00:00Z",
            updatedAtUtc: "2026-09-22T12:00:00Z",
          },
        ],
      }),
    );
    expect(view.otherQty).toBe(2);
    expect(view.otherReasonCode).toBe("WrongVariant");
    expect(view.otherCustodyDecision).toBe("ReturnToSource");
    expect(view.otherCustodyStatus).toBe("AwaitingReturn");
    expect(view.actualReceivedProductId).toBe(actualProductId);
    expect(view.expectedProductId).toBe(productId);
    expect(view.expectedProductName).toBe("Coke 1.5L");
    expect(view.actualReceivedProductName).toBe("Coke 500ml");
    expect(view.showExpectedAndActualItems).toBe(true);
    expect(view.otherFollowUp).toBe("RequestReplacement");
  });

  it("resolves WrongItem Apple expected / Banana actual with note", () => {
    const view = buildReceivingDecisionView(
      baseTransfer({
        totalReceivedQty: 5,
        damageCustodies: [],
        receipts: [
          {
            receiptId,
            sequence: 1,
            receivedAtUtc: "2026-09-22T12:00:00Z",
            receivedBy: branchId,
            lines: [
              {
                receiptLineId,
                lineId,
                productId,
                quantityReceived: 5,
                quantityOther: 5,
                otherReasonCode: "WrongItem",
                otherReasonNote: "Banana was packed instead of Apple",
                otherFollowUp: "RequestReplacement",
                actualReceivedProductId: actualBananaId,
                otherCustodyDecision: "ReturnToSource",
              },
            ],
          },
        ],
        exceptionCustodies: [
          {
            custodyId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            transferId,
            rootTransferId: transferId,
            receiptLineId,
            expectedProductId: productId,
            actualProductId: actualBananaId,
            expectedProductName: "Apple",
            actualProductName: "Banana",
            quantity: 5,
            reasonCode: "WrongItem",
            decision: "ReturnToSource",
            followUpIntent: "RequestReplacement",
            status: "AwaitingReturn",
            heldBranchId: destinationBranchId,
            recoveredSellableQty: 0,
            confirmedNonSellableQty: 0,
            replacementDemandQty: 5,
            createdAtUtc: "2026-09-22T12:00:00Z",
            updatedAtUtc: "2026-09-22T12:00:00Z",
          },
        ],
      }),
    );
    expect(view.otherReasonCode).toBe("WrongItem");
    expect(view.expectedProductName).toBe("Apple");
    expect(view.actualReceivedProductName).toBe("Banana");
    expect(view.actualReceivedProductName).not.toBe("Apple");
    expect(view.otherReasonNote).toBe("Banana was packed instead of Apple");
    expect(view.showExpectedAndActualItems).toBe(true);
    expect(view.otherCustodyDecision).toBe("ReturnToSource");
    expect(view.otherCustodyStatus).toBe("AwaitingReturn");
  });

  it("omits note when empty and does not invent text", () => {
    const view = buildReceivingDecisionView(
      baseTransfer({
        damageCustodies: [],
        receipts: [
          {
            receiptId,
            sequence: 1,
            receivedAtUtc: "2026-09-22T12:00:00Z",
            receivedBy: branchId,
            lines: [
              {
                receiptLineId,
                lineId,
                productId,
                quantityReceived: 5,
                quantityOther: 5,
                otherReasonCode: "Expired",
                otherReasonNote: "   ",
                note: null,
              },
            ],
          },
        ],
        exceptionCustodies: [
          {
            custodyId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            transferId,
            rootTransferId: transferId,
            receiptLineId,
            expectedProductId: productId,
            actualProductId: productId,
            expectedProductName: "Apple",
            actualProductName: "Apple",
            quantity: 5,
            reasonCode: "Expired",
            decision: "ReturnToSource",
            followUpIntent: "AcceptShortage",
            status: "AwaitingReturn",
            heldBranchId: destinationBranchId,
            recoveredSellableQty: 0,
            confirmedNonSellableQty: 0,
            replacementDemandQty: 0,
            createdAtUtc: "2026-09-22T12:00:00Z",
            updatedAtUtc: "2026-09-22T12:00:00Z",
          },
        ],
      }),
    );
    expect(view.otherReasonNote).toBeNull();
    expect(view.showExpectedAndActualItems).toBe(false);
  });

  it("surfaces missing disposition without inferring from remainingToDispatch", () => {
    const view = buildReceivingDecisionView(
      baseTransfer({
        totalReceivedQty: 5,
        remainingToDispatchQty: 99,
        damageCustodies: [],
        receipts: [
          {
            receiptId,
            sequence: 1,
            receivedAtUtc: "2026-09-22T12:00:00Z",
            receivedBy: branchId,
            lines: [
              {
                receiptLineId,
                lineId,
                productId,
                quantityReceived: 5,
                quantityDamaged: 0,
                quantityMissing: 5,
                missingDisposition: "ExpectedLater",
                note: "Partial truck — rest coming tomorrow",
              },
            ],
          },
        ],
      }),
    );
    expect(view.missingQty).toBe(5);
    expect(view.missingDisposition).toBe("ExpectedLater");
    expect(view.damagedFollowUp).toBeNull();
    expect(view.otherReasonNote).toBe("Partial truck — rest coming tomorrow");
  });
});

describe("friendly decision labels", () => {
  it("never maps to raw enum strings", () => {
    expect(transferDiscrepancyFollowUpLabelKey("RequestReplacement")).toBe(
      "transfer.decision.replacementRequested",
    );
    expect(transferDiscrepancyFollowUpLabelKey("AcceptShortage")).toBe(
      "transfer.decision.acceptedNoReplacement",
    );
    expect(transferMissingDispositionLabelKey("ExpectedLater")).toBe(
      "transfer.followUp.waitOriginal",
    );
    expect(transferMissingDispositionLabelKey("AcceptShortage")).toBe(
      "transfer.decision.acceptedShortage",
    );
    expect(transferCustodyDecisionLabelKey("KeepAtDestination")).toBe(
      "transfer.custody.keepAtDestination",
    );
    expect(transferCustodyDecisionLabelKey("ReturnToSource")).toBe(
      "transfer.custody.returnToSource",
    );
    expect(transferCustodyStatusLabelKey("HeldAtDestination")).toBe(
      "transfer.custody.heldAtDestination",
    );
    expect(transferCustodyStatusLabelKey("AwaitingReturn")).toBe(
      "transfer.custody.awaitingReturn",
    );
    expect(transferCustodyStatusLabelKey("ReturnInTransit")).toBe(
      "transfer.custody.returnInTransit",
    );
    expect(transferCustodyStatusLabelKey("ReceivedAtSource")).toBe(
      "transfer.custody.receivedAtSource",
    );
  });
});

describe("branch-aware exception custody labels", () => {
  it("uses source branch name for Return to {branch}", () => {
    expect(
      formatReturnToSourceCustodyLabel(
        "Main Branch",
        "Return to {branch}",
        "Return to source",
      ),
    ).toBe("Return to Main Branch");
    expect(
      formatReturnToSourceCustodyLabel(
        "Branch 2",
        "Return to {branch}",
        "Return to source",
      ),
    ).toBe("Return to Branch 2");
    expect(
      formatReturnToSourceCustodyLabel(null, "Return to {branch}", "Return to source"),
    ).toBe("Return to source");
  });

  it("formats return status with source branch, not destination", () => {
    const templates = {
      awaitingReturn: "Waiting to return",
      returningToBranch: "Returning to {branch}",
      returnInTransitFallback: "Return in transit",
      returnedToBranch: "Returned to {branch}",
      receivedAtSourceFallback: "Returned to source",
      heldAtDestination: "Held at destination",
      awaitingInspection: "Awaiting inspection",
    };
    expect(
      formatExceptionCustodyReturnStatusLabel("AwaitingReturn", "Main Branch", templates),
    ).toBe("Waiting to return");
    expect(
      formatExceptionCustodyReturnStatusLabel("ReturnInTransit", "Main Branch", templates),
    ).toBe("Returning to Main Branch");
    expect(
      formatExceptionCustodyReturnStatusLabel("ReceivedAtSource", "Main Branch", templates),
    ).toBe("Returned to Main Branch");
    expect(
      formatExceptionCustodyReturnStatusLabel("ReturnInTransit", "Branch 2", templates),
    ).not.toBe("Returning to Main Branch");
    expect(formatExceptionCustodyReturnStatusLabel("ReturnInTransit", null, templates)).toBe(
      "Return in transit",
    );
  });
});

describe("pickReceivingDecisionNote", () => {
  it("prefers otherReasonNote and omits empties", () => {
    expect(
      pickReceivingDecisionNote("Banana was packed instead of Apple", "other", "disc"),
    ).toBe("Banana was packed instead of Apple");
    expect(pickReceivingDecisionNote("  ", "Line note", null)).toBe("Line note");
    expect(pickReceivingDecisionNote(null, null, "  ")).toBeNull();
    expect(pickReceivingDecisionNote(undefined, undefined, undefined)).toBeNull();
  });
});

describe("resolveExceptionCustodyItemLabel", () => {
  it("prefers actualProductName and never falls back to expected when different", () => {
    const transfer = baseTransfer({
      lines: [
        {
          lineId,
          productId,
          productName: "Apple",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
          discrepancyReason: null,
          discrepancyNote: null,
          sourceLotId: null,
          lotNumber: null,
          expirationDate: null,
        },
      ],
    });
    expect(
      resolveExceptionCustodyItemLabel(transfer, {
        actualProductId: actualBananaId,
        expectedProductId: productId,
        actualProductName: "Banana",
        expectedProductName: "Apple",
      }),
    ).toBe("Banana");

    expect(
      resolveExceptionCustodyItemLabel(transfer, {
        actualProductId: actualBananaId,
        expectedProductId: productId,
        actualProductName: null,
        expectedProductName: "Apple",
      }),
    ).toBe("—");

    expect(
      resolveExceptionCustodyExpectedItemLabel(transfer, {
        expectedProductId: productId,
        expectedProductName: "Apple",
      }),
    ).toBe("Apple");
  });

  it("may use expected name only when actual product id equals expected", () => {
    const transfer = baseTransfer();
    expect(
      resolveExceptionCustodyItemLabel(transfer, {
        actualProductId: productId,
        expectedProductId: productId,
        actualProductName: null,
        expectedProductName: "Apple",
      }),
    ).toBe("Apple");
  });
});

describe("inventoryTransferDtoSchema retains exception product names", () => {
  it("does not strip actualProductName / expectedProductName from transfer GET payloads", async () => {
    const { inventoryTransferDtoSchema } = await import(
      "@/api/pos/pos-inventory-transfer-client"
    );
    const parsed = inventoryTransferDtoSchema.parse({
      transferId,
      organizationId: "88888888-8888-8888-8888-888888888888",
      transferNumber: "TR-260922-001",
      sourceBranchId: branchId,
      destinationBranchId,
      status: "ClosedWithDiscrepancy",
      notes: null,
      createdBy: branchId,
      createdAtUtc: "2026-09-22T10:00:00Z",
      updatedAtUtc: "2026-09-22T12:00:00Z",
      totalSentQty: 10,
      totalReceivedQty: 5,
      totalDifferenceQty: 5,
      lines: [
        {
          lineId,
          productId,
          productName: "Apple",
          unitOfMeasure: "kg",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
        },
      ],
      exceptionCustodies: [
        {
          custodyId,
          transferId,
          rootTransferId: transferId,
          receiptLineId,
          expectedProductId: productId,
          actualProductId: actualBananaId,
          expectedProductName: "Apple",
          actualProductName: "Banana",
          quantity: 5,
          reasonCode: "WrongItem",
          decision: "ReturnToSource",
          followUpIntent: "RequestReplacement",
          status: "AwaitingReturn",
          heldBranchId: destinationBranchId,
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 5,
          createdAtUtc: "2026-09-22T12:00:00Z",
          updatedAtUtc: "2026-09-22T12:00:00Z",
        },
      ],
    });
    expect(parsed.exceptionCustodies?.[0]?.expectedProductName).toBe("Apple");
    expect(parsed.exceptionCustodies?.[0]?.actualProductName).toBe("Banana");
  });
});

describe("lineOtherExceptionSecondaryText", () => {
  it("shows product name for Wrong variant and never GUID fragments", () => {
    const actualId = "7477f670-aaaa-bbbb-cccc-dddddddddddd";
    const transfer = baseTransfer({
      status: "ClosedWithDiscrepancy",
      totalReceivedQty: 5,
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          sequence: 1,
          receivedAtUtc: "2026-09-22T19:00:00Z",
          receivedBy: branchId,
          lines: [
            {
              receiptLineId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
              lineId,
              productId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "WrongVariant",
              otherCustodyDecision: "ReturnToSource",
              actualReceivedProductId: actualId,
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          transferId,
          rootTransferId: transferId,
          receiptLineId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          expectedProductId: productId,
          actualProductId: actualId,
          expectedProductName: "Coke 330ml",
          actualProductName: "Pepsi 330ml",
          quantity: 5,
          reasonCode: "WrongVariant",
          decision: "ReturnToSource",
          followUpIntent: "AcceptShortage",
          status: "AwaitingReturn",
          heldBranchId: "99999999-9999-9999-9999-999999999999",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 0,
          createdAtUtc: "2026-09-22T19:00:00Z",
          updatedAtUtc: "2026-09-22T19:00:00Z",
        },
      ],
    });

    expect(lineOtherExceptionSecondaryText(transfer, transfer.lines[0]!)).toBe(
      "Wrong variant · Actual: Pepsi 330ml",
    );
  });

  it("omits Actual GUID when product name is unavailable", () => {
    const actualId = "7477f670-aaaa-bbbb-cccc-dddddddddddd";
    const transfer = baseTransfer({
      status: "ClosedWithDiscrepancy",
      totalReceivedQty: 5,
      receipts: [
        {
          receiptId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          sequence: 1,
          receivedAtUtc: "2026-09-22T19:00:00Z",
          receivedBy: branchId,
          lines: [
            {
              receiptLineId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
              lineId,
              productId,
              quantityReceived: 5,
              quantityOther: 5,
              otherReasonCode: "WrongVariant",
              actualReceivedProductId: actualId,
            },
          ],
        },
      ],
      exceptionCustodies: [
        {
          custodyId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          transferId,
          rootTransferId: transferId,
          receiptLineId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          expectedProductId: productId,
          actualProductId: actualId,
          expectedProductName: null,
          actualProductName: null,
          quantity: 5,
          reasonCode: "WrongVariant",
          decision: "ReturnToSource",
          followUpIntent: "AcceptShortage",
          status: "AwaitingReturn",
          heldBranchId: "99999999-9999-9999-9999-999999999999",
          recoveredSellableQty: 0,
          confirmedNonSellableQty: 0,
          replacementDemandQty: 0,
          createdAtUtc: "2026-09-22T19:00:00Z",
          updatedAtUtc: "2026-09-22T19:00:00Z",
        },
      ],
      lines: [
        {
          lineId,
          productId,
          productName: "Coke 330ml",
          unitOfMeasure: "pcs",
          lineNumber: 1,
          sentQty: 10,
          receivedQty: 5,
          differenceQty: 5,
          lineStatus: "Partial",
          discrepancyReason: null,
          discrepancyNote: null,
          sourceLotId: null,
          lotNumber: null,
          expirationDate: null,
        },
      ],
    });

    expect(lineOtherExceptionSecondaryText(transfer, transfer.lines[0]!)).toBe("Wrong variant");
  });
});

describe("family fulfillment target", () => {
  it("uses root member sent qty when viewing a replacement", () => {
    expect(
      familyFulfillmentTargetQty(
        baseTransfer({
          totalSentQty: 5,
          rootTransferId: transferId,
          familyMembers: [
            {
              transferId,
              transferNumber: "TR-260922-001",
              status: "ClosedWithDiscrepancy",
              replacementSequence: null,
              isRoot: true,
              totalSentQty: 10,
              totalReceivedQty: 5,
              totalOutstandingQty: 0,
              totalDamagedQty: 5,
            },
            {
              transferId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              transferNumber: "TR-260922-001-R1",
              status: "Received",
              replacementSequence: 1,
              isRoot: false,
              totalSentQty: 5,
              totalReceivedQty: 5,
              totalOutstandingQty: 0,
            },
          ],
        }),
      ),
    ).toBe(10);
  });
});

describe("lineFollowUpDisplay", () => {
  it("shows Replace qty for damaged RequestReplacement", () => {
    expect(lineFollowUpDisplay(baseTransfer(), { lineId })).toEqual({
      labelKey: "transfer.followUp.replaceQty",
      qty: 5,
    });
  });
});
