import type { PersonalInAppNotificationDto } from "@/api/platform/personal-types";

export type NotificationDayGroupKey = "today" | "yesterday" | "earlier";

export type NotificationDayGroup = {
  key: NotificationDayGroupKey;
  items: PersonalInAppNotificationDto[];
};

export type NotificationMonthGroup = {
  key: string;
  year: number;
  month: number;
  items: PersonalInAppNotificationDto[];
};

function startOfUtcDay(date: Date): number {
  return Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
}

export function classifyNotificationDayGroup(
  createdAtUtc: string,
  now: Date = new Date(),
): NotificationDayGroupKey {
  const created = new Date(createdAtUtc);
  const createdDay = startOfUtcDay(created);
  const today = startOfUtcDay(now);
  const dayMs = 24 * 60 * 60 * 1000;
  if (createdDay === today) {
    return "today";
  }
  if (createdDay === today - dayMs) {
    return "yesterday";
  }
  return "earlier";
}

export function groupNotificationsByDay(
  items: ReadonlyArray<PersonalInAppNotificationDto>,
  now: Date = new Date(),
): NotificationDayGroup[] {
  const buckets: Record<NotificationDayGroupKey, PersonalInAppNotificationDto[]> = {
    today: [],
    yesterday: [],
    earlier: [],
  };
  for (const item of items) {
    buckets[classifyNotificationDayGroup(item.createdAtUtc, now)].push(item);
  }
  return (["today", "yesterday", "earlier"] as const)
    .filter((key) => buckets[key].length > 0)
    .map((key) => ({ key, items: buckets[key] }));
}

export function groupNotificationsByMonth(
  items: ReadonlyArray<PersonalInAppNotificationDto>,
): NotificationMonthGroup[] {
  const map = new Map<string, NotificationMonthGroup>();
  for (const item of items) {
    const created = new Date(item.createdAtUtc);
    const year = created.getUTCFullYear();
    const month = created.getUTCMonth();
    const key = `${year}-${String(month + 1).padStart(2, "0")}`;
    const existing = map.get(key);
    if (existing) {
      existing.items.push(item);
    } else {
      map.set(key, { key, year, month, items: [item] });
    }
  }
  return [...map.values()].sort((a, b) => {
    if (a.year !== b.year) {
      return b.year - a.year;
    }
    return b.month - a.month;
  });
}

export function formatNotificationMonthHeading(
  group: Pick<NotificationMonthGroup, "year" | "month">,
  locale = "en",
): string {
  const date = new Date(Date.UTC(group.year, group.month, 1));
  return new Intl.DateTimeFormat(locale, { month: "long", year: "numeric", timeZone: "UTC" }).format(
    date,
  );
}

/** Historical status label for archived rows — never Accept/Decline. */
export function resolveArchivedNotificationStatusLabel(
  item: Pick<PersonalInAppNotificationDto, "relatedType" | "preview" | "isRead">,
  connectionStatus: string | null | undefined,
): "connected" | "declined" | "revoked" | "expired" | "resolved" | "read" | "unread" {
  const type = item.relatedType.trim().toLowerCase();
  if (type.includes("connection")) {
    const status = (connectionStatus ?? "").toLowerCase();
    if (status === "accepted") return "connected";
    if (status === "declined") return "declined";
    if (status === "revoked") return "revoked";
    if (status === "expired") return "expired";
    const preview = item.preview.toLowerCase();
    if (preview.includes("accepted")) return "connected";
    if (preview.includes("declined")) return "declined";
  }
  if (!item.isRead) {
    return "unread";
  }
  return item.preview.toLowerCase().includes("accepted") || item.preview.toLowerCase().includes("declined")
    ? "resolved"
    : "read";
}
