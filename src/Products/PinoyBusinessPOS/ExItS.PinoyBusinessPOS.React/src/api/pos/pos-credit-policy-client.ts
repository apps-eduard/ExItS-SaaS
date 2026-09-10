import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";
import {
  buildPosMutationIdempotencyHeaders,
  OFFLINE_OPERATION_TYPES,
} from "@/api/pos/pos-mutation-idempotency";
import { createSecureMutationId } from "@/lib/secure-mutation-id";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

const isoDateTimeSchema = z.string().min(1);

export const CUSTOMER_CREDIT_POLICY_STATUSES = [
  "NotConfigured",
  "PendingApproval",
  "Approved",
  "Disabled",
] as const;

export type CustomerCreditPolicyStatus = (typeof CUSTOMER_CREDIT_POLICY_STATUSES)[number];

export const posCustomerCreditPolicySchema = z.object({
  customerId: guidSchema,
  status: z.string(),
  creditLimit: z.number().nullable().optional(),
  defaultTermDays: z.number().nullable().optional(),
  outstandingAmount: z.number(),
  availableCredit: z.number(),
  configuredByUserId: guidSchema.nullable().optional(),
  configuredAtUtc: isoDateTimeSchema.nullable().optional(),
  approvedByUserId: guidSchema.nullable().optional(),
  approvedAtUtc: isoDateTimeSchema.nullable().optional(),
  updatedByUserId: guidSchema.nullable().optional(),
  updatedAtUtc: isoDateTimeSchema.nullable().optional(),
  expectedUpdatedAtUtc: isoDateTimeSchema.nullable().optional(),
});

export const posCustomerCreditPolicyChangeSchema = z.object({
  changeId: guidSchema,
  customerId: guidSchema,
  customerCreditPolicyId: guidSchema,
  action: z.string(),
  previousStatus: z.string().nullable().optional(),
  newStatus: z.string(),
  previousCreditLimit: z.number().nullable().optional(),
  newCreditLimit: z.number().nullable().optional(),
  previousTermDays: z.number().nullable().optional(),
  newTermDays: z.number().nullable().optional(),
  actorUserId: guidSchema,
  reason: z.string(),
  changedAtUtc: isoDateTimeSchema,
});

export const posCustomerCreditPolicyHistoryPagedSchema = z.object({
  items: z.array(posCustomerCreditPolicyChangeSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type PosCustomerCreditPolicy = z.infer<typeof posCustomerCreditPolicySchema>;
export type PosCustomerCreditPolicyChange = z.infer<typeof posCustomerCreditPolicyChangeSchema>;
export type PosCustomerCreditPolicyHistoryPaged = z.infer<
  typeof posCustomerCreditPolicyHistoryPagedSchema
>;

export type UpsertCustomerCreditPolicyInput = {
  creditLimit: number;
  defaultTermDays: number;
  reason?: string | null;
  expectedUpdatedAtUtc?: string | null;
  /** Client-chosen idempotency entity id. */
  operationId?: string;
};

export type ApproveCustomerCreditPolicyInput = {
  reason: string;
  expectedUpdatedAtUtc: string;
  operationId?: string;
};

export type DisableCustomerCreditPolicyInput = {
  reason: string;
  expectedUpdatedAtUtc: string;
  operationId?: string;
};

function creditPolicyPath(customerId: string, suffix = ""): string {
  return `/api/v1/pos/customers/${customerId}/credit-policy${suffix}`;
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
  const qs = query.toString();
  return qs ? `${path}?${qs}` : path;
}

async function creditPolicyMutationHeaders(
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

export async function getCustomerCreditPolicy(
  workspace: PosWorkspaceScope,
  customerId: string,
  signal?: AbortSignal,
): Promise<PosCustomerCreditPolicy> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: creditPolicyPath(customerId),
  });
  return posCustomerCreditPolicySchema.parse(raw);
}

export async function upsertCustomerCreditPolicy(
  workspace: PosWorkspaceScope,
  customerId: string,
  input: UpsertCustomerCreditPolicyInput,
  signal?: AbortSignal,
): Promise<PosCustomerCreditPolicy> {
  const operationId = input.operationId ?? newMutationId();
  const body = {
    creditLimit: input.creditLimit,
    defaultTermDays: input.defaultTermDays,
    reason: input.reason?.trim() || null,
    expectedUpdatedAtUtc: input.expectedUpdatedAtUtc ?? null,
  };
  const raw = await posRequest<unknown>({
    method: "PUT",
    workspace,
    signal,
    path: creditPolicyPath(customerId),
    body,
    headers: await creditPolicyMutationHeaders(
      operationId,
      body,
      OFFLINE_OPERATION_TYPES.CustomerCreditPolicyUpsert,
    ),
  });
  return posCustomerCreditPolicySchema.parse(raw);
}

export async function approveCustomerCreditPolicy(
  workspace: PosWorkspaceScope,
  customerId: string,
  input: ApproveCustomerCreditPolicyInput,
  signal?: AbortSignal,
): Promise<PosCustomerCreditPolicy> {
  const operationId = input.operationId ?? newMutationId();
  const body = {
    reason: input.reason.trim(),
    expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
  };
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: creditPolicyPath(customerId, "/approve"),
    body,
    headers: await creditPolicyMutationHeaders(
      operationId,
      body,
      OFFLINE_OPERATION_TYPES.CustomerCreditPolicyApprove,
    ),
  });
  return posCustomerCreditPolicySchema.parse(raw);
}

export async function disableCustomerCreditPolicy(
  workspace: PosWorkspaceScope,
  customerId: string,
  input: DisableCustomerCreditPolicyInput,
  signal?: AbortSignal,
): Promise<PosCustomerCreditPolicy> {
  const operationId = input.operationId ?? newMutationId();
  const body = {
    reason: input.reason.trim(),
    expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
  };
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: creditPolicyPath(customerId, "/disable"),
    body,
    headers: await creditPolicyMutationHeaders(
      operationId,
      body,
      OFFLINE_OPERATION_TYPES.CustomerCreditPolicyDisable,
    ),
  });
  return posCustomerCreditPolicySchema.parse(raw);
}

export async function listCustomerCreditPolicyHistory(
  workspace: PosWorkspaceScope,
  customerId: string,
  options: { page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosCustomerCreditPolicyHistoryPaged> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(creditPolicyPath(customerId, "/history"), {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posCustomerCreditPolicyHistoryPagedSchema.parse(raw);
}
