import { globalCategoriesListRequestPath } from "@/api/global-catalog/category-list-query";
import {
  globalCatalogMultipartRequest,
  globalCatalogMutationRequest,
} from "@/api/global-catalog/global-catalog-http";
import {
  GLOBAL_CATALOG_LOOKUP_PAGE_SIZE,
  GLOBAL_PRODUCT_STATUSES,
  PRODUCT_SELLING_MODES,
  PRODUCT_UNITS,
  type CreateGlobalCategoryInput,
  type CreateGlobalProductInput,
  type GlobalBusinessTypeItem,
  type GlobalCategoryDetail,
  type GlobalCategoryListItem,
  type GlobalCategoryListQuery,
  type GlobalCategoryStatus,
  type GlobalProductDetail,
  type GlobalProductListItem,
  type GlobalProductListQuery,
  type GlobalProductStatus,
  type ProductSellingMode,
  type ProductUnit,
  type UpdateGlobalCategoryInput,
  type UpdateGlobalProductInput,
} from "@/api/global-catalog/global-catalog-types";
import { globalProductsListRequestPath } from "@/api/global-catalog/product-list-query";
import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { withQuery } from "@/lib/http/query-string";

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

function readOptionalString(
  record: Record<string, unknown>,
  ...keys: string[]
): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (value === null || value === undefined) {
      continue;
    }
    if (typeof value === "string") {
      return value;
    }
  }
  return undefined;
}

function readNumber(record: Record<string, unknown>, ...keys: string[]): number | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
  }
  return undefined;
}

function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return false;
}

function readStringArray(record: Record<string, unknown>, ...keys: string[]): string[] {
  for (const key of keys) {
    const value = record[key];
    if (Array.isArray(value)) {
      return value.filter((item): item is string => typeof item === "string");
    }
  }
  return [];
}

function readGuidArray(record: Record<string, unknown>, ...keys: string[]): string[] {
  for (const key of keys) {
    const value = record[key];
    if (Array.isArray(value)) {
      return value
        .map((item) => (typeof item === "string" ? item : null))
        .filter((item): item is string => item != null && item.length > 0);
    }
  }
  return [];
}

function readNullableGuid(record: Record<string, unknown>, ...keys: string[]): string | null {
  for (const key of keys) {
    const value = record[key];
    if (value === null || value === undefined || value === "") {
      return null;
    }
    if (typeof value === "string") {
      return value;
    }
  }
  return null;
}

function asRecord(payload: unknown): Record<string, unknown> | null {
  if (typeof payload !== "object" || payload === null) {
    return null;
  }
  return payload as Record<string, unknown>;
}

function readStatus<T extends string>(
  record: Record<string, unknown>,
  allowed: readonly T[],
  ...keys: string[]
): T {
  const raw = readString(record, ...keys);
  if (raw && (allowed as readonly string[]).includes(raw)) {
    return raw as T;
  }
  throw new Error("Invalid status value.");
}

function readUnit(record: Record<string, unknown>): ProductUnit {
  const raw = readString(record, "unit", "Unit");
  if (raw && (PRODUCT_UNITS as readonly string[]).includes(raw)) {
    return raw as ProductUnit;
  }
  throw new Error("Invalid product unit.");
}

function readSellingMode(record: Record<string, unknown>): ProductSellingMode {
  const raw = readString(record, "sellingMode", "SellingMode") ?? "PerItem";
  if ((PRODUCT_SELLING_MODES as readonly string[]).includes(raw)) {
    return raw as ProductSellingMode;
  }
  return "PerItem";
}

export function mapGlobalBusinessType(payload: unknown): GlobalBusinessTypeItem {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid business type.");
  }
  const id = readString(record, "id", "Id");
  const code = readString(record, "code", "Code");
  const name = readString(record, "name", "Name");
  const status = readString(record, "status", "Status");
  if (!id || !code || !name || !status) {
    throw new Error("Invalid business type.");
  }
  return {
    id,
    code,
    name,
    description: readOptionalString(record, "description", "Description"),
    status,
    sortOrder: readNumber(record, "sortOrder", "SortOrder") ?? 0,
  };
}

