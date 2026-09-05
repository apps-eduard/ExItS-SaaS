import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";

const CATEGORIES_PATH = "/api/v1/pos/expense-categories";
const EXPENSES_PATH = "/api/v1/pos/expenses";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const EXPENSE_STATUSES = ["Recorded", "Voided"] as const;
export type ExpenseStatusCode = (typeof EXPENSE_STATUSES)[number];

export const EXPENSE_PAYMENT_METHODS = ["Cash", "ManualGCash"] as const;
export type ExpensePaymentMethodCode = (typeof EXPENSE_PAYMENT_METHODS)[number];

export const EXPENSE_CATEGORY_STATUSES = ["Active", "Inactive"] as const;
export type ExpenseCategoryStatusCode = (typeof EXPENSE_CATEGORY_STATUSES)[number];

export const EXPENSE_DESCRIPTION_MAX = 512;
export const EXPENSE_PAYEE_MAX = 128;
export const EXPENSE_VOID_REASON_MAX = 512;
export const EXPENSE_GCASH_REFERENCE_MAX = 64;
export const EXPENSE_CATEGORY_NAME_MAX = 128;

export const expenseCategoryDtoSchema = z.object({
  categoryId: guidSchema,
  organizationId: guidSchema,
  name: z.string(),
  status: z.string(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
});

export const expenseCategoryPagedResultSchema = z.object({
  items: z.array(expenseCategoryDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const expenseDtoSchema = z.object({
  expenseId: guidSchema,
  organizationId: guidSchema,
  branchId: guidSchema.nullable().optional(),
  expenseNumber: z.string(),
  categoryId: guidSchema,
  categoryName: z.string().nullable().optional(),
  status: z.string(),
  paymentMethod: z.string(),
  amount: z.number(),
  description: z.string(),
  payee: z.string().nullable().optional(),
  gCashReference: z.string().nullable().optional(),
  expenseDate: z.string(),
  recordedAtUtc: z.string(),
  recordedBy: guidSchema,
  voidedAtUtc: z.string().nullable().optional(),
  voidedBy: guidSchema.nullable().optional(),
  voidReason: z.string().nullable().optional(),
  updatedAtUtc: z.string(),
});

export const expensePagedResultSchema = z.object({
  items: z.array(expenseDtoSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export const expenseCategorySummaryDtoSchema = z.object({
  categoryId: guidSchema,
  categoryName: z.string().nullable().optional(),
  totalAmount: z.number(),
  count: z.number(),
});

export const expensePaymentSummaryDtoSchema = z.object({
  paymentMethod: z.string(),
  totalAmount: z.number(),
  count: z.number(),
});

export const expenseSummaryDtoSchema = z.object({
  fromDate: z.string().nullable().optional(),
  toDate: z.string().nullable().optional(),
  grossTotal: z.number(),
  voidedTotal: z.number(),
  netTotal: z.number(),
  recordedCount: z.number(),
  voidedCount: z.number(),
  byCategory: z.array(expenseCategorySummaryDtoSchema),
  byPaymentMethod: z.array(expensePaymentSummaryDtoSchema),
});

export const expenseScopeBranchDtoSchema = z.object({
  branchId: guidSchema,
  name: z.string(),
});

export const expenseScopeOptionsDtoSchema = z.object({
  canViewOrganization: z.boolean(),
  canCreateOrganizationWide: z.boolean(),
  canViewAllBranches: z.boolean(),
  canViewAllExpenses: z.boolean(),
  branches: z.array(expenseScopeBranchDtoSchema),
});

export type PosExpenseCategoryDto = z.infer<typeof expenseCategoryDtoSchema>;
export type PosExpenseCategoryPagedResult = z.infer<typeof expenseCategoryPagedResultSchema>;
export type PosExpenseDto = z.infer<typeof expenseDtoSchema>;
export type PosExpensePagedResult = z.infer<typeof expensePagedResultSchema>;
export type PosExpenseSummaryDto = z.infer<typeof expenseSummaryDtoSchema>;
export type PosExpenseScopeBranchDto = z.infer<typeof expenseScopeBranchDtoSchema>;
export type PosExpenseScopeOptionsDto = z.infer<typeof expenseScopeOptionsDtoSchema>;

export type ListExpenseCategoriesOptions = {
  status?: string;
  search?: string;
  page?: number;
  pageSize?: number;
};

export type ListExpensesOptions = {
  status?: string;
  paymentMethod?: string;
  categoryId?: string;
  fromDate?: string;
  toDate?: string;
  expenseNumber?: string;
  page?: number;
  pageSize?: number;
  scope?: string;
  branchId?: string;
};

export type RecordExpenseRequest = {
  categoryId: string;
  paymentMethod: ExpensePaymentMethodCode | string;
  amount: number;
  description: string;
  expenseDate: string;
  payee?: string | null;
  gCashReference?: string | null;
  expenseId?: string | null;
  branchId?: string | null;
};

export type CreateExpenseCategoryRequest = {
  name: string;
  categoryId?: string | null;
};

export type UpdateExpenseCategoryRequest = {
  name: string;
  expectedUpdatedAtUtc?: string | null;
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

/** Expenses workspace — include bound branch so API can default preferred scope. */
export function expenseWorkspaceScope(
  organizationId: string,
  branchId?: string | null,
): PosWorkspaceScope {
  const trimmedBranch = branchId?.trim();
  return trimmedBranch
    ? { organizationId, branchId: trimmedBranch }
    : { organizationId };
}

export async function listExpenseCategories(
  workspace: PosWorkspaceScope,
  options: ListExpenseCategoriesOptions = {},
  signal?: AbortSignal,
): Promise<PosExpenseCategoryPagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(CATEGORIES_PATH, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 50,
      status: options.status,
      search: options.search,
    }),
  });
  return expenseCategoryPagedResultSchema.parse(raw);
}

export async function getExpenseCategory(
  workspace: PosWorkspaceScope,
  categoryId: string,
  signal?: AbortSignal,
): Promise<PosExpenseCategoryDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${CATEGORIES_PATH}/${categoryId}`,
  });
  return expenseCategoryDtoSchema.parse(raw);
}

export async function createExpenseCategory(
  workspace: PosWorkspaceScope,
  body: CreateExpenseCategoryRequest,
  signal?: AbortSignal,
): Promise<PosExpenseCategoryDto> {
  const payload: Record<string, unknown> = { name: body.name.trim() };
  if (body.categoryId?.trim()) {
    payload.categoryId = body.categoryId.trim();
  }
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: CATEGORIES_PATH,
    body: payload,
  });
  return expenseCategoryDtoSchema.parse(raw);
}

export async function updateExpenseCategory(
  workspace: PosWorkspaceScope,
  categoryId: string,
  body: UpdateExpenseCategoryRequest,
  signal?: AbortSignal,
): Promise<PosExpenseCategoryDto> {
  const payload: Record<string, unknown> = { name: body.name.trim() };
  if (body.expectedUpdatedAtUtc) {
    payload.expectedUpdatedAtUtc = body.expectedUpdatedAtUtc;
  }
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: `${CATEGORIES_PATH}/${categoryId}`,
    body: payload,
  });
  return expenseCategoryDtoSchema.parse(raw);
}

export async function deactivateExpenseCategory(
  workspace: PosWorkspaceScope,
  categoryId: string,
  signal?: AbortSignal,
): Promise<PosExpenseCategoryDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${CATEGORIES_PATH}/${categoryId}/deactivate`,
  });
  return expenseCategoryDtoSchema.parse(raw);
}

export async function reactivateExpenseCategory(
  workspace: PosWorkspaceScope,
  categoryId: string,
  signal?: AbortSignal,
): Promise<PosExpenseCategoryDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${CATEGORIES_PATH}/${categoryId}/reactivate`,
  });
  return expenseCategoryDtoSchema.parse(raw);
}

export async function getExpenseScopeOptions(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosExpenseScopeOptionsDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${EXPENSES_PATH}/scope-options`,
  });
  return expenseScopeOptionsDtoSchema.parse(raw);
}

