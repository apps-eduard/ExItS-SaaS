import { globalBusinessTypesListRequestPath } from "@/api/global-catalog/business-type-list-query";
import { globalCategoriesListRequestPath } from "@/api/global-catalog/category-list-query";
import { globalCatalogImportsListRequestPath } from "@/api/global-catalog/import-list-query";
import {
  globalCatalogTemplateAvailableProductsRequestPath,
  globalCatalogTemplatesListRequestPath,
} from "@/api/global-catalog/template-list-query";
import {
  globalCatalogImportUploadRequest,
  globalCatalogMultipartRequest,
  globalCatalogMutationRequest,
} from "@/api/global-catalog/global-catalog-http";
import {
  GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE,
  GLOBAL_CATALOG_IMPORT_STATUSES,
  GLOBAL_CATALOG_IMPORT_TEMPLATE_FILENAME,
  GLOBAL_CATALOG_LOOKUP_PAGE_SIZE,
  GLOBAL_PRODUCT_STATUSES,
  PRODUCT_SELLING_MODES,
  PRODUCT_UNITS,
  type AssignGlobalCatalogTemplateProductInput,
  type BulkAssignGlobalCatalogTemplateProductsInput,
  type BulkRemoveGlobalCatalogTemplateProductsInput,
  type CatalogTemplateSelectionMode,
  type ConfirmGlobalCatalogImportInput,
  type CreateGlobalBusinessTypeInput,
  type CreateGlobalCategoryInput,
  type CreateGlobalCatalogTemplateInput,
  type CreateGlobalProductInput,
  type GlobalBusinessTypeDetail,
  type GlobalBusinessTypeItem,
  type GlobalBusinessTypeListQuery,
  type GlobalBusinessTypeStatus,
  type GlobalCatalogImportDetail,
  type GlobalCatalogImportErrorItem,
  type GlobalCatalogImportErrorsQuery,
  type GlobalCatalogImportListItem,
  type GlobalCatalogImportListQuery,
  type GlobalCatalogImportPreviewItem,
  type GlobalCatalogImportStatus,
  type GlobalCatalogTemplateAvailableProductsQuery,
  type GlobalCatalogTemplateDetail,
  type GlobalCatalogTemplateListQuery,
  type GlobalCatalogTemplateProduct,
  type GlobalCatalogTemplateStatus,
  type GlobalCatalogTemplateSummary,
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
  type ReorderGlobalCatalogTemplateProductsInput,
  type UpdateGlobalBusinessTypeInput,
  type UpdateGlobalCategoryInput,
  type UpdateGlobalCatalogTemplateInput,
  type UpdateGlobalCatalogTemplateProductFlagsInput,
  type UpdateGlobalProductInput,
  type UploadGlobalCatalogImportInput,
} from "@/api/global-catalog/global-catalog-types";
import { globalProductsListRequestPath } from "@/api/global-catalog/product-list-query";
import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { createCorrelationId, PlatformApiError, platformRequest, type PlatformProblemDetails } from "@/api/platform-http";
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

function readImportStatus(
  record: Record<string, unknown>,
  ...keys: string[]
): GlobalCatalogImportStatus {
  return readStatus<GlobalCatalogImportStatus>(
    record,
    GLOBAL_CATALOG_IMPORT_STATUSES,
    ...keys,
  );
}

