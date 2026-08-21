import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const SALES_PATH = "/api/v1/pos/sales";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const checkoutSaleLineRequestSchema = z.object({
  productId: guidSchema,
  quantity: z.number(),
  sellingUnitId: guidSchema.optional(),
  enteredQuantity: z.number().optional(),
});

export const checkoutSaleRequestSchema = z.object({
  lines: z.array(checkoutSaleLineRequestSchema).min(1),
  paymentMethod: z.literal("Cash"),
  amountTendered: z.number(),
  saleId: guidSchema,
  shiftId: guidSchema,
  customerId: guidSchema.optional(),
});

export type CheckoutSaleLineRequest = z.infer<typeof checkoutSaleLineRequestSchema>;
export type CheckoutSaleRequest = z.infer<typeof checkoutSaleRequestSchema>;

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
});

export type PosSaleLineDto = z.infer<typeof posSaleLineDtoSchema>;
export type PosSaleDto = z.infer<typeof posSaleDtoSchema>;

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

/**
 * Online cash checkout. Omit snapshot fields; server prices from live catalog.
 * Do not send discounts (RMAP-11b), ManualGCash/Utang/Card (RMAP-12).
 */
export async function checkoutSale(
  workspace: PosWorkspaceScope,
  body: CheckoutSaleRequest,
  signal?: AbortSignal,
): Promise<PosSaleDto> {
  const validated = checkoutSaleRequestSchema.parse(body);
  const payload: Record<string, unknown> = {
    lines: validated.lines.map((line) => {
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
    }),
    paymentMethod: validated.paymentMethod,
    amountTendered: validated.amountTendered,
    saleId: validated.saleId,
    shiftId: validated.shiftId,
  };
  if (validated.customerId) {
    payload.customerId = validated.customerId;
  }

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: SALES_PATH,
    body: payload,
  });
  return parseSale(raw);
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
