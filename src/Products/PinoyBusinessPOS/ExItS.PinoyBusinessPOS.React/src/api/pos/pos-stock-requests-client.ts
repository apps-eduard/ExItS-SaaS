import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";
import { inventoryTransferDtoSchema } from "@/api/pos/pos-inventory-transfer-client";

const PATH = "/api/v1/pos/inventory/stock-requests";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const stockRequestLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  lineNumber: z.number(),
  requestedQuantity: z.number(),
  approvedQuantity: z.number().nullable().optional(),
  fulfilledQuantity: z.number(),
  inProgressQuantity: z.number(),
  nameSnapshot: z.string(),
  unitOfMeasure: z.string(),
});

export const stockRequestLinkedTransferDtoSchema = z.object({
  transferId: guidSchema,
  transferNumber: z.string().nullable().optional(),
  status: z.string(),
  totalSentQty: z.number(),
  totalReceivedQty: z.number(),
  updatedAtUtc: z.string(),
});

export const stockRequestDtoSchema = z.object({
  stockRequestId: guidSchema,
  organizationId: guidSchema,
  destinationLocationId: guidSchema,
  destinationLocationName: z.string().nullable().optional(),
  requestedSourceLocationId: guidSchema,
  requestedSourceLocationName: z.string().nullable().optional(),
  requestNumber: z.string().nullable().optional(),
  status: z.string(),
  notes: z.string().nullable().optional(),
  requestedBy: guidSchema,
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  approvedBy: guidSchema.nullable().optional(),
  approvedAtUtc: z.string().nullable().optional(),
  preparingStartedBy: guidSchema.nullable().optional(),
  preparingStartedAtUtc: z.string().nullable().optional(),
  dispatchedBy: guidSchema.nullable().optional(),
  dispatchedAtUtc: z.string().nullable().optional(),
  linkedInventoryTransferId: guidSchema.nullable().optional(),
  rejectedBy: guidSchema.nullable().optional(),
  rejectedAtUtc: z.string().nullable().optional(),
  rejectionReason: z.string().nullable().optional(),
  cancelledBy: guidSchema.nullable().optional(),
  cancelledAtUtc: z.string().nullable().optional(),
  lines: z.array(stockRequestLineDtoSchema),
  linkedTransfers: z.array(stockRequestLinkedTransferDtoSchema),
});

export const stockRequestListItemDtoSchema = z.object({
  stockRequestId: guidSchema,
  requestNumber: z.string().nullable().optional(),
  status: z.string(),
  destinationLocationId: guidSchema,
  destinationLocationName: z.string().nullable().optional(),
  requestedSourceLocationId: guidSchema,
  requestedSourceLocationName: z.string().nullable().optional(),
  lineCount: z.number(),
  updatedAtUtc: z.string(),
});