export async function listExpenses(
  workspace: PosWorkspaceScope,
  options: ListExpensesOptions = {},
  signal?: AbortSignal,
): Promise<PosExpensePagedResult> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(EXPENSES_PATH, {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
      status: options.status,
      paymentMethod: options.paymentMethod,
      categoryId: options.categoryId,
      fromDate: options.fromDate,
      toDate: options.toDate,
      expenseNumber: options.expenseNumber,
      scope: options.scope,
      branchId: options.branchId,
    }),
  });
  return expensePagedResultSchema.parse(raw);
}

export async function getExpense(
  workspace: PosWorkspaceScope,
  expenseId: string,
  signal?: AbortSignal,
): Promise<PosExpenseDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: `${EXPENSES_PATH}/${expenseId}`,
  });
  return expenseDtoSchema.parse(raw);
}

/**
 * Records an organization expense (online-only). Uses expense.create idempotency
 * headers when an expenseId is provided or generated.
 */
export async function recordExpense(
  workspace: PosWorkspaceScope,
  body: RecordExpenseRequest,
  signal?: AbortSignal,
): Promise<PosExpenseDto> {
  const expenseId = body.expenseId?.trim() || crypto.randomUUID();
  const paymentMethod = body.paymentMethod;
  const payload: Record<string, unknown> = {
    categoryId: body.categoryId,
    paymentMethod,
    amount: body.amount,
    description: body.description.trim(),
    expenseDate: body.expenseDate,
    expenseId,
  };
  if (body.branchId?.trim()) {
    payload.branchId = body.branchId.trim();
  } else if (body.branchId === null) {
    payload.branchId = null;
  }
  const payee = trimOrUndef(body.payee);
  if (payee) {
    payload.payee = payee;
  }
  if (paymentMethod === "ManualGCash") {
    const reference = trimOrUndef(body.gCashReference);
    if (reference) {
      payload.gCashReference = reference;
    }
  }

  const headers = await buildPosMutationIdempotencyHeaders(
    expenseId,
    JSON.stringify(payload),
    OFFLINE_OPERATION_TYPES.ExpenseCreate,
  );

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: EXPENSES_PATH,
    body: payload,
    headers,
  });
  return expenseDtoSchema.parse(raw);
}

export async function voidExpense(
  workspace: PosWorkspaceScope,
  expenseId: string,
  reason: string,
  signal?: AbortSignal,
): Promise<PosExpenseDto> {
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: `${EXPENSES_PATH}/${expenseId}/void`,
    body: { reason: reason.trim() },
  });
  return expenseDtoSchema.parse(raw);
}

export async function getExpenseSummary(
  workspace: PosWorkspaceScope,
  options: { fromDate?: string; toDate?: string; scope?: string; branchId?: string } = {},
  signal?: AbortSignal,
): Promise<PosExpenseSummaryDto> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(`${EXPENSES_PATH}/summary`, {
      fromDate: options.fromDate,
      toDate: options.toDate,
      scope: options.scope,
      branchId: options.branchId,
    }),
  });
  return expenseSummaryDtoSchema.parse(raw);
}
