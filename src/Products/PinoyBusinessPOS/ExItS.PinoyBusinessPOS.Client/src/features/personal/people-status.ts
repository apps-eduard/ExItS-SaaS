import type {
  PersonalContactDto,
  PersonalDebtRelationshipSummaryDto,
  PersonalUtangInvitationDto,
} from "@/api/platform/personal-types";

export type PeopleConnectionStatus = "not_connected" | "request_pending" | "connected";

export type PeopleRowModel = {
  contact: PersonalContactDto;
  connectionStatus: PeopleConnectionStatus;
  identityLine: "local" | "exits";
  publicUserId?: string;
  pendingInvitation?: PersonalUtangInvitationDto;
  utangSummary?: string;
};

const RESOLVED_ID_CACHE_KEY = "exits.personal.resolvedPublicIds";

export function readResolvedPublicIdCache(): Record<string, string> {
  try {
    const raw = sessionStorage.getItem(RESOLVED_ID_CACHE_KEY);
    if (!raw) {
      return {};
    }
    const parsed = JSON.parse(raw) as unknown;
    if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) {
      return {};
    }
    const out: Record<string, string> = {};
    for (const [key, value] of Object.entries(parsed)) {
      if (typeof value === "string" && value.trim()) {
        out[key] = value;
      }
    }
    return out;
  } catch {
    return {};
  }
}

export function rememberResolvedPublicId(contactId: string, publicUserId: string): void {
  const next = { ...readResolvedPublicIdCache(), [contactId]: publicUserId };
  sessionStorage.setItem(RESOLVED_ID_CACHE_KEY, JSON.stringify(next));
}

export function isPendingInvitation(invite: PersonalUtangInvitationDto): boolean {
  return invite.status.toLowerCase() === "pending";
}

export function deriveConnectionStatus(
  contact: PersonalContactDto,
  invitations: PersonalUtangInvitationDto[],
): { status: PeopleConnectionStatus; pendingInvitation?: PersonalUtangInvitationDto } {
  if (contact.linkedUserIdentityId) {
    return { status: "connected" };
  }

  const pendingInvitation = invitations.find(
    (invite) =>
      invite.inviteeContactId === contact.id &&
      isPendingInvitation(invite) &&
      !invite.acceptedByUserIdentityId,
  );

  if (pendingInvitation) {
    return { status: "request_pending", pendingInvitation };
  }

  return { status: "not_connected" };
}

function formatMoney(amount: number, currencyCode: string): string {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: currencyCode || "PHP",
      maximumFractionDigits: 2,
    }).format(amount);
  } catch {
    return `${currencyCode} ${amount.toFixed(2)}`;
  }
}

export function buildPeopleRows(input: {
  contacts: PersonalContactDto[];
  invitations: PersonalUtangInvitationDto[];
  lent: PersonalDebtRelationshipSummaryDto[];
  borrowed: PersonalDebtRelationshipSummaryDto[];
  resolvedPublicIds?: Record<string, string>;
  search?: string;
}): PeopleRowModel[] {
  const resolved = input.resolvedPublicIds ?? {};
  const needle = input.search?.trim().toLowerCase() ?? "";

  const active = input.contacts.filter((c) => c.status.toLowerCase() !== "archived");
  const rows = active.map((contact) => {
    const { status, pendingInvitation } = deriveConnectionStatus(contact, input.invitations);
    const publicUserId = resolved[contact.id];
    const related = [...input.lent, ...input.borrowed].filter(
      (rel) =>
        rel.creditorContactId === contact.id ||
        rel.debtorContactId === contact.id ||
        (contact.linkedUserIdentityId &&
          (rel.creditorUserIdentityId === contact.linkedUserIdentityId ||
            rel.debtorUserIdentityId === contact.linkedUserIdentityId)),
    );
    const open = related.find((rel) => rel.status.toLowerCase() === "active");
    let utangSummary: string | undefined;
    if (open) {
      const perspective = open.perspective.toLowerCase();
      const money = formatMoney(open.currentBalance, open.currencyCode);
      if (perspective.includes("lent") || open.creditorContactId === null) {
        utangSummary = `You lent ${money}`;
      } else if (perspective.includes("borrow")) {
        utangSummary = `You borrowed ${money}`;
      } else {
        utangSummary = money;
      }
    }

    return {
      contact,
      connectionStatus: status,
      identityLine: publicUserId ? ("exits" as const) : ("local" as const),
      publicUserId,
      pendingInvitation,
      utangSummary,
    };
  });

  if (!needle) {
    return rows.sort((a, b) => a.contact.displayName.localeCompare(b.contact.displayName));
  }

  return rows
    .filter((row) => {
      const hay = `${row.contact.displayName} ${row.publicUserId ?? ""}`.toLowerCase();
      return hay.includes(needle);
    })
    .sort((a, b) => a.contact.displayName.localeCompare(b.contact.displayName));
}

export function initialsFor(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }
  if (parts.length === 1) {
    return parts[0]!.slice(0, 2).toUpperCase();
  }
  return `${parts[0]![0] ?? ""}${parts[1]![0] ?? ""}`.toUpperCase();
}

export function formatShortDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" });
}
