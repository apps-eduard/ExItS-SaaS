import { PlatformApiError, readJson } from "../auth/platform-client";

export type MoneyTotal = { currency: string; amount: number };

export type DebtPerspective = "lent" | "borrowed";

export type OwnershipLabel = "private" | "sharedByMe" | "sharedWithMe" | "readOnly";

export type DebtRecord = {
  id: string;
  perspective: DebtPerspective;
  currencyCode: string;
  /** Display balance from the list API: confirmed CurrentBalance plus unresolved legacy Pending deltas. */
  currentBalance: number;
  dueDateUtc: string | null;
  status: string;
  updatedAtUtc: string;
  isLedgerOwner: boolean;
  isSharedLedger: boolean;
  isPrivate: boolean;
  ledgerOwnerUserIdentityId: string | null;
};

export type TaskRecord = {
  id: string;
  title: string;
  dueAtUtc: string | null;
  status: string;
};

export type StoreRecord = { id: string; name: string };

export type ActivityRecord = { id: string; title: string; detail: string; at: string };

export type HomeSnapshot = {
  /** My Records only. Shared with me is not included. */
  lent: MoneyTotal[];
  /** My Records only. Shared with me is not included. */
  borrowed: MoneyTotal[];
  /** Informational balances for ledgers another person owns. */
  sharedLent: MoneyTotal[];
  sharedBorrowed: MoneyTotal[];
  upcoming: Array<{ id: string; label: string; when: string }>;
  attention: string[];
  activity: ActivityRecord[];
  tasks: TaskRecord[];
  stores: StoreRecord[];
  unavailable: Partial<Record<"money" | "upcoming" | "attention" | "activity" | "tasks" | "stores", string>>;
};

type DebtDto = {
  id?: string;
  perspective?: string;
  currencyCode?: string;
  currentBalance?: number;
  dueDateUtc?: string | null;
  status?: string;
  updatedAtUtc?: string;
  isLedgerOwner?: boolean;
  isSharedLedger?: boolean;
  isPrivate?: boolean;
  ledgerOwnerUserIdentityId?: string | null;
};

type TodoDto = { id?: string; title?: string; dueAtUtc?: string | null; status?: string };

type StorePage = { items?: Array<{ linkedCustomerId?: string; organizationDisplayName?: string; customerDisplayName?: string }> };

type NoticeDto = { id?: string; title?: string; preview?: string; createdAtUtc?: string };

type DashboardDto = { pendingConfirmationCount?: number; utangAvailable?: boolean };

const CLOSED = new Set(["completed", "cancelled", "canceled", "done", "closed"]);

export function moneyTotals(rows: Array<{ currencyCode: string; currentBalance: number }>): MoneyTotal[] {
  const totals = new Map<string, number>();
  for (const row of rows) {
    if (!row.currencyCode) continue;
    totals.set(row.currencyCode, (totals.get(row.currencyCode) ?? 0) + row.currentBalance);
  }
  return [...totals.entries()].map(([currency, amount]) => ({ currency, amount }));
}

export function formatAmount(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency, maximumFractionDigits: 2 }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

function sectionMessage<T>(result: { value: T } | { message: string }) {
  return "message" in result ? result.message : undefined;
}

export function friendlyLoadError(error: unknown) {
  if (error instanceof PlatformApiError && error.status === 401) return "Your session has ended.";
  return "We couldn't load this. Try again.";
}

function messageFrom(error: unknown) {
  return friendlyLoadError(error);
}

async function loadSection<T>(path: string): Promise<{ value: T } | { message: string }> {
  try {
    return { value: await readJson<T>(path) };
  } catch (error) {
    if (error instanceof PlatformApiError && error.status === 401) throw error;
    return { message: messageFrom(error) };
  }
}

/** Ledger ownership comes only from isLedgerOwner. A missing value stays My Records, matching the list contract default. */
export function isMyRecord(row: { isLedgerOwner?: boolean }) {
  return row.isLedgerOwner !== false;
}

/** The ledger owner is the only financial writer. */
export function canChangeLedger(row: { isLedgerOwner?: boolean }) {
  return isMyRecord(row);
}

export function ownershipLabels(row: {
  isLedgerOwner?: boolean;
  isSharedLedger?: boolean;
  isPrivate?: boolean;
}): OwnershipLabel[] {
  if (!isMyRecord(row)) return ["sharedWithMe", "readOnly"];
  const sharedByMe = row.isSharedLedger === true || row.isPrivate === false;
  return sharedByMe ? ["sharedByMe"] : ["private"];
}

function perspectiveOf(row: DebtDto, fallback: DebtPerspective): DebtPerspective | null {
  const value = row.perspective?.toLowerCase();
  if (value === "lent") return "lent";
  if (value === "borrowed") return "borrowed";
  if (value) return null;
  return fallback;
}

function debts(rows: DebtDto[] | undefined, fallback: DebtPerspective): DebtRecord[] {
  return (rows ?? []).flatMap((row) => {
    const perspective = perspectiveOf(row, fallback);
    if (!row.id || !row.currencyCode || row.currentBalance === undefined || !row.updatedAtUtc || !perspective) return [];
    const isLedgerOwner = isMyRecord(row);
    const isSharedLedger = row.isSharedLedger === true || row.isPrivate === false;
    return [{
      id: row.id,
      perspective,
      currencyCode: row.currencyCode,
      currentBalance: row.currentBalance,
      dueDateUtc: row.dueDateUtc ?? null,
      status: row.status ?? "Unknown",
      updatedAtUtc: row.updatedAtUtc,
      isLedgerOwner,
      isSharedLedger,
      isPrivate: !isSharedLedger,
      ledgerOwnerUserIdentityId: row.ledgerOwnerUserIdentityId ?? null,
    }];
  });
}

