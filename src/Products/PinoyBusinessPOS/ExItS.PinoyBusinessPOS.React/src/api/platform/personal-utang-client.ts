import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const personalContactSchema = z.object({
  id: guidSchema,
  displayName: z.string(),
  phone: z.string().nullable().optional().default(null),
  email: z.string().nullable().optional().default(null),
  linkedUserIdentityId: guidSchema.nullable().optional().default(null),
  publicUserId: z.string().nullable().optional().default(null),
  linkedMaskedEmail: z.string().nullable().optional().default(null),
  linkedMaskedPhone: z.string().nullable().optional().default(null),
  status: z.string(),
  createdAtUtc: z.string(),
});

export const personalDebtRelationshipSummarySchema = z.object({
  id: guidSchema,
  perspective: z.string(),
  creditorUserIdentityId: guidSchema.nullable().optional().default(null),
  creditorContactId: guidSchema.nullable().optional().default(null),
  debtorUserIdentityId: guidSchema.nullable().optional().default(null),
  debtorContactId: guidSchema.nullable().optional().default(null),
  currencyCode: z.string(),
  currentBalance: z.number(),
  dueDateUtc: z.string().nullable().optional().default(null),
  status: z.string(),
  version: z.number().int(),
  updatedAtUtc: z.string(),
  isSharedLedger: z.boolean().optional().default(false),
  isPrivate: z.boolean().optional().default(true),
});

export const personalUtangBalanceSchema = z.object({
  relationshipId: guidSchema,
  currentBalance: z.number(),
  currencyCode: z.string(),
  version: z.number().int(),
  updatedAtUtc: z.string(),
});

export const personalUtangEntrySchema = z.object({
  id: guidSchema,
  relationshipId: guidSchema,
  entryType: z.string(),
  amount: z.number(),
  signedDelta: z.number(),
  balanceAfter: z.number(),
  notes: z.string().nullable().optional().default(null),
  dueDateUtc: z.string().nullable().optional().default(null),
  createdByUserIdentityId: guidSchema,
  createdAtUtc: z.string(),
  status: z.string().optional().default("Confirmed"),
  resolvedByUserIdentityId: guidSchema.nullable().optional().default(null),
  resolvedAtUtc: z.string().nullable().optional().default(null),
  disputeReason: z.string().nullable().optional().default(null),
  canConfirm: z.boolean().optional().default(false),
  canDispute: z.boolean().optional().default(false),
  canCancel: z.boolean().optional().default(false),
  affectsBalance: z.boolean().optional().default(true),
  isSharedLedger: z.boolean().optional().default(false),
  intent: z.string().optional().default("Regular"),
  settlementBalanceSnapshot: z.number().nullable().optional().default(null),
  isSettlement: z.boolean().optional().default(false),
});

export const settlePersonalDebtRelationshipResultSchema = z.object({
  outcome: z.enum(["Completed", "AwaitingCounterpartyConfirmation", "AlreadySettled"]),
  relationship: personalDebtRelationshipSummarySchema,
  settlementEntry: personalUtangEntrySchema.nullable().optional().default(null),
});

export const closePersonalDebtRelationshipResultSchema = z.object({
  outcome: z.enum(["Closed", "AlreadySettled"]),
  relationship: personalDebtRelationshipSummarySchema,
});

export type PersonalContactDto = z.infer<typeof personalContactSchema>;
export type PersonalDebtRelationshipSummaryDto = z.infer<
  typeof personalDebtRelationshipSummarySchema
>;
export type PersonalUtangBalanceDto = z.infer<typeof personalUtangBalanceSchema>;
export type PersonalUtangEntryDto = z.infer<typeof personalUtangEntrySchema>;
export type SettlePersonalDebtRelationshipResultDto = z.infer<
  typeof settlePersonalDebtRelationshipResultSchema
>;
export type ClosePersonalDebtRelationshipResultDto = z.infer<
  typeof closePersonalDebtRelationshipResultSchema
