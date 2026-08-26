import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError, posRequest } from "@/api/pos/pos-http";
import { formatPaymentMethodLabel } from "@/api/pos/pos-sales-client";
import { roundMoney } from "@/cart/sell-cart-helpers";

const RETURNS_PATH = "/api/v1/pos/sale-returns";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const RESTOCK_DISPOSITIONS = ["ReturnToStock", "DoNotRestock"] as const;
export type RestockDisposition = (typeof RESTOCK_DISPOSITIONS)[number];

export const restockDispositionSchema = z.enum(RESTOCK_DISPOSITIONS);

export const createSaleReturnLineRequestSchema = z.object({
  saleLineId: guidSchema,
  quantity: z.number().positive(),
  restockDisposition: restockDispositionSchema,
  lineReason: z.string().optional(),
});

export const createSaleReturnRequestSchema = z.object({
  saleId: guidSchema,
  reason: z.string().min(1),
  lines: z.array(createSaleReturnLineRequestSchema).min(1),
  notes: z.string().optional(),
  returnId: guidSchema.optional(),
});

export type CreateSaleReturnLineRequest = z.infer<typeof createSaleReturnLineRequestSchema>;
export type CreateSaleReturnRequest = z.infer<typeof createSaleReturnRequestSchema>;

export const posSaleReturnLineDtoSchema = z.object({
  saleReturnLineId: guidSchema,
  saleLineId: guidSchema,
  productId: guidSchema,
  productNameSnapshot: z.string(),
  unitOfMeasure: z.string(),
  quantityReturned: z.number(),
  unitPriceSnapshot: z.number(),
  refundAmount: z.number(),
  restockDisposition: z.string(),
  lineReason: z.string().nullable().optional(),
  inventoryMovementId: guidSchema.nullable().optional(),
});

export const posSaleReturnDtoSchema = z.object({
  returnId: guidSchema,
  organizationId: guidSchema,
  returnNumber: z.string(),
  saleId: guidSchema,
  refundMethod: z.string(),
  status: z.string(),
  returnDate: z.string(),
  reason: z.string(),
  notes: z.string().nullable().optional(),
  totalRefundAmount: z.number(),
  createdAtUtc: z.string(),
  createdBy: guidSchema,
  completedAtUtc: z.string(),
  cashierShiftId: guidSchema.nullable().optional(),
  lines: z.array(posSaleReturnLineDtoSchema),
});

export type PosSaleReturnLineDto = z.infer<typeof posSaleReturnLineDtoSchema>;
export type PosSaleReturnDto = z.infer<typeof posSaleReturnDtoSchema>;

export const posRefundableSaleLineDtoSchema = z.object({
  saleLineId: guidSchema,
  productId: guidSchema,
  productNameSnapshot: z.string(),
  unitOfMeasure: z.string(),
  sellingMode: z.string(),
  originalQuantity: z.number(),
  unitPriceSnapshot: z.number(),
  originalLineTotal: z.number(),
  previouslyReturnedQuantity: z.number(),
  refundableQuantity: z.number(),
  previouslyRefundedAmount: z.number(),
  refundableAmount: z.number(),
});

export const posRefundableSaleDtoSchema = z.object({
  saleId: guidSchema,
  saleNumber: z.string(),
  paymentMethod: z.string(),
  status: z.string(),
  lines: z.array(posRefundableSaleLineDtoSchema),
});

export type PosRefundableSaleLineDto = z.infer<typeof posRefundableSaleLineDtoSchema>;
export type PosRefundableSaleDto = z.infer<typeof posRefundableSaleDtoSchema>;

