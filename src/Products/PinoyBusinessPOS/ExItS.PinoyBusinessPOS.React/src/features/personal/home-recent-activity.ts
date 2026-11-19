import type { PersonalInAppNotificationDto } from "@/api/platform/personal-social-client";
import type { PersonalTodoDto } from "@/api/platform/personal-todo-client";
import type { PersonalUtangEntryDto } from "@/api/platform/personal-utang-client";
import type { CustomerOrderListItemDto } from "@/api/pos/pos-customer-orders-client";
import {
  localizePersonalNotification,
  resolveNotificationDeepLink,
} from "@/features/personal/personal-notifications";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";
import type { MessageKey } from "@/i18n/messages";

export type HomeRecentActivityIcon =
  | "notification"
  | "utang-loan"
  | "utang-payment"
  | "utang-adjustment"
  | "utang-shared"
  | "order"
  | "todo"
  | "history";

export type HomeRecentActivityItem = {
  id: string;
  title: string;
  detail: string;
  at: string;
  href: string;
  icon: HomeRecentActivityIcon;
};

/** Cap shown on Personal Home. */
export const HOME_RECENT_ACTIVITY_MAX_ITEMS = 5;

/**
 * Bound per-relationship history fetches on Home.
 * Prefer newest-updated relationships; reuse `["personal","utang","history",id]` cache keys.
 */
export const HOME_RECENT_ACTIVITY_HISTORY_RELATIONSHIP_LIMIT = 8;

/** Newest entries considered per loaded relationship history. */
export const HOME_RECENT_ACTIVITY_ENTRIES_PER_RELATIONSHIP = 5;

/** Small buyer-order page for Home (notifications still cover order lifecycle). */
export const HOME_RECENT_ACTIVITY_ORDERS_PAGE_SIZE = 5;

/**
 * Dedupe strategy (stable keys):
 * - utang entry → `utang-entry:{entryId}`
 * - order list / CustomerOrder* notification → `order:{orderId}`
 * - todo activity / PersonalTodo notification → `todo:{todoId}`
 * - PersonalDebtRelationship notification → `utang-relationship:{relationshipId}`
 * - other notifications → `notification:{notificationId}`
 * On collision, prefer history/order/todo (priority 2) over notification (priority 1).
 */
type ActivityCandidate = HomeRecentActivityItem & {
  dedupeKey: string;
  priority: number;
};

export type HomeRecentActivityHistoryBundle = {
  relationshipId: string;
  displayName: string;
  perspective: UtangAccountRow["perspective"];
  isLedgerOwner: boolean;
  entries: ReadonlyArray<PersonalUtangEntryDto>;
};

export function formatHomeActivityWhen(iso: string): string {
  const time = Date.parse(iso);
  if (!Number.isFinite(time)) {
    return "";
  }
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
  }).format(new Date(time));
}

export function selectRecentHistoryRelationships(
  accounts: ReadonlyArray<UtangAccountRow>,
  limit = HOME_RECENT_ACTIVITY_HISTORY_RELATIONSHIP_LIMIT,
): UtangAccountRow[] {
  return [...accounts]
    .sort((a, b) => (Date.parse(b.updatedAtUtc) || 0) - (Date.parse(a.updatedAtUtc) || 0))
    .slice(0, Math.max(0, limit));
}

function formatAmount(value: number): string {
  return new Intl.NumberFormat(undefined, {
    style: "decimal",
    maximumFractionDigits: 2,
  }).format(value);
}

function parseTime(iso: string | null | undefined): number {
  if (!iso) return Number.NaN;
  return Date.parse(iso);
}

function entryActivityAt(entry: PersonalUtangEntryDto): string {
  const status = (entry.status ?? "").toLowerCase();
  const resolved = entry.resolvedAtUtc;
  if (
    resolved &&
    (status === "confirmed" || status === "disputed" || status === "cancelled") &&
    Number.isFinite(parseTime(resolved))
  ) {
    return resolved;
  }
  return entry.createdAtUtc;
}

function entryTitle(
  entry: PersonalUtangEntryDto,
  t: (key: MessageKey) => string,
): string {
  if (entry.isSettlement) {
    return t("personal.utang.settlementEntry");
  }
  if (entry.entryType === "Payment") {
    return t("personal.utang.recordPayment");
  }
  if (entry.entryType === "Adjustment") {
    return t("personal.utang.adjustBalance");
  }
  return t("personal.utang.addAmount");
}

function entryIcon(entry: PersonalUtangEntryDto, isLedgerOwner: boolean): HomeRecentActivityIcon {
  if (!isLedgerOwner) {
    return "utang-shared";
  }
  if (entry.entryType === "Payment") {
    return "utang-payment";
  }
  if (entry.entryType === "Adjustment") {
    return "utang-adjustment";
  }
  return "utang-loan";
}

