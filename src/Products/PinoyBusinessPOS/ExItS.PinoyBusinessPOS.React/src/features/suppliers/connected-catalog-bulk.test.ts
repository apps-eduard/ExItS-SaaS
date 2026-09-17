import { describe, expect, it } from "vitest";
import type { CatalogProductReadinessItem } from "@/api/pos/pos-connected-suppliers-client";
import {
  canBulkAddAsNew,
  canBulkConfirmMatch,
  isBulkConnectSelectable,
  partitionBulkConnectSelection,
} from "@/features/suppliers/connected-catalog-bulk";

function item(
  overrides: Partial<CatalogProductReadinessItem> &
    Pick<CatalogProductReadinessItem, "exposureId" | "status">,
): CatalogProductReadinessItem {
  return {
    supplierProductId: "11111111-1111-4111-8111-111111111111",
    supplierName: "Sample",
    supplierSku: "SKU-1",
    supplierBarcode: null,
    unitOfMeasureCode: "Piece",
    poPrice: 10,
    canAutoLink: false,
    candidateBuyerProductId: null,
    candidateBuyerProductName: null,
    nameMatched: false,
    skuMatched: false,
    barcodeMatched: false,
    unitCompatible: false,
    matchDetails: "",
    linkedBuyerProductId: null,
    conflictCandidates: [],
    ...overrides,
  };
}

describe("connected-catalog-bulk", () => {
  it("allows multi-select only for New and Check match", () => {
    expect(isBulkConnectSelectable(item({ exposureId: "a", status: "New" }))).toBe(true);
    expect(isBulkConnectSelectable(item({ exposureId: "b", status: "Review" }))).toBe(true);
    expect(isBulkConnectSelectable(item({ exposureId: "c", status: "Conflict" }))).toBe(false);
    expect(isBulkConnectSelectable(item({ exposureId: "d", status: "Ready" }))).toBe(false);
  });

  it("partitions selected rows into add-as-new vs confirm-match", () => {
    const newItem = item({ exposureId: "n1", status: "New" });
    const reviewWithCandidate = item({
      exposureId: "r1",
      status: "Review",
      candidateBuyerProductId: "22222222-2222-4222-8222-222222222222",
      candidateBuyerProductName: "Match",
    });
    const reviewNoCandidate = item({ exposureId: "r2", status: "Review" });

    expect(canBulkAddAsNew(newItem)).toBe(true);
    expect(canBulkAddAsNew(reviewWithCandidate)).toBe(true);
    expect(canBulkConfirmMatch(reviewWithCandidate)).toBe(true);
    expect(canBulkConfirmMatch(reviewNoCandidate)).toBe(false);

    const partitioned = partitionBulkConnectSelection(
      [newItem, reviewWithCandidate, reviewNoCandidate],
      new Set(["n1", "r1", "r2"]),
    );
    expect(partitioned.addAsNew.map((x) => x.exposureId)).toEqual(["n1", "r1", "r2"]);
    expect(partitioned.confirmMatch.map((x) => x.exposureId)).toEqual(["r1"]);
  });
});
