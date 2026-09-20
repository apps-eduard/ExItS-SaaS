import { describe, expect, it } from "vitest";
import {
  actionableShareProductIds,
  isShareRowSelectable,
  resolvePoPrice,
  rowCanShare,
  rowCanStopSharing,
  rowHasValidPoPrice,
  shareRowHintReason,
  shareRowNeedsPrice,
} from "@/features/suppliers/connected-share-row-actionability";

describe("connected-share-row-actionability", () => {
  const sharedPriced = {
    supplierProductId: "apple",
    canShare: false,
    canStopSharing: true,
    isInventoryTracked: true,
    isEligible: true,
    isEffectivelyShared: true,
    hasValidPoPrice: true,
    sellingPrice: 200,
    defaultPoPrice: null,
    resolvedPoPrice: 200,
    sharingStatus: "Shared",
  };

  const sharedMissingFlagsButPriced = {
    supplierProductId: "apple-stale-dto",
    // Simulates older/partial DTO: Shared + selling price, flags defaulted false
    canShare: false,
    canStopSharing: false,
    isInventoryTracked: true,
    isEligible: true,
    isEffectivelyShared: true,
    hasValidPoPrice: false,
    sellingPrice: 200,
    defaultPoPrice: null,
    resolvedPoPrice: null,
    sharingStatus: "Shared",
  };

  const excludedPriced = {
    supplierProductId: "banana",
    canShare: true,
    canStopSharing: false,
    isInventoryTracked: true,
    isEligible: true,
    isEffectivelyShared: false,
    hasValidPoPrice: true,
    sellingPrice: 90,
    defaultPoPrice: null,
    resolvedPoPrice: 90,
    sharingStatus: "Excluded",
  };

  const excludedNoPrice = {
    supplierProductId: "zero",
    canShare: false,
    canStopSharing: false,
    isInventoryTracked: true,
    isEligible: true,
    isEffectivelyShared: false,
    hasValidPoPrice: false,
    sellingPrice: 0,
    defaultPoPrice: null,
    resolvedPoPrice: null,
    sharingStatus: "Excluded",
  };

  const untracked = {
    supplierProductId: "battery",
    canShare: false,
    canStopSharing: false,
    isInventoryTracked: false,
    isEligible: false,
    isEffectivelyShared: false,
    hasValidPoPrice: true,
    sellingPrice: 50,
    sharingStatus: "Ineligible",
  };

  it("A: SellingPrice 200 with null DefaultPo resolves and is not Needs price", () => {
    expect(resolvePoPrice(sharedMissingFlagsButPriced)).toBe(200);
    expect(rowHasValidPoPrice(sharedMissingFlagsButPriced)).toBe(true);
    expect(shareRowNeedsPrice(sharedMissingFlagsButPriced)).toBe(false);
  });

  it("Shared + priced stays selectable for Stop sharing even when canStopSharing flag false", () => {
    expect(rowCanStopSharing(sharedMissingFlagsButPriced)).toBe(true);
    expect(isShareRowSelectable(sharedMissingFlagsButPriced)).toBe(true);
    expect(shareRowHintReason(sharedMissingFlagsButPriced)).toBeNull();
  });

  it("D: Shared + missing price still canStopSharing", () => {
    const sharedNoPrice = {
      ...sharedPriced,
      hasValidPoPrice: false,
      sellingPrice: 0,
      resolvedPoPrice: null,
      defaultPoPrice: null,
      canStopSharing: true,
    };
    expect(rowCanStopSharing(sharedNoPrice)).toBe(true);
    expect(shareRowNeedsPrice(sharedNoPrice)).toBe(false);
    expect(isShareRowSelectable(sharedNoPrice)).toBe(true);
  });

  it("E: Shared + valid price checkbox enabled for Stop sharing", () => {
    expect(rowCanStopSharing(sharedPriced)).toBe(true);
    expect(rowCanShare(sharedPriced)).toBe(false);
    expect(isShareRowSelectable(sharedPriced)).toBe(true);
  });

  it("F: Excluded + valid price canShare", () => {
    expect(rowCanShare(excludedPriced)).toBe(true);
    expect(isShareRowSelectable(excludedPriced)).toBe(true);
  });

  it("G: Excluded + missing price Needs price and cannot Share", () => {
    expect(rowCanShare(excludedNoPrice)).toBe(false);
    expect(shareRowNeedsPrice(excludedNoPrice)).toBe(true);
    expect(shareRowHintReason(excludedNoPrice)).toBe("needsPrice");
  });

  it("Select All includes Shared stoppable rows (not all-disabled)", () => {
    const ids = actionableShareProductIds([
      sharedMissingFlagsButPriced,
      excludedPriced,
      excludedNoPrice,
      untracked,
    ]);
    expect(ids).toEqual(["apple-stale-dto", "banana"]);
  });

  it("ineligible remains disabled with tracking hint", () => {
    expect(isShareRowSelectable(untracked)).toBe(false);
    expect(shareRowHintReason(untracked)).toBe("needsTracking");
  });
});
