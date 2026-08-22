import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError, posRequest } from "@/api/pos/pos-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

const LINKED_CUSTOMERS_PATH = "/api/v1/pos/personal/linked-customers";

export const linkedCustomerActivityItemSchema = z.object({
  activityId: guidSchema,
  occurredAtUtc: z.string(),
  type: z.string(),
  referenceNumber: z.string(),
  chargeAmount: z.number().nullable().optional(),
  paymentAmount: z.number().nullable().optional(),
  adjustmentAmount: z.number().nullable().optional(),
  balanceAfter: z.number().nullable().optional(),
  status: z.string(),
  hasDetails: z.boolean(),
  sourceSaleId: guidSchema.nullable().optional(),
});

export const linkedCustomerStatementSummarySchema = z.object({
  organizationId: guidSchema,
  platformBusinessCustomerId: guidSchema,
  posCustomerId: guidSchema,
  linkedCustomerAppUserId: guidSchema,
  merchantDisplayName: z.string().nullable().optional(),
  customerDisplayName: z.string(),
  outstandingBalance: z.number(),
  currency: z.string(),
  asOfUtc: z.string(),
});

export const linkedCustomerRecentActivityPageSchema = z.object({
  organizationId: guidSchema,
  platformBusinessCustomerId: guidSchema,
  posCustomerId: guidSchema,
  items: z.array(linkedCustomerActivityItemSchema),
  page: z.number(),
  pageSize: z.number(),
  hasMore: z.boolean(),
  canAccessExtendedHistory: z.boolean(),
  freeHistoryStartsAtUtc: z.string(),
});

export const linkedCustomerOpenDebtActivityPageSchema = z.object({
  organizationId: guidSchema,
  platformBusinessCustomerId: guidSchema,
  posCustomerId: guidSchema,
  outstandingBalance: z.number(),
  items: z.array(linkedCustomerActivityItemSchema),
  page: z.number(),
  pageSize: z.number(),
  hasMore: z.boolean(),
});

export const linkedCustomerSaleReceiptLineSchema = z.object({
  lineNumber: z.number(),
  productNameSnapshot: z.string(),
  quantity: z.number(),
  unitOfMeasure: z.string(),
  sellingMode: z.string(),
  unitPriceSnapshot: z.number(),
  lineTotal: z.number(),
});

export const linkedCustomerSaleReceiptSchema = z.object({
  organizationId: guidSchema,
  platformBusinessCustomerId: guidSchema,
  posCustomerId: guidSchema,
  saleId: guidSchema,
  receiptNumber: z.string(),
  occurredAtUtc: z.string(),
  status: z.string(),
  paymentMethod: z.string(),
  currency: z.string(),
  merchantDisplayName: z.string().nullable().optional(),
  branchDisplayName: z.string().nullable().optional(),
  subtotal: z.number(),
  discountAmount: z.number().nullable().optional(),
  taxAmount: z.number(),
  total: z.number(),
  utangAmount: z.number().nullable().optional(),
  paidAmount: z.number().nullable().optional(),
  outstandingEffect: z.number().nullable().optional(),
  lines: z.array(linkedCustomerSaleReceiptLineSchema),
});

export type LinkedCustomerActivityItem = z.infer<typeof linkedCustomerActivityItemSchema>;
export type LinkedCustomerStatementSummary = z.infer<typeof linkedCustomerStatementSummarySchema>;
export type LinkedCustomerRecentActivityPage = z.infer<
  typeof linkedCustomerRecentActivityPageSchema
>;
export type LinkedCustomerOpenDebtActivityPage = z.infer<
  typeof linkedCustomerOpenDebtActivityPageSchema
>;
export type LinkedCustomerSaleReceipt = z.infer<typeof linkedCustomerSaleReceiptSchema>;

export const EXTENDED_HISTORY_REQUIRED = "pos.personal.extended_history_required";

function personalOrgScope(organizationId: string): PosWorkspaceScope {
  return { organizationId };
}

function appendQuery(path: string, query: Record<string, string | number | undefined>): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== "") {
      params.set(key, String(value));
    }
  }
  const qs = params.toString();
  return qs ? `${path}?${qs}` : path;
}

