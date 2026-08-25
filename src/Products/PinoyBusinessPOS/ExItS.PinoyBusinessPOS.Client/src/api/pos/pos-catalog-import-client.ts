import { posRequest, type PosWorkspaceScope } from "@/api/pos/pos-http";
import type {
  ImportSelectedProductsRequest,
  ImportTemplateBatchRequest,
  ImportedGlobalProducts,
  PosCatalogImportItemPaged,
  PosCatalogImportJob,
  PosTemplateImportStatus,
} from "@/api/pos/pos-catalog-import-types";

const IMPORTS_PATH = "/api/v1/pos/catalog-imports";

function appendQuery(path: string, params: Record<string, string | number | undefined>): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  const serialized = query.toString();
  return serialized ? `${path}?${serialized}` : path;
}

function newIdempotencyKey(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `import-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

/** POST /api/v1/pos/catalog-imports/template */
export function importTemplateBatch(
  workspace: PosWorkspaceScope,
  request: ImportTemplateBatchRequest,
  signal?: AbortSignal,
): Promise<PosCatalogImportJob> {
  const body: ImportTemplateBatchRequest = {
    platformTemplateId: request.platformTemplateId,
    batchNumber: request.batchNumber ?? 1,
    idempotencyKey: request.idempotencyKey ?? newIdempotencyKey(),
  };
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${IMPORTS_PATH}/template`,
    body,
  });
}

/** POST /api/v1/pos/catalog-imports/template/{templateId}/next-batch */
export function importTemplateNextBatch(
  workspace: PosWorkspaceScope,
  templateId: string,
  request?: Partial<ImportTemplateBatchRequest>,
  signal?: AbortSignal,
): Promise<PosCatalogImportJob> {
  const body: ImportTemplateBatchRequest = {
    platformTemplateId: templateId,
    batchNumber: request?.batchNumber,
    idempotencyKey: request?.idempotencyKey ?? newIdempotencyKey(),
  };
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${IMPORTS_PATH}/template/${templateId}/next-batch`,
    body,
  });
}

/** POST /api/v1/pos/catalog-imports/products */
export function importSelectedGlobalProducts(
  workspace: PosWorkspaceScope,
  request: ImportSelectedProductsRequest,
  signal?: AbortSignal,
): Promise<PosCatalogImportJob> {
  const body: ImportSelectedProductsRequest = {
    platformGlobalProductIds: request.platformGlobalProductIds,
    idempotencyKey: request.idempotencyKey ?? newIdempotencyKey(),
  };
  return posRequest({
    method: "POST",
    workspace,
    signal,
    path: `${IMPORTS_PATH}/products`,
    body,
  });
}

/** GET /api/v1/pos/catalog-imports/templates/{templateId}/status */
export function getTemplateImportStatus(
  workspace: PosWorkspaceScope,
  templateId: string,
  signal?: AbortSignal,
): Promise<PosTemplateImportStatus> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${IMPORTS_PATH}/templates/${templateId}/status`,
  });
}

/** GET /api/v1/pos/catalog-imports/imported-global-products?ids= */
export function listImportedGlobalProducts(
  workspace: PosWorkspaceScope,
  platformGlobalProductIds: string[],
  signal?: AbortSignal,
): Promise<ImportedGlobalProducts> {
  const ids = [...new Set(platformGlobalProductIds.filter(Boolean))];
  if (ids.length === 0) {
    return Promise.resolve({ importedIds: [] });
  }
  const query = ids.map((id) => `ids=${encodeURIComponent(id)}`).join("&");
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${IMPORTS_PATH}/imported-global-products?${query}`,
  });
}

/** GET /api/v1/pos/catalog-imports/{jobId} */
export function getCatalogImportJob(
  workspace: PosWorkspaceScope,
  jobId: string,
  signal?: AbortSignal,
): Promise<PosCatalogImportJob> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: `${IMPORTS_PATH}/${jobId}`,
  });
}

/** GET /api/v1/pos/catalog-imports/{jobId}/items */
export function listCatalogImportJobItems(
  workspace: PosWorkspaceScope,
  jobId: string,
  options: { status?: string; page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosCatalogImportItemPaged> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${IMPORTS_PATH}/${jobId}/items`, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
      status: options.status,
    }),
  });
}