export const stockRequestPagedResultSchema = z.object({
  items: z.array(stockRequestListItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type StockRequestDto = z.infer<typeof stockRequestDtoSchema>;
export type StockRequestListItemDto = z.infer<typeof stockRequestListItemDtoSchema>;

export type CreateStockRequestBody = {
  destinationLocationId: string;
  requestedSourceLocationId: string;
  lines: { productId: string; requestedQuantity: number }[];
  notes?: string | null;
};

export type ApproveStockRequestBody = {
  lineApprovals: { productId: string; approvedQuantity: number }[];
};

export type FulfillStockRequestBody = {
  lines: { productId: string; quantity: number; sourceLotId?: string | null }[];
  notes?: string | null;
};

export const stockRequestOutgoingSummaryDtoSchema = z.object({
  submittedCount: z.number(),
  inProgressCount: z.number(),
  inTransitCount: z.number(),
  recent: z.array(stockRequestListItemDtoSchema),
});

export const replenishmentCatalogItemDtoSchema = z.object({
  productId: guidSchema,
  name: z.string(),
  sku: z.string().nullable().optional(),
  barcode: z.string().nullable().optional(),
  categoryId: guidSchema.nullable().optional(),
  categoryName: z.string().nullable().optional(),
  unitOfMeasure: z.string(),
  branchOnHandQuantity: z.number(),
  warehouseAvailableQuantity: z.number(),
  isLowStock: z.boolean(),
  isTracked: z.boolean(),
});

export const replenishmentCatalogResultDtoSchema = z.object({
  items: z.array(replenishmentCatalogItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  supplyWarehouseBranchId: guidSchema,
  supplyWarehouseName: z.string().nullable().optional(),
});

export type StockRequestOutgoingSummaryDto = z.infer<typeof stockRequestOutgoingSummaryDtoSchema>;
export type ReplenishmentCatalogItemDto = z.infer<typeof replenishmentCatalogItemDtoSchema>;
export type ReplenishmentCatalogResultDto = z.infer<typeof replenishmentCatalogResultDtoSchema>;

export type ListOutgoingStockRequestsOptions = {
  page?: number;
  pageSize?: number;
  statuses?: readonly string[];
  signal?: AbortSignal;
};

export type ReplenishmentCatalogOptions = {
  supplyWarehouseBranchId: string;
  search?: string;
  stockFilter?: "all" | "low" | "out";
  categoryId?: string | null;
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
};

export async function listOutgoingStockRequests(
  workspace: PosWorkspaceScope,
  pageOrOptions: number | ListOutgoingStockRequestsOptions = 1,
  pageSize = 20,
  signal?: AbortSignal,
) {
  const options: ListOutgoingStockRequestsOptions =
    typeof pageOrOptions === "number"
      ? { page: pageOrOptions, pageSize, signal }
      : pageOrOptions;
  const page = options.page ?? 1;
  const size = options.pageSize ?? 20;
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(size),
  });
  if (options.statuses && options.statuses.length > 0) {
    params.set("statuses", options.statuses.join(","));
  }
  const data = await posRequest<unknown>({
    method: "GET",
    path: `${PATH}/outgoing?${params.toString()}`,
    workspace,
    signal: options.signal,
  });
  return stockRequestPagedResultSchema.parse(data);
}

export async function getOutgoingStockRequestSummary(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<StockRequestOutgoingSummaryDto> {
  const data = await posRequest<unknown>({
    method: "GET",
    path: `${PATH}/outgoing/summary`,
    workspace,
    signal,
  });
  return stockRequestOutgoingSummaryDtoSchema.parse(data);
}

export async function listReplenishmentCatalog(
  workspace: PosWorkspaceScope,
  options: ReplenishmentCatalogOptions,
): Promise<ReplenishmentCatalogResultDto> {
  const params = new URLSearchParams({
    supplyWarehouseBranchId: options.supplyWarehouseBranchId,
    page: String(options.page ?? 1),
    pageSize: String(options.pageSize ?? 40),
    stockFilter: options.stockFilter ?? "all",
  });
  if (options.search?.trim()) {
    params.set("search", options.search.trim());
  }
  if (options.categoryId) {
    params.set("categoryId", options.categoryId);
  }
  const data = await posRequest<unknown>({
    method: "GET",
    path: `${PATH}/replenishment-catalog?${params.toString()}`,
    workspace,
    signal: options.signal,
  });
  return replenishmentCatalogResultDtoSchema.parse(data);
}

export async function listIncomingStockRequests(
  workspace: PosWorkspaceScope,
  page = 1,
  pageSize = 20,
  signal?: AbortSignal,
) {
  const data = await posRequest<unknown>({
    method: "GET",
    path: `${PATH}/incoming?page=${page}&pageSize=${pageSize}`,
    workspace,
    signal,
  });
  return stockRequestPagedResultSchema.parse(data);
}

export async function getStockRequest(
  workspace: PosWorkspaceScope,
  stockRequestId: string,
  signal?: AbortSignal,
): Promise<StockRequestDto> {
  const data = await posRequest<unknown>({
    method: "GET",
    path: `${PATH}/${stockRequestId}`,
    workspace,
    signal,
  });
  return stockRequestDtoSchema.parse(data);
}

export async function createStockRequest(
  workspace: PosWorkspaceScope,
  body: CreateStockRequestBody,
  signal?: AbortSignal,
) {
  const operationId = crypto.randomUUID();
  const headers = await buildPosMutationIdempotencyHeaders(
    operationId,
    JSON.stringify(body),
    OFFLINE_OPERATION_TYPES.StockRequestCreate,
  );
  const data = await posRequest<unknown>({
    method: "POST",
    path: PATH,
    workspace,
    signal,
    headers,
    body,
  });
  return stockRequestDtoSchema.parse(data);
}

export async function approveStockRequest(
  workspace: PosWorkspaceScope,
  stockRequestId: string,
  body: ApproveStockRequestBody,
  signal?: AbortSignal,
) {
  const headers = await buildPosMutationIdempotencyHeaders(
    crypto.randomUUID(),
    JSON.stringify(body),
    OFFLINE_OPERATION_TYPES.StockRequestApprove,
  );
  const data = await posRequest<unknown>({
    method: "POST",
    path: `${PATH}/${stockRequestId}/approve`,
    workspace,
    signal,
    headers,
    body,
  });
  return stockRequestDtoSchema.parse(data);
}

export async function prepareStockRequest(
  workspace: PosWorkspaceScope,
  stockRequestId: string,
  signal?: AbortSignal,
) {
  const body = {};
  const headers = await buildPosMutationIdempotencyHeaders(
    crypto.randomUUID(),
    JSON.stringify(body),
    OFFLINE_OPERATION_TYPES.StockRequestPrepare,
  );
  const data = await posRequest<unknown>({
    method: "POST",
    path: `${PATH}/${stockRequestId}/prepare`,
    workspace,
    signal,
    headers,
    body,
  });
  return stockRequestDtoSchema.parse(data);
}

export async function dispatchStockRequest(
  workspace: PosWorkspaceScope,
  stockRequestId: string,
  signal?: AbortSignal,
) {
  const body = {};
  const headers = await buildPosMutationIdempotencyHeaders(
    crypto.randomUUID(),
    JSON.stringify(body),
    OFFLINE_OPERATION_TYPES.StockRequestDispatch,
  );
  const data = await posRequest<unknown>({
    method: "POST",
    path: `${PATH}/${stockRequestId}/dispatch`,
    workspace,
    signal,
    headers,
    body,
  });
  return inventoryTransferDtoSchema.parse(data);
}

export async function rejectStockRequest(
  workspace: PosWorkspaceScope,
  stockRequestId: string,
  reason: string,
  signal?: AbortSignal,
) {
  const body = { reason };
  const headers = await buildPosMutationIdempotencyHeaders(
    crypto.randomUUID(),
    JSON.stringify(body),
    OFFLINE_OPERATION_TYPES.StockRequestReject,
  );
  const data = await posRequest<unknown>({
    method: "POST",
    path: `${PATH}/${stockRequestId}/reject`,
    workspace,
    signal,
    headers,
    body,
  });
  return stockRequestDtoSchema.parse(data);
}

export async function cancelStockRequest(
  workspace: PosWorkspaceScope,
  stockRequestId: string,
  signal?: AbortSignal,
) {
  const body = {};
  const headers = await buildPosMutationIdempotencyHeaders(
    crypto.randomUUID(),
    JSON.stringify(body),
    OFFLINE_OPERATION_TYPES.StockRequestCancel,
  );
  const data = await posRequest<unknown>({
    method: "POST",
    path: `${PATH}/${stockRequestId}/cancel`,
    workspace,
    signal,
    headers,
    body,
  });
  return stockRequestDtoSchema.parse(data);
}

/** @deprecated Prefer dispatchStockRequest — legacy endpoint delegates to dispatch. */
export async function fulfillStockRequestViaTransfer(
  workspace: PosWorkspaceScope,
  stockRequestId: string,
  body: FulfillStockRequestBody,
  signal?: AbortSignal,
) {
  const headers = await buildPosMutationIdempotencyHeaders(
    crypto.randomUUID(),
    JSON.stringify(body),
    OFFLINE_OPERATION_TYPES.StockRequestFulfillTransfer,
  );
  const data = await posRequest<unknown>({
    method: "POST",
    path: `${PATH}/${stockRequestId}/fulfill-transfer`,
    workspace,
    signal,
    headers,
    body,
  });
  return inventoryTransferDtoSchema.passthrough().parse(data);
}
