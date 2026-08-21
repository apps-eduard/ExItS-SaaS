import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const SALES_PATH = "/api/v1/pos/sales";
const QUOTE_PATH = "/api/v1/pos/sales/quote";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

/** Current checkout methods only — never Card or provider GCash in React UX (RMAP-12). */
export const checkoutPaymentMethodSchema = z.enum(["Cash", "ManualGCash", "Utang"]);
export type CheckoutPaymentMethod = z.infer<typeof checkoutPaymentMethodSchema>;

export const GCASH_REFERENCE_MAX_LENGTH = 64;
export const VOID_REASON_MAX_LENGTH = 512;

export const checkoutSaleLineRequestSchema = z.object({
  productId: guidSchema,
  quantity: z.number(),
  sellingUnitId: guidSchema.optional(),
  enteredQuantity: z.number().optional(),
});

export const commercialDiscountIntentRequestSchema = z.object({
  scope: z.enum(["Line", "Sale"]),
  method: z.enum(["Percentage", "FixedAmount"]),
  value: z.number(),
  reason: z.string().min(1),
  productId: guidSchema.optional(),
  lineNumber: z.number().int().positive().optional(),
});

/** Per-sale unit-price override intent (RMAP-B01 / RMAP-12b). Server resolves baseline. */
export const salePriceOverrideIntentRequestSchema = z.object({
  requestedUnitPrice: z.number(),
  reason: z.string().min(1),
  productId: guidSchema.optional(),
  lineNumber: z.number().int().positive().optional(),
  expectedBaselineUnitPrice: z.number().optional(),
});

export const checkoutSaleRequestSchema = z.object({
  lines: z.array(checkoutSaleLineRequestSchema).min(1),
  paymentMethod: checkoutPaymentMethodSchema,
  amountTendered: z.number().optional(),
  gCashReference: z.string().max(GCASH_REFERENCE_MAX_LENGTH).optional(),
  saleId: guidSchema,
  shiftId: guidSchema,
  customerId: guidSchema.optional(),
  /** ISO date `YYYY-MM-DD` for optional Utang due date (DateOnly). */
  dueDate: z
    .string()
    .regex(/^\d{4}-\d{2}-\d{2}$/)
    .optional(),
  discounts: z.array(commercialDiscountIntentRequestSchema).optional(),
  priceOverrides: z.array(salePriceOverrideIntentRequestSchema).optional(),
});

/** Quote uses the same line/discount/override contract; tender/saleId/shift are not required. */
export const quoteSaleRequestSchema = z.object({
  lines: z.array(checkoutSaleLineRequestSchema).min(1),
  paymentMethod: checkoutPaymentMethodSchema.optional(),
  amountTendered: z.number().optional(),
  customerId: guidSchema.optional(),
  discounts: z.array(commercialDiscountIntentRequestSchema).optional(),
  priceOverrides: z.array(salePriceOverrideIntentRequestSchema).optional(),
});

export const voidSaleRequestSchema = z.object({
  reason: z.string().min(1).max(VOID_REASON_MAX_LENGTH),
});

export type CheckoutSaleLineRequest = z.infer<typeof checkoutSaleLineRequestSchema>;
export type CommercialDiscountIntentRequest = z.infer<typeof commercialDiscountIntentRequestSchema>;
export type SalePriceOverrideIntentRequest = z.infer<typeof salePriceOverrideIntentRequestSchema>;
export type CheckoutSaleRequest = z.infer<typeof checkoutSaleRequestSchema>;
export type QuoteSaleRequest = z.infer<typeof quoteSaleRequestSchema>;
export type VoidSaleRequest = z.infer<typeof voidSaleRequestSchema>;

export const posSaleLineDtoSchema = z.object({
  saleLineId: guidSchema,
  productId: guidSchema,
  lineNumber: z.number(),
  name: z.string(),
  sku: z.string().nullable().optional(),
  barcode: z.string().nullable().optional(),
  unitOfMeasure: z.string(),
  sellingMode: z.string(),
  unitPrice: z.number(),
  quantity: z.number(),
  lineTotal: z.number(),
  grossLineTotal: z.number().optional(),
  lineDiscountAmount: z.number().optional(),
  saleDiscountAllocatedAmount: z.number().optional(),
});

export const posSaleQuotePriceOverrideDtoSchema = z.object({
  lineNumber: z.number(),
  baselineUnitPrice: z.number(),
  appliedUnitPrice: z.number(),
  reason: z.string(),
});

