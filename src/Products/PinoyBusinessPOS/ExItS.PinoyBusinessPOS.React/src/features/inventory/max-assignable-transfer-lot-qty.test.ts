import { describe, expect, it } from "vitest";
import { maxAssignableTransferLotQty } from "@/features/inventory/TransferChangeLotsDialog";

describe("maxAssignableTransferLotQty", () => {
  it("caps at lot on-hand", () => {
    expect(maxAssignableTransferLotQty(50)).toBe(50);
    expect(maxAssignableTransferLotQty(0)).toBe(0);
  });

  it("ignores negative on-hand", () => {
    expect(maxAssignableTransferLotQty(-3)).toBe(0);
  });
});
