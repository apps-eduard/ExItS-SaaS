import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const CUSTOMERS_PATH = "/api/v1/pos/customers";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const posCustomerListItemSchema = z.object({
  customerId: guidSchema,
  organizationId: guidSchema,
  displayName: z.string(),
  mobileNumber: z.string().nullable().optional(),
  address: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
  status: z.string(),
  platformBusinessCustomerId: guidSchema.nullable().optional(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  linkedPersonalPublicUserId: z.string().nullable().optional(),
  linkedBuyerOrganizationId: guidSchema.nullable().optional(),
  linkedBuyerPublicOrganizationId: z.string().nullable().optional(),
});

export const posCustomerDetailSchema = posCustomerListItemSchema;

export const posCustomerPagedResultSchema = z.object({
  items: z.array(posCustomerListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const posCustomerCreditSummarySchema = z.object({
  customerId: guidSchema,
  organizationId: guidSchema,
  outstandingAmount: z.number(),
  activeEntryCount: z.number(),
  totalEntryCount: z.number(),
});

export const posCreditEntrySchema = z.object({
  creditEntryId: guidSchema,
  organizationId: guidSchema,
  customerId: guidSchema,
  amount: z.number(),
  remarks: z.string(),
  status: z.string(),
  createdAtUtc: z.string(),
  reversedAtUtc: z.string().nullable().optional(),
  reversalReason: z.string().nullable().optional(),
  currentDueDate: z.string().nullable().optional(),
  sourceSaleId: guidSchema.nullable().optional(),
});

export const posCreditEntryPagedResultSchema = z.object({
  items: z.array(posCreditEntrySchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const posRepaymentSchema = z.object({
  repaymentId: guidSchema,
  organizationId: guidSchema,
  customerId: guidSchema,
  amount: z.number(),
  remarks: z.string().nullable().optional(),
  status: z.string(),
  recordedAtUtc: z.string(),
  recordedBy: guidSchema,
  reversedAtUtc: z.string().nullable().optional(),
  reversalReason: z.string().nullable().optional(),
  reversedBy: guidSchema.nullable().optional(),
});

export const posRepaymentPagedResultSchema = z.object({
  items: z.array(posRepaymentSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const posLedgerEntrySchema = z.object({
  entryId: guidSchema,
  entryType: z.string(),
  organizationId: guidSchema,
  customerId: guidSchema,
  amount: z.number(),
  signedEffect: z.number(),
  remarks: z.string().nullable().optional(),
  status: z.string(),
  recordedAtUtc: z.string(),
  recordedBy: guidSchema.nullable().optional(),
  reversedAtUtc: z.string().nullable().optional(),
  reversalReason: z.string().nullable().optional(),
  reversedBy: guidSchema.nullable().optional(),
  runningBalance: z.number().nullable().optional(),
});

export const posLedgerPagedResultSchema = z.object({
  items: z.array(posLedgerEntrySchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const posCustomerStatementLineSchema = z.object({
  entryId: guidSchema,
  entryType: z.string(),
  recordedAtUtc: z.string(),
  amount: z.number(),
  signedEffect: z.number(),
  status: z.string(),
  remarks: z.string().nullable().optional(),
  dueDate: z.string().nullable().optional(),
  dueStatus: z.string().nullable().optional(),
  isOverdue: z.boolean(),
  isReversed: z.boolean(),
  runningBalance: z.number(),
});

export const posCustomerStatementSchema = z.object({
  organizationId: guidSchema,
  organizationDisplayName: z.string().nullable().optional(),
  customerId: guidSchema,
  customerDisplayName: z.string(),
  periodStart: z.string(),
  periodEnd: z.string(),
  openingBalance: z.number(),
  closingBalance: z.number(),
  periodCreditTotal: z.number(),
  periodRepaymentTotal: z.number(),
  periodReversalCreditTotal: z.number(),
  periodReversalRepaymentTotal: z.number(),
  outstandingBalance: z.number(),
  overdueAmount: z.number(),
  overdueCreditCount: z.number(),
  generatedAtUtc: z.string(),
  currencyCode: z.string(),
  cultureName: z.string(),
  lines: z.array(posCustomerStatementLineSchema),
});

export type PosCustomerListItem = z.infer<typeof posCustomerListItemSchema>;
export type PosCustomerDetail = z.infer<typeof posCustomerDetailSchema>;
export type PosCustomerPagedResult = z.infer<typeof posCustomerPagedResultSchema>;
export type PosCustomerCreditSummary = z.infer<typeof posCustomerCreditSummarySchema>;
export type PosCreditEntry = z.infer<typeof posCreditEntrySchema>;
export type PosCreditEntryPagedResult = z.infer<typeof posCreditEntryPagedResultSchema>;
export type PosRepayment = z.infer<typeof posRepaymentSchema>;
export type PosRepaymentPagedResult = z.infer<typeof posRepaymentPagedResultSchema>;
export type PosLedgerEntry = z.infer<typeof posLedgerEntrySchema>;
export type PosLedgerPagedResult = z.infer<typeof posLedgerPagedResultSchema>;
export type PosCustomerStatement = z.infer<typeof posCustomerStatementSchema>;

export type CreatePosCustomerInput = {
  displayName: string;
  mobileNumber?: string | null;
  address?: string | null;
  notes?: string | null;
};

export type UpdatePosCustomerInput = CreatePosCustomerInput & {
  expectedUpdatedAtUtc?: string | null;
};

export type CreatePosRepaymentInput = {
  amount: number;
  remarks?: string | null;
  repaymentId?: string;
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

function customerPath(customerId: string, suffix = ""): string {
  return `${CUSTOMERS_PATH}/${customerId}${suffix}`;
}

/**
 * Active-customer list/search.
 * Requires ViewCustomersAndHistory — Cashier lacks this capability.
 */
export async function listCustomers(
  workspace: PosWorkspaceScope,
  options: {
    status?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  } = {},
  signal?: AbortSignal,
): Promise<PosCustomerPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(CUSTOMERS_PATH, {
      status: options.status ?? "Active",
      search: options.search,
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posCustomerPagedResultSchema.parse(raw);
}

export async function getCustomer(
  workspace: PosWorkspaceScope,
  customerId: string,
  signal?: AbortSignal,
): Promise<PosCustomerDetail> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: customerPath(customerId),
  });
  return posCustomerDetailSchema.parse(raw);
}

export async function createCustomer(
  workspace: PosWorkspaceScope,
  input: CreatePosCustomerInput,
  signal?: AbortSignal,
): Promise<PosCustomerDetail> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: CUSTOMERS_PATH,
    body: {
      displayName: input.displayName.trim(),
      mobileNumber: input.mobileNumber?.trim() || null,
      address: input.address?.trim() || null,
      notes: input.notes?.trim() || null,
    },
  });
  return posCustomerDetailSchema.parse(raw);
}

export async function updateCustomer(
  workspace: PosWorkspaceScope,
  customerId: string,
  input: UpdatePosCustomerInput,
  signal?: AbortSignal,
): Promise<PosCustomerDetail> {
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: customerPath(customerId),
    body: {
      displayName: input.displayName.trim(),
      mobileNumber: input.mobileNumber?.trim() || null,
      address: input.address?.trim() || null,
      notes: input.notes?.trim() || null,
      expectedUpdatedAtUtc: input.expectedUpdatedAtUtc ?? null,
    },
  });
  return posCustomerDetailSchema.parse(raw);
}

export async function deactivateCustomer(
  workspace: PosWorkspaceScope,
  customerId: string,
  signal?: AbortSignal,
): Promise<PosCustomerDetail> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: customerPath(customerId, "/deactivate"),
  });
  return posCustomerDetailSchema.parse(raw);
}

export async function reactivateCustomer(
  workspace: PosWorkspaceScope,
  customerId: string,
  signal?: AbortSignal,
): Promise<PosCustomerDetail> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: customerPath(customerId, "/reactivate"),
  });
  return posCustomerDetailSchema.parse(raw);
}

export async function getCustomerCreditSummary(
  workspace: PosWorkspaceScope,
  customerId: string,
  signal?: AbortSignal,
): Promise<PosCustomerCreditSummary> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: customerPath(customerId, "/credit-summary"),
  });
  return posCustomerCreditSummarySchema.parse(raw);
}