function normalizeActivityItem(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    activityId: r.activityId ?? r.ActivityId,
    occurredAtUtc: r.occurredAtUtc ?? r.OccurredAtUtc,
    type: r.type ?? r.Type,
    referenceNumber: r.referenceNumber ?? r.ReferenceNumber,
    chargeAmount: r.chargeAmount ?? r.ChargeAmount ?? null,
    paymentAmount: r.paymentAmount ?? r.PaymentAmount ?? null,
    adjustmentAmount: r.adjustmentAmount ?? r.AdjustmentAmount ?? null,
    balanceAfter: r.balanceAfter ?? r.BalanceAfter ?? null,
    status: r.status ?? r.Status,
    hasDetails: r.hasDetails ?? r.HasDetails ?? false,
    sourceSaleId: r.sourceSaleId ?? r.SourceSaleId ?? null,
  };
}

function normalizeActivityPage(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  const items = (r.items ?? r.Items ?? []) as unknown[];
  return {
    organizationId: r.organizationId ?? r.OrganizationId,
    platformBusinessCustomerId: r.platformBusinessCustomerId ?? r.PlatformBusinessCustomerId,
    posCustomerId: r.posCustomerId ?? r.PosCustomerId,
    items: items.map(normalizeActivityItem),
    page: r.page ?? r.Page ?? 1,
    pageSize: r.pageSize ?? r.PageSize ?? 10,
    hasMore: r.hasMore ?? r.HasMore ?? false,
    canAccessExtendedHistory: r.canAccessExtendedHistory ?? r.CanAccessExtendedHistory ?? false,
    freeHistoryStartsAtUtc: r.freeHistoryStartsAtUtc ?? r.FreeHistoryStartsAtUtc ?? "",
  };
}

function normalizeStatement(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    organizationId: r.organizationId ?? r.OrganizationId,
    platformBusinessCustomerId: r.platformBusinessCustomerId ?? r.PlatformBusinessCustomerId,
    posCustomerId: r.posCustomerId ?? r.PosCustomerId,
    linkedCustomerAppUserId: r.linkedCustomerAppUserId ?? r.LinkedCustomerAppUserId,
    merchantDisplayName: r.merchantDisplayName ?? r.MerchantDisplayName ?? null,
    customerDisplayName: r.customerDisplayName ?? r.CustomerDisplayName,
    outstandingBalance: r.outstandingBalance ?? r.OutstandingBalance,
    currency: r.currency ?? r.Currency,
    asOfUtc: r.asOfUtc ?? r.AsOfUtc,
  };
}

function normalizeOpenDebtPage(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  const items = (r.items ?? r.Items ?? []) as unknown[];
  return {
    organizationId: r.organizationId ?? r.OrganizationId,
    platformBusinessCustomerId: r.platformBusinessCustomerId ?? r.PlatformBusinessCustomerId,
    posCustomerId: r.posCustomerId ?? r.PosCustomerId,
    outstandingBalance: r.outstandingBalance ?? r.OutstandingBalance,
    items: items.map(normalizeActivityItem),
    page: r.page ?? r.Page ?? 1,
    pageSize: r.pageSize ?? r.PageSize ?? 10,
    hasMore: r.hasMore ?? r.HasMore ?? false,
  };
}

function normalizeReceipt(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  const lines = (r.lines ?? r.Lines ?? []) as unknown[];
  return {
    organizationId: r.organizationId ?? r.OrganizationId,
    platformBusinessCustomerId: r.platformBusinessCustomerId ?? r.PlatformBusinessCustomerId,
    posCustomerId: r.posCustomerId ?? r.PosCustomerId,
    saleId: r.saleId ?? r.SaleId,
    receiptNumber: r.receiptNumber ?? r.ReceiptNumber,
    occurredAtUtc: r.occurredAtUtc ?? r.OccurredAtUtc,
    status: r.status ?? r.Status,
    paymentMethod: r.paymentMethod ?? r.PaymentMethod,
    currency: r.currency ?? r.Currency,
    merchantDisplayName: r.merchantDisplayName ?? r.MerchantDisplayName ?? null,
    branchDisplayName: r.branchDisplayName ?? r.BranchDisplayName ?? null,
    subtotal: r.subtotal ?? r.Subtotal,
    discountAmount: r.discountAmount ?? r.DiscountAmount ?? null,
    taxAmount: r.taxAmount ?? r.TaxAmount,
    total: r.total ?? r.Total,
    utangAmount: r.utangAmount ?? r.UtangAmount ?? null,
    paidAmount: r.paidAmount ?? r.PaidAmount ?? null,
    outstandingEffect: r.outstandingEffect ?? r.OutstandingEffect ?? null,
    lines: lines.map((line) => {
      const l = (line ?? {}) as Record<string, unknown>;
      return {
        lineNumber: l.lineNumber ?? l.LineNumber,
        productNameSnapshot: l.productNameSnapshot ?? l.ProductNameSnapshot,
        quantity: l.quantity ?? l.Quantity,
        unitOfMeasure: l.unitOfMeasure ?? l.UnitOfMeasure,
        sellingMode: l.sellingMode ?? l.SellingMode,
        unitPriceSnapshot: l.unitPriceSnapshot ?? l.UnitPriceSnapshot,
        lineTotal: l.lineTotal ?? l.LineTotal,
      };
    }),
  };
}

