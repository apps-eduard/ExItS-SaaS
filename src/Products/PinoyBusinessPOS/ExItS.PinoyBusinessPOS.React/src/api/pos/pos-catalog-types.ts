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
  /** Branch-effective sell price when branch context is supplied; otherwise null. */
  effectiveSellingPrice?: number | null;
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

export type CatalogProductScopeCode = "OrganizationStandard" | "BranchLocal";

export type PosCatalogProductDto = {
  productId: string;
  organizationId: string;
  name: string;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: string | null;
  brandId?: string | null;
  brandName?: string | null;
  unitOfMeasure: string;
  sellingMode: string;
  sellingPrice: number;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  canBeSold?: boolean;
  /** Resale | Ingredient | InternalUse | ProducedItem — prefer over canBeSold when present. */
  businessUsage?: string | null;
  canBeUsedAsIngredient?: boolean | null;
  isProduced?: boolean | null;
  usagePreset?: string | null;
  hasImage?: boolean;
  imageVersion?: number | null;
  imageSource?: string | null;
  units?: PosCatalogProductUnitDto[] | null;
  tracksExpiration?: boolean;
  expirationWarningDays?: number | null;
  /** Mirrors inventory IsTracked when catalog list/detail includes stock snapshot. */
  isTracked?: boolean;
  /** Branch sale-eligible quantity when branch context is stamped; otherwise org snapshot. */
  onHandQuantity?: number;
  stockStatus?: string;
  /** Organization aggregate on-hand when branch stock is stamped. */
  organizationOnHandQuantity?: number | null;
  /** Branch physical on-hand when branch stock is stamped. */
  branchOnHandQuantity?: number | null;
  /** Branch on-hand minus reservations when branch stock is stamped. */
  branchAvailableQuantity?: number | null;
  /** FEFO sellable quantity when expiration-tracked and branch context is supplied. */
  sellableQuantity?: number | null;
  /** Low-stock flag for branch context when stamped. */
  isLowStock?: boolean | null;
  /** OrganizationStandard | BranchLocal (MB2 product governance). Unknown values handled defensively. */
  scope?: CatalogProductScopeCode | string;
  /** Origin branch for BranchLocal; audit-only after promotion. Server-derived. */
  originBranchId?: string | null;
  /**
   * When workspace branch is present on management list/detail: offered at that branch.
   * When commerciallyOffered=true list: true for all returned items.
   */
  isOfferedAtBranch?: boolean | null;
  /** Branch-effective base selling price when branch context is supplied; otherwise null. */
  effectiveSellingPrice?: number | null;
  hasBranchPriceOverride?: boolean | null;
};

export type PosProductCategoryDto = {
  categoryId: string;
  organizationId: string;
  name: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type PosProductBrandDto = {
  brandId: string;
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

export type PosProductBrandPagedResult = {
  items: PosProductBrandDto[];
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

export type CreatePosProductBrandRequest = {
  name: string;
  brandId?: string | null;
};

export type UpdatePosProductBrandRequest = {
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
  brandId?: string | null;
  productId?: string | null;
  sellingMode?: PosSellingModeCode | string | null;
  canBeSold?: boolean | null;
  businessUsage?: string | null;
  units?: PosCatalogProductUnitInput[] | null;
  tracksExpiration?: boolean;
  expirationWarningDays?: number | null;
  /** OrganizationStandard | BranchLocal. Origin branch is server-derived — do not send originBranchId. */
  scope?: CatalogProductScopeCode | string | null;
};

export type UpdatePosCatalogProductRequest = {
  name: string;
  unitOfMeasure: PosUnitOfMeasureCode | string;
  sellingPrice: number;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  categoryId?: string | null;
  brandId?: string | null;
  expectedUpdatedAtUtc?: string | null;
  sellingMode?: PosSellingModeCode | string | null;
  canBeSold?: boolean | null;
  businessUsage?: string | null;
  units?: PosCatalogProductUnitInput[] | null;
  tracksExpiration?: boolean | null;
  expirationWarningDays?: number | null;
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

export type SetBranchProductAvailabilityRequest = {
  isOffered: boolean;
};

export type ProductBranchOfferingItemDto = {
  branchId: string;
  isOffered: boolean;
  reason: string;
  hasExplicitOverride: boolean;
};

export type ProductBranchAvailabilityReadDto = {
  productId: string;
  scope: CatalogProductScopeCode | string;
  originBranchId?: string | null;
  /** Sparse overrides only; merge with Platform Active branches for Standard. */
  explicitRows: ProductBranchOfferingItemDto[];
};

/** Advisory duplicate-name check (MB2-01C-H1). Server create/update remain authoritative. */
export type CatalogProductNameConflictDto = {
  isDuplicate: boolean;
  canRevealExisting: boolean;
  existingProduct?: PosCatalogProductDto | null;
};

export type BranchProductPricingItemDto = {
  productUnitId?: string | null;
  organizationDefaultPrice: number;
  branchOverridePrice?: number | null;
  effectivePrice: number;
  hasBranchPriceOverride: boolean;
};

export type BranchProductPricingDto = {
  productId: string;
  branchId: string;
  basePrice: BranchProductPricingItemDto;
  unitPrices: BranchProductPricingItemDto[];
};

export type SetBranchProductPriceOverrideRequest = {
  branchId: string;
  sellingPrice: number;
  productUnitId?: string | null;
};
