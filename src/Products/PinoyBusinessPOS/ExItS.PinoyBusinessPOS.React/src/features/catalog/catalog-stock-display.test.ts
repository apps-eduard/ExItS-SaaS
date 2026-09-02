import { describe, expect, it } from "vitest";
import {
  hasBranchStockStamp,
  resolveBranchSaleEligibleQuantity,
  resolveBranchStockGuardQuantity,
  resolveCatalogStockDisplay,
} from "@/features/catalog/catalog-stock-display";

const branchStampedTracked = {
  isTracked: true as const,
  onHandQuantity: 10,
  organizationOnHandQuantity: 10,
  branchOnHandQuantity: 0,
  branchAvailableQuantity: 0,
  sellableQuantity: null,
  tracksExpiration: false,
  unitOfMeasure: "Piece",
  stockStatus: "OutOfStock",
  isLowStock: false,
};

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

  it("BRANCHLEAK-04 returns 0 when branch available is zero despite org on-hand", () => {
    expect(resolveBranchStockGuardQuantity(branchStampedTracked)).toBe(0);
    const stock = resolveCatalogStockDisplay(branchStampedTracked);
    expect(stock.tone).toBe("out");
    expect(stock.quantity).toBe(0);
  });

  it("BRANCHLEAK-05 does not fall back to organization on-hand when branch availability is missing", () => {
    expect(
      resolveBranchSaleEligibleQuantity({
        isTracked: true,
        onHandQuantity: 10,
        organizationOnHandQuantity: 10,
        branchAvailableQuantity: null,
        sellableQuantity: null,
        tracksExpiration: false,
      }),
    ).toBeNull();
    const stock = resolveCatalogStockDisplay({
      isTracked: true,
      onHandQuantity: 10,
      organizationOnHandQuantity: 10,
      branchAvailableQuantity: null,
      unitOfMeasure: "Piece",
      tracksExpiration: false,
      sellableQuantity: null,
      stockStatus: "InStock",
      isLowStock: false,
    });
    expect(stock.tone).toBe("out");
    expect(stock.quantity).toBe(0);
  });

  it("BRANCHLEAK-01 catalog branch B shows zero/out when org total is ten elsewhere", () => {
    const stock = resolveCatalogStockDisplay({
      ...branchStampedTracked,
      stockStatus: "OutOfStock",
    });
    expect(stock.quantity).toBe(0);
    expect(stock.tone).toBe("out");
  });

  it("BRANCHLEAK-03 catalog branch A shows ten when branch availability is ten", () => {
    const stock = resolveCatalogStockDisplay({
      isTracked: true,
      onHandQuantity: 10,
      organizationOnHandQuantity: 10,
      branchOnHandQuantity: 10,
      branchAvailableQuantity: 10,
      unitOfMeasure: "Piece",
      tracksExpiration: false,
      sellableQuantity: null,
      stockStatus: "InStock",
      isLowStock: false,
    });
    expect(stock.quantity).toBe(10);
    expect(stock.tone).toBe("ok");
  });

  it("BRANCHLEAK-06 branch switch A to B drops availability from ten to zero", () => {
    const branchA = resolveBranchStockGuardQuantity({
      isTracked: true,
      onHandQuantity: 10,
      organizationOnHandQuantity: 10,
      branchOnHandQuantity: 10,
      branchAvailableQuantity: 10,
      sellableQuantity: null,
      tracksExpiration: false,
    });
    const branchB = resolveBranchStockGuardQuantity(branchStampedTracked);
    expect(branchA).toBe(10);
    expect(branchB).toBe(0);
  });

  it("BRANCHLEAK-07 branch switch B to A restores ten", () => {
    const branchB = resolveBranchStockGuardQuantity(branchStampedTracked);
    const branchA = resolveBranchStockGuardQuantity({
      isTracked: true,
      onHandQuantity: 10,
      organizationOnHandQuantity: 10,
      branchOnHandQuantity: 10,
      branchAvailableQuantity: 10,
      sellableQuantity: null,
      tracksExpiration: false,
    });
    expect(branchB).toBe(0);
    expect(branchA).toBe(10);
  });

  it("hasBranchStockStamp detects stamped branch catalog rows", () => {
    expect(hasBranchStockStamp({ organizationOnHandQuantity: 10 })).toBe(true);
    expect(hasBranchStockStamp({})).toBe(false);
  });
});
