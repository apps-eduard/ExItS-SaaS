import type {
  CreatePersonalContactRequest,
  PersonalContactDto,
  PersonalDebtRelationshipSummaryDto,
  PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import { assertOfflineScope, type OfflineDb } from "@/offline/db";
import { enqueueEncryptedOperation } from "@/offline/outbox";
import {
  cacheLocalPersonalContact,
  cacheLocalPersonalEntry,
  cacheLocalPersonalRelationship,
} from "@/offline/personal-utang-cache";
import {
  localRefToken,
  QUEUED_REQUEST_PAYLOAD_VERSION,
  serializeQueuedRequest,
} from "@/offline/queued-request";
import { PERSONAL_OPERATION_TYPES } from "@/offline/server-dedupe-policy";
import type { OfflineOperationRecord } from "@/offline/types";

/**
 * Offline Personal Utang (RMAP-21F).
 *
 * Offline-capable, because each is a private record the signed-in person is making about their own
 * money and the server needs no live state to accept it:
 *   - personal.contact.create               (a name in this person's own address book)
 *   - personal.utang.relationship.create    (a debt this person is recording, contact-side only)
 *   - personal.utang.entry.record           (Loan or Payment — append-only, balance recomputed)
 *
 * Deliberately online-only, because each needs the server or another human:
 *   - linking a contact to an ExItS identity, and any relationship naming a second real user
 *   - invitations, QR share, accept, decline, resend, revoke
 *   - reminders and their delivery
 *   - Adjustment entries, which correct a balance the device may no longer be looking at
 *
 * These routes have no idempotency support, so `serverDedupeMode` is "none" and the sync processor
 * must not silently replay them. See `server-dedupe-policy.ts`.
 */

export const PERSONAL_UTANG_PRODUCT_DOMAIN = "personal.utang";

const UTANG_PATH = "/api/v1/personal/utang";

export type OfflinePersonalUtangRejectionCode =
  | "offline.personal.contact.name_required"
  | "offline.personal.contact.identity_link_not_supported"
  | "offline.personal.relationship.contact_required"
  | "offline.personal.relationship.counterparty_identity_not_supported"
  | "offline.personal.relationship.owner_unknown"
  | "offline.personal.entry.amount_invalid"
  | "offline.personal.entry.adjustment_not_supported"
  | "offline.personal.entry.relationship_required";

export class OfflinePersonalUtangRejectedError extends Error {
  readonly code: OfflinePersonalUtangRejectionCode;

  constructor(code: OfflinePersonalUtangRejectionCode, message: string) {
    super(message);
    this.name = "OfflinePersonalUtangRejectedError";
    this.code = code;
  }
}

export type PersonalOfflineScope = {
  db: OfflineDb;
  /** Personal scope key — also the envelope key material. */
  scopeBinding: string;
  userId: string;
};

async function personalScopeFields(scope: PersonalOfflineScope) {
  // A Personal debt must never be written into an Organization outbox.
  await assertOfflineScope(scope.db, "Personal");
  return {
    db: scope.db,
    scopeKind: "Personal" as const,
    scopeBinding: scope.scopeBinding,
    userId: scope.userId,
    organizationId: null,
    branchId: null,
    installationDeviceId: null,
    posDeviceId: null,
    payloadVersion: QUEUED_REQUEST_PAYLOAD_VERSION,
  };
}

/**
 * The Personal routes mint their own ids, so a queued operation has no server id to reuse. The
 * local operation id is still the idempotency key: it makes the queue row unique and it is the
 * key the processor would send the day these routes learn to deduplicate.
 */
function localIdempotencyKey(localId: string): string {
  return localId.replace(/-/g, "").toLowerCase();
}

export type EnqueuePersonalContactInput = PersonalOfflineScope & {
  /** Local id, replaced by the server id once the queued contact posts. */
  contactId: string;
  contact: CreatePersonalContactRequest;
  /** Present only so an offline attempt to link an identity is refused instead of dropped. */
  linkedUserIdentityId?: string | null;
};

export type EnqueuedPersonalContact = {
  operation: OfflineOperationRecord;
  /** Optimistic row written to the Personal cache so the person sees what they just added. */
  contact: PersonalContactDto;
};

export async function enqueuePersonalContactCreate(
  input: EnqueuePersonalContactInput,
): Promise<EnqueuedPersonalContact> {
  if (input.linkedUserIdentityId) {
    throw new OfflinePersonalUtangRejectedError(
      "offline.personal.contact.identity_link_not_supported",
      "Linking a person to their ExItS account requires an internet connection.",
    );
  }
  const displayName = input.contact.displayName.trim();
  if (!displayName) {
    throw new OfflinePersonalUtangRejectedError(
      "offline.personal.contact.name_required",
      "A person needs a name before they can be saved on this device.",
    );
  }

  const scope = await personalScopeFields(input);
  const body: CreatePersonalContactRequest = {
    displayName,
    phone: input.contact.phone?.trim() || null,
    email: input.contact.email?.trim() || null,
  };

  const operation = await enqueueEncryptedOperation({
    ...scope,
    productDomain: PERSONAL_UTANG_PRODUCT_DOMAIN,
    operationType: PERSONAL_OPERATION_TYPES.ContactCreate,
    operationId: input.contactId,
    idempotencyKey: localIdempotencyKey(input.contactId),
    plaintextJson: serializeQueuedRequest({
      api: "platform",
      method: "POST",
      path: `${UTANG_PATH}/contacts`,
      body,
    }),
    entityLocalId: input.contactId,
  });

  const contact: PersonalContactDto = {
    id: input.contactId,
    displayName,
    phone: body.phone ?? null,
    email: body.email ?? null,
    linkedUserIdentityId: null,
    publicUserId: null,
    status: "Active",
    createdAtUtc: operation.createdAt,
  };
  await cacheLocalPersonalContact(input.db, input.scopeBinding, contact);
  return { operation, contact };
}

export type EnqueuePersonalRelationshipInput = PersonalOfflineScope & {
  /** Local id, replaced by the server id once the queued relationship posts. */
  relationshipId: string;
  /** "Lent" = this person is the creditor, "Borrowed" = this person is the debtor. */
  perspective: "Lent" | "Borrowed";
  /** Local or server contact id for the other side of the debt. */
  contactId: string;
  /** This person's Personal identity id, cached while online. */
  ownerUserIdentityId: string;
  /** Set when the contact is still queued, so the request waits for the contact to exist. */
  dependsOnContactOperationId?: string | null;
  /** True when `contactId` is a local id that must be rewritten at replay time. */
  contactIsLocal?: boolean;
  currencyCode?: string;
  dueDateUtc?: string | null;
  initialLoanAmount: number;
  initialLoanNotes?: string | null;
  /** Present only so an offline attempt to name a second real user is refused. */
  counterpartyUserIdentityId?: string | null;
};

export type EnqueuedPersonalRelationship = {
  operation: OfflineOperationRecord;
  relationship: PersonalDebtRelationshipSummaryDto;
};

/**
 * Queue a new debt against a contact.
 *
 * Only the contact side may be filled in offline. Naming a second real ExItS user creates an
 * obligation for somebody who is not holding this device, which the server must decide.
 */
export async function enqueuePersonalRelationshipCreate(
  input: EnqueuePersonalRelationshipInput,
): Promise<EnqueuedPersonalRelationship> {
  if (input.counterpartyUserIdentityId) {
    throw new OfflinePersonalUtangRejectedError(
      "offline.personal.relationship.counterparty_identity_not_supported",
      "Recording a debt against another ExItS account requires an internet connection.",
    );
  }
  if (!input.contactId.trim()) {
    throw new OfflinePersonalUtangRejectedError(
      "offline.personal.relationship.contact_required",
      "Choose the person this debt belongs to.",
    );
  }
  if (!input.ownerUserIdentityId.trim()) {
    throw new OfflinePersonalUtangRejectedError(
      "offline.personal.relationship.owner_unknown",
      "This device has not yet learned your ExItS id, so open Utang once while online.",
    );
  }
  if (!Number.isFinite(input.initialLoanAmount) || input.initialLoanAmount <= 0) {
    throw new OfflinePersonalUtangRejectedError(
      "offline.personal.entry.amount_invalid",
      "The amount must be greater than zero.",
    );
  }

  const scope = await personalScopeFields(input);
  const amount = Number(input.initialLoanAmount.toFixed(2));
  const currencyCode = input.currencyCode ?? "PHP";
  const dueDateUtc = input.dueDateUtc ?? null;
  const notes = input.initialLoanNotes?.trim() || null;
  // A contact queued on this device has no server id yet, so the request carries a placeholder
  // that the sync processor rewrites once the contact has actually been created.
  const contactRef = input.contactIsLocal ? localRefToken(input.contactId) : input.contactId;
  const lent = input.perspective === "Lent";

  const body = {
    creditorUserIdentityId: lent ? input.ownerUserIdentityId : null,
    creditorContactId: lent ? null : contactRef,
    debtorUserIdentityId: lent ? null : input.ownerUserIdentityId,
    debtorContactId: lent ? contactRef : null,
    currencyCode,
    dueDateUtc,
    initialLoanAmount: amount,
    initialLoanNotes: notes,
  };

  const operation = await enqueueEncryptedOperation({
    ...scope,
    productDomain: PERSONAL_UTANG_PRODUCT_DOMAIN,
    operationType: PERSONAL_OPERATION_TYPES.RelationshipCreate,
    operationId: input.relationshipId,
    idempotencyKey: localIdempotencyKey(input.relationshipId),
    plaintextJson: serializeQueuedRequest({
      api: "platform",
      method: "POST",
      path: `${UTANG_PATH}/relationships`,
      body,
    }),
    dependsOnOperationId: input.dependsOnContactOperationId ?? null,
    entityLocalId: input.relationshipId,
  });

  const relationship: PersonalDebtRelationshipSummaryDto = {
    id: input.relationshipId,
    perspective: input.perspective,
    creditorUserIdentityId: lent ? input.ownerUserIdentityId : null,
    creditorContactId: lent ? null : input.contactId,
    debtorUserIdentityId: lent ? null : input.ownerUserIdentityId,
    debtorContactId: lent ? input.contactId : null,
    currencyCode,
    currentBalance: amount,
    dueDateUtc,
    status: "Active",
    version: 0,
    updatedAtUtc: operation.createdAt,
    isSharedLedger: false,
    isPrivate: true,
  };
  await cacheLocalPersonalRelationship(
    input.db,
    input.scopeBinding,
    input.perspective,
    relationship,
  );
  return { operation, relationship };
}

export type EnqueuePersonalEntryInput = PersonalOfflineScope & {
  /** Local id for this entry — one per queued entry, so two payments never collapse into one. */
  entryId: string;
  /** Local or server relationship id. */
  relationshipId: string;
  relationshipIsLocal?: boolean;
  /** Set when the relationship is still queued. */
  dependsOnRelationshipOperationId?: string | null;
  entryType: "Loan" | "Payment";
  amount: number;
  notes?: string | null;
  dueDateUtc?: string | null;
  /** This person's Personal identity id, used only for the optimistic history row. */
  ownerUserIdentityId: string;
  /** Balance the device is showing, used only for the optimistic row's running total. */
  localBalanceBefore?: number;
};

export type EnqueuedPersonalEntry = {
  operation: OfflineOperationRecord;
  entry: PersonalUtangEntryDto;
};

/**
 * Queue a Loan or Payment entry.
 *
 * `expectedVersion` is deliberately omitted. A Loan or Payment is an append-only fact — the money
 * changed hands in the real world and the server recomputes the running balance when it lands, so
 * pinning a version the device read hours ago would only reject a true payment. An Adjustment is
 * different: it rewrites a balance to a number the person believed at the time, so it stays online.
 */
export async function enqueuePersonalUtangEntry(
  input: EnqueuePersonalEntryInput,
): Promise<EnqueuedPersonalEntry> {
  if (!input.relationshipId.trim()) {
    throw new OfflinePersonalUtangRejectedError(
      "offline.personal.entry.relationship_required",
      "This entry needs the debt it belongs to.",
    );
  }
  if (!Number.isFinite(input.amount) || input.amount <= 0) {
    throw new OfflinePersonalUtangRejectedError(
      "offline.personal.entry.amount_invalid",
      "The amount must be greater than zero.",
    );
  }

  const scope = await personalScopeFields(input);
  const amount = Number(input.amount.toFixed(2));
  const relationshipRef = input.relationshipIsLocal
    ? localRefToken(input.relationshipId)
    : input.relationshipId;

  const operation = await enqueueEncryptedOperation({
    ...scope,
    productDomain: PERSONAL_UTANG_PRODUCT_DOMAIN,
    operationType: PERSONAL_OPERATION_TYPES.EntryRecord,
    operationId: input.entryId,
    idempotencyKey: localIdempotencyKey(input.entryId),
    plaintextJson: serializeQueuedRequest({
      api: "platform",
      method: "POST",
      path: `${UTANG_PATH}/relationships/${relationshipRef}/entries`,
      body: {
        entryType: input.entryType,
        amount,
        expectedVersion: null,
        notes: input.notes?.trim() || null,
        dueDateUtc: input.dueDateUtc ?? null,
      },
    }),
    dependsOnOperationId: input.dependsOnRelationshipOperationId ?? null,
    entityLocalId: input.entryId,
  });

  const signedDelta = input.entryType === "Payment" ? -amount : amount;
  const entry: PersonalUtangEntryDto = {
    id: input.entryId,
    relationshipId: input.relationshipId,
    entryType: input.entryType,
    amount,
    signedDelta,
    balanceAfter: Number(((input.localBalanceBefore ?? 0) + signedDelta).toFixed(2)),
    notes: input.notes?.trim() || null,
    dueDateUtc: input.dueDateUtc ?? null,
    createdByUserIdentityId: input.ownerUserIdentityId,
    createdAtUtc: operation.createdAt,
    status: "Confirmed",
    resolvedByUserIdentityId: null,
    resolvedAtUtc: null,
    disputeReason: null,
    canConfirm: false,
    canDispute: false,
    canCancel: false,
    affectsBalance: true,
    isSharedLedger: false,
  };
  await cacheLocalPersonalEntry(input.db, input.scopeBinding, entry);
  return { operation, entry };
}

/**
 * Adjustment stays online. Kept as an explicit refusal so a future caller cannot quietly widen the
 * offline surface by passing `entryType: "Adjustment"` through the generic path.
 */
export function rejectOfflineAdjustment(): never {
  throw new OfflinePersonalUtangRejectedError(
    "offline.personal.entry.adjustment_not_supported",
    "Correcting a balance requires an internet connection.",
  );
}
