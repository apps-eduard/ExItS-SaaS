import { describe, expect, it } from "vitest";
import {
  clampRequestQuantityToAvailable,
  findRequestAvailabilityIssues,
  isRequestQuantityAllowed,
  remainingWarehouseAvailable,
} from "@/features/warehouse/retail-warehouse-request-availability";

describe("retail-warehouse-request-availability", () => {
  it("allows qty within warehouse available and rejects over", () => {
    expect(isRequestQuantityAllowed(55, 55)).toBe(true);
    expect(isRequestQuantityAllowed(54.999, 55)).toBe(true);
    expect(isRequestQuantityAllowed(55.001, 55)).toBe(false);
    expect(isRequestQuantityAllowed(0, 55)).toBe(false);
    expect(isRequestQuantityAllowed(1, 0)).toBe(false);
  });

  it("clamps to available without inventing stock", () => {
    expect(clampRequestQuantityToAvailable(60, 55)).toBe(55);
    expect(clampRequestQuantityToAvailable(10, 55)).toBe(10);
    expect(clampRequestQuantityToAvailable(1, 0)).toBeNull();
  });

  it("reports stale stock warnings without rewriting qty", () => {
    const issues = findRequestAvailabilityIssues(
      [
        {
          productId: "a",
          name: "Banana Lakatan",
          quantity: 50,
          warehouseAvailableQuantity: 40,
          unitOfMeasure: "kg",
        },
      ],
      "Iloilo Jaro Warehouse",
    );
    expect(issues).toHaveLength(1);
    expect(issues[0]?.message).toMatch(/Only 40 kg is now available/i);
  });

  it("projects remaining warehouse availability without going negative", () => {
    expect(remainingWarehouseAvailable(300, 0)).toBe(300);
    expect(remainingWarehouseAvailable(300, 6)).toBe(294);
    expect(remainingWarehouseAvailable(55, 10)).toBe(45);
    expect(remainingWarehouseAvailable(55, 20)).toBe(35);
    expect(remainingWarehouseAvailable(55, 55)).toBe(0);
    expect(remainingWarehouseAvailable(55, 56)).toBe(0);
    expect(remainingWarehouseAvailable(250, 6)).toBe(244);
  });

  it("tracks remaining per product independently (no mixed UOM aggregate)", () => {
    expect(remainingWarehouseAvailable(55, 10)).toBe(45);
    expect(remainingWarehouseAvailable(300, 6)).toBe(294);
  });
});
