import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const PATH = "/api/v1/pos/quotations";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const quotationLineSchema = z.object({
  lineId: guidSchema,
  productId: guidSchema,
  lineNumber: z.number(),
  nameSnapshot: z.string().nullable().optional(),
  skuSnapshot: z.string().nullable().optional(),
  uomSnapshot: z.string().nullable().optional(),
  quantity: z.number(),
  unitPrice: z.number(),
  discountAmount: z.number().nullable().optional(),
  lineTotal: z.number(),
});

export const quotationSchema = z.object({
  quotationId: guidSchema,
  organizationId: guidSchema,
  quotationNumber: z.string().nullable().optional(),
  status: z.string(),
  customerId: guidSchema,
  customerDisplayName: z.string(),
  customerMobileNumber: z.string().nullable().optional(),
  customerAddress: z.string().nullable().optional(),
  customerNotes: z.string().nullable().optional(),
  branchId: guidSchema,
  preparedBy: guidSchema,
  validUntil: z.string().nullable().optional(),
  reference: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  terms: z.string().nullable().optional(),
  convertedSaleId: guidSchema.nullable().optional(),
  isCustomerVisible: z.boolean().optional().default(false),
  issuedAtUtc: z.string().nullable().optional(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  subtotal: z.number(),
  lines: z.array(quotationLineSchema),
});

export const quotationPagedSchema = z.object({
  items: z.array(quotationSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type PosQuotation = z.infer<typeof quotationSchema>;
export type PosQuotationLine = z.infer<typeof quotationLineSchema>;
export type PosQuotationPaged = z.infer<typeof quotationPagedSchema>;

export type CreateQuotationLineInput = {
  productId: string;
  quantity: number;
  unitPrice: number;
  discountAmount?: number | null;
};

export type CreateQuotationInput = {
  customerId: string;
  branchId: string;
  lines: CreateQuotationLineInput[];
  validUntil?: string | null;
  reference?: string | null;
  notes?: string | null;
  terms?: string | null;
  quotationId?: string;
  isCustomerVisible?: boolean;
};

export type UpdateQuotationInput = CreateQuotationInput & {
  expectedUpdatedAtUtc: string;
};

function normalizeLine(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    lineId: pick(r, "lineId", "LineId"),
    productId: pick(r, "productId", "ProductId"),
    lineNumber: pick(r, "lineNumber", "LineNumber"),
    nameSnapshot: pick(r, "nameSnapshot", "NameSnapshot") ?? null,
    skuSnapshot: pick(r, "skuSnapshot", "SkuSnapshot") ?? null,
    uomSnapshot: pick(r, "uomSnapshot", "UomSnapshot") ?? null,
    quantity: pick(r, "quantity", "Quantity"),
    unitPrice: pick(r, "unitPrice", "UnitPrice"),
    discountAmount: pick(r, "discountAmount", "DiscountAmount") ?? null,
    lineTotal: pick(r, "lineTotal", "LineTotal"),
  };
}

function normalizeQuotation(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  const lines = ((pick(r, "lines", "Lines") as unknown[]) ?? []).map(normalizeLine);
  return {
    quotationId: pick(r, "quotationId", "QuotationId"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    quotationNumber: pick(r, "quotationNumber", "QuotationNumber") ?? null,
    status: pick(r, "status", "Status"),
    customerId: pick(r, "customerId", "CustomerId"),
    customerDisplayName: pick(r, "customerDisplayName", "CustomerDisplayName") ?? "",
    customerMobileNumber: pick(r, "customerMobileNumber", "CustomerMobileNumber") ?? null,
    customerAddress: pick(r, "customerAddress", "CustomerAddress") ?? null,
    customerNotes: pick(r, "customerNotes", "CustomerNotes") ?? null,
    branchId: pick(r, "branchId", "BranchId"),
    preparedBy: pick(r, "preparedBy", "PreparedBy"),
    validUntil: pick(r, "validUntil", "ValidUntil") ?? null,
    reference: pick(r, "reference", "Reference") ?? null,
    notes: pick(r, "notes", "Notes") ?? null,
    terms: pick(r, "terms", "Terms") ?? null,
    convertedSaleId: pick(r, "convertedSaleId", "ConvertedSaleId") ?? null,
    isCustomerVisible: pick(r, "isCustomerVisible", "IsCustomerVisible") ?? false,
    issuedAtUtc: pick(r, "issuedAtUtc", "IssuedAtUtc") ?? null,
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
    subtotal: pick(r, "subtotal", "Subtotal"),
    lines,
  };
}

function normalizePaged(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  const items = ((pick(r, "items", "Items") as unknown[]) ?? []).map(normalizeQuotation);
  return {
    items,
    totalCount: pick(r, "totalCount", "TotalCount") ?? items.length,
    page: pick(r, "page", "Page") ?? 1,
    pageSize: pick(r, "pageSize", "PageSize") ?? items.length,
  };
}

export async function listQuotations(
  workspace: PosWorkspaceScope,
  params: { status?: string; page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosQuotationPaged> {
  const search = new URLSearchParams();
  if (params.status) search.set("status", params.status);
  if (params.page != null) search.set("page", String(params.page));
  if (params.pageSize != null) search.set("pageSize", String(params.pageSize));
  const qs = search.toString();
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: qs ? `${PATH}?${qs}` : PATH,
  });
  return quotationPagedSchema.parse(normalizePaged(raw));
}

export async function getQuotation(
  workspace: PosWorkspaceScope,
  quotationId: string,
  signal?: AbortSignal,
): Promise<PosQuotation> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${PATH}/${quotationId}`,
  });
  return quotationSchema.parse(normalizeQuotation(raw));
}

export async function createQuotation(
  workspace: PosWorkspaceScope,
  input: CreateQuotationInput,
  signal?: AbortSignal,
): Promise<PosQuotation> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: PATH,
    body: input,
  });
  return quotationSchema.parse(normalizeQuotation(raw));
}

export async function updateQuotation(
  workspace: PosWorkspaceScope,
  quotationId: string,
  input: UpdateQuotationInput,
  signal?: AbortSignal,
): Promise<PosQuotation> {
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${PATH}/${quotationId}`,
    body: input,
  });
  return quotationSchema.parse(normalizeQuotation(raw));
}

export async function issueQuotation(
  workspace: PosWorkspaceScope,
  quotationId: string,
  signal?: AbortSignal,
): Promise<PosQuotation> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${quotationId}/issue`,
  });
  return quotationSchema.parse(normalizeQuotation(raw));
}

export async function cancelQuotation(
  workspace: PosWorkspaceScope,
  quotationId: string,
  signal?: AbortSignal,
): Promise<PosQuotation> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${PATH}/${quotationId}/cancel`,
  });
  return quotationSchema.parse(normalizeQuotation(raw));
}

export const PENDING_QUOTATION_CONVERT_KEY = "exits.pos.pending-quotation-convert.v1";

export type PendingQuotationConvert = {
  quotationId: string;
  customerId: string;
  customerDisplayName: string;
  lines: Array<{ productId: string; quantity: number; unitPrice: number; name?: string }>;
};

export function writePendingQuotationConvert(payload: PendingQuotationConvert): void {
  sessionStorage.setItem(PENDING_QUOTATION_CONVERT_KEY, JSON.stringify(payload));
}

export function readPendingQuotationConvert(): PendingQuotationConvert | null {
  try {
    const raw = sessionStorage.getItem(PENDING_QUOTATION_CONVERT_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as PendingQuotationConvert;
  } catch {
    return null;
  }
}

export function clearPendingQuotationConvert(): void {
  sessionStorage.removeItem(PENDING_QUOTATION_CONVERT_KEY);
}
