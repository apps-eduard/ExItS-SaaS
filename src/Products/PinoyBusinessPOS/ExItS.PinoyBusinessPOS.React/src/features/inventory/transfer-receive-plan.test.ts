import { describe, expect, it } from "vitest";
import type { TransferReceiveLineEdit } from "@/features/inventory/inventory-transfer-receive-helpers";
import { buildTransferReceivePayload } from "@/features/inventory/transfer-receive-plan";

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
    missingFollowUp: "wait_original",
    damagedFollowUp: "request_replacement",
    otherFollowUp: "request_replacement",
    damagedCustodyDecision: "KeepAtDestination",
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
        damagedFollowUp: "RequestReplacement",
        damagedCustodyDecision: "KeepAtDestination",
        missingQty: 1,
        missingDisposition: "ExpectedLater",
        discrepancyNote: "Mixed",
      },
    ]);
  });

  it("maps request replacement to CloseMissing when missing qty present", () => {
    const result = buildTransferReceivePayload([
      baseEdit({
        missingFollowUp: "request_replacement",
        notDeliveredText: "2",
        damagedText: "0",
      }),
    ]);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.lines[0]?.missingDisposition).toBe("CloseMissing");
    expect(result.lines[0]?.missingQty).toBe(2);
  });

  it("maps accept shortage to AcceptShortage and damaged follow-up", () => {
    const result = buildTransferReceivePayload([
      baseEdit({
        goodText: "18",
        damagedText: "2",
        notDeliveredText: "4",
        missingFollowUp: "accept_shortage",
        damagedFollowUp: "accept_shortage",
      }),
    ]);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.lines[0]?.missingDisposition).toBe("AcceptShortage");
    expect(result.lines[0]?.damagedFollowUp).toBe("AcceptShortage");
  });

  it("omits missing disposition when no missing qty", () => {
    const result = buildTransferReceivePayload([
      baseEdit({
        goodText: "20",
        damagedText: "4",
        notDeliveredText: "0",
        missingFollowUp: null,
        damagedFollowUp: "request_replacement",
      }),
    ]);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.lines[0]?.missingDisposition).toBeUndefined();
    expect(result.lines[0]?.damagedQty).toBe(4);
  });
});