>;

export type SettlePersonalDebtRelationshipRequest = {
  expectedVersion?: number | null;
  /** Client-stable id for offline replay / ambiguous-outcome reconciliation (PERS-IDEM). */
  settlementEntryId?: string | null;
  notes?: string | null;
};

export type ClosePersonalDebtRelationshipRequest = {
  expectedVersion?: number | null;
};

export type CreatePersonalContactRequest = {
  displayName: string;
  phone?: string | null;
  email?: string | null;
  linkedUserIdentityId?: string | null;
  publicUserId?: string | null;
  /** Client-stable id for offline replay / ambiguous-outcome reconciliation (PERS-IDEM-01). */
  contactId?: string | null;
};

export type UpdatePersonalContactRequest = {
  displayName: string;
  phone?: string | null;
  email?: string | null;
  linkedUserIdentityId?: string | null;
  publicUserId?: string | null;
};

export type CreatePersonalDebtRelationshipRequest = {
  creditorUserIdentityId: string | null;
  creditorContactId: string | null;
  debtorUserIdentityId: string | null;
  debtorContactId: string | null;
  currencyCode?: string | null;
  dueDateUtc?: string | null;
  initialLoanAmount?: number | null;
  initialLoanNotes?: string | null;
  /** Client-stable id for offline replay / ambiguous-outcome reconciliation (PERS-IDEM-01). */
  relationshipId?: string | null;
  /** Client-stable id for the initial loan entry when initialLoanAmount is set. */
  initialLoanEntryId?: string | null;
};

export type RecordPersonalUtangEntryRequest = {
  entryType: "Loan" | "Payment" | "Adjustment";
  amount: number;
  adjustmentDelta?: number | null;
  expectedVersion?: number | null;
  notes?: string | null;
  dueDateUtc?: string | null;
  /** Client-stable id for offline replay / ambiguous-outcome reconciliation (PERS-IDEM-01). */
  entryId?: string | null;
};

export type ConfirmPersonalUtangEntryRequest = {
  expectedVersion?: number | null;
};

export type DisputePersonalUtangEntryRequest = {
  expectedVersion?: number | null;
  reason?: string | null;
};

export type CancelPersonalUtangEntryRequest = {
  expectedVersion?: number | null;
};

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

function normalizeContact(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    displayName: pick(r, "displayName", "DisplayName"),
    phone: pick(r, "phone", "Phone") ?? null,
    email: pick(r, "email", "Email") ?? null,
    linkedUserIdentityId: pick(r, "linkedUserIdentityId", "LinkedUserIdentityId") ?? null,
    publicUserId: pick(r, "publicUserId", "PublicUserId") ?? null,
    linkedMaskedEmail: pick(r, "linkedMaskedEmail", "LinkedMaskedEmail") ?? null,
    linkedMaskedPhone: pick(r, "linkedMaskedPhone", "LinkedMaskedPhone") ?? null,
    status: pick(r, "status", "Status"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
  };
}

function normalizeRelationship(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  const isSharedLedger = Boolean(pick(r, "isSharedLedger", "IsSharedLedger") ?? false);
  const isPrivateRaw = pick(r, "isPrivate", "IsPrivate");
  return {
    id: pick(r, "id", "Id"),
    perspective: pick(r, "perspective", "Perspective"),
    creditorUserIdentityId: pick(r, "creditorUserIdentityId", "CreditorUserIdentityId") ?? null,
    creditorContactId: pick(r, "creditorContactId", "CreditorContactId") ?? null,
    debtorUserIdentityId: pick(r, "debtorUserIdentityId", "DebtorUserIdentityId") ?? null,
    debtorContactId: pick(r, "debtorContactId", "DebtorContactId") ?? null,
    currencyCode: pick(r, "currencyCode", "CurrencyCode"),
    currentBalance: Number(pick(r, "currentBalance", "CurrentBalance") ?? 0),
    dueDateUtc: pick(r, "dueDateUtc", "DueDateUtc") ?? null,
    status: pick(r, "status", "Status"),
    version: Number(pick(r, "version", "Version") ?? 0),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
    isSharedLedger,
    isPrivate: isPrivateRaw == null ? !isSharedLedger : Boolean(isPrivateRaw),
  };
}

