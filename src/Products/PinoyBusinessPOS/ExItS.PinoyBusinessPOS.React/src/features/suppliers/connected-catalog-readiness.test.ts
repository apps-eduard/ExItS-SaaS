import { describe, expect, it } from "vitest";
import type { CatalogProductReadinessItem, CatalogReadinessResult } from "@/api/pos/pos-connected-suppliers-client";
import {
  countByUserState,
  filterReadinessItems,
  mapBackendStatusToUserState,
  resolveCardState,
} from "@/features/suppliers/connected-catalog-readiness";

function item(
  overrides: Partial<CatalogProductReadinessItem> & Pick<CatalogProductReadinessItem, "exposureId" | "status">,
): CatalogProductReadinessItem {
  return {
    supplierProductId: "11111111-1111-4111-8111-111111111111",
    supplierName: "Rice",
    supplierSku: "SKU-1",
    supplierBarcode: null,
    unitOfMeasureCode: "Kilogram",
    poPrice: 45,
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

describe("connected catalog readiness mapping", () => {
  it("never treats missing readiness as New", () => {
    expect(resolveCardState(undefined, false)).toBe("unclassified");
    expect(resolveCardState(undefined, true)).toBe("unclassified");
    expect(mapBackendStatusToUserState(undefined)).toBe("unclassified");
    expect(mapBackendStatusToUserState(null)).toBe("unclassified");
    expect(mapBackendStatusToUserState("")).toBe("unclassified");
  });

  it("maps backend statuses to user-facing states", () => {
    expect(mapBackendStatusToUserState("Ready")).toBe("linked");
    expect(mapBackendStatusToUserState("AlreadyLinked")).toBe("linked");
    expect(mapBackendStatusToUserState("New")).toBe("newProduct");
    expect(mapBackendStatusToUserState("Review")).toBe("checkMatch");
    expect(mapBackendStatusToUserState("Conflict")).toBe("attention");
  });

  it("counters match classified rows", () => {
    const result: CatalogReadinessResult = {
      relationshipId: "22222222-2222-4222-8222-222222222222",
      ready: 2,
      new: 3,
      review: 1,
      conflict: 1,
      items: [
        item({ exposureId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", status: "AlreadyLinked" }),
        item({ exposureId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb", status: "Ready" }),
        item({ exposureId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc", status: "New" }),
        item({ exposureId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd", status: "New" }),
        item({ exposureId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee", status: "New" }),
        item({ exposureId: "ffffffff-ffff-4fff-8fff-ffffffffffff", status: "Review" }),
        item({ exposureId: "99999999-9999-4999-8999-999999999999", status: "Conflict" }),
      ],
    };
    expect(countByUserState(result)).toEqual({
      all: 7,
      linked: 2,
      newProduct: 3,
      checkMatch: 1,
      attention: 1,
    });
    expect(filterReadinessItems(result.items, "linked", "").map((x) => x.status)).toEqual([
      "AlreadyLinked",
      "Ready",
    ]);
    expect(filterReadinessItems(result.items, "newProduct", "").every((x) => x.status === "New")).toBe(
      true,
    );
    expect(filterReadinessItems(result.items, "checkMatch", "").every((x) => x.status === "Review")).toBe(
      true,
    );
    expect(filterReadinessItems(result.items, "attention", "").every((x) => x.status === "Conflict")).toBe(
      true,
    );
  });
});
