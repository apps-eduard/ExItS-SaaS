import { platformRequest } from "./platform-http";
import { asRecord, bool, num, nullableStr, str } from "./widget";

export type PersonalContactDto = {
  id: string;
  displayName: string;
  linkedUserIdentityId: string | null;
};

export type PersonalDebtRelationshipSummaryDto = {
  id: string;
  perspective: string;
  creditorUserIdentityId: string | null;
  creditorContactId: string | null;
  debtorUserIdentityId: string | null;
  debtorContactId: string | null;
  currentBalance: number;
  dueDateUtc: string | null;
  status: string;
  updatedAtUtc: string;
  isSharedLedger: boolean;
  isPrivate: boolean;
  isLedgerOwner: boolean;
};

export type DueKind = "none" | "overdue" | "dueSoon" | "upcoming";

export type UtangAttentionItem = {
  key: string;
  kind: "pendingConfirmation" | "overdue" | "dueSoon";
  count: number;
  displayName?: string;
};

function mapRelationship(raw: unknown): PersonalDebtRelationshipSummaryDto {
  const r = asRecord(raw);
  return {
    id: str(r, "id", "Id"),
    perspective: str(r, "perspective", "Perspective"),
    creditorUserIdentityId: nullableStr(r, "creditorUserIdentityId", "CreditorUserIdentityId"),
    creditorContactId: nullableStr(r, "creditorContactId", "CreditorContactId"),
    debtorUserIdentityId: nullableStr(r, "debtorUserIdentityId", "DebtorUserIdentityId"),
    debtorContactId: nullableStr(r, "debtorContactId", "DebtorContactId"),
    currentBalance: num(r, "currentBalance", "CurrentBalance"),
    dueDateUtc: nullableStr(r, "dueDateUtc", "DueDateUtc"),
    status: str(r, "status", "Status") || "Active",
    updatedAtUtc: str(r, "updatedAtUtc", "UpdatedAtUtc"),
    isSharedLedger: bool(r, "isSharedLedger", "IsSharedLedger"),
    isPrivate: r.isPrivate !== false && r.IsPrivate !== false,
    isLedgerOwner: r.isLedgerOwner !== false && r.IsLedgerOwner !== false,
  };
}

export async function listLentRelationships(
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/utang/relationships/lent",
    signal,
  });
  return (Array.isArray(raw) ? raw : []).map(mapRelationship);
}

export async function listBorrowedRelationships(
  signal?: AbortSignal,
): Promise<PersonalDebtRelationshipSummaryDto[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/utang/relationships/borrowed",
    signal,
  });
  return (Array.isArray(raw) ? raw : []).map(mapRelationship);
}

export async function listPersonalContacts(
  signal?: AbortSignal,
): Promise<PersonalContactDto[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/utang/contacts",
    signal,
  });
  return (Array.isArray(raw) ? raw : []).map((item) => {
    const r = asRecord(item);
    return {
      id: str(r, "id", "Id"),
      displayName: str(r, "displayName", "DisplayName"),
      linkedUserIdentityId: nullableStr(
        r,
        "linkedUserIdentityId",
        "LinkedUserIdentityId",
      ),
    };
  });
}

/** Same due buckets as original personal-utang-client.formatDueLabel. */
export function formatDueLabel(
  dueDateUtc: string | null | undefined,
  now = new Date(),
): { kind: DueKind; iso: string | null } {
  if (!dueDateUtc) return { kind: "none", iso: null };
  const due = new Date(dueDateUtc);
  if (Number.isNaN(due.getTime())) return { kind: "none", iso: dueDateUtc };
  const days = (due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);
  if (days < 0) return { kind: "overdue", iso: dueDateUtc };
  if (days <= 3) return { kind: "dueSoon", iso: dueDateUtc };
  return { kind: "upcoming", iso: dueDateUtc };
}

function resolveContactName(
  contacts: PersonalContactDto[],
  relationship: PersonalDebtRelationshipSummaryDto,
): string {
  const borrowed = relationship.perspective === "Borrowed";
  const contactId = borrowed
    ? relationship.creditorContactId
    : relationship.debtorContactId;
  if (contactId) {
    const byId = contacts.find((c) => c.id === contactId)?.displayName?.trim();
    if (byId) return byId;
  }
  const counterpartyUserId = borrowed
    ? relationship.creditorUserIdentityId
    : relationship.debtorUserIdentityId;
  if (counterpartyUserId) {
    const byLinked = contacts
      .find((c) => c.linkedUserIdentityId === counterpartyUserId)
      ?.displayName?.trim();
    if (byLinked) return byLinked;
  }
  return "—";
}

type AccountRow = {
  relationshipId: string;
  displayName: string;
  dueKind: DueKind;
  status: string;
  currentBalance: number;
  dueDateUtc: string | null;
};

function toAccountRow(
  relationship: PersonalDebtRelationshipSummaryDto,
  contacts: PersonalContactDto[],
  now: Date,
): AccountRow {
  return {
    relationshipId: relationship.id,
    displayName: resolveContactName(contacts, relationship),
    dueKind: formatDueLabel(relationship.dueDateUtc, now).kind,
    status: relationship.status,
    currentBalance: relationship.currentBalance,
    dueDateUtc: relationship.dueDateUtc,
  };
}

function isActiveAccount(row: AccountRow): boolean {
  if (row.status.toLowerCase() !== "active") return false;
  return row.currentBalance > 0 || Boolean(row.dueDateUtc);
}

/** Mirrors original buildHomeAttentionItems (max 3). */
export function buildHomeAttentionItems(input: {
  pendingConfirmationCount: number;
  lent: PersonalDebtRelationshipSummaryDto[];
  borrowed: PersonalDebtRelationshipSummaryDto[];
  contacts: PersonalContactDto[];
  now?: Date;
}): UtangAttentionItem[] {
  const now = input.now ?? new Date();
  const byId = new Map<string, AccountRow>();
  for (const row of input.lent) {
    byId.set(row.id, toAccountRow(row, input.contacts, now));
  }
  for (const row of input.borrowed) {
    byId.set(row.id, toAccountRow(row, input.contacts, now));
  }
  const accounts = [...byId.values()].filter(isActiveAccount);

  const items: UtangAttentionItem[] = [];
  if (input.pendingConfirmationCount > 0) {
    items.push({
      key: "pending",
      kind: "pendingConfirmation",
      count: input.pendingConfirmationCount,
    });
  }

  const overdue = accounts.filter((a) => a.dueKind === "overdue");
  if (overdue.length === 1) {
    items.push({
      key: `overdue-${overdue[0]!.relationshipId}`,
      kind: "overdue",
      count: 1,
      displayName: overdue[0]!.displayName,
    });
  } else if (overdue.length > 1) {
    items.push({ key: "overdue", kind: "overdue", count: overdue.length });
  }

  if (items.length >= 3) return items.slice(0, 3);

  const dueSoon = accounts.filter((a) => a.dueKind === "dueSoon");
  if (dueSoon.length === 1) {
    items.push({
      key: `dueSoon-${dueSoon[0]!.relationshipId}`,
      kind: "dueSoon",
      count: 1,
      displayName: dueSoon[0]!.displayName,
    });
  } else if (dueSoon.length > 1) {
    items.push({ key: "dueSoon", kind: "dueSoon", count: dueSoon.length });
  }

  return items.slice(0, 3);
}