export const posSaleDtoSchema = z.object({
  saleId: guidSchema,
  organizationId: guidSchema,
  saleNumber: z.string(),
  status: z.string(),
  paymentMethod: z.string(),
  subtotal: z.number(),
  total: z.number(),
  taxAmount: z.number(),
  amountTendered: z.number().nullable().optional(),
  changeAmount: z.number().nullable().optional(),
  gCashReference: z.string().nullable().optional(),
  recordedAtUtc: z.string(),
  recordedBy: guidSchema,
  voidedAtUtc: z.string().nullable().optional(),
  voidedBy: guidSchema.nullable().optional(),
  voidReason: z.string().nullable().optional(),
  updatedAtUtc: z.string(),
  lines: z.array(posSaleLineDtoSchema),
  customerId: guidSchema.nullable().optional(),
  linkedCreditEntryId: guidSchema.nullable().optional(),
  customerDisplayName: z.string().nullable().optional(),
  shiftId: guidSchema.nullable().optional(),
  shiftNumber: z.string().nullable().optional(),
  registerId: guidSchema.nullable().optional(),
  registerCode: z.string().nullable().optional(),
  registerName: z.string().nullable().optional(),
  storeDisplayName: z.string().nullable().optional(),
  currencyCode: z.string().nullable().optional(),
  documentKind: z.string().optional(),
  branchId: guidSchema.nullable().optional(),
  grossSubtotal: z.number().optional(),
  discountTotal: z.number().optional(),
  lineDiscountTotal: z.number().optional(),
  saleDiscountTotal: z.number().optional(),
  priceOverrides: z.array(posSaleQuotePriceOverrideDtoSchema).optional(),
});

export type PosSaleLineDto = z.infer<typeof posSaleLineDtoSchema>;
export type PosSaleDto = z.infer<typeof posSaleDtoSchema>;
export type PosSaleQuotePriceOverrideDto = z.infer<typeof posSaleQuotePriceOverrideDtoSchema>;

export const posSaleQuoteLineDtoSchema = z.object({
  lineNumber: z.number(),
  productId: guidSchema,
  name: z.string(),
  unitOfMeasure: z.string(),
  sellingMode: z.string(),
  unitPrice: z.number(),
  quantity: z.number(),
  grossLineTotal: z.number(),
  lineDiscountAmount: z.number(),
  saleDiscountAllocatedAmount: z.number(),
  lineTotal: z.number(),
  baselineUnitPrice: z.number().nullable().optional(),
});

export const posSaleQuoteDiscountDtoSchema = z.object({
  scope: z.string(),
  method: z.string(),
  requestedValue: z.number(),
  calculatedAmount: z.number(),
  reason: z.string(),
  lineNumber: z.number().nullable().optional(),
});

export const posSaleQuoteDtoSchema = z.object({
  grossSubtotal: z.number(),
  lineDiscountTotal: z.number(),
  saleDiscountTotal: z.number(),
  discountTotal: z.number(),
  subtotal: z.number(),
  taxAmount: z.number(),
  total: z.number(),
  taxPricingMode: z.string().nullable().optional(),
  lines: z.array(posSaleQuoteLineDtoSchema),
  discounts: z.array(posSaleQuoteDiscountDtoSchema),
  priceOverrides: z.array(posSaleQuotePriceOverrideDtoSchema).nullable().optional(),
});

export type PosSaleQuoteLineDto = z.infer<typeof posSaleQuoteLineDtoSchema>;
export type PosSaleQuoteDiscountDto = z.infer<typeof posSaleQuoteDiscountDtoSchema>;
export type PosSaleQuoteDto = z.infer<typeof posSaleQuoteDtoSchema>;

