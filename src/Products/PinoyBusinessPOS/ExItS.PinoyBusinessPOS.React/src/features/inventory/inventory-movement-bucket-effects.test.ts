import { describe, expect, it } from "vitest";
import {
  describeMovementBucketEffects,
  movementNeedsBucketBreakdown,
} from "./inventory-movement-bucket-effects";

describe("inventory-movement-bucket-effects", () => {
  it("treats damaged hold as physical non-sellable", () => {
    const e = describeMovementBucketEffects("TransferDamageHold", 5);
    expect(e.physicalDelta).toBe(5);
    expect(e.sellableDelta).toBe(0);
    expect(e.damagedDelta).toBe(5);
    expect(movementNeedsBucketBreakdown("TransferDamageHold")).toBe(true);
  });

  it("treats good transfer in as sellable", () => {
    const e = describeMovementBucketEffects("TransferIn", 5);
    expect(e.physicalDelta).toBe(5);
    expect(e.sellableDelta).toBe(5);
    expect(e.damagedDelta).toBe(0);
  });

  it("return in parks inspection hold", () => {
    const e = describeMovementBucketEffects("TransferDamageReturnIn", 5);
    expect(e.physicalDelta).toBe(5);
    expect(e.sellableDelta).toBe(0);
    expect(e.inspectionHoldDelta).toBe(5);
  });
});
