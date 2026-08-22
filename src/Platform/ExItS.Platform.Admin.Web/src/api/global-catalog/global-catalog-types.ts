export const GLOBAL_BUSINESS_TYPE_STATUSES = ["Active", "Inactive", "Archived"] as const;
export type GlobalBusinessTypeStatus = (typeof GLOBAL_BUSINESS_TYPE_STATUSES)[number];

export const GLOBAL_BUSINESS_TYPE_LIST_SORT_BY = [
  "SortOrder",
  "Name",
  "Code",
  "Status",
  "UpdatedAtUtc",
  "CreatedAtUtc",
] as const;
export type GlobalBusinessTypeListSortBy = (typeof GLOBAL_BUSINESS_TYPE_LIST_SORT_BY)[number];

export const GLOBAL_CATEGORY_STATUSES = ["Active", "Inactive", "Archived"] as const;
export type GlobalCategoryStatus = (typeof GLOBAL_CATEGORY_STATUSES)[number];

export const GLOBAL_PRODUCT_STATUSES = ["Draft", "Active", "Archived"] as const;
export type GlobalProductStatus = (typeof GLOBAL_PRODUCT_STATUSES)[number];

export const GLOBAL_CATEGORY_LIST_SORT_BY = [
  "Name",
  "SortOrder",
  "Status",
  "UpdatedAtUtc",
  "CreatedAtUtc",
] as const;
export type GlobalCategoryListSortBy = (typeof GLOBAL_CATEGORY_LIST_SORT_BY)[number];

export const GLOBAL_PRODUCT_LIST_SORT_BY = [
  "Name",
  "Sku",
  "Barcode",
  "Brand",
  "Category",
  "Unit",
  "Status",
  "UpdatedAtUtc",
  "CreatedAtUtc",
  "CostPrice",
  "SellingPrice",
] as const;
export type GlobalProductListSortBy = (typeof GLOBAL_PRODUCT_LIST_SORT_BY)[number];

export const PRODUCT_UNITS = [
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
] as const;
export type ProductUnit = (typeof PRODUCT_UNITS)[number];

export const PRODUCT_SELLING_MODES = ["PerItem", "ByWeight"] as const;
export type ProductSellingMode = (typeof PRODUCT_SELLING_MODES)[number];

export const GLOBAL_PRODUCT_IMAGE_VARIANTS = ["thumb", "medium"] as const;
export type GlobalProductImageVariant = (typeof GLOBAL_PRODUCT_IMAGE_VARIANTS)[number];

export const BUSINESS_TYPE_ASSIGNMENT_MODES = ["Add", "Remove", "Replace"] as const;
export type BusinessTypeAssignmentMode = (typeof BUSINESS_TYPE_ASSIGNMENT_MODES)[number];

export const GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE = 20;
export const GLOBAL_CATEGORY_LIST_PAGE_SIZE = 20;
export const GLOBAL_PRODUCT_LIST_PAGE_SIZE = 20;
export const GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE = 20;
export const GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE = 20;
export const GLOBAL_CATALOG_IMPORT_MAX_FILE_BYTES = 5 * 1024 * 1024;
export const GLOBAL_CATALOG_IMPORT_MAX_ROWS = 5000;
export const GLOBAL_CATALOG_IMPORT_TEMPLATE_FILENAME = "exits-global-product-import-template.csv";
export const GLOBAL_CATALOG_LOOKUP_PAGE_SIZE = 100;

export const GLOBAL_CATALOG_IMPORT_STATUSES = [
  "Validated",
  "Queued",
  "Processing",
  "Completed",
  "CompletedWithWarnings",
  "Failed",
] as const;
export type GlobalCatalogImportStatus = (typeof GLOBAL_CATALOG_IMPORT_STATUSES)[number];

export const GLOBAL_CATALOG_IMPORT_PREVIEW_ITEM_STATUSES = [
  "Valid",
  "ValidWithNewCategory",
  "Pending",
  "Imported",
  "Skipped",
  "Failed",
] as const;
export type GlobalCatalogImportPreviewItemStatus =
  (typeof GLOBAL_CATALOG_IMPORT_PREVIEW_ITEM_STATUSES)[number];

