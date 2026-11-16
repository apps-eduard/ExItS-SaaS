import { PlatformApiError, readJson } from "../auth/platform-client";

export type MoneyTotal = { currency: string; amount: number };

export type DebtRecord = {
  id: string;
  perspective: "lent" | "borrowed";
  currencyCode: string;
  currentBalance: number;
  dueDateUtc: string | null;
  status: string;
  updatedAtUtc: string;
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
  lent: MoneyTotal[];
  borrowed: MoneyTotal[];
  upcoming: Array<{ id: string; label: string; when: string }>;
  attention: string[];
  activity: ActivityRecord[];
  tasks: TaskRecord[];
  stores: StoreRecord[];
  unavailable: Partial<Record<"money" | "upcoming" | "attention" | "activity" | "tasks" | "stores", string>>;
};

type DebtDto = {
  id?: string;
  currencyCode?: string;
  currentBalance?: number;
  dueDateUtc?: string | null;
  status?: string;
  updatedAtUtc?: string;
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

function debts(rows: DebtDto[] | undefined, perspective: DebtRecord["perspective"]): DebtRecord[] {
  return (rows ?? []).flatMap((row) => {
    if (!row.id || !row.currencyCode || row.currentBalance === undefined || !row.updatedAtUtc) return [];
    return [{
      id: row.id,
      perspective,
      currencyCode: row.currencyCode,
      currentBalance: row.currentBalance,
      dueDateUtc: row.dueDateUtc ?? null,
      status: row.status ?? "Unknown",
      updatedAtUtc: row.updatedAtUtc,
    }];
  });
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
  const records = [
    ...("value" in lent ? debts(lent.value, "lent") : []),
    ...("value" in borrowed ? debts(borrowed.value, "borrowed") : []),
  ];
  const moneyMessage = sectionMessage(lent) ?? sectionMessage(borrowed);
  if (moneyMessage) unavailable.money = moneyMessage;
  const attentionMessage = sectionMessage(dashboard);
  if (attentionMessage && sectionMessage(lent)) unavailable.attention = attentionMessage;

  const upcoming = records.flatMap((row) => {
    if (!row.dueDateUtc || new Date(row.dueDateUtc).getTime() < now.getTime()) return [];
    const kind = row.perspective === "lent" ? "Money lent" : "Money owed";
    return [{ id: row.id, label: `${kind} · ${formatAmount(row.currentBalance, row.currencyCode)}`, when: row.dueDateUtc }];
  });
  if (moneyMessage) unavailable.upcoming = moneyMessage;

  const attention = records.flatMap((row) => {
    if (!row.dueDateUtc || new Date(row.dueDateUtc).getTime() >= now.getTime()) return [];
    const kind = row.perspective === "lent" ? "Money lent" : "Money owed";
    return [`${kind} was due ${formatAmount(row.currentBalance, row.currencyCode)}.`];
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
      title: row.perspective === "lent" ? "Money lent" : "Money owed",
      detail: `${formatAmount(row.currentBalance, row.currencyCode)} · ${row.status}`,
      at: row.updatedAtUtc,
    }));
  const activity = noticeRows.length > 0 ? noticeRows : balanceRows;
  const activityMessage = sectionMessage(notices);
  if (activityMessage && activity.length === 0) unavailable.activity = activityMessage;

  return {
    lent: moneyTotals(records.filter((row) => row.perspective === "lent")),
    borrowed: moneyTotals(records.filter((row) => row.perspective === "borrowed")),
    upcoming,
    attention,
    activity,
    tasks,
    stores: storeRows,
    unavailable,
  };
}
