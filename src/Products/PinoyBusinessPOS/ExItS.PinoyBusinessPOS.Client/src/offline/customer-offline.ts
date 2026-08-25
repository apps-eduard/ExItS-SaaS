import {
  buildCreateCustomerPayload,
  buildCreateRepaymentPayload,
  buildUpdateCustomerPayload,
  type CreatePosCustomerInput,
  type CreatePosRepaymentInput,
  type UpdatePosCustomerInput,
} from "@/api/pos/pos-customers-client";
import {
  OFFLINE_OPERATION_TYPES,
  posIdempotencyKeyForEntity,
} from "@/api/pos/pos-mutation-idempotency";
import type { OfflineDb } from "@/offline/db";
import { enqueueEncryptedOperation } from "@/offline/outbox";
import { QUEUED_REQUEST_PAYLOAD_VERSION, serializeQueuedRequest } from "@/offline/queued-request";
import type { OfflineOperationRecord } from "@/offline/types";

/**
 * Offline Business customer and customer-credit work (RMAP-21E).
 *
 * Offline-capable, because the server owns a client-supplied entity id and honours
 * `Idempotency-Key` + `X-Pos-Payload-Hash` on each of these routes, so a replay lands on the
 * same row instead of creating a second one:
 *   - customer.create   (POST /customers, server adopts the client customerId)
 *   - customer.update   (PUT /customers/{id}, keyed on the edit attempt)
 *   - repayment.create  (POST /customers/{id}/repayments, server adopts the client repaymentId)
 *
 * Deliberately online-only: extending credit, reversing a credit or repayment, changing a due
 * date, activating/deactivating a customer, Business Utang checkout, and every Personal↔Business
 * identity link. Those are either authorization acts or decisions the server must make against a
 * live balance, and this client must never approximate them offline.
 */

export const POS_CUSTOMER_PRODUCT_DOMAIN = "pos.customer";
export const POS_CUSTOMER_CREDIT_PRODUCT_DOMAIN = "pos.customer_credit";

const CUSTOMERS_PATH = "/api/v1/pos/customers";

export type OfflineCustomerRejectionCode =
  | "offline.customer.display_name_required"
  | "offline.customer.identity_link_not_supported"
  | "offline.customer.operation_id_required"
  | "offline.repayment.amount_invalid"
  | "offline.repayment.customer_required";

export class OfflineCustomerRejectedError extends Error {
  readonly code: OfflineCustomerRejectionCode;

  constructor(code: OfflineCustomerRejectionCode, message: string) {
    super(message);
    this.name = "OfflineCustomerRejectedError";
    this.code = code;
  }
}

export type OfflineCustomerScope = {
  db: OfflineDb;
  /** Organization scope key — also the envelope key material. */
  scopeBinding: string;
  userId: string;
  organizationId: string;
  branchId: string;
  installationDeviceId: string;
  posDeviceId?: string | null;
};

function scopeFields(scope: OfflineCustomerScope) {
  return {
    db: scope.db,
    scopeKind: "Organization" as const,
    scopeBinding: scope.scopeBinding,
    userId: scope.userId,
    organizationId: scope.organizationId,
    branchId: scope.branchId,
    installationDeviceId: scope.installationDeviceId,
    posDeviceId: scope.posDeviceId ?? null,
    payloadVersion: QUEUED_REQUEST_PAYLOAD_VERSION,
  };
}

function requireDisplayName(displayName: string): string {
  const trimmed = displayName.trim();
  if (!trimmed) {
    throw new OfflineCustomerRejectedError(
      "offline.customer.display_name_required",
      "A customer needs a name before it can be saved on this device.",
    );
  }
  return trimmed;
}

export type EnqueueOfflineCustomerCreateInput = OfflineCustomerScope & {
  /** Client-chosen id the server adopts, so the queued row already has its final identity. */
  customerId: string;
  customer: Omit<CreatePosCustomerInput, "customerId">;
  /** Present only so an offline attempt to link identities is rejected instead of dropped. */
  platformBusinessCustomerId?: string | null;
  linkedPersonalPublicUserId?: string | null;
};

/**
 * Queue a new Business customer. The customer id is chosen here and sent in the body, so the
 * customer that eventually exists on the server has the same id this device already showed.
 */
