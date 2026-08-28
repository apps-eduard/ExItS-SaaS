import { describe, expect, it } from "vitest";

import {
  buildEnableInventoryBody,
  computeOpeningStockValue,
  validateOpeningStockInput,
} from "@/features/catalog/opening-stock-helpers";

describe("opening-stock-helpers", () => {
  it("allows zero stock when add opening stock is unchecked", () => {
    expect(
      validateOpeningStockInput({
        trackStockQuantity: true,
        addOpeningStock: false,
        openingQuantity: "",
        unitCost: "",
        expiryDate: "",
        batchLot: "",
        tracksExpiration: true,
      }),
    ).toBeNull();

    expect(
      buildEnableInventoryBody({
        trackStockQuantity: true,
        addOpeningStock: false,
        openingQuantity: "",
        unitCost: "",
        expiryDate: "",
        batchLot: "",
        tracksExpiration: true,
      }),
    ).toEqual({ openingQuantity: 0 });
  });

  it("requires quantity and unit cost when add opening stock is checked", () => {
    const base = {
      trackStockQuantity: true,
      addOpeningStock: true,
      openingQuantity: "",
      unitCost: "",
      expiryDate: "",
      batchLot: "",
      tracksExpiration: false,
    };

    expect(validateOpeningStockInput(base)).toBe("openingStock.quantityRequired");

    expect(
      validateOpeningStockInput({ ...base, openingQuantity: "0", unitCost: "18" }),
    ).toBe("openingStock.quantityInvalid");

    expect(
      validateOpeningStockInput({ ...base, openingQuantity: "24", unitCost: "" }),
    ).toBe("openingStock.unitCostRequired");

    expect(
      validateOpeningStockInput({ ...base, openingQuantity: "24", unitCost: "0" }),
    ).toBe("openingStock.unitCostInvalid");
  });

  it("requires expiry when expiration tracking is on", () => {
    expect(
      validateOpeningStockInput({
        trackStockQuantity: true,
        addOpeningStock: true,
        openingQuantity: "24",
        unitCost: "18",
        expiryDate: "",
        batchLot: "",
        tracksExpiration: true,
      }),
    ).toBe("openingStock.expiryRequired");
  });

  it("computes opening stock value", () => {
    expect(computeOpeningStockValue(24, 18)).toBe(432);
    expect(computeOpeningStockValue(0, 18)).toBeNull();
  });

  it("builds enable body with optional lot", () => {
    expect(
      buildEnableInventoryBody({
        trackStockQuantity: true,
        addOpeningStock: true,
        openingQuantity: "24",
        unitCost: "18",
        expiryDate: "2027-12-30",
        batchLot: "LOT-A123",
        tracksExpiration: true,
      }),
    ).toEqual({
      openingQuantity: 24,
      unitCost: 18,
      expirationDate: "2027-12-30",
      lotNumber: "LOT-A123",
    });
  });
});
