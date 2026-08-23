/** Platform merchant catalog discovery DTOs (camelCase wire). */

export type PlatformMerchantCatalogTemplateSummary = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  iconReference?: string | null;
  primaryBusinessType: string;
  primaryBusinessTypeId: string;
  status: string;
  defaultBatchSize: number;
  selectionMode: string;
  publishedAtUtc?: string | null;
  productCount: number;
  firstBatchCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type PlatformMerchantCatalogTemplateProduct = {
  id: string;
  globalProductId: string;
  sortOrder: number;
  isFeatured: boolean;
  isFirstBatch: boolean;
  productName?: string | null;
  sku?: string | null;
  barcode?: string | null;
  brand?: string | null;
  categoryId?: string | null;
  categoryName?: string | null;
  status?: string | null;
  unit?: string | null;
  sellingMode?: string | null;
  costPrice?: number | null;
  sellingPrice?: number | null;
  hasImage?: boolean;
  imageVersion?: number | null;
};

export type PlatformMerchantCatalogTemplate = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  iconReference?: string | null;
  primaryBusinessType: string;
  primaryBusinessTypeId: string;
  status: string;
  defaultBatchSize: number;
  selectionMode: string;
  publishedAtUtc?: string | null;
  productCount: number;
  firstBatchCount: number;
  products: PlatformMerchantCatalogTemplateProduct[];
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type PlatformMerchantGlobalProduct = {
  id: string;
  name: string;
  description?: string | null;
  sku?: string | null;
  barcode?: string | null;
  brand?: string | null;
  globalCategoryId?: string | null;
  unit: string;
  sellingMode?: string;
  costPrice?: number | null;
  sellingPrice?: number | null;
  imageReference?: string | null;
  status?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  hasImage?: boolean;
  imageVersion?: number | null;
};

export type PlatformMerchantGlobalCategory = {
  id: string;
  name: string;
  parentId?: string | null;
  iconReference?: string | null;
  sortOrder: number;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type PlatformPagedResult<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};
