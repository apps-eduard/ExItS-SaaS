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
    actualReceivedProductId: null,
    actualReceivedProductName: null,
    remarksText: "Mixed",
    missingFollowUp: "wait_original",
    damagedFollowUp: "request_replacement",
    otherFollowUp: "request_replacement",
    damagedCustodyDecision: "KeepAtDestination",
    otherCustodyDecision: null,
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

  it("maps WrongVariant other qty with actual product and forced ReturnToSource", () => {
    const actualProductId = "99999999-9999-9999-9999-999999999999";
    const result = buildTransferReceivePayload([
      baseEdit({
        goodText: "20",
        damagedText: "0",
        notDeliveredText: "0",
        otherText: "4",
        otherReasonCode: "WrongVariant",
        otherFollowUp: "request_replacement",
        otherCustodyDecision: "KeepAtDestination",
        actualReceivedProductId: actualProductId,
      }),
    ]);
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.lines[0]).toMatchObject({
      otherQty: 4,
      otherReasonCode: "WrongVariant",
      otherCustodyDecision: "ReturnToSource",
      actualReceivedProductId: actualProductId,
      otherFollowUp: "RequestReplacement",
    });
  });

  it("requires actual product for WrongVariant", () => {
    const result = buildTransferReceivePayload([
      baseEdit({
        goodText: "20",
        damagedText: "0",
        notDeliveredText: "0",
        otherText: "4",
        otherReasonCode: "WrongVariant",
        otherFollowUp: "request_replacement",
      }),
    ]);
    expect(result.ok).toBe(false);
    if (result.ok) {
      return;
    }
    expect(result.error).toBe("other_actual_product_required");
  });

  it("rejects actual product same as expected for WrongItem/WrongVariant", () => {
    const result = buildTransferReceivePayload([
      baseEdit({
        goodText: "20",
        damagedText: "0",
        notDeliveredText: "0",
        otherText: "4",
        otherReasonCode: "WrongVariant",
        otherFollowUp: "request_replacement",
        actualReceivedProductId: "11111111-1111-1111-1111-111111111111",
        actualReceivedProductName: "Expected Apple",
      }),
    ]);
    expect(result.ok).toBe(false);
    if (result.ok) {
      return;
    }
    expect(result.error).toBe("other_actual_product_same_as_expected");
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