export const posSalePagedResultSchema = z.object({
  items: z.array(posSaleDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type PosSalePagedResult = z.infer<typeof posSalePagedResultSchema>;

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

function parseSale(payload: unknown): PosSaleDto {
  return posSaleDtoSchema.parse(payload);
}

function serializeLines(lines: CheckoutSaleLineRequest[]): Record<string, unknown>[] {
  return lines.map((line) => {
    const entry: Record<string, unknown> = {
      productId: line.productId,
      quantity: line.quantity,
    };
    if (line.sellingUnitId) {
      entry.sellingUnitId = line.sellingUnitId;
    }
    if (line.enteredQuantity !== undefined) {
      entry.enteredQuantity = line.enteredQuantity;
    }
    return entry;
  });
}

function serializeDiscounts(
  discounts: CommercialDiscountIntentRequest[] | undefined,
): Record<string, unknown>[] | undefined {
  if (!discounts || discounts.length === 0) {
    return undefined;
  }
  return discounts.map((discount) => {
    const entry: Record<string, unknown> = {
      scope: discount.scope,
      method: discount.method,
      value: discount.value,
      reason: discount.reason,
    };
    if (discount.productId) {
      entry.productId = discount.productId;
    }
    if (discount.lineNumber !== undefined) {
      entry.lineNumber = discount.lineNumber;
    }
    return entry;
  });
}

function serializePriceOverrides(
  overrides: SalePriceOverrideIntentRequest[] | undefined,
): Record<string, unknown>[] | undefined {
  if (!overrides || overrides.length === 0) {
    return undefined;
  }
  return overrides.map((item) => {
    const entry: Record<string, unknown> = {
      requestedUnitPrice: item.requestedUnitPrice,
      reason: item.reason,
    };
    if (item.productId) {
      entry.productId = item.productId;
    }
    if (item.lineNumber !== undefined) {
      entry.lineNumber = item.lineNumber;
    }
    if (item.expectedBaselineUnitPrice !== undefined) {
      entry.expectedBaselineUnitPrice = item.expectedBaselineUnitPrice;
    }
    return entry;
  });
}

/**
 * Wire body for POST /api/v1/pos/sales.
 * Shared with the offline outbox so a queued sale replays the exact online contract.
 */
export function buildCheckoutSalePayload(body: CheckoutSaleRequest): Record<string, unknown> {
  const validated = checkoutSaleRequestSchema.parse(body);
  const payload: Record<string, unknown> = {
    lines: serializeLines(validated.lines),
    paymentMethod: validated.paymentMethod,
    saleId: validated.saleId,
    shiftId: validated.shiftId,
  };

  if (validated.paymentMethod === "Cash") {
    payload.amountTendered = validated.amountTendered ?? 0;
  }

  if (validated.paymentMethod === "ManualGCash" && validated.gCashReference) {
    payload.gCashReference = validated.gCashReference;
  }

  if (validated.customerId) {
    payload.customerId = validated.customerId;
  }
  if (validated.paymentMethod === "Utang" && validated.dueDate) {
    payload.dueDate = validated.dueDate;
  }

  const discounts = serializeDiscounts(validated.discounts);
  if (discounts) {
    payload.discounts = discounts;
  }

  const priceOverrides = serializePriceOverrides(validated.priceOverrides);
  if (priceOverrides) {
    payload.priceOverrides = priceOverrides;
  }

  return payload;
}

/**
 * Online checkout for Cash / ManualGCash / Utang.
 * Omit snapshot fields; server prices from live catalog.
 * Optional commercial discount intents (RMAP-11b) — server recomputes all money.
 * Optional priceOverrides (RMAP-12b) — server applies before discounts; never mutates catalog.
 * Never send Card or provider GCash from this client.
 * Sends sale idempotency headers keyed on saleId so an online retry and a replayed
 * offline queued sale collapse to one recorded sale.
 */
export async function checkoutSale(
  workspace: PosWorkspaceScope,
  body: CheckoutSaleRequest,
  signal?: AbortSignal,
): Promise<PosSaleDto> {
  const validated = checkoutSaleRequestSchema.parse(body);
  const payload = buildCheckoutSalePayload(validated);
  const payloadJson = JSON.stringify(payload);
  const headers = await buildPosMutationIdempotencyHeaders(
    validated.saleId,
    payloadJson,
    OFFLINE_OPERATION_TYPES.SaleCheckout,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: SALES_PATH,
    body: payload,
    headers,
  });
  return parseSale(raw);
}

/**
 * Non-persisting authoritative checkout quote (RMAP-B03 / RMAP-11b / RMAP-12b).
 * Never mutates UnitPrice client-side; never treats quote as authorization to record.
 */
export async function quoteSale(
  workspace: PosWorkspaceScope,
  body: QuoteSaleRequest,
  signal?: AbortSignal,
): Promise<PosSaleQuoteDto> {
  const validated = quoteSaleRequestSchema.parse(body);
  const payload: Record<string, unknown> = {
    lines: serializeLines(validated.lines),
    paymentMethod: validated.paymentMethod ?? "Cash",
  };
  if (validated.amountTendered !== undefined) {
    payload.amountTendered = validated.amountTendered;
  }
  if (validated.customerId) {
    payload.customerId = validated.customerId;
  }
  const discounts = serializeDiscounts(validated.discounts);
  if (discounts) {
    payload.discounts = discounts;
  }
  const priceOverrides = serializePriceOverrides(validated.priceOverrides);
  if (priceOverrides) {
    payload.priceOverrides = priceOverrides;
  }

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: QUOTE_PATH,
    body: payload,
  });
  return posSaleQuoteDtoSchema.parse(raw);
}

export async function getSale(
  workspace: PosWorkspaceScope,
  saleId: string,
  signal?: AbortSignal,
): Promise<PosSaleDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${SALES_PATH}/${saleId}`,
  });
  return parseSale(raw);
}

export async function listSales(
  workspace: PosWorkspaceScope,
  options: {
    status?: string;
    paymentMethod?: string;
    fromDate?: string;
    toDate?: string;
    saleNumber?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<PosSalePagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(SALES_PATH, {
      status: options.status,
      paymentMethod: options.paymentMethod,
      fromDate: options.fromDate,
      toDate: options.toDate,
      saleNumber: options.saleNumber,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posSalePagedResultSchema.parse(raw);
}

/** POST /api/v1/pos/sales/{saleId}/void — Owner/Admin/Manager (VoidSale). */
export async function voidSale(
  workspace: PosWorkspaceScope,
  saleId: string,
  body: VoidSaleRequest,
  signal?: AbortSignal,
): Promise<PosSaleDto> {
  const validated = voidSaleRequestSchema.parse(body);
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${SALES_PATH}/${saleId}/void`,
    body: { reason: validated.reason.trim() },
  });
  return parseSale(raw);
}

/** User-facing payment label — never show ManualGCash to operators. */
export function formatPaymentMethodLabel(paymentMethod: string): string {
  if (paymentMethod === "ManualGCash") {
    return "GCash";
  }
  return paymentMethod;
}
