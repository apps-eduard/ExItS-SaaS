/** Controlled POS catalog codes — mirrors PosCatalogOptions / UnitOfMeasures / SellingModes. */

export const POS_UNIT_OF_MEASURE_CODES = [
  "Piece",
  "Pack",
  "Box",
  "Bottle",
  "Can",
  "Sachet",
  "Kilogram",
  "Gram",
  "Liter",
  "Milliliter",
  "Meter",
] as const;

export type PosUnitOfMeasureCode = (typeof POS_UNIT_OF_MEASURE_CODES)[number];

export const POS_SELLING_MODE_CODES = ["PerItem", "ByWeight"] as const;

export type PosSellingModeCode = (typeof POS_SELLING_MODE_CODES)[number];

export const DEFAULT_CATALOG_UNIT_OF_MEASURE: PosUnitOfMeasureCode = "Piece";
export const DEFAULT_CATALOG_SELLING_MODE: PosSellingModeCode = "PerItem";
export const DEFAULT_CATALOG_SELLING_PRICE = 0;

export const POS_PRODUCT_UNIT_KINDS = ["Purchase", "Sell"] as const;

export type PosProductUnitKind = (typeof POS_PRODUCT_UNIT_KINDS)[number];