export type GlobalBusinessTypeItem = {
  id: string;
  code: string;
  name: string;
  description?: string;
  status: GlobalBusinessTypeStatus;
  sortOrder: number;
  iconReference?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type GlobalBusinessTypeDetail = GlobalBusinessTypeItem;

export type GlobalBusinessTypeListQuery = {
  page?: number;
  pageSize?: number;
  status?: GlobalBusinessTypeStatus;
  search?: string;
  sortBy?: GlobalBusinessTypeListSortBy;
  sortDesc?: boolean;
  signal?: AbortSignal;
};

export type CreateGlobalBusinessTypeInput = {
  code: string;
  name: string;
  description?: string;
  sortOrder?: number;
  iconReference?: string;
};

export type UpdateGlobalBusinessTypeInput = {
  name: string;
  description?: string;
  sortOrder?: number;
  iconReference?: string;
  expectedUpdatedAtUtc: string;
};

export type GlobalCategoryListItem = {
  id: string;
  name: string;
  parentId: string | null;
  iconReference?: string;
  sortOrder: number;
  status: GlobalCategoryStatus;
  businessTypes: string[];
  businessTypeIds: string[];
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type GlobalCategoryDetail = GlobalCategoryListItem;

export type GlobalProductListItem = {
  id: string;
  name: string;
  description?: string;
  sku: string;
  barcode?: string;
  brand: string;
  globalCategoryId: string | null;
  unit: ProductUnit;
  sellingMode: ProductSellingMode;
  costPrice?: number;
  sellingPrice?: number;
  imageReference?: string;
  status: GlobalProductStatus;
  searchTags: string[];
  businessTypes: string[];
  businessTypeIds: string[];
  hasImage: boolean;
  imageVersion?: number;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type GlobalProductDetail = GlobalProductListItem;

export type GlobalCategoryListQuery = {
  page?: number;
  pageSize?: number;
  status?: GlobalCategoryStatus;
  parentId?: string;
  businessTypeId?: string;
  businessTypeCode?: string;
  search?: string;
  sortBy?: GlobalCategoryListSortBy;
  sortDesc?: boolean;
  signal?: AbortSignal;
};

export type GlobalProductListQuery = {
  page?: number;
  pageSize?: number;
  status?: GlobalProductStatus;
  categoryId?: string;
  businessTypeId?: string;
  businessTypeCode?: string;
  search?: string;
  barcode?: string;
  sku?: string;
  sortBy?: GlobalProductListSortBy;
  sortDesc?: boolean;
  signal?: AbortSignal;
};

export type CreateGlobalCategoryInput = {
  name: string;
  parentId?: string | null;
  iconReference?: string;
  sortOrder?: number;
  businessTypeIds?: string[];
  businessTypes?: string[];
};

export type UpdateGlobalCategoryInput = CreateGlobalCategoryInput & {
  expectedUpdatedAtUtc: string;
};

export type CreateGlobalProductInput = {
  name: string;
  unit: ProductUnit;
  sku: string;
  barcode?: string;
  brand: string;
  globalCategoryId: string;
  description?: string;
  costPrice?: number;
  sellingPrice?: number;
  imageReference?: string;
  searchTags?: string[];
  businessTypeIds?: string[];
  businessTypes?: string[];
  sellingMode?: ProductSellingMode;
};

export type UpdateGlobalProductInput = CreateGlobalProductInput & {
  expectedUpdatedAtUtc: string;
};

export type GlobalCatalogImportListItem = {
  id: string;
  fileName: string;
  fileFormat: string;
  status: GlobalCatalogImportStatus;
  totalCount: number;
  processedCount: number;
  importedCount: number;
  skippedCount: number;
  failedCount: number;
  pendingCount: number;
  validProductCount: number;
  warningCount: number;
  errorSummary?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  completedAtUtc?: string;
};

export type GlobalCatalogImportPreviewItem = {
  id: string;
  rowNumber: number;
  name: string;
  description?: string;
  sku?: string;
  barcode?: string;
  globalCategoryId?: string | null;
  categoryName?: string;
  unit: ProductUnit;
  costPrice?: number;
  sellingPrice?: number;
  imageReference?: string;
  searchTagsRaw?: string;
  businessTypesRaw?: string;
  status: string;
  errorCode?: string;
  errorMessage?: string;
  willCreateCategory: boolean;
  createdGlobalProductId?: string | null;
};

export type GlobalCatalogImportDetail = GlobalCatalogImportListItem & {
  contentType?: string;
  fileSizeBytes: number;
  fileSha256: string;
  idempotencyKey?: string;
  requestedBy: string;
  existingCategoriesReferencedCount: number;
  newCategoriesToCreateCount: number;
  previewSummary?: string;
  currentStage?: string;
  startedAtUtc?: string;
  lastHeartbeatAtUtc?: string;
  previewItems: GlobalCatalogImportPreviewItem[];
  targetTemplateId?: string | null;
  targetTemplateName?: string;
  targetTemplateProductCount?: number;
  estimatedTemplateLinks?: number;
  productsAlreadyInTemplate?: number;
};

export type GlobalCatalogImportErrorItem = {
  id: string;
  rowNumber: number;
  name: string;
  sku?: string;
  barcode?: string;
  status: string;
  errorCode?: string;
  errorMessage?: string;
};

export type GlobalCatalogImportListQuery = {
  page?: number;
  pageSize?: number;
  status?: GlobalCatalogImportStatus;
  signal?: AbortSignal;
};

export type GlobalCatalogImportErrorsQuery = {
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
};

export type UploadGlobalCatalogImportInput = {
  file: File;
  idempotencyKey?: string;
  signal?: AbortSignal;
};

export type ConfirmGlobalCatalogImportInput = {
  idempotencyKey?: string;
  signal?: AbortSignal;
};