function uniqueRecords(rows: DebtRecord[]) {
  const byId = new Map<string, DebtRecord>();
  for (const row of rows) {
    if (!byId.has(row.id)) byId.set(row.id, row);
  }
  return [...byId.values()];
}

function activeRecords(rows: DebtRecord[]) {
  return rows.filter((row) => row.status.toLowerCase() === "active");
}

function openTasks(rows: TodoDto[] | undefined): TaskRecord[] {
  return (rows ?? []).flatMap((row) => {
    if (!row.id || !row.title) return [];
    const status = row.status ?? "Unknown";
    if (CLOSED.has(status.toLowerCase())) return [];
    return [{ id: row.id, title: row.title, dueAtUtc: row.dueAtUtc ?? null, status }];
  });
}

export async function loadPersonalHome(now = new Date()): Promise<HomeSnapshot> {
  const [dashboard, lent, borrowed, todos, stores, notices] = await Promise.all([
    loadSection<DashboardDto>("/api/v1/personal/dashboard"),
    loadSection<DebtDto[]>("/api/v1/personal/utang/relationships/lent"),
    loadSection<DebtDto[]>("/api/v1/personal/utang/relationships/borrowed"),
    loadSection<TodoDto[]>("/api/v1/personal/todos"),
    loadSection<StorePage>("/api/v1/personal/linked-merchants?page=1&pageSize=20"),
    loadSection<NoticeDto[]>("/api/v1/personal/notifications?scope=recent"),
  ]);

  const unavailable: HomeSnapshot["unavailable"] = {};
  const records = activeRecords(uniqueRecords([
    ...("value" in lent ? debts(lent.value, "lent") : []),
    ...("value" in borrowed ? debts(borrowed.value, "borrowed") : []),
  ]));
  const mine = records.filter((row) => row.isLedgerOwner);
  const shared = records.filter((row) => !row.isLedgerOwner);
  const moneyMessage = sectionMessage(lent) ?? sectionMessage(borrowed);
  if (moneyMessage) unavailable.money = moneyMessage;
  const attentionMessage = sectionMessage(dashboard);
  if (attentionMessage && sectionMessage(lent)) unavailable.attention = attentionMessage;

  const upcoming = records.flatMap((row) => {
    if (!row.dueDateUtc || new Date(row.dueDateUtc).getTime() < now.getTime()) return [];
    const kind = row.perspective === "lent" ? "Money lent" : "Money owed";
    const prefix = row.isLedgerOwner ? kind : `Shared with me · ${kind}`;
    return [{ id: row.id, label: `${prefix} · ${formatAmount(row.currentBalance, row.currencyCode)}`, when: row.dueDateUtc }];
  });
  if (moneyMessage) unavailable.upcoming = moneyMessage;

  const attention = records.flatMap((row) => {
    if (!row.dueDateUtc || new Date(row.dueDateUtc).getTime() >= now.getTime()) return [];
    const kind = row.perspective === "lent" ? "Money lent" : "Money owed";
    const prefix = row.isLedgerOwner ? kind : `Shared with me: ${kind}`;
    return [`${prefix} was due ${formatAmount(row.currentBalance, row.currencyCode)}.`];
  });
  if ("value" in dashboard && (dashboard.value.pendingConfirmationCount ?? 0) > 0) {
    attention.push(`${dashboard.value.pendingConfirmationCount} payment confirmation${dashboard.value.pendingConfirmationCount === 1 ? "" : "s"} waiting.`);
  }

  const tasks = "value" in todos ? openTasks(todos.value) : [];
  const tasksMessage = sectionMessage(todos);
  if (tasksMessage) unavailable.tasks = tasksMessage;

  const storeRows: StoreRecord[] = "value" in stores
    ? (stores.value.items ?? []).flatMap((item) => {
      const name = item.organizationDisplayName || item.customerDisplayName;
      if (!item.linkedCustomerId || !name) return [];
      return [{ id: item.linkedCustomerId, name }];
    })
    : [];
  const storesMessage = sectionMessage(stores);
  if (storesMessage) unavailable.stores = storesMessage;

  const noticeRows: ActivityRecord[] = "value" in notices
    ? notices.value.flatMap((item) => {
      if (!item.id || !item.title || !item.createdAtUtc) return [];
      return [{ id: item.id, title: item.title, detail: item.preview ?? "", at: item.createdAtUtc }];
    })
    : [];
  const balanceRows = records
    .slice()
    .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
    .slice(0, 6)
    .map((row) => ({
      id: row.id,
      title: row.isLedgerOwner
        ? (row.perspective === "lent" ? "Money lent" : "Money owed")
        : "Shared with me",
      detail: row.isLedgerOwner
        ? `${formatAmount(row.currentBalance, row.currencyCode)} · ${row.status}`
        : `${row.perspective === "lent" ? "Money lent" : "Money owed"} · ${formatAmount(row.currentBalance, row.currencyCode)} · Read only`,
      at: row.updatedAtUtc,
    }));
  const activity = noticeRows.length > 0 ? noticeRows : balanceRows;
  const activityMessage = sectionMessage(notices);
  if (activityMessage && activity.length === 0) unavailable.activity = activityMessage;

  return {
    lent: moneyTotals(mine.filter((row) => row.perspective === "lent")),
    borrowed: moneyTotals(mine.filter((row) => row.perspective === "borrowed")),
    sharedLent: moneyTotals(shared.filter((row) => row.perspective === "lent")),
    sharedBorrowed: moneyTotals(shared.filter((row) => row.perspective === "borrowed")),
    upcoming,
    attention,
    activity,
    tasks,
    stores: storeRows,
    unavailable,
  };
}
