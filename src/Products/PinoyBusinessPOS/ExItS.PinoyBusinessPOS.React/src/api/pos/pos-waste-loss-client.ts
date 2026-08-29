import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const PATH = "/api/v1/pos/inventory/waste-losses";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const WASTE_LOSS_REASONS = [
  "Spoiled",
  "Expired",
  "Damaged",
  "Broken",
  "Spillage",
  "MissingOrShrinkage",
  "Other",
] as const;

export type WasteLossReasonCode = (typeof WASTE_LOSS_REASONS)[number];

export const WASTE_LOSS_STATUSES = ["Posted", "Voided"] as const;
export type WasteLossStatusCode = (typeof WASTE_LOSS_STATUSES)[number];

export const WASTE_LOSS_COST_STATUSES = ["Complete", "Partial", "Unavailable"] as const;
export type WasteLossCostStatusCode = (typeof WASTE_LOSS_COST_STATUSES)[number];

export const wasteLossLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  productUnitId: guidSchema.nullable().optional(),
  inventoryLotId: guidSchema.nullable().optional(),
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

export const wasteLossDtoSchema = z.object({
  wasteLossId: guidSchema,
  organizationId: guidSchema,
  branchId: guidSchema.nullable().optional(),
  wasteLossNumber: z.string(),
  referenceNumber: z.string().nullable().optional(),
  occurredAtUtc: z.string(),
  reason: z.string(),
  notes: z.string().nullable().optional(),
  status: z.string(),
  costStatus: z.string(),
  totalCostSnapshot: z.number().nullable().optional(),
  createdByUserId: guidSchema,
  createdAtUtc: z.string(),
  voidedByUserId: guidSchema.nullable().optional(),
  voidedAtUtc: z.string().nullable().optional(),
  lines: z.array(wasteLossLineDtoSchema),
});

export const wasteLossListItemDtoSchema = z.object({
  wasteLossId: guidSchema,
  wasteLossNumber: z.string(),
  branchId: guidSchema.nullable().optional(),
  referenceNumber: z.string().nullable().optional(),
  occurredAtUtc: z.string(),
  reason: z.string(),
  status: z.string(),
  costStatus: z.string(),
  lineCount: z.number(),
  createdAtUtc: z.string(),
});

export const wasteLossPagedResultSchema = z.object({
  items: z.array(wasteLossListItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type WasteLossLineDto = z.infer<typeof wasteLossLineDtoSchema>;
export type WasteLossDto = z.infer<typeof wasteLossDtoSchema>;
export type WasteLossListItemDto = z.infer<typeof wasteLossListItemDtoSchema>;
export type WasteLossPagedResult = z.infer<typeof wasteLossPagedResultSchema>;

export type CreateWasteLossLineRequest = {
  productId: string;
  quantity: number;
  productUnitId?: string | null;
  inventoryLotId?: string | null;
};

export type CreateWasteLossRequest = {
  reason: WasteLossReasonCode | string;
  lines: CreateWasteLossLineRequest[];
  branchId?: string | null;
  referenceNumber?: string | null;
  notes?: string | null;
  occurredAtUtc?: string | null;
  wasteLossId?: string | null;
};

export type ListWasteLossesOptions = {
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

export async function listWasteLosses(
  workspace: PosWorkspaceScope,
  options: ListWasteLossesOptions = {},
  signal?: AbortSignal,
): Promise<WasteLossPagedResult> {
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
  return wasteLossPagedResultSchema.parse(raw);
}

export async function getWasteLoss(
  workspace: PosWorkspaceScope,
  wasteLossId: string,
  signal?: AbortSignal,
): Promise<WasteLossDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/${wasteLossId}`,
  });
  return wasteLossDtoSchema.parse(raw);
}

/**
 * Creates a waste/loss document (decreases inventory). Online-only mutation.
 * Idempotency via headers when wasteLossId is provided (or auto-generated).
 */
export async function createWasteLoss(
  workspace: PosWorkspaceScope,
  body: CreateWasteLossRequest,
  signal?: AbortSignal,
): Promise<WasteLossDto> {
  const wasteLossId = body.wasteLossId?.trim() || crypto.randomUUID();
  const payload: Record<string, unknown> = {
    reason: body.reason,
    wasteLossId,
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = {
        productId: line.productId,
        quantity: line.quantity,
      };
      if (line.productUnitId) {
        entry.productUnitId = line.productUnitId;
      }
      if (line.inventoryLotId) {
        entry.inventoryLotId = line.inventoryLotId;
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
    wasteLossId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.WasteLoss,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: PATH,
    body: payload,
    headers,
  });
  return wasteLossDtoSchema.parse(raw);
}

export async function voidWasteLoss(
  workspace: PosWorkspaceScope,
  wasteLossId: string,
  signal?: AbortSignal,
): Promise<WasteLossDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${wasteLossId}/void`,
  });
  return wasteLossDtoSchema.parse(raw);
}
