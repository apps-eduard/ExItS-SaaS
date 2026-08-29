import { describe, expect, it } from "vitest";
import {
  inventoryTransferDiscrepancyLabelKey,
  inventoryTransferStatusLabelKey,
  isInventoryTransferDiscrepancyReason,
  parseReceivedQuantity,
  parseTransferQuantity,
} from "@/features/inventory/inventory-transfer-labels";

describe("inventory-transfer-labels", () => {
  it("maps status and discrepancy reason keys from backend codes", () => {
    expect(inventoryTransferStatusLabelKey("InTransit")).toBe("transfer.status.inTransit");
    expect(inventoryTransferStatusLabelKey("PartiallyReceived")).toBe(
      "transfer.status.partiallyReceived",
    );
    expect(inventoryTransferDiscrepancyLabelKey("ShortShipment")).toBe(
      "transfer.discrepancy.shortShipment",
    );
    expect(isInventoryTransferDiscrepancyReason("LostInTransit")).toBe(true);
    expect(isInventoryTransferDiscrepancyReason("MadeUp")).toBe(false);
  });

  it("validates transfer and receive quantities", () => {
    expect(parseTransferQuantity("2.5")).toBe(2.5);
    expect(parseTransferQuantity("0")).toBe("invalid");
    expect(parseReceivedQuantity("0", 10)).toBe(0);
    expect(parseReceivedQuantity("11", 10)).toBe("invalid");
    expect(parseReceivedQuantity("8", 10)).toBe(8);
  });
});
