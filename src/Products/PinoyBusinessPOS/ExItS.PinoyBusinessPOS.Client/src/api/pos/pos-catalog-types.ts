/** Hand-typed POS catalog DTOs — TYPED_CLIENT_GENERATION_CONTRACT_MISSING remains open. */

/** API create still requires UOM + price; RMAP-04 UI sends defaults (editors deferred to RMAP-05/06). */
export const DEFAULT_CATALOG_UNIT_OF_MEASURE = "Piece";
export const DEFAULT_CATALOG_SELLING_PRICE = 0;

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
  unitOfMeasure: string;
  sellingPrice: number;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: string | null;
  productId?: string | null;
  sellingMode?: string | null;
  canBeSold?: boolean | null;
};

export type UpdatePosCatalogProductRequest = {
  name: string;
  unitOfMeasure: string;
  sellingPrice: number;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: string | null;
  expectedUpdatedAtUtc?: string | null;
  sellingMode?: string | null;
  canBeSold?: boolean | null;
};

export type CatalogProductImageVariant = "thumb" | "medium";
