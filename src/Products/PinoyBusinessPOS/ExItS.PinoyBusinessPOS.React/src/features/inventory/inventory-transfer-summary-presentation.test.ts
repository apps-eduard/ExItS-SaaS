import { describe, expect, it } from "vitest";
import type { InventoryTransferDto } from "@/api/pos/pos-inventory-transfer-client";
import {
  buildReceivingDecisionView,
  computeThisShipmentTotals,
  familyFulfillmentTargetQty,
  lineFollowUpDisplay,
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

function baseTransfer(
  overrides: Partial<InventoryTransferDto> = {},
): InventoryTransferDto {
  return {
    transferId,
    organizationId: "88888888-8888-8888-8888-888888888888",
    transferNumber: "TR-260922-001",
    sourceBranchId: branchId,
    destinationBranchId: "99999999-9999-9999-9999-999999999999",
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
              },
            ],
          },
        ],
      }),
    );
    expect(view.missingQty).toBe(5);
    expect(view.missingDisposition).toBe("ExpectedLater");
    expect(view.damagedFollowUp).toBeNull();
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
