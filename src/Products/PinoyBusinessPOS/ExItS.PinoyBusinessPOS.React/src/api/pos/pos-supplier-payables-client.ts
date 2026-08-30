import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const PAYABLES_PATH = "/api/v1/pos/supplier-payables";
const REPORTS_PATH = "/api/v1/pos/reports/supplier-payables";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const SUPPLIER_PAYABLE_STATUSES = ["Open", "PartiallyPaid", "Paid", "Voided"] as const;
export type SupplierPayableStatusCode = (typeof SUPPLIER_PAYABLE_STATUSES)[number];

export const SUPPLIER_PAYABLE_PAYMENT_METHODS = [
  "Cash",
  "BankTransfer",
  "GCash",
  "Other",
] as const;
export type SupplierPayablePaymentMethodCode = (typeof SUPPLIER_PAYABLE_PAYMENT_METHODS)[number];

export const SUPPLIER_PAYABLE_SOURCE_TYPES = ["GoodsReceipt", "DirectPurchaseReceipt"] as const;
export type SupplierPayableSourceTypeCode = (typeof SUPPLIER_PAYABLE_SOURCE_TYPES)[number];

export const SUPPLIER_PAYABLE_PAYMENT_REFERENCE_MAX = 128;
export const SUPPLIER_PAYABLE_PAYMENT_NOTES_MAX = 512;

export const supplierPayableDtoSchema = z.object({
  payableId: guidSchema,
  organizationId: guidSchema,
  supplierId: guidSchema,
  supplierName: z.string().nullable().optional(),
  sourceType: z.string(),
  sourceId: guidSchema,
  originalAmount: z.number(),
  paidAtReceiptAmount: z.number(),
  paidAmount: z.number(),
  balance: z.number(),
  status: z.string(),
  dueDate: z.string().nullable().optional(),
  paymentMethodAtReceipt: z.string().nullable().optional(),
  createdAtUtc: z.string(),
  createdBy: guidSchema,
  updatedAtUtc: z.string(),
  voidedAtUtc: z.string().nullable().optional(),
  voidedBy: guidSchema.nullable().optional(),
  voidReason: z.string().nullable().optional(),
  hasPostedPayments: z.boolean(),
  isOverdue: z.boolean(),
});

