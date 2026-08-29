import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const PATH = "/api/v1/pos/inventory/stock-uses";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const STOCK_USE_REASONS = [
  "InternalOperations",
  "StaffUse",
  "SampleOrTesting",
  "Other",
] as const;

export type StockUseReasonCode = (typeof STOCK_USE_REASONS)[number];

export const STOCK_USE_STATUSES = ["Posted", "Voided"] as const;
export type StockUseStatusCode = (typeof STOCK_USE_STATUSES)[number];

export const stockUseLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  productUnitId: guidSchema.nullable().optional(),
  lineNumber: z.number(),
  quantityEntered: z.number(),
  multiplierToBase: z.number(),
  baseQuantity: z.number(),
  nameSnapshot: z.string(),
  unitLabelSnapshot: z.string(),
  unitCostSnapshot: z.number().nullable().optional(),
  lineCostSnapshot: z.number().nullable().optional(),
  inventoryMovementId: guidSchema.nullable().optional(),
});

export const stockUseDtoSchema = z.object({
  stockUseId: guidSchema,
  organizationId: guidSchema,
  branchId: guidSchema.nullable().optional(),
  stockUseNumber: z.string(),
  referenceNumber: z.string().nullable().optional(),
  occurredAtUtc: z.string(),
  reason: z.string(),
  notes: z.string().nullable().optional(),
  status: z.string(),
  createdByUserId: guidSchema,
  createdAtUtc: z.string(),
  voidedByUserId: guidSchema.nullable().optional(),
  voidedAtUtc: z.string().nullable().optional(),
  lines: z.array(stockUseLineDtoSchema),
});

export const stockUseListItemDtoSchema = z.object({
  stockUseId: guidSchema,
  stockUseNumber: z.string(),
  branchId: guidSchema.nullable().optional(),
  referenceNumber: z.string().nullable().optional(),
  occurredAtUtc: z.string(),
  reason: z.string(),
  status: z.string(),
  lineCount: z.number(),
  createdAtUtc: z.string(),
});

export const stockUsePagedResultSchema = z.object({
  items: z.array(stockUseListItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type StockUseLineDto = z.infer<typeof stockUseLineDtoSchema>;
export type StockUseDto = z.infer<typeof stockUseDtoSchema>;
export type StockUseListItemDto = z.infer<typeof stockUseListItemDtoSchema>;
export type StockUsePagedResult = z.infer<typeof stockUsePagedResultSchema>;

export type CreateStockUseLineRequest = {
  productId: string;
  quantity: number;
  productUnitId?: string | null;
};

export type CreateStockUseRequest = {
  reason: StockUseReasonCode | string;
  lines: CreateStockUseLineRequest[];
  branchId?: string | null;
  referenceNumber?: string | null;
  notes?: string | null;
  occurredAtUtc?: string | null;
  stockUseId?: string | null;
};

export type ListStockUsesOptions = {
  page?: number;
  pageSize?: number;
  reason?: string;
  status?: string;
  fromOccurredAtUtc?: string;
  toOccurredAtUtc?: string;
  referenceNumber?: string;
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

export async function listStockUses(
  workspace: PosWorkspaceScope,
  options: ListStockUsesOptions = {},
  signal?: AbortSignal,
): Promise<StockUsePagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(PATH, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      reason: options.reason,
      status: options.status,
      fromOccurredAtUtc: options.fromOccurredAtUtc,
      toOccurredAtUtc: options.toOccurredAtUtc,
      referenceNumber: options.referenceNumber,
    }),
  });
  return stockUsePagedResultSchema.parse(raw);
}

export async function getStockUse(
  workspace: PosWorkspaceScope,
  stockUseId: string,
  signal?: AbortSignal,
): Promise<StockUseDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/${stockUseId}`,
  });
  return stockUseDtoSchema.parse(raw);
}

/**
 * Creates a stock-use document (decreases inventory). Online-only mutation.
 * Idempotency via headers when stockUseId is provided (or auto-generated).
 */
export async function createStockUse(
  workspace: PosWorkspaceScope,
  body: CreateStockUseRequest,
  signal?: AbortSignal,
): Promise<StockUseDto> {
  const stockUseId = body.stockUseId?.trim() || crypto.randomUUID();
  const payload: Record<string, unknown> = {
    reason: body.reason,
    stockUseId,
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = {
        productId: line.productId,
        quantity: line.quantity,
      };
      if (line.productUnitId) {
        entry.productUnitId = line.productUnitId;
      }
      return entry;
    }),
  };
  if (body.branchId) {
    payload.branchId = body.branchId;
  }
  const reference = trimOrUndef(body.referenceNumber);
  if (reference) {
    payload.referenceNumber = reference;
  }
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }
  if (body.occurredAtUtc) {
    payload.occurredAtUtc = body.occurredAtUtc;
  }

  const headers = await buildPosMutationIdempotencyHeaders(
    stockUseId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.StockUse,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: PATH,
    body: payload,
    headers,
  });
  return stockUseDtoSchema.parse(raw);
}

export async function voidStockUse(
  workspace: PosWorkspaceScope,
  stockUseId: string,
  signal?: AbortSignal,
): Promise<StockUseDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${stockUseId}/void`,
  });
  return stockUseDtoSchema.parse(raw);
}
