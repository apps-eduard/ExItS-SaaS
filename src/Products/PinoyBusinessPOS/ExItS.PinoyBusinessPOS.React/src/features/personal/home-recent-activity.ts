import type { PersonalInAppNotificationDto } from "@/api/platform/personal-social-client";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";
import { localizePersonalNotification } from "@/features/personal/personal-notifications";
import type { MessageKey } from "@/i18n/messages";

export type HomeRecentActivityItem = {
  id: string;
  title: string;
  detail: string;
  at: string;
  href: string;
};

const MAX_ITEMS = 5;

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

export function buildHomeRecentActivityItems(input: {
  notifications: ReadonlyArray<PersonalInAppNotificationDto> | undefined;
  accounts: ReadonlyArray<UtangAccountRow>;
  t: (key: MessageKey) => string;
}): HomeRecentActivityItem[] {
  const fromNotices = (input.notifications ?? [])
    .filter((item) => item.id && item.title && item.createdAtUtc)
    .slice(0, MAX_ITEMS)
    .map((item) => {
      const localized = localizePersonalNotification(item, input.t);
      return {
        id: item.id,
        title: localized.title,
        detail: localized.preview,
        at: item.createdAtUtc,
        href: "/personal/notifications",
      };
    });

  if (fromNotices.length > 0) {
    return fromNotices;
  }

  return [...input.accounts]
    .sort((a, b) => (Date.parse(b.updatedAtUtc) || 0) - (Date.parse(a.updatedAtUtc) || 0))
    .slice(0, MAX_ITEMS)
    .map((row) => {
      const mine = row.isLedgerOwner;
      const lent = row.perspective === "lent";
      const title = mine
        ? lent
          ? input.t("personal.home.activityMoneyLent")
          : input.t("personal.home.activityMoneyOwed")
        : input.t("personal.home.activityShared");
      const kind = lent
        ? input.t("personal.home.activityMoneyLent")
        : input.t("personal.home.activityMoneyOwed");
      const amount = new Intl.NumberFormat(undefined, {
        style: "decimal",
        maximumFractionDigits: 2,
      }).format(row.currentBalance);
      const detail = mine
        ? `${row.displayName} · ${amount} · ${row.status}`
        : `${kind} · ${row.displayName} · ${amount}`;
      return {
        id: row.relationshipId,
        title,
        detail,
        at: row.updatedAtUtc,
        href: lent ? "/personal/utang/lent" : "/personal/utang/owe",
      };
    });
}
