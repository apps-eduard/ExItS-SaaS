import type {
  PersonalConnectionRequestDto,
  PersonalContactDto,
  PersonalDebtRelationshipSummaryDto,
} from "@/api/platform/personal-types";

export type PeopleConnectionStatus =
  | "local"
  | "not_connected"
  | "request_sent"
  | "request_received"
  | "connected"
  | "blocked";

export type PeopleRowModel = {
  contact: PersonalContactDto;
  connectionStatus: PeopleConnectionStatus;
  identityLine: "local" | "exits";
  publicUserId?: string;
  pendingConnectionRequest?: PersonalConnectionRequestDto;
  utangSummary?: string;
};

export function isPendingConnectionRequest(request: PersonalConnectionRequestDto): boolean {
  return request.status.toLowerCase() === "pending";
}

function sameId(a: string | null | undefined, b: string | null | undefined): boolean {
  if (!a || !b) {
    return false;
  }
  return a.localeCompare(b, undefined, { sensitivity: "accent" }) === 0;
}

function isSentDirection(request: PersonalConnectionRequestDto): boolean {
  return request.direction.toLowerCase() === "sent";
}

function isReceivedDirection(request: PersonalConnectionRequestDto): boolean {
  return request.direction.toLowerCase() === "received";
}

export function deriveConnectionStatus(
  contact: PersonalContactDto,
  connectionRequests: PersonalConnectionRequestDto[],
): { status: PeopleConnectionStatus; pendingConnectionRequest?: PersonalConnectionRequestDto } {
  if (contact.blockedAtUtc) {
    return { status: "blocked" };
  }

  if (contact.linkedUserIdentityId) {
    return { status: "connected" };
  }

  const pendingOutgoing = connectionRequests.find(
    (request) =>
      isPendingConnectionRequest(request) &&
      isSentDirection(request) &&
      (sameId(request.requesterContactId, contact.id) ||
        sameId(request.targetUserIdentityId, contact.resolvedUserIdentityId)),
  );

  if (pendingOutgoing) {
    return { status: "request_sent", pendingConnectionRequest: pendingOutgoing };
  }

  const pendingIncoming = connectionRequests.find(
    (request) =>
      isPendingConnectionRequest(request) &&
      isReceivedDirection(request) &&
      sameId(request.requesterUserIdentityId, contact.resolvedUserIdentityId),
  );

  if (pendingIncoming) {
    return { status: "request_received", pendingConnectionRequest: pendingIncoming };
  }

  if (!contact.resolvedUserIdentityId && !contact.resolvedPublicUserId) {
    return { status: "local" };
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
  connectionRequests: PersonalConnectionRequestDto[];
  lent: PersonalDebtRelationshipSummaryDto[];
  borrowed: PersonalDebtRelationshipSummaryDto[];
  search?: string;
}): PeopleRowModel[] {
  const needle = input.search?.trim().toLowerCase() ?? "";

  const active = input.contacts.filter((c) => c.status.toLowerCase() !== "archived");
  const rows = active.map((contact) => {
    const { status, pendingConnectionRequest } = deriveConnectionStatus(
      contact,
      input.connectionRequests,
    );
    const publicUserId = contact.resolvedPublicUserId ?? undefined;
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
      pendingConnectionRequest,
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

export function summarizePeopleContacts(contacts: PersonalContactDto[]): {
  identified: number;
  local: number;
  total: number;
} {
  const active = contacts.filter((c) => c.status.toLowerCase() !== "archived");
  const identified = active.filter(
    (c) => c.resolvedUserIdentityId || c.resolvedPublicUserId,
  ).length;
  const local = active.length - identified;
  return { identified, local, total: active.length };
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