export function mapGlobalCategoryListItem(payload: unknown): GlobalCategoryListItem {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid global category.");
  }
  const id = readString(record, "id", "Id");
  const name = readString(record, "name", "Name");
  if (!id || !name) {
    throw new Error("Invalid global category.");
  }
  return {
    id,
    name,
    parentId: readNullableGuid(record, "parentId", "ParentId"),
    iconReference: readOptionalString(record, "iconReference", "IconReference"),
    sortOrder: readNumber(record, "sortOrder", "SortOrder") ?? 0,
    status: readStatus<GlobalCategoryStatus>(
      record,
      ["Active", "Inactive", "Archived"],
      "status",
      "Status",
    ),
    businessTypes: readStringArray(record, "businessTypes", "BusinessTypes"),
    businessTypeIds: readGuidArray(record, "businessTypeIds", "BusinessTypeIds"),
    createdAtUtc: readOptionalString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readOptionalString(record, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

export function mapGlobalProductListItem(payload: unknown): GlobalProductListItem {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid global product.");
  }
  const id = readString(record, "id", "Id");
  const name = readString(record, "name", "Name");
  const sku = readString(record, "sku", "Sku");
  const brand = readString(record, "brand", "Brand");
  if (!id || !name || !sku || !brand) {
    throw new Error("Invalid global product.");
  }
  return {
    id,
    name,
    description: readOptionalString(record, "description", "Description"),
    sku,
    barcode: readOptionalString(record, "barcode", "Barcode"),
    brand,
    globalCategoryId: readNullableGuid(record, "globalCategoryId", "GlobalCategoryId"),
    unit: readUnit(record),
    sellingMode: readSellingMode(record),
    costPrice: readNumber(record, "costPrice", "CostPrice"),
    sellingPrice: readNumber(record, "sellingPrice", "SellingPrice"),
    imageReference: readOptionalString(record, "imageReference", "ImageReference"),
    status: readStatus<GlobalProductStatus>(
      record,
      GLOBAL_PRODUCT_STATUSES,
      "status",
      "Status",
    ),
    searchTags: readStringArray(record, "searchTags", "SearchTags"),
    businessTypes: readStringArray(record, "businessTypes", "BusinessTypes"),
    businessTypeIds: readGuidArray(record, "businessTypeIds", "BusinessTypeIds"),
    hasImage: readBoolean(record, "hasImage", "HasImage"),
    imageVersion: readNumber(record, "imageVersion", "ImageVersion"),
    createdAtUtc: readOptionalString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readOptionalString(record, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

export function listGlobalBusinessTypes(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<PagedResult<GlobalBusinessTypeItem>> {
  return platformRequest<unknown>(baseUrl, {
    path: withQuery("/api/v1/platform/global-catalog/business-types", {
      status: "Active",
      pageSize: GLOBAL_CATALOG_LOOKUP_PAGE_SIZE,
    }),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapGlobalBusinessType),
    };
  });
}

export function listGlobalCategories(
  baseUrl: string,
  query: GlobalCategoryListQuery,
): Promise<PagedResult<GlobalCategoryListItem>> {
  return platformRequest<unknown>(baseUrl, {
    path: globalCategoriesListRequestPath(query),
    signal: query.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapGlobalCategoryListItem),
    };
  });
}

export function getGlobalCategory(
  baseUrl: string,
  categoryId: string,
  signal?: AbortSignal,
): Promise<GlobalCategoryDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/global-catalog/categories/${categoryId}`,
    signal,
  }).then(mapGlobalCategoryListItem);
}

export function createGlobalCategory(
  baseUrl: string,
  input: CreateGlobalCategoryInput,
  signal?: AbortSignal,
): Promise<GlobalCategoryDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/global-catalog/categories",
    body: {
      name: input.name,
      parentId: input.parentId ?? null,
      iconReference: input.iconReference ?? null,
      sortOrder: input.sortOrder ?? 0,
      businessTypeIds: input.businessTypeIds ?? [],
      businessTypes: input.businessTypes ?? [],
    },
    signal,
  }).then(mapGlobalCategoryListItem);
}

export function updateGlobalCategory(
  baseUrl: string,
  categoryId: string,
  input: UpdateGlobalCategoryInput,
  signal?: AbortSignal,
): Promise<GlobalCategoryDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: `/api/v1/platform/global-catalog/categories/${categoryId}`,
    body: {
      name: input.name,
      parentId: input.parentId ?? null,
      iconReference: input.iconReference ?? null,
      sortOrder: input.sortOrder ?? 0,
      businessTypeIds: input.businessTypeIds ?? [],
      businessTypes: input.businessTypes ?? [],
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
    },
    signal,
  }).then(mapGlobalCategoryListItem);
}

export function setGlobalCategoryStatus(
  baseUrl: string,
  categoryId: string,
  status: GlobalCategoryStatus,
  expectedUpdatedAtUtc: string,
  signal?: AbortSignal,
): Promise<GlobalCategoryDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "PATCH",
    path: `/api/v1/platform/global-catalog/categories/${categoryId}/status`,
    body: { status, expectedUpdatedAtUtc },
    signal,
  }).then(mapGlobalCategoryListItem);
}

export function listGlobalProducts(
  baseUrl: string,
  query: GlobalProductListQuery,
): Promise<PagedResult<GlobalProductListItem>> {
  return platformRequest<unknown>(baseUrl, {
    path: globalProductsListRequestPath(query),
    signal: query.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapGlobalProductListItem),
    };
  });
}

export function getGlobalProduct(
  baseUrl: string,
  productId: string,
  signal?: AbortSignal,
): Promise<GlobalProductDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/global-catalog/products/${productId}`,
    signal,
  }).then(mapGlobalProductListItem);
}