export async function listCustomerCreditEntries(
  workspace: PosWorkspaceScope,
  customerId: string,
  options: { page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosCreditEntryPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(customerPath(customerId, "/credit-entries"), {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posCreditEntryPagedResultSchema.parse(raw);
}

export async function listCustomerRepayments(
  workspace: PosWorkspaceScope,
  customerId: string,
  options: { page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosRepaymentPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(customerPath(customerId, "/repayments"), {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posRepaymentPagedResultSchema.parse(raw);
}

export async function createCustomerRepayment(
  workspace: PosWorkspaceScope,
  customerId: string,
  input: CreatePosRepaymentInput,
  signal?: AbortSignal,
): Promise<PosRepayment> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: customerPath(customerId, "/repayments"),
    body: {
      amount: input.amount,
      remarks: input.remarks?.trim() || null,
      ...(input.repaymentId ? { repaymentId: input.repaymentId } : {}),
    },
  });
  return posRepaymentSchema.parse(raw);
}

export async function listCustomerLedger(
  workspace: PosWorkspaceScope,
  customerId: string,
  options: { page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosLedgerPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(customerPath(customerId, "/ledger"), {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
    }),
  });
  return posLedgerPagedResultSchema.parse(raw);
}

export async function getCustomerStatement(
  workspace: PosWorkspaceScope,
  customerId: string,
  options: {
    periodStart: string;
    periodEnd: string;
    organizationDisplayName?: string;
    currencyCode?: string;
    culture?: string;
  },
  signal?: AbortSignal,
): Promise<PosCustomerStatement> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(customerPath(customerId, "/statement"), {
      periodStart: options.periodStart,
      periodEnd: options.periodEnd,
      organizationDisplayName: options.organizationDisplayName,
      currencyCode: options.currencyCode,
      culture: options.culture,
    }),
  });
  return posCustomerStatementSchema.parse(raw);
}

/** True when POS customer is linked to an ExItS Personal identity (read-only surface). */
export function hasExItsPersonalLink(
  customer: Pick<PosCustomerListItem, "linkedPersonalPublicUserId">,
): boolean {
  return Boolean(customer.linkedPersonalPublicUserId?.trim());
}