export const supplierPayablePagedResultSchema = z.object({
  items: z.array(supplierPayableDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const supplierPayablePaymentDtoSchema = z.object({
  paymentId: guidSchema,
  payableId: guidSchema,
  amount: z.number(),
  paymentMethod: z.string(),
  reference: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  paidAtUtc: z.string(),
  recordedBy: guidSchema,
  recordedAtUtc: z.string(),
});

export const supplierPayableSummaryDtoSchema = z.object({
  supplierId: guidSchema,
  outstandingTotal: z.number(),
  overdueTotal: z.number(),
  openCount: z.number(),
});

export const supplierPayableReportRowDtoSchema = z.object({
  payableId: guidSchema,
  supplierId: guidSchema,
  supplierName: z.string().nullable().optional(),
  sourceType: z.string(),
  sourceId: guidSchema,
  originalAmount: z.number(),
  paidAtReceiptAmount: z.number(),
  paidAmount: z.number(),
  balance: z.number(),
  status: z.string(),
  dueDate: z.string().nullable().optional(),
  isOverdue: z.boolean(),
  createdAtUtc: z.string(),
});

export const supplierPayableReportSummaryDtoSchema = z.object({
  outstandingTotal: z.number(),
  overdueTotal: z.number(),
  openCount: z.number(),
  partiallyPaidCount: z.number(),
  paidCount: z.number(),
  voidedCount: z.number(),
});

export const supplierPayableSupplierBalanceDtoSchema = z.object({
  supplierId: guidSchema,
  supplierName: z.string().nullable().optional(),
  outstandingBalance: z.number(),
  overdueBalance: z.number(),
  openPayables: z.number(),
  oldestDueDate: z.string().nullable().optional(),
});

export const supplierPayableReportDtoSchema = z.object({
  asOfDate: z.string(),
  summary: supplierPayableReportSummaryDtoSchema,
  suppliers: z.array(supplierPayableSupplierBalanceDtoSchema),
  payables: z.array(supplierPayableReportRowDtoSchema),
});

export type PosSupplierPayableDto = z.infer<typeof supplierPayableDtoSchema>;
export type PosSupplierPayablePagedResult = z.infer<typeof supplierPayablePagedResultSchema>;
export type PosSupplierPayablePaymentDto = z.infer<typeof supplierPayablePaymentDtoSchema>;
export type PosSupplierPayableSummaryDto = z.infer<typeof supplierPayableSummaryDtoSchema>;
export type PosSupplierPayableReportRowDto = z.infer<typeof supplierPayableReportRowDtoSchema>;
export type PosSupplierPayableReportDto = z.infer<typeof supplierPayableReportDtoSchema>;
export type PosSupplierPayableReportSummaryDto = z.infer<typeof supplierPayableReportSummaryDtoSchema>;
export type PosSupplierPayableSupplierBalanceDto = z.infer<
  typeof supplierPayableSupplierBalanceDtoSchema
>;

export type ListSupplierPayablesOptions = {
  supplierId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
};

export type RecordSupplierPayablePaymentRequest = {
  amount: number;
  paymentMethod: SupplierPayablePaymentMethodCode | string;
  reference?: string | null;
  notes?: string | null;
  paidAtUtc?: string | null;
  /** Client-generated idempotency entity id (not sent in body). */
  paymentId?: string | null;
};

export type ListSupplierPayableReportOptions = {
  supplierId?: string;
  status?: string;
  outstandingOnly?: boolean;
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

/** Organization-scoped workspace — supplier payables have no BranchId. */
export function supplierPayableWorkspaceScope(organizationId: string): PosWorkspaceScope {
  return { organizationId };
}

export async function listSupplierPayables(
  workspace: PosWorkspaceScope,
  options: ListSupplierPayablesOptions = {},
  signal?: AbortSignal,
): Promise<PosSupplierPayablePagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(PAYABLES_PATH, {
      supplierId: options.supplierId,
      status: options.status,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
    }),
  });
  return supplierPayablePagedResultSchema.parse(raw);
}

export async function getSupplierPayable(
  workspace: PosWorkspaceScope,
  payableId: string,
  signal?: AbortSignal,
): Promise<PosSupplierPayableDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PAYABLES_PATH}/${payableId}`,
  });
  return supplierPayableDtoSchema.parse(raw);
}

export async function listSupplierPayablePayments(
  workspace: PosWorkspaceScope,
  payableId: string,
  signal?: AbortSignal,
): Promise<PosSupplierPayablePaymentDto[]> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PAYABLES_PATH}/${payableId}/payments`,
  });
  return z.array(supplierPayablePaymentDtoSchema).parse(raw);
}

/**
 * Records a supplier payable payment (online-only). Uses supplier_payable.payment
 * idempotency headers with a client-generated payment operation id.
 */
export async function recordSupplierPayablePayment(
  workspace: PosWorkspaceScope,
  payableId: string,
  body: RecordSupplierPayablePaymentRequest,
  signal?: AbortSignal,
): Promise<PosSupplierPayablePaymentDto> {
  const paymentId = body.paymentId?.trim() || crypto.randomUUID();
  const payload: Record<string, unknown> = {
    amount: body.amount,
    paymentMethod: body.paymentMethod,
  };
  const reference = trimOrUndef(body.reference);
  if (reference) {
    payload.reference = reference;
  }
  const notes = trimOrUndef(body.notes);
  if (notes) {
    payload.notes = notes;
  }
  if (body.paidAtUtc?.trim()) {
    payload.paidAtUtc = body.paidAtUtc.trim();
  }

  const headers = await buildPosMutationIdempotencyHeaders(
    paymentId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.SupplierPayablePayment,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PAYABLES_PATH}/${payableId}/payments`,
    body: payload,
    headers,
  });
  return supplierPayablePaymentDtoSchema.parse(raw);
}

export async function getSupplierPayableSummary(
  workspace: PosWorkspaceScope,
  supplierId: string,
  signal?: AbortSignal,
): Promise<PosSupplierPayableSummaryDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `/api/v1/pos/suppliers/${supplierId}/payable-summary`,
  });
  return supplierPayableSummaryDtoSchema.parse(raw);
}

export async function getSupplierPayablesReport(
  workspace: PosWorkspaceScope,
  options: ListSupplierPayableReportOptions = {},
  signal?: AbortSignal,
): Promise<PosSupplierPayableReportDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(REPORTS_PATH, {
      supplierId: options.supplierId,
      status: options.status,
      outstandingOnly: options.outstandingOnly ?? false,
    }),
  });
  return supplierPayableReportDtoSchema.parse(raw);
}