export function mapGlobalCatalogImportPreviewItem(
  payload: unknown,
): GlobalCatalogImportPreviewItem {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid import preview item.");
  }
  const id = readString(record, "id", "Id");
  const name = readString(record, "name", "Name");
  const rowNumber = readNumber(record, "rowNumber", "RowNumber");
  if (!id || !name || rowNumber == null) {
    throw new Error("Invalid import preview item.");
  }
  return {
    id,
    rowNumber,
    name,
    description: readOptionalString(record, "description", "Description"),
    sku: readOptionalString(record, "sku", "Sku"),
    barcode: readOptionalString(record, "barcode", "Barcode"),
    globalCategoryId: readNullableGuid(record, "globalCategoryId", "GlobalCategoryId"),
    categoryName: readOptionalString(record, "categoryName", "CategoryName"),
    unit: readUnit(record),
    costPrice: readNumber(record, "costPrice", "CostPrice"),
    sellingPrice: readNumber(record, "sellingPrice", "SellingPrice"),
    imageReference: readOptionalString(record, "imageReference", "ImageReference"),
    searchTagsRaw: readOptionalString(record, "searchTagsRaw", "SearchTagsRaw"),
    businessTypesRaw: readOptionalString(record, "businessTypesRaw", "BusinessTypesRaw"),
    status: readString(record, "status", "Status") ?? "Pending",
    errorCode: readOptionalString(record, "errorCode", "ErrorCode"),
    errorMessage: readOptionalString(record, "errorMessage", "ErrorMessage"),
    willCreateCategory: readBoolean(record, "willCreateCategory", "WillCreateCategory"),
    createdGlobalProductId: readNullableGuid(
      record,
      "createdGlobalProductId",
      "CreatedGlobalProductId",
    ),
  };
}

function mapGlobalCatalogImportListFields(
  record: Record<string, unknown>,
): Omit<GlobalCatalogImportListItem, "id" | "fileName" | "fileFormat" | "status" | "createdAtUtc" | "updatedAtUtc"> {
  return {
    totalCount: readNumber(record, "totalCount", "TotalCount") ?? 0,
    processedCount: readNumber(record, "processedCount", "ProcessedCount") ?? 0,
    importedCount: readNumber(record, "importedCount", "ImportedCount") ?? 0,
    skippedCount: readNumber(record, "skippedCount", "SkippedCount") ?? 0,
    failedCount: readNumber(record, "failedCount", "FailedCount") ?? 0,
    pendingCount: readNumber(record, "pendingCount", "PendingCount") ?? 0,
    validProductCount: readNumber(record, "validProductCount", "ValidProductCount") ?? 0,
    warningCount: readNumber(record, "warningCount", "WarningCount") ?? 0,
    errorSummary: readOptionalString(record, "errorSummary", "ErrorSummary"),
    completedAtUtc: readOptionalString(record, "completedAtUtc", "CompletedAtUtc"),
  };
}

export function mapGlobalCatalogImportListItem(payload: unknown): GlobalCatalogImportListItem {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid import job.");
  }
  const id = readString(record, "id", "Id");
  const fileName = readString(record, "fileName", "FileName");
  const fileFormat = readString(record, "fileFormat", "FileFormat");
  const createdAtUtc = readString(record, "createdAtUtc", "CreatedAtUtc");
  const updatedAtUtc = readString(record, "updatedAtUtc", "UpdatedAtUtc");
  if (!id || !fileName || !fileFormat || !createdAtUtc || !updatedAtUtc) {
    throw new Error("Invalid import job.");
  }
  return {
    id,
    fileName,
    fileFormat,
    status: readImportStatus(record, "status", "Status"),
    createdAtUtc,
    updatedAtUtc,
    ...mapGlobalCatalogImportListFields(record),
  };
}