function normalizeBalance(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    relationshipId: pick(r, "relationshipId", "RelationshipId"),
    currentBalance: Number(pick(r, "currentBalance", "CurrentBalance") ?? 0),
    currencyCode: pick(r, "currencyCode", "CurrencyCode"),
    version: Number(pick(r, "version", "Version") ?? 0),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

function normalizeEntry(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  const status = String(pick(r, "status", "Status") ?? "Confirmed");
  const affectsBalanceRaw = pick(r, "affectsBalance", "AffectsBalance");
  const settlementSnapshotRaw = pick(r, "settlementBalanceSnapshot", "SettlementBalanceSnapshot");
  const isSettlement = Boolean(pick(r, "isSettlement", "IsSettlement") ?? false);
  return {
    id: pick(r, "id", "Id"),
    relationshipId: pick(r, "relationshipId", "RelationshipId"),
    entryType: pick(r, "entryType", "EntryType"),
    amount: Number(pick(r, "amount", "Amount") ?? 0),
    signedDelta: Number(pick(r, "signedDelta", "SignedDelta") ?? 0),
    balanceAfter: Number(pick(r, "balanceAfter", "BalanceAfter") ?? 0),
    notes: pick(r, "notes", "Notes") ?? null,
    dueDateUtc: pick(r, "dueDateUtc", "DueDateUtc") ?? null,
    createdByUserIdentityId: pick(r, "createdByUserIdentityId", "CreatedByUserIdentityId"),
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    status,
    resolvedByUserIdentityId: pick(r, "resolvedByUserIdentityId", "ResolvedByUserIdentityId") ?? null,
    resolvedAtUtc: pick(r, "resolvedAtUtc", "ResolvedAtUtc") ?? null,
    disputeReason: pick(r, "disputeReason", "DisputeReason") ?? null,
    canConfirm: Boolean(pick(r, "canConfirm", "CanConfirm") ?? false),
    canDispute: Boolean(pick(r, "canDispute", "CanDispute") ?? false),
    canCancel: Boolean(pick(r, "canCancel", "CanCancel") ?? false),
    affectsBalance:
      affectsBalanceRaw == null ? status === "Confirmed" : Boolean(affectsBalanceRaw),
    isSharedLedger: Boolean(pick(r, "isSharedLedger", "IsSharedLedger") ?? false),
    intent: String(pick(r, "intent", "Intent") ?? (isSettlement ? "Settlement" : "Regular")),
    settlementBalanceSnapshot:
      settlementSnapshotRaw == null || settlementSnapshotRaw === ""
        ? null
        : Number(settlementSnapshotRaw),
    isSettlement,
  };
}

function normalizeSettleResult(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  const settlementEntry = pick(r, "settlementEntry", "SettlementEntry");
  return {
    outcome: pick(r, "outcome", "Outcome"),
    relationship: normalizeRelationship(pick(r, "relationship", "Relationship")),
    settlementEntry: settlementEntry == null ? null : normalizeEntry(settlementEntry),
  };
}

function normalizeCloseResult(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    outcome: pick(r, "outcome", "Outcome"),
    relationship: normalizeRelationship(pick(r, "relationship", "Relationship")),
  };
}

const UTANG = "/api/v1/personal/utang";

export async function listPersonalContacts(signal?: AbortSignal): Promise<PersonalContactDto[]> {
  const raw = await platformRequest<unknown>({ path: `${UTANG}/contacts`, signal });
  const items = Array.isArray(raw) ? raw : [];
  return items.map((item) => personalContactSchema.parse(normalizeContact(item)));
}

