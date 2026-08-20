import type {
  PosCatalogProductUnitDto,
  PosCatalogProductUnitInput,
} from "@/api/pos/pos-catalog-types";
import type { PosProductUnitKind } from "@/api/pos/pos-catalog-options";

export type ProductUnitDraft = {
  key: string;
  kind: PosProductUnitKind;
  displayName: string;
  shortLabel: string;
  multiplierToBase: string;
  sellingPrice: string;
  allowsCustomQuantity: boolean;
  sortOrder: number;
};

let draftKey = 0;

export function createEmptyUnitDraft(kind: PosProductUnitKind): ProductUnitDraft {
  draftKey += 1;
  return {
    key: `draft-${draftKey}`,
    kind,
    displayName: "",
    shortLabel: "",
    multiplierToBase: kind === "Sell" ? "1" : "1",
    sellingPrice: kind === "Sell" ? "0" : "",
    allowsCustomQuantity: false,
    sortOrder: 0,
  };
}

export function unitsFromDto(
  units: PosCatalogProductUnitDto[] | null | undefined,
): ProductUnitDraft[] {
  if (!units?.length) {
    return [];
  }
  return units
    .filter((unit) => unit.isActive !== false)
    .map((unit) => {
      draftKey += 1;
      return {
        key: `unit-${unit.unitId}-${draftKey}`,
        kind: unit.kind === "Purchase" ? "Purchase" : "Sell",
        displayName: unit.displayName,
        shortLabel: unit.shortLabel,
        multiplierToBase: String(unit.multiplierToBase),
        sellingPrice: unit.sellingPrice == null ? "" : String(unit.sellingPrice),
        allowsCustomQuantity: unit.allowsCustomQuantity,
        sortOrder: unit.sortOrder,
      };
    });
}

export function draftsToUnitInputs(drafts: ProductUnitDraft[]): PosCatalogProductUnitInput[] {
  return drafts.map((draft, index) => {
    const multiplier = Number(draft.multiplierToBase);
    const priceRaw = draft.sellingPrice.trim();
    const sellingPrice =
      draft.kind === "Sell"
        ? Number(priceRaw === "" ? "0" : priceRaw)
        : priceRaw === ""
          ? null
          : Number(priceRaw);

    return {
      kind: draft.kind,
      displayName: draft.displayName.trim(),
      shortLabel: draft.shortLabel.trim(),
      multiplierToBase: multiplier,
      sellingPrice,
      allowsCustomQuantity: draft.allowsCustomQuantity,
      sortOrder: draft.sortOrder || index,
    };
  });
}

export function validateUnitDrafts(drafts: ProductUnitDraft[]): string | null {
  for (const draft of drafts) {
    if (!draft.displayName.trim() || !draft.shortLabel.trim()) {
      return "Each package needs a display name and short label.";
    }
    const multiplier = Number(draft.multiplierToBase);
    if (!(multiplier > 0) || Number.isNaN(multiplier)) {
      return "Multiplier to base must be greater than zero.";
    }
    if (draft.kind === "Sell") {
      const price = Number(draft.sellingPrice === "" ? "0" : draft.sellingPrice);
      if (Number.isNaN(price) || price < 0) {
        return "Sell unit price must be a non-negative number.";
      }
    }
  }
  return null;
}
