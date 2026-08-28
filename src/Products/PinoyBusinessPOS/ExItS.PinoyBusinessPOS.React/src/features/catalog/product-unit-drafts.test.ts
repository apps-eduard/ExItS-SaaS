import { describe, expect, it } from "vitest";
import {
  createEmptyUnitDraft,
  draftsToUnitInputs,
  isUnitDraftBlank,
  validateUnitDrafts,
} from "@/features/catalog/product-unit-drafts";

describe("product-unit-drafts", () => {
  it("maps rice-style independent sell prices without multiplying", () => {
    const kg = createEmptyUnitDraft("Sell");
    kg.displayName = "Kilogram";
    kg.shortLabel = "kg";
    kg.multiplierToBase = "1";
    kg.sellingPrice = "55";

    const sack = createEmptyUnitDraft("Sell");
    sack.displayName = "Sack 50kg";
    sack.shortLabel = "sack";
    sack.multiplierToBase = "50";
    sack.sellingPrice = "2600";

    const inputs = draftsToUnitInputs([kg, sack]);
    expect(inputs[0]?.sellingPrice).toBe(55);
    expect(inputs[1]?.sellingPrice).toBe(2600);
    expect(inputs[1]?.multiplierToBase).toBe(50);
  });

  it("rejects invalid multipliers", () => {
    const draft = createEmptyUnitDraft("Purchase");
    draft.displayName = "Box";
    draft.shortLabel = "box";
    draft.multiplierToBase = "0";
    expect(validateUnitDrafts([draft])).toBe("catalog.unitValidationMultiplier");
  });

  it("rejects blank package rows", () => {
    const blank = createEmptyUnitDraft("Purchase");
    expect(isUnitDraftBlank(blank)).toBe(true);
    expect(validateUnitDrafts([blank])).toBe("catalog.unitValidationBlankRow");
  });

  it("rejects sell packages without a positive price", () => {
    const sell = createEmptyUnitDraft("Sell");
    sell.displayName = "Piece";
    sell.shortLabel = "pc";
    sell.multiplierToBase = "1";
    sell.sellingPrice = "0";
    expect(validateUnitDrafts([sell])).toBe("catalog.unitValidationSellPrice");
  });

  it("maps piece to box of 12", () => {
    const box = createEmptyUnitDraft("Sell");
    box.displayName = "Box of 12";
    box.shortLabel = "box";
    box.multiplierToBase = "12";
    box.sellingPrice = "110";
    const [input] = draftsToUnitInputs([box]);
    expect(input?.multiplierToBase).toBe(12);
    expect(input?.sellingPrice).toBe(110);
  });
});