export function mapGlobalCatalogImportDetail(payload: unknown): GlobalCatalogImportDetail {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid import job.");
  }
  const listItem = mapGlobalCatalogImportListItem(payload);
  const fileSha256 = readString(record, "fileSha256", "FileSha256");
  const requestedBy = readString(record, "requestedBy", "RequestedBy");
  const fileSizeBytes = readNumber(record, "fileSizeBytes", "FileSizeBytes");
  if (!fileSha256 || !requestedBy || fileSizeBytes == null) {
    throw new Error("Invalid import job.");
  }
  const previewRaw = record.previewItems ?? record.PreviewItems;
  const previewItems = Array.isArray(previewRaw)
    ? previewRaw.map(mapGlobalCatalogImportPreviewItem)
    : [];
  return {
    ...listItem,
    contentType: readOptionalString(record, "contentType", "ContentType"),
    fileSizeBytes,
    fileSha256,
    idempotencyKey: readOptionalString(record, "idempotencyKey", "IdempotencyKey"),
    requestedBy,
    existingCategoriesReferencedCount:
      readNumber(record, "existingCategoriesReferencedCount", "ExistingCategoriesReferencedCount") ??
      0,
    newCategoriesToCreateCount:
      readNumber(record, "newCategoriesToCreateCount", "NewCategoriesToCreateCount") ?? 0,
    previewSummary: readOptionalString(record, "previewSummary", "PreviewSummary"),
    currentStage: readOptionalString(record, "currentStage", "CurrentStage"),
    startedAtUtc: readOptionalString(record, "startedAtUtc", "StartedAtUtc"),
    lastHeartbeatAtUtc: readOptionalString(record, "lastHeartbeatAtUtc", "LastHeartbeatAtUtc"),
    previewItems,
    targetTemplateId: readNullableGuid(record, "targetTemplateId", "TargetTemplateId"),
    targetTemplateName: readOptionalString(record, "targetTemplateName", "TargetTemplateName"),
    targetTemplateProductCount: readNumber(
      record,
      "targetTemplateProductCount",
      "TargetTemplateProductCount",
    ),
    estimatedTemplateLinks: readNumber(record, "estimatedTemplateLinks", "EstimatedTemplateLinks"),
    productsAlreadyInTemplate: readNumber(
      record,
      "productsAlreadyInTemplate",
      "ProductsAlreadyInTemplate",
    ),
  };
}

export function mapGlobalCatalogImportErrorItem(payload: unknown): GlobalCatalogImportErrorItem {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid import error item.");
  }
  const id = readString(record, "id", "Id");
  const name = readString(record, "name", "Name");
  const rowNumber = readNumber(record, "rowNumber", "RowNumber");
  if (!id || !name || rowNumber == null) {
    throw new Error("Invalid import error item.");
  }
  return {
    id,
    rowNumber,
    name,
    sku: readOptionalString(record, "sku", "Sku"),
    barcode: readOptionalString(record, "barcode", "Barcode"),
    status: readString(record, "status", "Status") ?? "Failed",
    errorCode: readOptionalString(record, "errorCode", "ErrorCode"),
    errorMessage: readOptionalString(record, "errorMessage", "ErrorMessage"),
  };
}

export function mapGlobalBusinessType(payload: unknown): GlobalBusinessTypeDetail {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid business type.");
  }
  const id = readString(record, "id", "Id");
  const code = readString(record, "code", "Code");
  const name = readString(record, "name", "Name");
  if (!id || !code || !name) {
    throw new Error("Invalid business type.");
  }
  return {
    id,
    code,
    name,
    description: readOptionalString(record, "description", "Description"),
    status: readStatus<GlobalBusinessTypeStatus>(
      record,
      ["Active", "Inactive", "Archived"],
      "status",
      "Status",
    ),
    sortOrder: readNumber(record, "sortOrder", "SortOrder") ?? 0,
    iconReference: readOptionalString(record, "iconReference", "IconReference"),
    createdAtUtc: readOptionalString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readOptionalString(record, "updatedAtUtc", "UpdatedAtUtc"),
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

export function listActiveGlobalBusinessTypes(
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

export function listGlobalBusinessTypes(
  baseUrl: string,
  query: GlobalBusinessTypeListQuery,
): Promise<PagedResult<GlobalBusinessTypeItem>> {
  return platformRequest<unknown>(baseUrl, {
    path: globalBusinessTypesListRequestPath(query),
    signal: query.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapGlobalBusinessType),
    };
  });
}

