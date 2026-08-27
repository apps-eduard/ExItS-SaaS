import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError, posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";
import { createSecureMutationId } from "@/lib/secure-mutation-id";

const CUSTOMERS_PATH = "/api/v1/pos/customers";
const REPAYMENTS_PATH = "/api/v1/pos/repayments";

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
  /** Client-chosen id so a replayed create returns the same customer instead of a duplicate. */
  customerId?: string;
  /** Platform BusinessCustomer correlation after with-personal-link. */
  platformBusinessCustomerId?: string | null;
};

export type UpdatePosCustomerInput = CreatePosCustomerInput & {
  expectedUpdatedAtUtc?: string | null;
  /** Stable id for this edit attempt; two different offline edits must not share one key. */
  operationId?: string;
};

export type CreatePosRepaymentInput = {
  amount: number;
  remarks?: string | null;
  repaymentId?: string;
};

/**
 * Idempotency headers for a customer/credit mutation, mirroring MAUI
 * PosMutationIdempotencyHelper. Returns no headers when secure randomness is unavailable and
 * the caller has no id to key on — the server treats a header-less mutation as non-idempotent
 * rather than trusting a guessable key.
 */
async function customerMutationHeaders(
  entityId: string | undefined,
  body: unknown,
  operationType: string,
): Promise<Record<string, string> | undefined> {
  if (!entityId) {
    return undefined;
  }
  return buildPosMutationIdempotencyHeaders(entityId, JSON.stringify(body), operationType);
}

function newMutationId(): string | undefined {
  const generated = createSecureMutationId();
  return generated.ok ? generated.id : undefined;
}

/** Payload the server expects for a customer create — shared by the online and offline paths. */
export function buildCreateCustomerPayload(input: CreatePosCustomerInput) {
  return {
    displayName: input.displayName.trim(),
    mobileNumber: input.mobileNumber?.trim() || null,
    address: input.address?.trim() || null,
    notes: input.notes?.trim() || null,
    ...(input.customerId ? { customerId: input.customerId } : {}),
    ...(input.platformBusinessCustomerId
      ? { platformBusinessCustomerId: input.platformBusinessCustomerId }
      : {}),
  };
}

/** Payload the server expects for a customer update — shared by the online and offline paths. */
export function buildUpdateCustomerPayload(input: UpdatePosCustomerInput) {
  return {
    displayName: input.displayName.trim(),
    mobileNumber: input.mobileNumber?.trim() || null,
    address: input.address?.trim() || null,
    notes: input.notes?.trim() || null,
    expectedUpdatedAtUtc: input.expectedUpdatedAtUtc ?? null,
  };
}

/** Payload the server expects for a repayment — shared by the online and offline paths. */
export function buildCreateRepaymentPayload(input: CreatePosRepaymentInput) {
  return {
    amount: input.amount,
    remarks: input.remarks?.trim() || null,
    ...(input.repaymentId ? { repaymentId: input.repaymentId } : {}),
  };
}

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

export const checkoutCustomerSearchItemSchema = z.object({
  customerId: guidSchema,
  displayName: z.string(),
  mobileNumber: z.string().nullable().optional(),
  status: z.string(),
});

export const checkoutCustomerSearchResultSchema = z.object({
  items: z.array(checkoutCustomerSearchItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type CheckoutCustomerSearchItem = z.infer<typeof checkoutCustomerSearchItemSchema>;
export type CheckoutCustomerSearchResult = z.infer<typeof checkoutCustomerSearchResultSchema>;

/**
 * Narrow Active-only checkout customer search.
 * Requires CreateSale (Cashier allowed). Does not require ViewCustomersAndHistory.
 * Search term must be non-blank; pageSize capped at 20 server-side.
 */
export async function searchCheckoutCustomers(
  workspace: PosWorkspaceScope,
  options: {
    search: string;
    page?: number;
    pageSize?: number;
  },
  signal?: AbortSignal,
): Promise<CheckoutCustomerSearchResult> {
  const search = options.search.trim();
  if (!search) {
    return { items: [], totalCount: 0, page: 1, pageSize: Math.min(options.pageSize ?? 20, 20) };
  }

  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${CUSTOMERS_PATH}/checkout-search`, {
      search,
      page: options.page ?? 1,
      pageSize: Math.min(options.pageSize ?? 20, 20),
    }),
  });
  return checkoutCustomerSearchResultSchema.parse(raw);
}

/**
 * Exact org-scoped lookup by linked Personal ExItS public ID (Active customers only).
 * Requires CreateSale. Returns null when no correlated customer exists in this organization.
 */
export async function findCustomerByLinkedPersonalPublicUserId(
  workspace: PosWorkspaceScope,
  personalPublicUserId: string,
  signal?: AbortSignal,
): Promise<CheckoutCustomerSearchItem | null> {
  const normalized = personalPublicUserId.trim();
  if (!normalized) {
    return null;
  }

  try {
    const raw = await posRequest<unknown>({
      method: "GET",
      workspace,
      signal,
      path: `${CUSTOMERS_PATH}/by-linked-personal/${encodeURIComponent(normalized)}`,
    });
    return checkoutCustomerSearchItemSchema.parse(raw);
  } catch (error) {
    if (error instanceof PosApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
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
  // The server adopts a client-chosen id and returns the existing row on replay, so a retried
  // create can never produce a second customer.
  const customerId = input.customerId ?? newMutationId();
  const body = buildCreateCustomerPayload({ ...input, customerId });
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: CUSTOMERS_PATH,
    body,
    headers: await customerMutationHeaders(
      customerId,
      body,
      OFFLINE_OPERATION_TYPES.CustomerCreate,
    ),
  });
  return posCustomerDetailSchema.parse(raw);
}

export async function updateCustomer(
  workspace: PosWorkspaceScope,
  customerId: string,
  input: UpdatePosCustomerInput,
  signal?: AbortSignal,
): Promise<PosCustomerDetail> {
  // Keyed on the edit attempt, not the customer, so two different offline edits of one customer
  // do not collide on a single idempotency key.
  const operationId = input.operationId ?? newMutationId();
  const body = buildUpdateCustomerPayload(input);
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: customerPath(customerId),
    body,
    headers: await customerMutationHeaders(
      operationId,
      body,
      OFFLINE_OPERATION_TYPES.CustomerUpdate,
    ),
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
  // A repayment is money: the client-chosen repaymentId plus the idempotency key make a retry
  // land on the same recorded payment instead of crediting the customer twice.
  const repaymentId = input.repaymentId ?? newMutationId();
  const body = buildCreateRepaymentPayload({ ...input, repaymentId });
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: customerPath(customerId, "/repayments"),
    body,
    headers: await customerMutationHeaders(
      repaymentId,
      body,
      OFFLINE_OPERATION_TYPES.RepaymentCreate,
    ),
  });
  return posRepaymentSchema.parse(raw);
}

/** GET /api/v1/pos/repayments/{repaymentId} — status lookup after ambiguous create. */
export async function getRepayment(
  workspace: PosWorkspaceScope,
  repaymentId: string,
  signal?: AbortSignal,
): Promise<PosRepayment> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${REPAYMENTS_PATH}/${repaymentId}`,
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
/**
 * True when the POS customer row stores a Personal ExItS public user id.
 * This is product-local identity correlation for checkout/lookup — NOT proof that
 * Platform CustomerLink status is Linked / LinkedCustomerAppUser is Active.
 */
export function hasExItsPersonalLink(
  customer: Pick<PosCustomerListItem, "linkedPersonalPublicUserId">,
): boolean {
  return Boolean(customer.linkedPersonalPublicUserId?.trim());
}