export async function getLinkedCustomerStatement(
  organizationId: string,
  platformBusinessCustomerId: string,
  options: { currency?: string; signal?: AbortSignal } = {},
): Promise<LinkedCustomerStatementSummary> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace: personalOrgScope(organizationId),
    signal: options.signal,
    path: appendQuery(
      `${LINKED_CUSTOMERS_PATH}/${encodeURIComponent(platformBusinessCustomerId)}/statement`,
      { organizationId, currency: options.currency ?? "PHP" },
    ),
  });
  return linkedCustomerStatementSummarySchema.parse(normalizeStatement(raw));
}

export async function listLinkedCustomerRecentActivity(
  organizationId: string,
  platformBusinessCustomerId: string,
  options: { page?: number; pageSize?: number; signal?: AbortSignal } = {},
): Promise<LinkedCustomerRecentActivityPage> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace: personalOrgScope(organizationId),
    signal: options.signal,
    path: appendQuery(
      `${LINKED_CUSTOMERS_PATH}/${encodeURIComponent(platformBusinessCustomerId)}/activity`,
      {
        organizationId,
        page: options.page ?? 1,
        pageSize: Math.min(options.pageSize ?? 10, 20),
      },
    ),
  });
  return linkedCustomerRecentActivityPageSchema.parse(normalizeActivityPage(raw));
}

export async function listLinkedCustomerOpenDebtActivity(
  organizationId: string,
  platformBusinessCustomerId: string,
  options: { page?: number; pageSize?: number; signal?: AbortSignal } = {},
): Promise<LinkedCustomerOpenDebtActivityPage> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace: personalOrgScope(organizationId),
    signal: options.signal,
    path: appendQuery(
      `${LINKED_CUSTOMERS_PATH}/${encodeURIComponent(platformBusinessCustomerId)}/open-debt-activity`,
      {
        organizationId,
        page: options.page ?? 1,
        pageSize: Math.min(options.pageSize ?? 10, 20),
      },
    ),
  });
  return linkedCustomerOpenDebtActivityPageSchema.parse(normalizeOpenDebtPage(raw));
}

export async function listLinkedCustomerOlderActivity(
  organizationId: string,
  platformBusinessCustomerId: string,
  options: { page?: number; pageSize?: number; signal?: AbortSignal } = {},
): Promise<LinkedCustomerRecentActivityPage> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace: personalOrgScope(organizationId),
    signal: options.signal,
    path: appendQuery(
      `${LINKED_CUSTOMERS_PATH}/${encodeURIComponent(platformBusinessCustomerId)}/older-activity`,
      {
        organizationId,
        page: options.page ?? 1,
        pageSize: Math.min(options.pageSize ?? 10, 20),
      },
    ),
  });
  return linkedCustomerRecentActivityPageSchema.parse(normalizeActivityPage(raw));
}

export async function getLinkedCustomerSaleReceipt(
  organizationId: string,
  platformBusinessCustomerId: string,
  saleId: string,
  options: { currency?: string; signal?: AbortSignal } = {},
): Promise<LinkedCustomerSaleReceipt> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace: personalOrgScope(organizationId),
    signal: options.signal,
    path: appendQuery(
      `${LINKED_CUSTOMERS_PATH}/${encodeURIComponent(platformBusinessCustomerId)}/receipts/${encodeURIComponent(saleId)}`,
      { organizationId, currency: options.currency ?? "PHP" },
    ),
  });
  return linkedCustomerSaleReceiptSchema.parse(normalizeReceipt(raw));
}

export function isExtendedHistoryRequiredError(error: unknown): boolean {
  return error instanceof PosApiError && error.errorCode === EXTENDED_HISTORY_REQUIRED;
}