export function getGlobalBusinessType(
  baseUrl: string,
  businessTypeId: string,
  signal?: AbortSignal,
): Promise<GlobalBusinessTypeDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/global-catalog/business-types/${businessTypeId}`,
    signal,
  }).then(mapGlobalBusinessType);
}

export function createGlobalBusinessType(
  baseUrl: string,
  input: CreateGlobalBusinessTypeInput,
  signal?: AbortSignal,
): Promise<GlobalBusinessTypeDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/global-catalog/business-types",
    body: {
      code: input.code,
      name: input.name,
      description: input.description ?? null,
      sortOrder: input.sortOrder ?? 0,
      iconReference: input.iconReference ?? null,
    },
    signal,
  }).then(mapGlobalBusinessType);
}

export function updateGlobalBusinessType(
  baseUrl: string,
  businessTypeId: string,
  input: UpdateGlobalBusinessTypeInput,
  signal?: AbortSignal,
): Promise<GlobalBusinessTypeDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: `/api/v1/platform/global-catalog/business-types/${businessTypeId}`,
    body: {
      name: input.name,
      description: input.description ?? null,
      sortOrder: input.sortOrder ?? 0,
      iconReference: input.iconReference ?? null,
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
    },
    signal,
  }).then(mapGlobalBusinessType);
}

export function setGlobalBusinessTypeStatus(
  baseUrl: string,
  businessTypeId: string,
  status: GlobalBusinessTypeStatus,
  expectedUpdatedAtUtc: string,
  signal?: AbortSignal,
): Promise<GlobalBusinessTypeDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/global-catalog/business-types/${businessTypeId}/status`,
    body: { status, expectedUpdatedAtUtc },
    signal,
  }).then(mapGlobalBusinessType);
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

export function listGlobalCatalogImports(
  baseUrl: string,
  query: GlobalCatalogImportListQuery,
): Promise<PagedResult<GlobalCatalogImportListItem>> {
  return platformRequest<unknown>(baseUrl, {
    path: globalCatalogImportsListRequestPath(query),
    signal: query.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapGlobalCatalogImportListItem),
    };
  });
}

export function getGlobalCatalogImport(
  baseUrl: string,
  jobId: string,
  signal?: AbortSignal,
): Promise<GlobalCatalogImportDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/global-catalog/products/imports/${jobId}`,
    signal,
  }).then(mapGlobalCatalogImportDetail);
}

export function uploadGlobalCatalogImport(
  baseUrl: string,
  input: UploadGlobalCatalogImportInput,
): Promise<GlobalCatalogImportDetail> {
  const formData = new FormData();
  formData.append("file", input.file);
  if (input.idempotencyKey) {
    formData.append("idempotencyKey", input.idempotencyKey);
  }
  return globalCatalogImportUploadRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/global-catalog/products/imports",
    formData,
    idempotencyKey: input.idempotencyKey,
    signal: input.signal,
  }).then(mapGlobalCatalogImportDetail);
}

export function confirmGlobalCatalogImport(
  baseUrl: string,
  jobId: string,
  input: ConfirmGlobalCatalogImportInput = {},
): Promise<GlobalCatalogImportDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/global-catalog/products/imports/${jobId}/confirm`,
    body: input.idempotencyKey ? { idempotencyKey: input.idempotencyKey } : {},
    signal: input.signal,
  }).then(mapGlobalCatalogImportDetail);
}

export function listGlobalCatalogImportErrors(
  baseUrl: string,
  jobId: string,
  query: GlobalCatalogImportErrorsQuery = {},
): Promise<PagedResult<GlobalCatalogImportErrorItem>> {
  return platformRequest<unknown>(baseUrl, {
    path: withQuery(`/api/v1/platform/global-catalog/products/imports/${jobId}/errors`, {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE,
    }),
    signal: query.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapGlobalCatalogImportErrorItem),
    };
  });
}

