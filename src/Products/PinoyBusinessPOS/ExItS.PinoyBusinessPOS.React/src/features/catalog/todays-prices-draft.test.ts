import { describe, expect, it } from "vitest";
import {
  applySuccessfulPriceSave,
  canSavePriceDraft,
  isPriceDraftDirty,
  mergePriceDraftMap,
  parseDraftPrice,
  type PriceDraft,
} from "@/features/catalog/todays-prices-draft";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";

function product(
  overrides: Partial<PosCatalogProductDto> & Pick<PosCatalogProductDto, "productId" | "name" | "sellingPrice">,
): PosCatalogProductDto {
  return {
    organizationId: "org",
    unitOfMeasure: "Piece",
    sellingMode: "PerItem",
    status: "Active",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function draft(overrides: Partial<PriceDraft> = {}): PriceDraft {
  return {
    productId: "a",
    name: "Bath Soap Bar",
    brandName: null,
    currentPrice: 28,
    draftPrice: "28",
    expectedUpdatedAtUtc: "token-a",
    rowError: null,
    ...overrides,
  };
}

describe("todays-prices-draft", () => {
  it("parseDraftPrice accepts 0 and rejects negatives / empty / excess decimals", () => {
    expect(parseDraftPrice("0")).toEqual({ ok: true, value: 0 });
    expect(parseDraftPrice("30.5")).toEqual({ ok: true, value: 30.5 });
    expect(parseDraftPrice("")).toEqual({ ok: false, reason: "empty" });
    expect(parseDraftPrice("-1")).toEqual({ ok: false, reason: "invalid" });
    expect(parseDraftPrice("1.234")).toEqual({ ok: false, reason: "invalid" });
    expect(parseDraftPrice("abc")).toEqual({ ok: false, reason: "invalid" });
  });

  it("dirty and canSave treat empty-as-zero carefully", () => {
    expect(isPriceDraftDirty(draft({ draftPrice: "" }))).toBe(true);
    expect(canSavePriceDraft(draft({ draftPrice: "" }))).toBe(false);
    expect(canSavePriceDraft(draft({ draftPrice: "0" }))).toBe(true);
    expect(canSavePriceDraft(draft({ draftPrice: "28" }))).toBe(false);
    expect(canSavePriceDraft(draft({ draftPrice: "30" }))).toBe(true);
  });

  it("merge preserves dirty draft and token while refreshing current price", () => {
    const previous = {
      a: draft({ draftPrice: "30", expectedUpdatedAtUtc: "token-a" }),
      b: draft({ productId: "b", name: "Biscuit", currentPrice: 15, draftPrice: "15", expectedUpdatedAtUtc: "token-b" }),
    };
    const merged = mergePriceDraftMap(previous, [
      product({ productId: "a", name: "Bath Soap Bar", sellingPrice: 29, updatedAtUtc: "token-a-new" }),
      product({ productId: "b", name: "Biscuit Pack", sellingPrice: 16, updatedAtUtc: "token-b-new" }),
    ]);

    expect(merged.a.draftPrice).toBe("30");
    expect(merged.a.expectedUpdatedAtUtc).toBe("token-a");
    expect(merged.a.currentPrice).toBe(29);
    expect(merged.b.draftPrice).toBe("16");
    expect(merged.b.currentPrice).toBe(16);
    expect(merged.b.expectedUpdatedAtUtc).toBe("token-b-new");
  });

  it("merge retains dirty draft when product temporarily absent from search results", () => {
    const previous = {
      a: draft({ draftPrice: "30" }),
    };
    const merged = mergePriceDraftMap(previous, [
      product({ productId: "b", name: "Biscuit", sellingPrice: 15 }),
    ]);
    expect(merged.a.draftPrice).toBe("30");
    expect(merged.b.currentPrice).toBe(15);
  });

  it("applySuccessfulPriceSave canonicalizes price and token", () => {
    const next = applySuccessfulPriceSave(draft({ draftPrice: "30" }), 30, "token-saved");
    expect(next.currentPrice).toBe(30);
    expect(next.draftPrice).toBe("30");
    expect(next.expectedUpdatedAtUtc).toBe("token-saved");
    expect(next.rowError).toBeNull();
    expect(isPriceDraftDirty(next)).toBe(false);
  });
});
