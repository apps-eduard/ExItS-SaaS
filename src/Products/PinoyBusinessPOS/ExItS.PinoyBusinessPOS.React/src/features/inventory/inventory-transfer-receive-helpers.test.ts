import { describe, expect, it } from "vitest";
import type { InventoryTransferDto } from "@/api/pos/pos-inventory-transfer-client";
import {
  canDestinationCloseRemainder,
  canDestinationReceiveTransfer,
  defaultReceiveNowByLine,
  isReceiveSubmissionReady,
  lineOutstandingQty,
  parseReceiveNowQuantity,
} from "@/features/inventory/inventory-transfer-receive-helpers";

const line = {
  lineId: "22222222-2222-2222-2222-222222222222",
  productId: "11111111-1111-1111-1111-111111111111",
  productName: "Coke",
  unitOfMeasure: "pcs",
  lineNumber: 1,
  sentQty: 24,
  receivedQty: 10,
  outstandingQty: 14,
  closedQty: 0,
  differenceQty: 14,
  lineStatus: "Short",
  discrepancyReason: null,
  discrepancyNote: null,
};

const transfer = {
  transferId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  sourceBranchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  destinationBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  status: "PartiallyReceived",
  createdBy: "99999999-9999-9999-9999-999999999999",
  createdAtUtc: "2026-08-29T08:00:00Z",
  updatedAtUtc: "2026-08-29T10:00:00Z",
  totalSentQty: 24,
  totalReceivedQty: 10,
  totalOutstandingQty: 14,
  totalDifferenceQty: 14,
  lines: [line],
} as InventoryTransferDto;

describe("inventory-transfer-receive-helpers", () => {
  it("derives outstanding from dto fields", () => {
    expect(lineOutstandingQty(line)).toBe(14);
    expect(lineOutstandingQty({ sentQty: 20, receivedQty: 5, closedQty: 3 })).toBe(12);
  });

  it("validates receive-now against outstanding", () => {
    expect(parseReceiveNowQuantity("14", 14)).toBe(14);
    expect(parseReceiveNowQuantity("15", 14)).toBe("exceeds");
    expect(parseReceiveNowQuantity("0", 14)).toBe(0);
  });

  it("defaults receive-now to outstanding per line", () => {
    expect(defaultReceiveNowByLine(transfer)).toEqual({
      [line.lineId]: "14",
    });
  });

  it("knows actionable destination states", () => {
    expect(canDestinationReceiveTransfer(transfer)).toBe(true);
    expect(canDestinationCloseRemainder(transfer)).toBe(true);
    expect(canDestinationReceiveTransfer({ ...transfer, status: "Received" })).toBe(false);
  });

  it("requires at least one positive receive-now line", () => {
    const byLine = { [line.lineId]: "0" };
    expect(isReceiveSubmissionReady(transfer.lines, byLine)).toBe(false);
    expect(isReceiveSubmissionReady(transfer.lines, { [line.lineId]: "2" })).toBe(true);
  });
});