export async function downloadGlobalCatalogImportTemplate(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<{ blob: Blob; fileName: string }> {
  const requestCorrelationId = createCorrelationId();
  const response = await fetch(
    `${baseUrl}/api/v1/platform/global-catalog/products/imports/template.csv`,
    {
      method: "GET",
      credentials: "include",
      headers: {
        Accept: "text/csv",
        "X-Correlation-Id": requestCorrelationId,
      },
      signal,
    },
  );

  if (!response.ok) {
    let problem: PlatformProblemDetails = { status: response.status };
    try {
      const payload = await response.json();
      if (typeof payload === "object" && payload !== null) {
        const record = payload as Record<string, unknown>;
        problem = {
          ...problem,
          detail: typeof record.detail === "string" ? record.detail : undefined,
          title: typeof record.title === "string" ? record.title : undefined,
        };
      }
    } catch {
      // Non-JSON error bodies still surface as status-only problems.
    }
    throw new PlatformApiError(response.status, problem, requestCorrelationId);
  }

  const disposition = response.headers.get("Content-Disposition");
  const fileNameMatch = disposition?.match(/filename="?([^";]+)"?/i);
  const fileName = fileNameMatch?.[1] ?? GLOBAL_CATALOG_IMPORT_TEMPLATE_FILENAME;
  const blob = await response.blob();
  return { blob, fileName };
}

const CATALOG_TEMPLATE_STATUSES = ["Draft", "Published", "Archived"] as const;
const CATALOG_TEMPLATE_SELECTION_MODES = ["Curated", "Auto", "Hybrid"] as const;

function readSelectionMode(record: Record<string, unknown>): CatalogTemplateSelectionMode {
  const raw = readString(record, "selectionMode", "SelectionMode") ?? "Curated";
  if ((CATALOG_TEMPLATE_SELECTION_MODES as readonly string[]).includes(raw)) {
    return raw as CatalogTemplateSelectionMode;
  }
  return "Curated";
}

export function mapGlobalCatalogTemplateProduct(payload: unknown): GlobalCatalogTemplateProduct {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid template product.");
  }
  const id = readString(record, "id", "Id");
  const globalProductId = readString(record, "globalProductId", "GlobalProductId");
  const sortOrder = readNumber(record, "sortOrder", "SortOrder");
  if (!id || !globalProductId || sortOrder == null) {
    throw new Error("Invalid template product.");
  }
  return {
    id,
    globalProductId,
    sortOrder,
    isFeatured: readBoolean(record, "isFeatured", "IsFeatured"),
    isFirstBatch: readBoolean(record, "isFirstBatch", "IsFirstBatch"),
    productName: readOptionalString(record, "productName", "ProductName"),
    sku: readOptionalString(record, "sku", "Sku"),
    barcode: readOptionalString(record, "barcode", "Barcode"),
    brand: readOptionalString(record, "brand", "Brand"),
    categoryId: readNullableGuid(record, "categoryId", "CategoryId"),
    categoryName: readOptionalString(record, "categoryName", "CategoryName"),
    status: readOptionalString(record, "status", "Status"),
    unit: readOptionalString(record, "unit", "Unit"),
    sellingMode: readOptionalString(record, "sellingMode", "SellingMode"),
    costPrice: readNumber(record, "costPrice", "CostPrice"),
    sellingPrice: readNumber(record, "sellingPrice", "SellingPrice"),
    hasImage: readBoolean(record, "hasImage", "HasImage"),
    imageVersion: readNumber(record, "imageVersion", "ImageVersion"),
  };
}

