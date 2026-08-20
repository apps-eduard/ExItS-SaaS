/** Hand-typed POS catalog DTOs — TYPED_CLIENT_GENERATION_CONTRACT_MISSING remains open. */

import {
  DEFAULT_CATALOG_SELLING_MODE,
  DEFAULT_CATALOG_SELLING_PRICE,
  DEFAULT_CATALOG_UNIT_OF_MEASURE,
  type PosProductUnitKind,
  type PosSellingModeCode,
  type PosUnitOfMeasureCode,
} from "@/api/pos/pos-catalog-options";

export {
  DEFAULT_CATALOG_SELLING_MODE,
  DEFAULT_CATALOG_SELLING_PRICE,
  DEFAULT_CATALOG_UNIT_OF_MEASURE,
};

export type PosCatalogProductUnitDto = {
  unitId: string;
  productId: string;
  kind: string;
  displayName: string;
  shortLabel: string;
  multiplierToBase: number;
  sellingPrice?: number | null;
  allowsCustomQuantity: boolean;
  isActive: boolean;
  sortOrder: number;
};

export type PosCatalogProductUnitInput = {
  kind: PosProductUnitKind | string;
  displayName: string;
  shortLabel: string;
  multiplierToBase: number;
  sellingPrice?: number | null;
  allowsCustomQuantity?: boolean;
  sortOrder?: number;
  /** Prefer omit on update replace — server soft-deactivates then inserts. */
  unitId?: string | null;
};

export type PosCatalogProductDto = {
  productId: string;
  organizationId: string;
  name: string;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: string | null;
  unitOfMeasure: string;
  sellingMode: string;
  sellingPrice: number;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  canBeSold?: boolean;
  hasImage?: boolean;
  imageVersion?: number | null;
  imageSource?: string | null;
  units?: PosCatalogProductUnitDto[] | null;
};

export type PosProductCategoryDto = {
  categoryId: string;
  organizationId: string;
  name: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type PosCatalogProductPagedResult = {
  items: PosCatalogProductDto[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type PosProductCategoryPagedResult = {
  items: PosProductCategoryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type CreatePosProductCategoryRequest = {
  name: string;
  categoryId?: string | null;
};

export type UpdatePosProductCategoryRequest = {
  name: string;
  expectedUpdatedAtUtc?: string | null;
};

export type CreatePosCatalogProductRequest = {
  name: string;
  unitOfMeasure: PosUnitOfMeasureCode | string;
  sellingPrice: number;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: string | null;
  productId?: string | null;
  sellingMode?: PosSellingModeCode | string | null;
  canBeSold?: boolean | null;
  units?: PosCatalogProductUnitInput[] | null;
};

export type UpdatePosCatalogProductRequest = {
  name: string;
  unitOfMeasure: PosUnitOfMeasureCode | string;
  sellingPrice: number;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: string | null;
  expectedUpdatedAtUtc?: string | null;
  sellingMode?: PosSellingModeCode | string | null;
  canBeSold?: boolean | null;
  units?: PosCatalogProductUnitInput[] | null;
};

export type UpdatePosCatalogProductPriceItem = {
  productId: string;
  sellingPrice: number;
  expectedUpdatedAtUtc: string;
};

export type UpdatePosCatalogProductPricesRequest = {
  items: UpdatePosCatalogProductPriceItem[];
};

export type UpdatePosCatalogProductPriceResultItem = {
  productId: string;
  succeeded: boolean;
  changed: boolean;
  product?: PosCatalogProductDto | null;
  errorCode?: string | null;
  errorMessage?: string | null;
};

export type UpdatePosCatalogProductPricesResponse = {
  results: UpdatePosCatalogProductPriceResultItem[];
  succeededCount: number;
  failedCount: number;
  changedCount: number;
};

export type CatalogProductImageVariant = "thumb" | "medium";
