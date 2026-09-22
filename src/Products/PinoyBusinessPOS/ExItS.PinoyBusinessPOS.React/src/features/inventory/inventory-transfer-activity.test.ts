import { describe, expect, it } from "vitest";
import type { InventoryTransferDto } from "@/api/pos/pos-inventory-transfer-client";
import { buildTransferActivityEvents } from "@/features/inventory/inventory-transfer-activity";

const base: InventoryTransferDto = {
  transferId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  sourceBranchId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  destinationBranchId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
  status: "Draft",
  createdBy: "11111111-1111-1111-1111-111111111111",
  createdAtUtc: "2026-03-01T10:00:00Z",
  updatedAtUtc: "2026-03-01T10:00:00Z",
  totalSentQty: 2,
  totalReceivedQty: 0,
  totalDifferenceQty: 0,
  lines: [],
};

describe("buildTransferActivityEvents", () => {
  it("always includes created and sorts lifecycle timestamps", () => {
    const events = buildTransferActivityEvents({
      ...base,
      status: "Received",
      dispatchedAtUtc: "2026-03-02T10:00:00Z",
      dispatchedBy: "22222222-2222-2222-2222-222222222222",
      receivedAtUtc: "2026-03-03T10:00:00Z",
      receivedBy: "33333333-3333-3333-3333-333333333333",
    });

    expect(events.map((e) => e.kind)).toEqual(["created", "dispatched", "received"]);
  });

  it("emits receipt waves from receipt history", () => {
    const events = buildTransferActivityEvents({
      ...base,
      status: "PartiallyReceived",
      receipts: [
        {
          receiptId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          sequence: 1,
          receivedAtUtc: "2026-03-03T10:00:00Z",
          receivedBy: "33333333-3333-3333-3333-333333333333",
          lines: [
            {
              receiptLineId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
              lineId: "12121212-1212-1212-1212-121212121212",
              productId: "13131313-1313-1313-1313-131313131313",
              quantityReceived: 8,
              quantityDamaged: 1,
              quantityMissing: 2,
              missingDisposition: "ExpectedLater",
              note: "Damaged in transit",
            },
          ],
        },
      ],
    });

    expect(events.map((e) => e.kind)).toEqual(["created", "receipt"]);
    const receipt = events.find((e) => e.kind === "receipt");
    expect(receipt?.receiptLines?.[0]).toMatchObject({
      quantityReceived: 8,
      quantityDamaged: 1,
      quantityMissing: 2,
      missingDisposition: "ExpectedLater",
    });
  });

  it("uses closedAtUtc and closedBy for closed remainder without legacy fallbacks", () => {
    const events = buildTransferActivityEvents({
      ...base,
      status: "ClosedWithDiscrepancy",
      closedAtUtc: "2026-03-04T10:00:00Z",
      closedBy: "55555555-5555-5555-5555-555555555555",
      lastReceiptAtUtc: "2026-03-03T10:00:00Z",
      updatedAtUtc: "2026-03-03T12:00:00Z",
      receivedBy: "33333333-3333-3333-3333-333333333333",
    });

    expect(events.map((e) => e.kind)).toEqual(["created", "closedRemainder"]);
    const closed = events.find((e) => e.kind === "closedRemainder");
    expect(closed?.atUtc).toBe("2026-03-04T10:00:00Z");
    expect(closed?.actorId).toBe("55555555-5555-5555-5555-555555555555");
  });

  it("omits closed remainder when closedAtUtc is missing", () => {
    const events = buildTransferActivityEvents({
      ...base,
      status: "ClosedWithDiscrepancy",
      lastReceiptAtUtc: "2026-03-03T10:00:00Z",
      updatedAtUtc: "2026-03-03T12:00:00Z",
      receivedBy: "33333333-3333-3333-3333-333333333333",
    });

    expect(events.map((e) => e.kind)).toEqual(["created"]);
  });

  it("includes cancelled when present", () => {
    const events = buildTransferActivityEvents({
      ...base,
      status: "Cancelled",
      cancelledAtUtc: "2026-03-01T12:00:00Z",
      cancelledBy: "44444444-4444-4444-4444-444444444444",
    });

    expect(events.map((e) => e.kind)).toEqual(["created", "cancelled"]);
  });
});