function mapGlobalCatalogTemplateSummaryFields(
  record: Record<string, unknown>,
): Omit<GlobalCatalogTemplateSummary, "id" | "name" | "slug" | "createdAtUtc" | "updatedAtUtc"> {
  const primaryBusinessType = readString(record, "primaryBusinessType", "PrimaryBusinessType");
  const primaryBusinessTypeId = readString(record, "primaryBusinessTypeId", "PrimaryBusinessTypeId");
  if (!primaryBusinessType || !primaryBusinessTypeId) {
    throw new Error("Invalid catalog template.");
  }
  return {
    description: readOptionalString(record, "description", "Description"),
    iconReference: readOptionalString(record, "iconReference", "IconReference"),
    primaryBusinessType,
    primaryBusinessTypeId,
    status: readStatus<GlobalCatalogTemplateStatus>(
      record,
      CATALOG_TEMPLATE_STATUSES,
      "status",
      "Status",
    ),
    defaultBatchSize: readNumber(record, "defaultBatchSize", "DefaultBatchSize") ?? 0,
    selectionMode: readSelectionMode(record),
    publishedAtUtc: readOptionalString(record, "publishedAtUtc", "PublishedAtUtc"),
    productCount: readNumber(record, "productCount", "ProductCount") ?? 0,
    firstBatchCount: readNumber(record, "firstBatchCount", "FirstBatchCount") ?? 0,
  };
}

export function mapGlobalCatalogTemplateSummary(payload: unknown): GlobalCatalogTemplateSummary {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid catalog template.");
  }
  const id = readString(record, "id", "Id");
  const name = readString(record, "name", "Name");
  const slug = readString(record, "slug", "Slug");
  const createdAtUtc = readString(record, "createdAtUtc", "CreatedAtUtc");
  const updatedAtUtc = readString(record, "updatedAtUtc", "UpdatedAtUtc");
  if (!id || !name || !slug || !createdAtUtc || !updatedAtUtc) {
    throw new Error("Invalid catalog template.");
  }
  return {
    id,
    name,
    slug,
    createdAtUtc,
    updatedAtUtc,
    ...mapGlobalCatalogTemplateSummaryFields(record),
  };
}

export function mapGlobalCatalogTemplateDetail(payload: unknown): GlobalCatalogTemplateDetail {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid catalog template.");
  }
  const summary = mapGlobalCatalogTemplateSummary(payload);
  const productsRaw = record.products ?? record.Products;
  const products = Array.isArray(productsRaw)
    ? productsRaw.map(mapGlobalCatalogTemplateProduct)
    : [];
  return {
    ...summary,
    products,
  };
}

export function listGlobalCatalogTemplates(
  baseUrl: string,
  query: GlobalCatalogTemplateListQuery,
): Promise<PagedResult<GlobalCatalogTemplateSummary>> {
  return platformRequest<unknown>(baseUrl, {
    path: globalCatalogTemplatesListRequestPath(query),
    signal: query.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapGlobalCatalogTemplateSummary),
    };
  });
}

