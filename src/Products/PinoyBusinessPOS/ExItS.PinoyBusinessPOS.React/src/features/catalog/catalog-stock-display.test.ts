import { describe, expect, it } from "vitest";
import {
  resolveBranchSaleEligibleQuantity,
  resolveCatalogStockDisplay,
} from "@/features/catalog/catalog-stock-display";

describe("catalog-stock-display", () => {
  it("STOCKVIS-04 uses branch available not organization total", () => {
    expect(
      resolveBranchSaleEligibleQuantity({
        isTracked: true,
        onHandQuantity: 42,
        branchAvailableQuantity: 0,
        sellableQuantity: null,
        tracksExpiration: false,
      }),
    ).toBe(0);
  });

  it("STOCKVIS-01 shows branch in-stock quantity", () => {
    const stock = resolveCatalogStockDisplay({
      isTracked: true,
      onHandQuantity: 30,
      branchAvailableQuantity: 30,
      unitOfMeasure: "Piece",
      tracksExpiration: false,
      sellableQuantity: null,
      stockStatus: "InStock",
      isLowStock: false,
    });
    expect(stock.tone).toBe("ok");
    expect(stock.quantity).toBe(30);
  });

  it("STOCKVIS-02 shows out of stock at zero branch availability", () => {
    const stock = resolveCatalogStockDisplay({
      isTracked: true,
      onHandQuantity: 0,
      branchAvailableQuantity: 0,
      unitOfMeasure: "Piece",
      tracksExpiration: false,
      sellableQuantity: null,
      stockStatus: "OutOfStock",
      isLowStock: false,
    });
    expect(stock.tone).toBe("out");
  });

  it("STOCKVIS-05 untracked products are not treated as out of stock", () => {
    const stock = resolveCatalogStockDisplay({
      isTracked: false,
      onHandQuantity: 0,
      branchAvailableQuantity: 0,
      unitOfMeasure: "Piece",
      tracksExpiration: false,
      sellableQuantity: null,
      stockStatus: "OutOfStock",
      isLowStock: false,
    });
    expect(stock.tone).toBe("untracked");
  });

  it("STOCKVIS-09 caps expiration-tracked availability by sellable quantity", () => {
    expect(
      resolveBranchSaleEligibleQuantity({
        isTracked: true,
        onHandQuantity: 10,
        branchAvailableQuantity: 10,
        sellableQuantity: 6,
        tracksExpiration: true,
      }),
    ).toBe(6);
  });

  it("STOCKVIS-10 supports weighted decimal availability", () => {
    const stock = resolveCatalogStockDisplay({
      isTracked: true,
      onHandQuantity: 2.75,
      branchAvailableQuantity: 2.75,
      unitOfMeasure: "Kilogram",
      tracksExpiration: false,
      sellableQuantity: null,
      stockStatus: "InStock",
      isLowStock: false,
    });
    expect(stock.quantity).toBe(2.75);
  });
});
