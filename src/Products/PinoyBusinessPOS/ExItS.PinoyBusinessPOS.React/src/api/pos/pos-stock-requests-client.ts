import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const PATH = "/api/v1/pos/inventory/stock-requests";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const stockRequestLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  lineNumber: z.number(),
  requestedQuantity: z.number(),
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
  return z
    .object({
      transferId: guidSchema,
      stockRequestId: guidSchema.nullable().optional(),
      transferNumber: z.string().nullable().optional(),
      status: z.string(),
    })
    .passthrough()
    .parse(data);
}
