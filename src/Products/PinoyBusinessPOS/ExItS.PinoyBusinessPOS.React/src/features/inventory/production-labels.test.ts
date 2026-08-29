import { describe, expect, it } from "vitest";
import {
  formatProductionDate,
  productionCostStatusLabelKey,
  productionDefinitionStatusLabelKey,
  productionRunStatusLabelKey,
  productionScaleFactor,
  scaleProductionQuantity,
} from "@/features/inventory/production-labels";

describe("production-labels", () => {
  it("maps definition status codes", () => {
    expect(productionDefinitionStatusLabelKey("Active")).toBe(
      "production.setups.status.active",
    );
    expect(productionDefinitionStatusLabelKey("Inactive")).toBe(
      "production.setups.status.inactive",
    );
    expect(productionDefinitionStatusLabelKey("Other")).toBe(
      "production.setups.status.active",
    );
  });

  it("maps run and cost status codes", () => {
    expect(productionRunStatusLabelKey("Posted")).toBe("production.runs.status.posted");
    expect(productionRunStatusLabelKey("Voided")).toBe("production.runs.status.voided");
    expect(productionCostStatusLabelKey("Complete")).toBe("production.runs.costComplete");
    expect(productionCostStatusLabelKey("Partial")).toBe("production.runs.costPartial");
    expect(productionCostStatusLabelKey("Unavailable")).toBe(
      "production.runs.costUnavailable",
    );
  });

  it("computes scale factors", () => {
    expect(productionScaleFactor(10, 20)).toBe(2);
    expect(productionScaleFactor(4, 1)).toBe(0.25);
    expect(productionScaleFactor(0, 5)).toBeNull();
    expect(productionScaleFactor(-1, 5)).toBeNull();
    expect(scaleProductionQuantity(2.5, 2)).toBe(5);
  });

  it("formats produced dates", () => {
    const formatted = formatProductionDate("2026-08-29T12:00:00.000Z");
    expect(formatted.length).toBeGreaterThan(0);
    expect(formatProductionDate("not-a-date")).toBe("not-a-date");
  });
});
