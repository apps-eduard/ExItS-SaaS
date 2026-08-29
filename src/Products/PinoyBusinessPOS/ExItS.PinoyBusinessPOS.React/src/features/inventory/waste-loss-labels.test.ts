import { describe, expect, it } from "vitest";
import type { PosInventoryLotDto } from "@/api/pos/pos-inventory-client";
import {
  formatWasteLossOccurredDate,
  isWasteLossReasonCode,
  sortLotsForWasteLoss,
  sumWasteLossLineCosts,
  wasteLossCostStatusLabelKey,
  wasteLossReasonLabelKey,
  wasteLossStatusLabelKey,
} from "@/features/inventory/waste-loss-labels";

function lot(partial: Partial<PosInventoryLotDto> & Pick<PosInventoryLotDto, "lotId">): PosInventoryLotDto {
  return {
    productId: "22222222-2222-2222-2222-222222222222",
    expirationDate: "2026-12-01",
    quantityOnHand: 5,
    expiryStatus: "Ok",
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...partial,
  };
}

describe("waste-loss-labels", () => {
  it("maps reason codes to i18n keys", () => {
    expect(wasteLossReasonLabelKey("Spoiled")).toBe("wasteLoss.reason.spoiled");
    expect(wasteLossReasonLabelKey("Expired")).toBe("wasteLoss.reason.expired");
    expect(wasteLossReasonLabelKey("Damaged")).toBe("wasteLoss.reason.damaged");
    expect(wasteLossReasonLabelKey("Broken")).toBe("wasteLoss.reason.broken");
    expect(wasteLossReasonLabelKey("Spillage")).toBe("wasteLoss.reason.spillage");
    expect(wasteLossReasonLabelKey("MissingOrShrinkage")).toBe(
      "wasteLoss.reason.missingOrShrinkage",
    );
    expect(wasteLossReasonLabelKey("Other")).toBe("wasteLoss.reason.other");
    expect(wasteLossReasonLabelKey("Unknown")).toBe("wasteLoss.reason.other");
  });

  it("maps status and cost status codes", () => {
    expect(wasteLossStatusLabelKey("Posted")).toBe("wasteLoss.status.posted");
    expect(wasteLossStatusLabelKey("Voided")).toBe("wasteLoss.status.voided");
    expect(wasteLossCostStatusLabelKey("Complete")).toBe("wasteLoss.costComplete");
    expect(wasteLossCostStatusLabelKey("Partial")).toBe("wasteLoss.costPartial");
    expect(wasteLossCostStatusLabelKey("Unavailable")).toBe("wasteLoss.costUnavailable");
  });

  it("validates reason codes", () => {
    expect(isWasteLossReasonCode("Expired")).toBe(true);
    expect(isWasteLossReasonCode("Nope")).toBe(false);
  });

  it("sums line costs only when all authoritative", () => {
    expect(sumWasteLossLineCosts([{ lineCostSnapshot: 10 }, { lineCostSnapshot: 5 }])).toBe(15);
    expect(sumWasteLossLineCosts([{ lineCostSnapshot: 10 }, { lineCostSnapshot: null }])).toBe(
      null,
    );
    expect(sumWasteLossLineCosts([])).toBe(null);
  });

  it("formats occurred dates", () => {
    const formatted = formatWasteLossOccurredDate("2026-08-29T12:00:00.000Z");
    expect(formatted.length).toBeGreaterThan(0);
    expect(formatWasteLossOccurredDate("not-a-date")).toBe("not-a-date");
  });

  it("prioritizes expired lots when reason is Expired", () => {
    const lots = [
      lot({ lotId: "a", expirationDate: "2026-12-01", expiryStatus: "Ok" }),
      lot({ lotId: "b", expirationDate: "2026-01-01", expiryStatus: "Expired" }),
      lot({ lotId: "c", expirationDate: "2026-02-01", expiryStatus: "Expired" }),
    ];
    expect(sortLotsForWasteLoss(lots, false).map((entry) => entry.lotId)).toEqual([
      "b",
      "c",
      "a",
    ]);
    expect(sortLotsForWasteLoss(lots, true).map((entry) => entry.lotId)).toEqual([
      "b",
      "c",
      "a",
    ]);
  });
});