export function getGlobalCatalogTemplate(
  baseUrl: string,
  templateId: string,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/global-catalog/templates/${templateId}`,
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function createGlobalCatalogTemplate(
  baseUrl: string,
  input: CreateGlobalCatalogTemplateInput,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/global-catalog/templates",
    body: {
      name: input.name,
      primaryBusinessType: input.primaryBusinessType ?? null,
      primaryBusinessTypeId: input.primaryBusinessTypeId ?? null,
      slug: input.slug ?? null,
      description: input.description ?? null,
      iconReference: input.iconReference ?? null,
      defaultBatchSize: input.defaultBatchSize ?? null,
      selectionMode: input.selectionMode ?? null,
    },
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function updateGlobalCatalogTemplate(
  baseUrl: string,
  templateId: string,
  input: UpdateGlobalCatalogTemplateInput,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: `/api/v1/platform/global-catalog/templates/${templateId}`,
    body: {
      name: input.name,
      primaryBusinessType: input.primaryBusinessType ?? null,
      primaryBusinessTypeId: input.primaryBusinessTypeId ?? null,
      slug: input.slug ?? null,
      description: input.description ?? null,
      iconReference: input.iconReference ?? null,
      defaultBatchSize: input.defaultBatchSize ?? null,
      selectionMode: input.selectionMode ?? null,
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
    },
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

function lifecycleBody(expectedUpdatedAtUtc?: string) {
  return expectedUpdatedAtUtc ? { expectedUpdatedAtUtc } : {};
}

export function publishGlobalCatalogTemplate(
  baseUrl: string,
  templateId: string,
  expectedUpdatedAtUtc?: string,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/global-catalog/templates/${templateId}/publish`,
    body: lifecycleBody(expectedUpdatedAtUtc),
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function unpublishGlobalCatalogTemplate(
  baseUrl: string,
  templateId: string,
  expectedUpdatedAtUtc?: string,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/global-catalog/templates/${templateId}/unpublish`,
    body: lifecycleBody(expectedUpdatedAtUtc),
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function archiveGlobalCatalogTemplate(
  baseUrl: string,
  templateId: string,
  expectedUpdatedAtUtc?: string,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/global-catalog/templates/${templateId}/archive`,
    body: lifecycleBody(expectedUpdatedAtUtc),
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function assignGlobalCatalogTemplateProduct(
  baseUrl: string,
  templateId: string,
  input: AssignGlobalCatalogTemplateProductInput,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/global-catalog/templates/${templateId}/products`,
    body: {
      globalProductId: input.globalProductId,
      isFeatured: input.isFeatured ?? false,
      isFirstBatch: input.isFirstBatch ?? false,
      sortOrder: input.sortOrder ?? null,
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc ?? null,
    },
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function bulkAssignGlobalCatalogTemplateProducts(
  baseUrl: string,
  templateId: string,
  input: BulkAssignGlobalCatalogTemplateProductsInput,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/global-catalog/templates/${templateId}/products/bulk`,
    body: {
      globalProductIds: input.globalProductIds,
      isFeatured: input.isFeatured ?? false,
      isFirstBatch: input.isFirstBatch ?? false,
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc ?? null,
    },
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function bulkRemoveGlobalCatalogTemplateProducts(
  baseUrl: string,
  templateId: string,
  input: BulkRemoveGlobalCatalogTemplateProductsInput,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/global-catalog/templates/${templateId}/products/bulk-remove`,
    body: {
      globalProductIds: input.globalProductIds,
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc ?? null,
    },
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function removeGlobalCatalogTemplateProduct(
  baseUrl: string,
  templateId: string,
  productId: string,
  expectedUpdatedAtUtc?: string,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "DELETE",
    path: withQuery(
      `/api/v1/platform/global-catalog/templates/${templateId}/products/${productId}`,
      { expectedUpdatedAtUtc },
    ),
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function updateGlobalCatalogTemplateProductFlags(
  baseUrl: string,
  templateId: string,
  productId: string,
  input: UpdateGlobalCatalogTemplateProductFlagsInput,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "PATCH",
    path: `/api/v1/platform/global-catalog/templates/${templateId}/products/${productId}`,
    body: {
      isFeatured: input.isFeatured ?? null,
      isFirstBatch: input.isFirstBatch ?? null,
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc ?? null,
    },
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function reorderGlobalCatalogTemplateProducts(
  baseUrl: string,
  templateId: string,
  input: ReorderGlobalCatalogTemplateProductsInput,
  signal?: AbortSignal,
): Promise<GlobalCatalogTemplateDetail> {
  return globalCatalogMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: `/api/v1/platform/global-catalog/templates/${templateId}/products/order`,
    body: {
      orderedGlobalProductIds: input.orderedGlobalProductIds,
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc ?? null,
    },
    signal,
  }).then(mapGlobalCatalogTemplateDetail);
}

export function listGlobalCatalogTemplateAvailableProducts(
  baseUrl: string,
  templateId: string,
  query: GlobalCatalogTemplateAvailableProductsQuery,
): Promise<PagedResult<GlobalProductListItem>> {
  return platformRequest<unknown>(baseUrl, {
    path: globalCatalogTemplateAvailableProductsRequestPath(templateId, query),
    signal: query.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapGlobalProductListItem),
    };
  });
}
