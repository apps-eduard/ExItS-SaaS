import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const PATH = "/api/v1/pos/inventory/transfers";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const INVENTORY_TRANSFER_STATUSES = [
  "Draft",
  "InTransit",
  "PartiallyReceived",
  "Received",
  "Cancelled",
] as const;
export type InventoryTransferStatusCode = (typeof INVENTORY_TRANSFER_STATUSES)[number];

/** Exact backend InventoryTransferDiscrepancyReason codes. */
export const INVENTORY_TRANSFER_DISCREPANCY_REASONS = [
  "ShortShipment",
  "Damaged",
  "LostInTransit",
  "WrongItem",
  "Other",
] as const;
export type InventoryTransferDiscrepancyReasonCode =
  (typeof INVENTORY_TRANSFER_DISCREPANCY_REASONS)[number];

export const INVENTORY_TRANSFER_DIRECTIONS = ["outgoing", "incoming"] as const;
export type InventoryTransferDirection = (typeof INVENTORY_TRANSFER_DIRECTIONS)[number];

export const inventoryTransferLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  productName: z.string(),
  unitOfMeasure: z.string(),
  lineNumber: z.number(),
  sentQty: z.number(),
  receivedQty: z.number(),
  differenceQty: z.number(),
  lineStatus: z.string(),
  discrepancyReason: z.string().nullable().optional(),
  discrepancyNote: z.string().nullable().optional(),
  sourceLotId: guidSchema.nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  expirationDate: z.string().nullable().optional(),
});

export const inventoryTransferDtoSchema = z.object({
  transferId: guidSchema,
  organizationId: guidSchema,
  transferNumber: z.string().nullable().optional(),
  sourceBranchId: guidSchema,
  sourceBranchName: z.string().nullable().optional(),
  destinationBranchId: guidSchema,
  destinationBranchName: z.string().nullable().optional(),
  status: z.string(),
  notes: z.string().nullable().optional(),
  createdBy: guidSchema,
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  dispatchedAtUtc: z.string().nullable().optional(),
  dispatchedBy: guidSchema.nullable().optional(),
  receivedAtUtc: z.string().nullable().optional(),
  receivedBy: guidSchema.nullable().optional(),
  cancelledAtUtc: z.string().nullable().optional(),
  cancelledBy: guidSchema.nullable().optional(),
  totalSentQty: z.number(),
  totalReceivedQty: z.number(),
  totalDifferenceQty: z.number(),
  lines: z.array(inventoryTransferLineDtoSchema),
});

export const inventoryTransferListItemDtoSchema = z.object({
  transferId: guidSchema,
  transferNumber: z.string().nullable().optional(),
  sourceBranchId: guidSchema,
  sourceBranchName: z.string().nullable().optional(),
  destinationBranchId: guidSchema,
  destinationBranchName: z.string().nullable().optional(),
  status: z.string(),
  lineCount: z.number(),
  totalSentQty: z.number(),
  totalReceivedQty: z.number(),
  totalDifferenceQty: z.number(),
  updatedAtUtc: z.string(),
});

export const inventoryTransferPagedResultSchema = z.object({
  items: z.array(inventoryTransferListItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type InventoryTransferLineDto = z.infer<typeof inventoryTransferLineDtoSchema>;
export type InventoryTransferDto = z.infer<typeof inventoryTransferDtoSchema>;
export type InventoryTransferListItemDto = z.infer<typeof inventoryTransferListItemDtoSchema>;
export type InventoryTransferPagedResult = z.infer<typeof inventoryTransferPagedResultSchema>;

export type InventoryTransferLineRequest = {
  productId: string;
  quantity: number;
  sourceLotId?: string | null;
};

export type CreateInventoryTransferRequest = {
  sourceBranchId: string;
  destinationBranchId: string;
  lines: InventoryTransferLineRequest[];
  notes?: string | null;
  /** Client-generated idempotency entity id. */
  operationId?: string | null;
};

export type InventoryTransferReceiveLineRequest = {
  productId: string;
  receivedQty: number;
  discrepancyReason?: string | null;
  discrepancyNote?: string | null;
  lineId?: string | null;
};

export type ReceiveInventoryTransferRequest = {
  lines: InventoryTransferReceiveLineRequest[];
};

export type ListInventoryTransfersOptions = {
  page?: number;
  pageSize?: number;
  status?: string;
  transferNumber?: string;
  direction?: InventoryTransferDirection | string;
  sourceBranchId?: string;
  destinationBranchId?: string;
};

function appendQuery(
  path: string,
  params: Record<string, string | number | boolean | undefined>,
): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  const serialized = query.toString();
  return serialized ? `${path}?${serialized}` : path;
}

function trimOrUndef(value: string | null | undefined): string | undefined {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}

export async function listInventoryTransfers(
  workspace: PosWorkspaceScope,
  options: ListInventoryTransfersOptions = {},
  signal?: AbortSignal,
): Promise<InventoryTransferPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(PATH, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      status: options.status,
      transferNumber: options.transferNumber,
      direction: options.direction,
      sourceBranchId: options.sourceBranchId,
      destinationBranchId: options.destinationBranchId,
    }),
  });
  return inventoryTransferPagedResultSchema.parse(raw);
}

export async function getInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/${transferId}`,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function createInventoryTransfer(
  workspace: PosWorkspaceScope,
  body: CreateInventoryTransferRequest,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const operationId = body.operationId?.trim() || crypto.randomUUID();
  const payload: Record<string, unknown> = {
    sourceBranchId: body.sourceBranchId,
    destinationBranchId: body.destinationBranchId,
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = {
        productId: line.productId,
        quantity: line.quantity,
      };
      if (line.sourceLotId) {
        entry.sourceLotId = line.sourceLotId;
      }
      return entry;
    }),
  };
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }

  const headers = await buildPosMutationIdempotencyHeaders(
    operationId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.InventoryTransferCreate,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: PATH,
    body: payload,
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function dispatchInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const headers = await buildPosMutationIdempotencyHeaders(
    transferId,
    "{}",
    OFFLINE_OPERATION_TYPES.InventoryTransferDispatch,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${transferId}/dispatch`,
    body: {},
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function receiveInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  body: ReceiveInventoryTransferRequest,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const payload = {
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = {
        productId: line.productId,
        receivedQty: line.receivedQty,
      };
      if (line.lineId) {
        entry.lineId = line.lineId;
      }
      const reason = trimOrUndef(line.discrepancyReason);
      if (reason) {
        entry.discrepancyReason = reason;
      }
      const note = trimOrUndef(line.discrepancyNote);
      if (note) {
        entry.discrepancyNote = note;
      }
      return entry;
    }),
  };
  const headers = await buildPosMutationIdempotencyHeaders(
    transferId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.InventoryTransferReceive,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${transferId}/receive`,
    body: payload,
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}

export async function cancelInventoryTransfer(
  workspace: PosWorkspaceScope,
  transferId: string,
  signal?: AbortSignal,
): Promise<InventoryTransferDto> {
  const headers = await buildPosMutationIdempotencyHeaders(
    transferId,
    "{}",
    OFFLINE_OPERATION_TYPES.InventoryTransferCancel,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${transferId}/cancel`,
    body: {},
    headers,
  });
  return inventoryTransferDtoSchema.parse(raw);
}