export async function getPersonalContact(
  contactId: string,
  signal?: AbortSignal,
): Promise<PersonalContactDto> {
  const raw = await platformRequest<unknown>({
    path: `${UTANG}/contacts/${contactId}`,
    signal,
  });
  return personalContactSchema.parse(normalizeContact(raw));
}

export async function createPersonalContact(
  body: CreatePersonalContactRequest,
  signal?: AbortSignal,
): Promise<PersonalContactDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/contacts`,
    body,
    signal,
  });
  return personalContactSchema.parse(normalizeContact(raw));
}

export type LinkPersonalContactRequest = {
  linkedUserIdentityId?: string | null;
  publicUserId?: string | null;
};

export async function linkPersonalContact(
  contactId: string,
  body: LinkPersonalContactRequest,
  signal?: AbortSignal,
): Promise<PersonalContactDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/contacts/${contactId}/link`,
    body,
    signal,
  });
  return personalContactSchema.parse(normalizeContact(raw));
}

export async function updatePersonalContact(
  contactId: string,
  body: UpdatePersonalContactRequest,
  signal?: AbortSignal,
): Promise<PersonalContactDto> {
  const raw = await platformRequest<unknown>({
    method: "PUT",
    path: `${UTANG}/contacts/${contactId}`,
    body,
    signal,
  });
  return personalContactSchema.parse(normalizeContact(raw));
}

export async function listLentRelationships(
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto[]> {
  const raw = await platformRequest<unknown>({ path: `${UTANG}/relationships/lent`, signal });
  const items = Array.isArray(raw) ? raw : [];
  return items.map((item) =>
    personalDebtRelationshipSummarySchema.parse(normalizeRelationship(item)),
  );
}

export async function listBorrowedRelationships(
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto[]> {
  const raw = await platformRequest<unknown>({ path: `${UTANG}/relationships/borrowed`, signal });
  const items = Array.isArray(raw) ? raw : [];
  return items.map((item) =>
    personalDebtRelationshipSummarySchema.parse(normalizeRelationship(item)),
  );
}

export async function getPersonalDebtRelationship(
  relationshipId: string,
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto> {
  const raw = await platformRequest<unknown>({
    path: `${UTANG}/relationships/${relationshipId}`,
    signal,
  });
  return personalDebtRelationshipSummarySchema.parse(normalizeRelationship(raw));
}

export async function getPersonalUtangBalance(
  relationshipId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangBalanceDto> {
  const raw = await platformRequest<unknown>({
    path: `${UTANG}/relationships/${relationshipId}/balance`,
    signal,
  });
  return personalUtangBalanceSchema.parse(normalizeBalance(raw));
}

export async function listPersonalUtangHistory(
  relationshipId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangEntryDto[]> {
  const raw = await platformRequest<unknown>({
    path: `${UTANG}/relationships/${relationshipId}/history`,
    signal,
  });
  const items = Array.isArray(raw) ? raw : [];
  return items.map((item) => personalUtangEntrySchema.parse(normalizeEntry(item)));
}

export async function getPersonalUtangEntry(
  entryId: string,
  signal?: AbortSignal,
): Promise<PersonalUtangEntryDto> {
  const raw = await platformRequest<unknown>({
    path: `${UTANG}/entries/${entryId}`,
    signal,
  });
  return personalUtangEntrySchema.parse(normalizeEntry(raw));
}

export async function createPersonalDebtRelationship(
  body: CreatePersonalDebtRelationshipRequest,
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships`,
    body,
    signal,
  });
  return personalDebtRelationshipSummarySchema.parse(normalizeRelationship(raw));
}

export async function recordPersonalUtangEntry(
  relationshipId: string,
  body: RecordPersonalUtangEntryRequest,
  signal?: AbortSignal,
): Promise<PersonalUtangEntryDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships/${relationshipId}/entries`,
    body,
    signal,
  });
  return personalUtangEntrySchema.parse(normalizeEntry(raw));
}

export async function confirmPersonalUtangEntry(
  relationshipId: string,
  entryId: string,
  body: ConfirmPersonalUtangEntryRequest = {},
  signal?: AbortSignal,
): Promise<PersonalUtangEntryDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships/${relationshipId}/entries/${entryId}/confirm`,
    body,
    signal,
  });
  return personalUtangEntrySchema.parse(normalizeEntry(raw));
}

export async function disputePersonalUtangEntry(
  relationshipId: string,
  entryId: string,
  body: DisputePersonalUtangEntryRequest = {},
  signal?: AbortSignal,
): Promise<PersonalUtangEntryDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships/${relationshipId}/entries/${entryId}/dispute`,
    body,
    signal,
  });
  return personalUtangEntrySchema.parse(normalizeEntry(raw));
}

