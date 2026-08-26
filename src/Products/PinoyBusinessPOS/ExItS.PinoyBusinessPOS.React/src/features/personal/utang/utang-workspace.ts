import type {
  PersonalContactDto,
  PersonalDebtRelationshipSummaryDto,
} from "@/api/platform/personal-utang-client";
import { formatDueLabel } from "@/api/platform/personal-utang-client";

export type UtangPerspective = "lent" | "owe";

export type UtangAccountSegment = "all" | "lent" | "owe";

export type UtangAccountRow = {
  relationshipId: string;
  perspective: UtangPerspective;
  displayName: string;
  currentBalance: number;
  dueDateUtc: string | null;
  updatedAtUtc: string;
  isSharedLedger: boolean;
  status: string;
  dueKind: "none" | "overdue" | "dueSoon" | "upcoming";
};

export type UtangAttentionItem = {
  key: string;
  kind: "pendingConfirmation" | "overdue" | "dueSoon";
  count: number;
  /** Optional person name for single overdue/due-soon rows */
  displayName?: string;
  href: string;
};

export function resolveRelationshipContactName(
  contacts: ReadonlyArray<Pick<PersonalContactDto, "id" | "displayName">>,
  relationship: PersonalDebtRelationshipSummaryDto,
): string {
  const contactId =
    relationship.perspective === "Borrowed"
      ? relationship.creditorContactId
      : relationship.debtorContactId;
  if (contactId) {
    return contacts.find((c) => c.id === contactId)?.displayName ?? "—";
  }
  return "—";
}

export function toUtangAccountRow(
  relationship: PersonalDebtRelationshipSummaryDto,
  contacts: ReadonlyArray<Pick<PersonalContactDto, "id" | "displayName">>,
  now = new Date(),
): UtangAccountRow {
  const perspective: UtangPerspective =
    relationship.perspective === "Borrowed" ? "owe" : "lent";
  const due = formatDueLabel(relationship.dueDateUtc, now);
  return {
    relationshipId: relationship.id,
    perspective,
    displayName: resolveRelationshipContactName(contacts, relationship),
    currentBalance: relationship.currentBalance,
    dueDateUtc: relationship.dueDateUtc ?? null,
    updatedAtUtc: relationship.updatedAtUtc,
    isSharedLedger: Boolean(relationship.isSharedLedger),
    status: relationship.status,
    dueKind: due.kind,
  };
}

export function mergeUtangAccounts(
  lent: PersonalDebtRelationshipSummaryDto[],
  borrowed: PersonalDebtRelationshipSummaryDto[],
  contacts: ReadonlyArray<Pick<PersonalContactDto, "id" | "displayName">>,
  now = new Date(),
): UtangAccountRow[] {
  const byId = new Map<string, UtangAccountRow>();
  for (const row of lent) {
    byId.set(row.id, toUtangAccountRow(row, contacts, now));
  }
  for (const row of borrowed) {
    byId.set(row.id, toUtangAccountRow(row, contacts, now));
  }
  return [...byId.values()];
}

/** Active financial relationships: non-zero balance or with a due date still tracked. */
export function isActiveUtangAccount(row: UtangAccountRow): boolean {
  if (row.status.toLowerCase() !== "active") {
    return false;
  }
  return row.currentBalance > 0 || Boolean(row.dueDateUtc);
}

export function filterUtangAccounts(
  rows: UtangAccountRow[],
  segment: UtangAccountSegment,
  search: string,
): UtangAccountRow[] {
  const q = search.trim().toLowerCase();
  return rows.filter((row) => {
    if (segment === "lent" && row.perspective !== "lent") return false;
    if (segment === "owe" && row.perspective !== "owe") return false;
    if (q && !row.displayName.toLowerCase().includes(q)) return false;
    return true;
  });
}

/**
 * Sort: overdue → due soon → upcoming due → recently updated → name.
 * Pending confirmation is surfaced as a separate attention section (count-only from dashboard).
 */
export function sortUtangAccounts(rows: UtangAccountRow[]): UtangAccountRow[] {
  const dueRank = (kind: UtangAccountRow["dueKind"]): number => {
    switch (kind) {
      case "overdue":
        return 0;
      case "dueSoon":
        return 1;
      case "upcoming":
        return 2;
      default:
        return 3;
    }
  };

  return [...rows].sort((a, b) => {
    const dueDiff = dueRank(a.dueKind) - dueRank(b.dueKind);
    if (dueDiff !== 0) return dueDiff;
    const aTime = Date.parse(a.updatedAtUtc) || 0;
    const bTime = Date.parse(b.updatedAtUtc) || 0;
    if (bTime !== aTime) return bTime - aTime;
    return a.displayName.localeCompare(b.displayName);
  });
}

export function buildHomeAttentionItems(input: {
  pendingConfirmationCount: number;
  accounts: UtangAccountRow[];
}): UtangAttentionItem[] {
  const items: UtangAttentionItem[] = [];
  if (input.pendingConfirmationCount > 0) {
    items.push({
      key: "pending",
      kind: "pendingConfirmation",
      count: input.pendingConfirmationCount,
      href: "/personal/utang",
    });
  }

  const overdue = input.accounts.filter((a) => a.dueKind === "overdue");
  if (overdue.length === 1) {
    items.push({
      key: `overdue-${overdue[0].relationshipId}`,
      kind: "overdue",
      count: 1,
      displayName: overdue[0].displayName,
      href: `/personal/utang/relationships/${overdue[0].relationshipId}`,
    });
  } else if (overdue.length > 1) {
    items.push({
      key: "overdue",
      kind: "overdue",
      count: overdue.length,
      href: "/personal/utang?segment=all",
    });
  }

  if (items.length >= 3) {
    return items.slice(0, 3);
  }

  const dueSoon = input.accounts.filter((a) => a.dueKind === "dueSoon");
  if (dueSoon.length === 1) {
    items.push({
      key: `dueSoon-${dueSoon[0].relationshipId}`,
      kind: "dueSoon",
      count: 1,
      displayName: dueSoon[0].displayName,
      href: `/personal/utang/relationships/${dueSoon[0].relationshipId}`,
    });
  } else if (dueSoon.length > 1) {
    items.push({
      key: "dueSoon",
      kind: "dueSoon",
      count: dueSoon.length,
      href: "/personal/utang",
    });
  }

  return items.slice(0, 3);
}

export function countSegment(rows: UtangAccountRow[], segment: UtangAccountSegment): number {
  if (segment === "all") return rows.length;
  return rows.filter((r) => r.perspective === (segment === "lent" ? "lent" : "owe")).length;
}
