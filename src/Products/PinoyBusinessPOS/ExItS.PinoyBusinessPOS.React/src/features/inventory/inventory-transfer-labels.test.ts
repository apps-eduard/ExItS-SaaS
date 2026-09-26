import { describe, expect, it } from "vitest";
import {
  inventoryTransferDiscrepancyLabelKey,
  inventoryTransferExecutor,
  inventoryTransferStatusLabelKey,
  inventoryTransferStatusPresentation,
  inventoryTransferStatusTone,
  isInventoryTransferDiscrepancyReason,
  isReceiveLineReady,
  parseReceivedQuantity,
  parseTransferQuantity,
} from "@/features/inventory/inventory-transfer-labels";

describe("inventory-transfer-labels", () => {
  it("maps status and discrepancy reason keys from backend codes", () => {
    expect(inventoryTransferStatusLabelKey("InTransit")).toBe("transfer.status.inTransit");
    expect(inventoryTransferStatusLabelKey("PartiallyReceived")).toBe(
      "transfer.status.partiallyReceived",
    );
    expect(inventoryTransferStatusLabelKey("ClosedWithDiscrepancy")).toBe(
      "transfer.status.closedWithDiscrepancy",
    );
    expect(inventoryTransferDiscrepancyLabelKey("ShortShipment")).toBe(
      "transfer.discrepancy.shortShipment",
    );
    expect(isInventoryTransferDiscrepancyReason("LostInTransit")).toBe(true);
    expect(isInventoryTransferDiscrepancyReason("MadeUp")).toBe(false);
  });

  it("presents ClosedWithDiscrepancy as Discrepancy closed success or open warning", () => {
    expect(inventoryTransferStatusTone("ClosedWithDiscrepancy")).toBe("success");
    expect(inventoryTransferStatusPresentation("ClosedWithDiscrepancy")).toEqual({
      labelKey: "transfer.status.closedWithDiscrepancy",
      tone: "success",
      discrepancyIcon: "check",
    });
    expect(
      inventoryTransferStatusPresentation("ClosedWithDiscrepancy", {
        hasOpenDiscrepancyFollowUp: true,
      }),
    ).toEqual({
      labelKey: "transfer.status.closedWithDiscrepancy",
      tone: "warning",
      discrepancyIcon: "clock",
    });
    expect(inventoryTransferStatusPresentation("Received").discrepancyIcon).toBe("check");
    expect(inventoryTransferStatusPresentation("InTransit").discrepancyIcon).toBe("clock");
    expect(inventoryTransferStatusPresentation("PartiallyReceived").discrepancyIcon).toBe("clock");
  });

  it("keeps Discrepancy open while return custody is unfinished even if fulfillment is done", async () => {
    const { inventoryTransferHasOpenDiscrepancyFollowUp } = await import(
      "@/features/inventory/InventoryTransferStatusChip"
    );
    const awaitingSend = [{ followUpIntent: "RequestReplacement", status: "AwaitingReturn" }];
    expect(
      inventoryTransferHasOpenDiscrepancyFollowUp(awaitingSend, {
        remainingToDispatchQty: 0,
        openInTransitQty: 0,
      }),
    ).toBe(true);
    expect(
      inventoryTransferHasOpenDiscrepancyFollowUp(
        [{ followUpIntent: "AcceptShortage", status: "ReturnInTransit" }],
        { remainingToDispatchQty: 0, openInTransitQty: 0 },
      ),
    ).toBe(true);
    expect(
      inventoryTransferHasOpenDiscrepancyFollowUp(
        [{ followUpIntent: "RequestReplacement", status: "ReceivedAtSource" }],
        { remainingToDispatchQty: 0, openInTransitQty: 0 },
      ),
    ).toBe(false);
    expect(
      inventoryTransferHasOpenDiscrepancyFollowUp(
        [{ followUpIntent: "RequestReplacement", status: "Inspected" }],
        { remainingToDispatchQty: 0, openInTransitQty: 0 },
      ),
    ).toBe(false);
    expect(
      inventoryTransferHasOpenDiscrepancyFollowUp(
        [{ followUpIntent: "RequestReplacement", status: "HeldAtDestination" }],
        { remainingToDispatchQty: 0, openInTransitQty: 0 },
      ),
    ).toBe(false);
  });

  it("treats RequestReplacement as open while family fulfillment is incomplete", async () => {
    const { inventoryTransferHasOpenDiscrepancyFollowUp } = await import(
      "@/features/inventory/InventoryTransferStatusChip"
    );
    const rr = [{ followUpIntent: "RequestReplacement", status: "HeldAtDestination" }];
    expect(
      inventoryTransferHasOpenDiscrepancyFollowUp(rr, {
        remainingToDispatchQty: 0,
        openInTransitQty: 0,
      }),
    ).toBe(false);
    expect(
      inventoryTransferHasOpenDiscrepancyFollowUp(rr, {
        remainingToDispatchQty: 5,
        openInTransitQty: 0,
      }),
    ).toBe(true);
    expect(
      inventoryTransferHasOpenDiscrepancyFollowUp([{ followUpIntent: "AcceptShortage" }], {
        remainingToDispatchQty: 5,
      }),
    ).toBe(false);
  });

  it("validates transfer and receive quantities", () => {
    expect(parseTransferQuantity("2.5")).toBe(2.5);
    expect(parseTransferQuantity("0")).toBe("invalid");
    expect(parseReceivedQuantity("0", 10)).toBe(0);
    expect(parseReceivedQuantity("11", 10)).toBe("exceeds");
    expect(parseReceivedQuantity("8", 10)).toBe(8);
    expect(parseReceivedQuantity("-1", 10)).toBe("invalid");
    expect(isReceiveLineReady("5", 5, null)).toBe(true);
    expect(isReceiveLineReady("4", 5, null)).toBe(false);
    expect(isReceiveLineReady("4", 5, "ShortShipment")).toBe(true);
    expect(isReceiveLineReady("0", 5, "LostInTransit")).toBe(true);
    expect(isReceiveLineReady("6", 5, "ShortShipment")).toBe(false);
  });

  it("uses closedBy for ClosedWithDiscrepancy executor", () => {
    expect(
      inventoryTransferExecutor({
        status: "ClosedWithDiscrepancy",
        createdBy: "created",
        receivedBy: "received",
        closedBy: "closed",
      }),
    ).toEqual({
      actorId: "closed",
      labelKey: "transfer.byClosedWithDiscrepancy",
    });
  });
});