export async function cancelPersonalUtangEntry(
  relationshipId: string,
  entryId: string,
  body: CancelPersonalUtangEntryRequest = {},
  signal?: AbortSignal,
): Promise<PersonalUtangEntryDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships/${relationshipId}/entries/${entryId}/cancel`,
    body,
    signal,
  });
  return personalUtangEntrySchema.parse(normalizeEntry(raw));
}

export async function settlePersonalDebtRelationship(
  relationshipId: string,
  body: SettlePersonalDebtRelationshipRequest = {},
  signal?: AbortSignal,
): Promise<SettlePersonalDebtRelationshipResultDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships/${relationshipId}/settle`,
    body,
    signal,
  });
  return settlePersonalDebtRelationshipResultSchema.parse(normalizeSettleResult(raw));
}

export async function closePersonalDebtRelationship(
  relationshipId: string,
  body: ClosePersonalDebtRelationshipRequest = {},
  signal?: AbortSignal,
): Promise<ClosePersonalDebtRelationshipResultDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${UTANG}/relationships/${relationshipId}/close`,
    body,
    signal,
  });
  return closePersonalDebtRelationshipResultSchema.parse(normalizeCloseResult(raw));
}

export async function getPersonalMe(signal?: AbortSignal): Promise<{ userIdentityId: string }> {
  const raw = await platformRequest<unknown>({ path: "/api/v1/personal/me", signal });
  const r = (raw ?? {}) as Record<string, unknown>;
  const userIdentityId = String(r.userIdentityId ?? r.UserIdentityId ?? "");
  return { userIdentityId: guidSchema.parse(userIdentityId) };
}

export function isUtangConcurrencyConflict(error: unknown): boolean {
  if (!error || typeof error !== "object") return false;
  const err = error as { status?: number; errorCode?: string };
  return (
    err.status === 409 &&
    (err.errorCode === "application.concurrency_conflict" ||
      err.errorCode === "platform.personal.utang.concurrency_conflict")
  );
}

export function isUtangSettlementStaleConflict(error: unknown): boolean {
  if (!error || typeof error !== "object") return false;
  const err = error as { status?: number; errorCode?: string };
  const code = (err.errorCode ?? "").toLowerCase();
  if (code.includes("settlement.stale")) return true;
  return err.status === 409 && code.includes("settlement.stale");
}

export function formatDueLabel(
  dueDateUtc: string | null | undefined,
  now = new Date(),
): {
  kind: "none" | "overdue" | "dueSoon" | "upcoming";
  iso: string | null;
} {
  if (!dueDateUtc) return { kind: "none", iso: null };
  const due = new Date(dueDateUtc);
  if (Number.isNaN(due.getTime())) return { kind: "none", iso: dueDateUtc };
  const ms = due.getTime() - now.getTime();
  const days = ms / (1000 * 60 * 60 * 24);
  if (days < 0) return { kind: "overdue", iso: dueDateUtc };
  if (days <= 3) return { kind: "dueSoon", iso: dueDateUtc };
  return { kind: "upcoming", iso: dueDateUtc };
}