function notificationDedupeKey(item: PersonalInAppNotificationDto): string {
  const type = item.relatedType.trim().toLowerCase();
  const relatedId = item.relatedId?.trim() || "";
  if (type.startsWith("customerorder") && relatedId) {
    return `order:${relatedId}`;
  }
  if ((type === "personaltodo" || type.includes("todo")) && relatedId) {
    return `todo:${relatedId}`;
  }
  if (
    (type === "personaldebtrelationship" ||
      type.includes("debtrelationship") ||
      type.includes("utangentry")) &&
    relatedId
  ) {
    return `utang-relationship:${relatedId}`;
  }
  return `notification:${item.id}`;
}

function candidatesFromNotifications(
  notifications: ReadonlyArray<PersonalInAppNotificationDto> | undefined,
  t: (key: MessageKey) => string,
): ActivityCandidate[] {
  const out: ActivityCandidate[] = [];
  for (const item of notifications ?? []) {
    if (!item.id || !item.title || !item.createdAtUtc) {
      continue;
    }
    if (!Number.isFinite(parseTime(item.createdAtUtc))) {
      continue;
    }
    const localized = localizePersonalNotification(item, t);
    out.push({
      id: `notification:${item.id}`,
      title: localized.title,
      detail: localized.preview,
      at: item.createdAtUtc,
      href: resolveNotificationDeepLink(item.relatedType, item.relatedId),
      icon: "notification",
      dedupeKey: notificationDedupeKey(item),
      priority: 1,
    });
  }
  return out;
}

function candidatesFromHistory(
  bundles: ReadonlyArray<HomeRecentActivityHistoryBundle> | undefined,
  t: (key: MessageKey) => string,
): ActivityCandidate[] {
  const out: ActivityCandidate[] = [];
  for (const bundle of bundles ?? []) {
    const newest = [...bundle.entries]
      .filter((entry) => entry.id && entry.createdAtUtc && Number.isFinite(parseTime(entry.createdAtUtc)))
      .sort(
        (a, b) =>
          (parseTime(entryActivityAt(b)) || 0) - (parseTime(entryActivityAt(a)) || 0) ||
          a.id.localeCompare(b.id),
      )
      .slice(0, HOME_RECENT_ACTIVITY_ENTRIES_PER_RELATIONSHIP);

    for (const entry of newest) {
      const at = entryActivityAt(entry);
      if (!Number.isFinite(parseTime(at))) {
        continue;
      }
      const amount = formatAmount(Math.abs(entry.amount));
      const status = entry.status || "Confirmed";
      const readOnly =
        bundle.isLedgerOwner === false ? ` · ${t("personal.utang.readOnly")}` : "";
      const detail = `${bundle.displayName} · ${amount} · ${status}${readOnly}`;
      out.push({
        id: `utang-entry:${entry.id}`,
        title: entryTitle(entry, t),
        detail,
        at,
        href: `/personal/utang/relationships/${bundle.relationshipId}`,
        icon: entryIcon(entry, bundle.isLedgerOwner),
        dedupeKey: `utang-entry:${entry.id}`,
        priority: 2,
      });
    }
  }
  return out;
}

function candidatesFromOrders(
  orders: ReadonlyArray<CustomerOrderListItemDto> | undefined,
  t: (key: MessageKey) => string,
): ActivityCandidate[] {
  const out: ActivityCandidate[] = [];
  for (const order of orders ?? []) {
    if (!order.orderId || !order.updatedAtUtc || !Number.isFinite(parseTime(order.updatedAtUtc))) {
      continue;
    }
    const amount = formatAmount(order.total);
    const store = order.branchNameSnapshot?.trim() || "";
    const detailParts = [
      `#${order.orderNumber}`,
      store || null,
      amount,
      order.status || null,
    ].filter(Boolean);
    out.push({
      id: `order:${order.orderId}`,
      title: t("personal.home.activityOrder"),
      detail: detailParts.join(" · "),
      at: order.updatedAtUtc,
      href: `/personal/orders/${order.orderId}`,
      icon: "order",
      dedupeKey: `order:${order.orderId}`,
      priority: 2,
    });
  }
  return out;
}