export const posSaleReturnPagedResultSchema = z.object({
  items: z.array(posSaleReturnDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type PosSaleReturnPagedResult = z.infer<typeof posSaleReturnPagedResultSchema>;

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

function parseReturn(payload: unknown): PosSaleReturnDto {
  return posSaleReturnDtoSchema.parse(payload);
}

/**
 * Optional UI estimate only — mirrors backend cumulative NET LineTotal allocation.
 * Never uses UnitPrice. POST result always wins.
 */
export function estimateLineRefundAmount(input: {
  originalQuantity: number;
  originalLineTotal: number;
  previouslyReturnedQuantity: number;
  previouslyRefundedAmount: number;
  requestedQty: number;
}): number {
  const {
    originalQuantity,
    originalLineTotal,
    previouslyReturnedQuantity,
    previouslyRefundedAmount,
    requestedQty,
  } = input;

  if (!(requestedQty > 0) || !(originalQuantity > 0)) {
    return 0;
  }

  const newCumulativeQty = previouslyReturnedQuantity + requestedQty;
  let targetCumulative: number;
  if (newCumulativeQty >= originalQuantity) {
    targetCumulative = originalLineTotal;
  } else {
    targetCumulative = roundMoney((originalLineTotal * newCumulativeQty) / originalQuantity);
  }

  return roundMoney(targetCumulative - previouslyRefundedAmount);
}

export function estimateTotalRefundAmount(
  lines: Array<{
    originalQuantity: number;
    originalLineTotal: number;
    previouslyReturnedQuantity: number;
    previouslyRefundedAmount: number;
    requestedQty: number;
  }>,
): number {
  return roundMoney(lines.reduce((sum, line) => sum + estimateLineRefundAmount(line), 0));
}

/** User-facing refund method — never show ManualGCash. */
export function formatRefundMethodLabel(refundMethod: string): string {
  return formatPaymentMethodLabel(refundMethod);
}

export function isCashRefundMethod(method: string): boolean {
  return method.trim().toLowerCase() === "cash";
}

export function isGCashRefundMethod(method: string): boolean {
  const normalized = method.trim().toLowerCase();
  return normalized === "manualgcash" || normalized === "gcash";
}

export function isUtangRefundMethod(method: string): boolean {
  return method.trim().toLowerCase() === "utang";
}

/** Stale refundable / concurrency — refresh snapshot; do not silently clamp qty. */
export function isStaleReturnConflict(error: unknown): boolean {
  if (!(error instanceof PosApiError)) {
    return false;
  }
  if (isCashShiftRequiredError(error)) {
    return false;
  }
  const code = (error.errorCode ?? "").toLowerCase();
  if (error.status === 409) {
    return true;
  }
  return (
    code.includes("quantity.exceeds_refundable") ||
    code.includes("concurrency_conflict") ||
    code.includes("sale_not_returnable") ||
    code.includes("sale_return.sale_not_returnable")
  );
}

export function isCashShiftRequiredError(error: unknown): boolean {
  if (!(error instanceof PosApiError)) {
    return false;
  }
  const code = (error.errorCode ?? "").toLowerCase();
  const detail = (error.problem.detail ?? error.message).toLowerCase();
  return (
    code.includes("no_open_shift") ||
    code.includes("cash_shift.required") ||
    detail.includes("open cashier shift") ||
    detail.includes("open shift")
  );
}

export async function getRefundableSale(
  workspace: PosWorkspaceScope,
  saleId: string,
  signal?: AbortSignal,
): Promise<PosRefundableSaleDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${RETURNS_PATH}/refundable/${saleId}`,
  });
  return posRefundableSaleDtoSchema.parse(raw);
}

export async function getSaleReturn(
  workspace: PosWorkspaceScope,
  returnId: string,
  signal?: AbortSignal,
): Promise<PosSaleReturnDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${RETURNS_PATH}/${returnId}`,
  });
  return parseReturn(raw);
}

export async function listSaleReturns(
  workspace: PosWorkspaceScope,
  options: {
    saleId?: string;
    returnNumber?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<PosSaleReturnPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(RETURNS_PATH, {
      saleId: options.saleId,
      returnNumber: options.returnNumber,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posSaleReturnPagedResultSchema.parse(raw);
}

/** POST /api/v1/pos/sale-returns — ProcessReturn; device header via pos-http. */
export async function createSaleReturn(
  workspace: PosWorkspaceScope,
  body: CreateSaleReturnRequest,
  signal?: AbortSignal,
): Promise<PosSaleReturnDto> {
  const validated = createSaleReturnRequestSchema.parse(body);
  const payload: Record<string, unknown> = {
    saleId: validated.saleId,
    reason: validated.reason.trim(),
    lines: validated.lines.map((line) => {
      const entry: Record<string, unknown> = {
        saleLineId: line.saleLineId,
        quantity: line.quantity,
        restockDisposition: line.restockDisposition,
      };
      if (line.lineReason?.trim()) {
        entry.lineReason = line.lineReason.trim();
      }
      return entry;
    }),
  };
  if (validated.notes?.trim()) {
    payload.notes = validated.notes.trim();
  }
  if (validated.returnId) {
    payload.returnId = validated.returnId;
  }

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: RETURNS_PATH,
    body: payload,
  });
  return parseReturn(raw);
}
