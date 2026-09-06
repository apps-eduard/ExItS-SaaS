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

export async function listOutgoingStockRequests(
  workspace: PosWorkspaceScope,
  page = 1,
  pageSize = 20,
  signal?: AbortSignal,
) {
  const data = await posRequest<unknown>({
    method: "GET",
    path: `${PATH}/outgoing?page=${page}&pageSize=${pageSize}`,
    workspace,
    signal,
  });
  return stockRequestPagedResultSchema.parse(data);
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