export async function enqueueOfflineCustomerCreate(
  input: EnqueueOfflineCustomerCreateInput,
): Promise<OfflineOperationRecord> {
  if (input.platformBusinessCustomerId || input.linkedPersonalPublicUserId) {
    throw new OfflineCustomerRejectedError(
      "offline.customer.identity_link_not_supported",
      "Linking a customer to an ExItS identity requires an internet connection.",
    );
  }

  const displayName = requireDisplayName(input.customer.displayName);
  const body = buildCreateCustomerPayload({
    ...input.customer,
    displayName,
    customerId: input.customerId,
  });

  return enqueueEncryptedOperation({
    ...scopeFields(input),
    productDomain: POS_CUSTOMER_PRODUCT_DOMAIN,
    operationType: OFFLINE_OPERATION_TYPES.CustomerCreate,
    operationId: input.customerId,
    idempotencyKey: posIdempotencyKeyForEntity(input.customerId),
    plaintextJson: serializeQueuedRequest({
      api: "pos",
      method: "POST",
      path: CUSTOMERS_PATH,
      body,
    }),
    entityLocalId: input.customerId,
  });
}

export type EnqueueOfflineCustomerUpdateInput = OfflineCustomerScope & {
  customerId: string;
  /** Stable id for this edit attempt — two offline edits must not share one idempotency key. */
  operationId: string;
  customer: Omit<UpdatePosCustomerInput, "operationId">;
};

export async function enqueueOfflineCustomerUpdate(
  input: EnqueueOfflineCustomerUpdateInput,
): Promise<OfflineOperationRecord> {
  if (!input.operationId.trim()) {
    throw new OfflineCustomerRejectedError(
      "offline.customer.operation_id_required",
      "This edit needs a secure operation id before it can be queued.",
    );
  }

  const displayName = requireDisplayName(input.customer.displayName);
  const body = buildUpdateCustomerPayload({ ...input.customer, displayName });

  return enqueueEncryptedOperation({
    ...scopeFields(input),
    productDomain: POS_CUSTOMER_PRODUCT_DOMAIN,
    operationType: OFFLINE_OPERATION_TYPES.CustomerUpdate,
    operationId: input.operationId,
    idempotencyKey: posIdempotencyKeyForEntity(input.operationId),
    plaintextJson: serializeQueuedRequest({
      api: "pos",
      method: "PUT",
      path: `${CUSTOMERS_PATH}/${input.customerId}`,
      body,
    }),
    entityLocalId: input.customerId,
  });
}

export type EnqueueOfflineRepaymentInput = OfflineCustomerScope & {
  customerId: string;
  /** Client-chosen id the server adopts, so a replay records one payment, not two. */
  repaymentId: string;
  repayment: Omit<CreatePosRepaymentInput, "repaymentId">;
};

/**
 * Queue a customer repayment. The server still decides whether the amount is acceptable against
 * the live balance — a queued repayment that the server rejects surfaces in Connection & Sync as
 * needing attention rather than silently reducing what the customer owes.
 */
export async function enqueueOfflineCustomerRepayment(
  input: EnqueueOfflineRepaymentInput,
): Promise<OfflineOperationRecord> {
  if (!input.customerId.trim()) {
    throw new OfflineCustomerRejectedError(
      "offline.repayment.customer_required",
      "A payment needs the customer it belongs to.",
    );
  }
  const amount = input.repayment.amount;
  if (!Number.isFinite(amount) || amount <= 0) {
    throw new OfflineCustomerRejectedError(
      "offline.repayment.amount_invalid",
      "A payment must be greater than zero.",
    );
  }

  const body = buildCreateRepaymentPayload({
    ...input.repayment,
    amount: Number(amount.toFixed(2)),
    repaymentId: input.repaymentId,
  });

  return enqueueEncryptedOperation({
    ...scopeFields(input),
    productDomain: POS_CUSTOMER_CREDIT_PRODUCT_DOMAIN,
    operationType: OFFLINE_OPERATION_TYPES.RepaymentCreate,
    operationId: input.repaymentId,
    idempotencyKey: posIdempotencyKeyForEntity(input.repaymentId),
    plaintextJson: serializeQueuedRequest({
      api: "pos",
      method: "POST",
      path: `${CUSTOMERS_PATH}/${input.customerId}/repayments`,
      body,
    }),
    entityLocalId: input.repaymentId,
  });
}
