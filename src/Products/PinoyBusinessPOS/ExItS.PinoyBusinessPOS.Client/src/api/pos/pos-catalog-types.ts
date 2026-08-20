/** Hand-typed POS catalog DTOs — TYPED_CLIENT_GENERATION_CONTRACT_MISSING remains open. */

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