export function createGlobalProduct(
  baseUrl: string,
  input: CreateGlobalProductInput,
  signal?: AbortSignal,
): Promise<GlobalProductDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/global-catalog/products",
    body: {
      name: input.name,
      unit: input.unit,
      sku: input.sku,
      barcode: input.barcode ?? null,
      brand: input.brand,
      globalCategoryId: input.globalCategoryId,
      description: input.description ?? null,
      costPrice: input.costPrice ?? null,
      sellingPrice: input.sellingPrice ?? null,
      imageReference: input.imageReference ?? null,
      searchTags: input.searchTags ?? [],
      businessTypeIds: input.businessTypeIds ?? [],
      businessTypes: input.businessTypes ?? [],
      sellingMode: input.sellingMode ?? "PerItem",
    },
    signal,
  }).then(mapGlobalProductListItem);
}

export function updateGlobalProduct(
  baseUrl: string,
  productId: string,
  input: UpdateGlobalProductInput,
  signal?: AbortSignal,
): Promise<GlobalProductDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: `/api/v1/platform/global-catalog/products/${productId}`,
    body: {
      name: input.name,
      unit: input.unit,
      sku: input.sku,
      barcode: input.barcode ?? null,
      brand: input.brand,
      globalCategoryId: input.globalCategoryId,
      description: input.description ?? null,
      costPrice: input.costPrice ?? null,
      sellingPrice: input.sellingPrice ?? null,
      imageReference: input.imageReference ?? null,
      searchTags: input.searchTags ?? [],
      businessTypeIds: input.businessTypeIds ?? [],
      businessTypes: input.businessTypes ?? [],
      sellingMode: input.sellingMode ?? "PerItem",
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
    },
    signal,
  }).then(mapGlobalProductListItem);
}

export function setGlobalProductStatus(
  baseUrl: string,
  productId: string,
  status: GlobalProductStatus,
  expectedUpdatedAtUtc: string,
  signal?: AbortSignal,
): Promise<GlobalProductDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "PATCH",
    path: `/api/v1/platform/global-catalog/products/${productId}/status`,
    body: { status, expectedUpdatedAtUtc },
    signal,
  }).then(mapGlobalProductListItem);
}

export function uploadGlobalProductImage(
  baseUrl: string,
  productId: string,
  file: File,
  signal?: AbortSignal,
): Promise<GlobalProductDetail> {
  const formData = new FormData();
  formData.append("file", file);
  return globalCatalogMultipartRequest<unknown>(baseUrl, {
    method: "PUT",
    path: `/api/v1/platform/global-catalog/products/${productId}/image`,
    formData,
    signal,
  }).then(mapGlobalProductListItem);
}

export function deleteGlobalProductImage(
  baseUrl: string,
  productId: string,
  signal?: AbortSignal,
): Promise<void> {
  return globalCatalogMutationRequest<void>(baseUrl, {
    method: "DELETE",
    path: `/api/v1/platform/global-catalog/products/${productId}/image`,
    signal,
  });
}