function candidatesFromTodos(
  todos: ReadonlyArray<PersonalTodoDto> | undefined,
  t: (key: MessageKey) => string,
): ActivityCandidate[] {
  const out: ActivityCandidate[] = [];
  for (const todo of todos ?? []) {
    if (!todo.id || !todo.title) {
      continue;
    }
    const completedAt = todo.completedAtUtc;
    const createdAt = todo.createdAtUtc;
    if (completedAt && Number.isFinite(parseTime(completedAt))) {
      out.push({
        id: `todo-completed:${todo.id}`,
        title: t("personal.home.activityTodoCompleted"),
        detail: todo.title,
        at: completedAt,
        href: `/personal/todo/${todo.id}`,
        icon: "todo",
        dedupeKey: `todo:${todo.id}`,
        priority: 2,
      });
      continue;
    }
    if (createdAt && Number.isFinite(parseTime(createdAt))) {
      out.push({
        id: `todo-created:${todo.id}`,
        title: t("personal.home.activityTodoCreated"),
        detail: todo.title,
        at: createdAt,
        href: `/personal/todo/${todo.id}`,
        icon: "todo",
        dedupeKey: `todo:${todo.id}`,
        priority: 2,
      });
    }
  }
  return out;
}

/**
 * Legacy coarse relationship rows — only when no typed history/order/todo/notification
 * candidates exist for the feed. Prefer real entry history when available.
 */
function candidatesFromAccounts(
  accounts: ReadonlyArray<UtangAccountRow> | undefined,
  t: (key: MessageKey) => string,
): ActivityCandidate[] {
  const out: ActivityCandidate[] = [];
  for (const row of accounts ?? []) {
    if (!row.relationshipId || !row.updatedAtUtc || !Number.isFinite(parseTime(row.updatedAtUtc))) {
      continue;
    }
    const mine = row.isLedgerOwner;
    const lent = row.perspective === "lent";
    const title = mine
      ? lent
        ? t("personal.home.activityMoneyLent")
        : t("personal.home.activityMoneyOwed")
      : t("personal.home.activityShared");
    const kind = lent
      ? t("personal.home.activityMoneyLent")
      : t("personal.home.activityMoneyOwed");
    const amount = formatAmount(row.currentBalance);
    const readOnly = !mine ? ` · ${t("personal.utang.readOnly")}` : "";
    const detail = mine
      ? `${row.displayName} · ${amount} · ${row.status}`
      : `${kind} · ${row.displayName} · ${amount}${readOnly}`;
    out.push({
      id: `utang-relationship:${row.relationshipId}`,
      title,
      detail,
      at: row.updatedAtUtc,
      href: `/personal/utang/relationships/${row.relationshipId}`,
      icon: mine ? (lent ? "utang-loan" : "utang-payment") : "utang-shared",
      dedupeKey: `utang-relationship:${row.relationshipId}`,
      priority: 1,
    });
  }
  return out;
}

function mergeActivityCandidates(candidates: ReadonlyArray<ActivityCandidate>): HomeRecentActivityItem[] {
  const byKey = new Map<string, ActivityCandidate>();
  for (const candidate of candidates) {
    const existing = byKey.get(candidate.dedupeKey);
    if (!existing) {
      byKey.set(candidate.dedupeKey, candidate);
      continue;
    }
    if (candidate.priority > existing.priority) {
      byKey.set(candidate.dedupeKey, candidate);
      continue;
    }
    if (candidate.priority < existing.priority) {
      continue;
    }
    const candidateTime = parseTime(candidate.at) || 0;
    const existingTime = parseTime(existing.at) || 0;
    if (candidateTime > existingTime || (candidateTime === existingTime && candidate.id < existing.id)) {
      byKey.set(candidate.dedupeKey, candidate);
    }
  }

  return [...byKey.values()]
    .sort(
      (a, b) =>
        (parseTime(b.at) || 0) - (parseTime(a.at) || 0) || a.id.localeCompare(b.id),
    )
    .slice(0, HOME_RECENT_ACTIVITY_MAX_ITEMS)
    .map(({ dedupeKey: _dedupeKey, priority: _priority, ...item }) => item);
}

export function buildHomeRecentActivityItems(input: {
  notifications?: ReadonlyArray<PersonalInAppNotificationDto> | undefined;
  accounts?: ReadonlyArray<UtangAccountRow>;
  history?: ReadonlyArray<HomeRecentActivityHistoryBundle>;
  orders?: ReadonlyArray<CustomerOrderListItemDto>;
  todos?: ReadonlyArray<PersonalTodoDto>;
  t: (key: MessageKey) => string;
}): HomeRecentActivityItem[] {
  const typed: ActivityCandidate[] = [
    ...candidatesFromNotifications(input.notifications, input.t),
    ...candidatesFromHistory(input.history, input.t),
    ...candidatesFromOrders(input.orders, input.t),
    ...candidatesFromTodos(input.todos, input.t),
  ];

  if (typed.length > 0) {
    return mergeActivityCandidates(typed);
  }

  // Preserve available relationship summaries only when typed sources are empty
  // (e.g. history not loaded yet / unavailable), so Home does not falsely look empty.
  return mergeActivityCandidates(candidatesFromAccounts(input.accounts, input.t));
}
