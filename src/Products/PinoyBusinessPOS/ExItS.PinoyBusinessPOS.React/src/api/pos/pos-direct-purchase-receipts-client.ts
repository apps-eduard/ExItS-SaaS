import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const PATH = "/api/v1/pos/direct-purchase-receipts";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const directPurchaseReceiptLineDtoSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  lineNumber: z.number(),
  productNameSnapshot: z.string(),
  skuSnapshot: z.string().nullable().optional(),
  unitOfMeasure: z.string(),
  quantity: z.number(),
  unitCost: z.number(),
  lineTotal: z.number(),
  expiryDate: z.string().nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  inventoryMovementId: guidSchema.nullable().optional(),
});

export const directPurchaseReceiptDtoSchema = z.object({
  directPurchaseReceiptId: guidSchema,
  organizationId: guidSchema,
  receiptNumber: z.string(),
  purchaseDate: z.string(),
  supplierId: guidSchema.nullable().optional(),
  sourceNameSnapshot: z.string().nullable().optional(),
  referenceNumber: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  totalCost: z.number(),
  createdByUserId: guidSchema,
  createdAtUtc: z.string(),
  lines: z.array(directPurchaseReceiptLineDtoSchema),
  status: z.string().default("Posted"),
  voidedAtUtc: z.string().nullable().optional(),
  voidedByUserId: guidSchema.nullable().optional(),
  voidReason: z.string().nullable().optional(),
});

export const directPurchaseReceiptListItemDtoSchema = z.object({
  directPurchaseReceiptId: guidSchema,
  receiptNumber: z.string(),
  purchaseDate: z.string(),
  supplierId: guidSchema.nullable().optional(),
  sourceNameSnapshot: z.string().nullable().optional(),
  referenceNumber: z.string().nullable().optional(),
  totalCost: z.number(),
  lineCount: z.number(),
  createdAtUtc: z.string(),
  status: z.string().default("Posted"),
});

export const directPurchaseReceiptPagedResultSchema = z.object({
  items: z.array(directPurchaseReceiptListItemDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type DirectPurchaseReceiptLineDto = z.infer<typeof directPurchaseReceiptLineDtoSchema>;
export type DirectPurchaseReceiptDto = z.infer<typeof directPurchaseReceiptDtoSchema>;
export type DirectPurchaseReceiptListItemDto = z.infer<
  typeof directPurchaseReceiptListItemDtoSchema
>;
export type DirectPurchaseReceiptPagedResult = z.infer<
  typeof directPurchaseReceiptPagedResultSchema
>;

export type CreateDirectPurchaseReceiptLineRequest = {
  productId: string;
  quantity: number;
  unitCost: number;
  expiryDate?: string | null;
  lotNumber?: string | null;
};

export type CreateDirectPurchaseReceiptRequest = {
  purchaseDate: string;
  lines: CreateDirectPurchaseReceiptLineRequest[];
  supplierId?: string | null;
  sourceName?: string | null;
  referenceNumber?: string | null;
  notes?: string | null;
  /** Body idempotency key (API contract) — client-generated UUID string. */
  idempotencyKey?: string | null;
};

export type ListDirectPurchaseReceiptsOptions = {
  fromPurchaseDate?: string;
  toPurchaseDate?: string;
  supplierId?: string;
  sourceSearch?: string;
  referenceNumber?: string;
  page?: number;
  pageSize?: number;
};

export type VoidDirectPurchaseReceiptRequest = {
  reason: string;
  notes?: string | null;
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

export async function listDirectPurchaseReceipts(
  workspace: PosWorkspaceScope,
  options: ListDirectPurchaseReceiptsOptions = {},
  signal?: AbortSignal,
): Promise<DirectPurchaseReceiptPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(PATH, {
      fromPurchaseDate: options.fromPurchaseDate,
      toPurchaseDate: options.toPurchaseDate,
      supplierId: options.supplierId,
      sourceSearch: options.sourceSearch,
      referenceNumber: options.referenceNumber,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return directPurchaseReceiptPagedResultSchema.parse(raw);
}

export async function getDirectPurchaseReceipt(
  workspace: PosWorkspaceScope,
  receiptId: string,
  signal?: AbortSignal,
): Promise<DirectPurchaseReceiptDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/${receiptId}`,
  });
  return directPurchaseReceiptDtoSchema.parse(raw);
}

/**
 * Creates a direct purchase receipt (bypasses PO). Increases inventory via DirectPurchaseReceipt movements.
 * Idempotency is body `idempotencyKey` per API contract (not header-based).
 */
export async function createDirectPurchaseReceipt(
  workspace: PosWorkspaceScope,
  body: CreateDirectPurchaseReceiptRequest,
  signal?: AbortSignal,
): Promise<DirectPurchaseReceiptDto> {
  const payload: Record<string, unknown> = {
    purchaseDate: body.purchaseDate,
    lines: body.lines.map((line) => {
      const entry: Record<string, unknown> = {
        productId: line.productId,
        quantity: line.quantity,
        unitCost: line.unitCost,
      };
      if (line.expiryDate) {
        entry.expiryDate = line.expiryDate;
      }
      const lot = trimOrUndef(line.lotNumber);
      if (lot) {
        entry.lotNumber = lot;
      }
      return entry;
    }),
  };
  if (body.supplierId) {
    payload.supplierId = body.supplierId;
  }
  const source = trimOrUndef(body.sourceName);
  if (source) {
    payload.sourceName = source;
  }
  const reference = trimOrUndef(body.referenceNumber);
  if (reference) {
    payload.referenceNumber = reference;
  }
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }
  payload.idempotencyKey = body.idempotencyKey?.trim() || crypto.randomUUID();

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: PATH,
    body: payload,
  });
  return directPurchaseReceiptDtoSchema.parse(raw);
}

/**
 * Full reverse of a posted direct purchase receipt. Compensating stock out at original unit cost.
 * Online-only; preserves the original receipt as Voided.
 */
export async function voidDirectPurchaseReceipt(
  workspace: PosWorkspaceScope,
  receiptId: string,
  body: VoidDirectPurchaseReceiptRequest,
  signal?: AbortSignal,
): Promise<DirectPurchaseReceiptDto> {
  const payload = {
    reason: body.reason.trim(),
    ...(body.notes?.trim() ? { notes: body.notes.trim() } : {}),
  };
  const headers = await buildPosMutationIdempotencyHeaders(
    receiptId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.DirectPurchaseReceiptVoid,
  );
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${receiptId}/void`,
    body: payload,
    headers,
  });
  return directPurchaseReceiptDtoSchema.parse(raw);
}
