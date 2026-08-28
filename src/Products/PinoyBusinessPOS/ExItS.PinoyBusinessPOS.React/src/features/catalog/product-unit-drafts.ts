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

export type UnitDraftValidationKey =
  | "catalog.unitValidationBlankRow"
  | "catalog.unitValidationNameLabel"
  | "catalog.unitValidationMultiplier"
  | "catalog.unitValidationSellPrice";

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

export function isUnitDraftBlank(draft: ProductUnitDraft): boolean {
  const nameEmpty = !draft.displayName.trim();
  const labelEmpty = !draft.shortLabel.trim();
  const multiplier = draft.multiplierToBase.trim();
  const multiplierDefault = multiplier === "" || multiplier === "1";

  if (draft.kind === "Purchase") {
    return nameEmpty && labelEmpty && multiplierDefault;
  }

  const price = draft.sellingPrice.trim();
  const priceDefault = price === "" || price === "0";
  return nameEmpty && labelEmpty && multiplierDefault && priceDefault && !draft.allowsCustomQuantity;
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

export function validateUnitDrafts(drafts: ProductUnitDraft[]): UnitDraftValidationKey | null {
  if (drafts.length === 0) {
    return "catalog.unitValidationBlankRow";
  }

  for (const draft of drafts) {
    if (isUnitDraftBlank(draft)) {
      return "catalog.unitValidationBlankRow";
    }
  }

  for (const draft of drafts) {
    if (!draft.displayName.trim() || !draft.shortLabel.trim()) {
      return "catalog.unitValidationNameLabel";
    }

    const multiplier = Number(draft.multiplierToBase);
    if (!(multiplier > 0) || Number.isNaN(multiplier)) {
      return "catalog.unitValidationMultiplier";
    }

    if (draft.kind === "Sell") {
      const price = Number(draft.sellingPrice === "" ? "0" : draft.sellingPrice);
      if (Number.isNaN(price) || price <= 0) {
        return "catalog.unitValidationSellPrice";
      }
    }
  }

  return null;
}
