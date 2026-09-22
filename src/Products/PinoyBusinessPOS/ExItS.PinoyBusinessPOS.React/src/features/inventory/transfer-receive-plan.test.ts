import { describe, expect, it } from "vitest";
import type { TransferReceiveLineEdit } from "@/features/inventory/inventory-transfer-receive-helpers";
import {
  buildTransferReceivePayload,
  missingDispositionFromRemainingAction,
} from "@/features/inventory/transfer-receive-plan";

const lineId = "22222222-2222-2222-2222-222222222222";
const productId = "11111111-1111-1111-1111-111111111111";

function baseEdit(overrides: Partial<TransferReceiveLineEdit> = {}): TransferReceiveLineEdit {
  return {
    lineId,
    productId,
    name: "Coke",
    sku: "",
    uom: "pcs",
    sentQty: 24,
    receivedQty: 0,
    outstandingQty: 24,
    goodText: "22",
    damagedText: "1",
    notDeliveredText: "1",
    otherText: "0",
    otherReasonCode: "",
    otherReasonText: "",
    remarksText: "Mixed",
    remainingAction: "replace_later",
    ...overrides,
  };
}

describe("transfer-receive-plan", () => {
  it("maps mixed classification to API payload with ExpectedLater", () => {
    const result = buildTransferReceivePayload([baseEdit()]);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.lines).toEqual([
      {
        lineId,
        productId,
        goodQty: 22,
        receivedQty: 22,
        damagedQty: 1,
        missingQty: 1,
        missingDisposition: "ExpectedLater",
        discrepancyNote: "Mixed",
      },
    ]);
  });

  it("maps close remaining to CloseMissing when missing qty present", () => {
    const result = buildTransferReceivePayload([
      baseEdit({ remainingAction: "cancel_remaining", notDeliveredText: "2", damagedText: "0" }),
    ]);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.lines[0]?.missingDisposition).toBe("CloseMissing");
    expect(result.lines[0]?.missingQty).toBe(2);
  });

  it("omits missing disposition when no missing qty", () => {
    expect(missingDispositionFromRemainingAction(0, "cancel_remaining")).toBeNull();
    const result = buildTransferReceivePayload([
      baseEdit({ goodText: "20", damagedText: "4", notDeliveredText: "0", remainingAction: null }),
    ]);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.lines[0]?.missingDisposition).toBeUndefined();
    expect(result.lines[0]?.damagedQty).toBe(4);
  });
});
