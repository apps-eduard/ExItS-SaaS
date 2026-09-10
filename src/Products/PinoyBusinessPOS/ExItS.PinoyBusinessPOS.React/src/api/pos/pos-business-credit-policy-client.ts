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

export const posBusinessCustomerCreditPolicySchema = z.object({
  connectionId: guidSchema,
  sellerOrganizationId: guidSchema,
  buyerOrganizationId: guidSchema,
  status: z.string(),
  creditLimit: z.number().nullable().optional(),
  defaultTermDays: z.number().int().nullable().optional(),
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

export const posBusinessCustomerCreditPolicyChangeSchema = z.object({
  changeId: guidSchema,
  sellerOrganizationId: guidSchema,
  buyerOrganizationId: guidSchema,
  businessCustomerCreditPolicyId: guidSchema,
  action: z.string(),
  previousStatus: z.string().nullable().optional(),
  newStatus: z.string(),
  previousCreditLimit: z.number().nullable().optional(),
  newCreditLimit: z.number().nullable().optional(),
  previousTermDays: z.number().int().nullable().optional(),
  newTermDays: z.number().int().nullable().optional(),
  actorUserId: guidSchema,
  reason: z.string(),
  changedAtUtc: isoDateTimeSchema,
});

export const posBusinessCustomerCreditPolicyHistoryPagedSchema = z.object({
  items: z.array(posBusinessCustomerCreditPolicyChangeSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
});

export type PosBusinessCustomerCreditPolicy = z.infer<
  typeof posBusinessCustomerCreditPolicySchema
>;
export type PosBusinessCustomerCreditPolicyChange = z.infer<
  typeof posBusinessCustomerCreditPolicyChangeSchema
>;
export type PosBusinessCustomerCreditPolicyHistoryPaged = z.infer<
  typeof posBusinessCustomerCreditPolicyHistoryPagedSchema
>;

export type UpsertBusinessCustomerCreditPolicyInput = {
  creditLimit: number;
  defaultTermDays: number;
  reason?: string | null;
  expectedUpdatedAtUtc?: string | null;
  operationId?: string;
};

export type ApproveBusinessCustomerCreditPolicyInput = {
  reason: string;
  expectedUpdatedAtUtc: string;
  operationId?: string;
};

export type DisableBusinessCustomerCreditPolicyInput = {
  reason: string;
  expectedUpdatedAtUtc: string;
  operationId?: string;
};

function creditPolicyPath(connectionId: string, suffix = ""): string {
  return `/api/v1/pos/connected-suppliers/business-customers/${connectionId}/credit-policy${suffix}`;
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

async function mutationHeaders(
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

export async function getBusinessCustomerCreditPolicy(
  workspace: PosWorkspaceScope,
  connectionId: string,
  signal?: AbortSignal,
): Promise<PosBusinessCustomerCreditPolicy> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: creditPolicyPath(connectionId),
  });
  return posBusinessCustomerCreditPolicySchema.parse(raw);
}

export async function upsertBusinessCustomerCreditPolicy(
  workspace: PosWorkspaceScope,
  connectionId: string,
  input: UpsertBusinessCustomerCreditPolicyInput,
  signal?: AbortSignal,
): Promise<PosBusinessCustomerCreditPolicy> {
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
    path: creditPolicyPath(connectionId),
    body,
    headers: await mutationHeaders(
      operationId,
      body,
      OFFLINE_OPERATION_TYPES.BusinessCustomerCreditPolicyUpsert,
    ),
  });
  return posBusinessCustomerCreditPolicySchema.parse(raw);
}

export async function approveBusinessCustomerCreditPolicy(
  workspace: PosWorkspaceScope,
  connectionId: string,
  input: ApproveBusinessCustomerCreditPolicyInput,
  signal?: AbortSignal,
): Promise<PosBusinessCustomerCreditPolicy> {
  const operationId = input.operationId ?? newMutationId();
  const body = {
    reason: input.reason.trim(),
    expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
  };
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: creditPolicyPath(connectionId, "/approve"),
    body,
    headers: await mutationHeaders(
      operationId,
      body,
      OFFLINE_OPERATION_TYPES.BusinessCustomerCreditPolicyApprove,
    ),
  });
  return posBusinessCustomerCreditPolicySchema.parse(raw);
}

export async function disableBusinessCustomerCreditPolicy(
  workspace: PosWorkspaceScope,
  connectionId: string,
  input: DisableBusinessCustomerCreditPolicyInput,
  signal?: AbortSignal,
): Promise<PosBusinessCustomerCreditPolicy> {
  const operationId = input.operationId ?? newMutationId();
  const body = {
    reason: input.reason.trim(),
    expectedUpdatedAtUtc: input.expectedUpdatedAtUtc,
  };
  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: creditPolicyPath(connectionId, "/disable"),
    body,
    headers: await mutationHeaders(
      operationId,
      body,
      OFFLINE_OPERATION_TYPES.BusinessCustomerCreditPolicyDisable,
    ),
  });
  return posBusinessCustomerCreditPolicySchema.parse(raw);
}

export async function listBusinessCustomerCreditPolicyHistory(
  workspace: PosWorkspaceScope,
  connectionId: string,
  options: { page?: number; pageSize?: number } = {},
  signal?: AbortSignal,
): Promise<PosBusinessCustomerCreditPolicyHistoryPaged> {
  const raw = await posRequest<unknown>({
    method: "GET",
    workspace,
    signal,
    path: appendQuery(creditPolicyPath(connectionId, "/history"), {
      page: options.page ?? 1,
      pageSize: options.pageSize ?? 20,
    }),
  });
  return posBusinessCustomerCreditPolicyHistoryPagedSchema.parse(raw);
}
