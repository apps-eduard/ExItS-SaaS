import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingState } from "@/components/ui/skeleton";
import { groupNotificationsByDay } from "@/features/personal/notification-archive";
import {
  useMarkNotificationReadMutation,
  usePersonalNotificationsQuery,
} from "@/features/personal/people-queries";
import {
  localizePersonalNotification,
  resolveNotificationDeepLink,
} from "@/features/personal/personal-notifications";
import { formatShortDate } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export function NotificationsPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [tab, setTab] = useState<"all" | "unread">("all");
  const notificationsQuery = usePersonalNotificationsQuery();
  const markRead = useMarkNotificationReadMutation();

  if (notificationsQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (notificationsQuery.error) {
    const err = notificationsQuery.error;
    return (
      <ErrorState
        title={t("error.title")}
        detail={err instanceof PlatformApiError ? err.message : t("error.body")}
        error={err}
      />
    );
  }

  const items = notificationsQuery.data ?? [];
  const visible = tab === "unread" ? items.filter((item) => !item.isRead) : items;
  const groups = groupNotificationsByDay(visible);

  return (
    <section className="mx-auto flex w-full max-w-lg flex-col gap-4" data-testid="personal-notifications-page">
      <PageHeader title={t("notifications.title")} subtitle={t("notifications.lede")} />

      <UnderlineTabBar
        items={[
          { key: "all", label: t("notifications.tabAll"), testId: "notifications-tab-all" },
          { key: "unread", label: t("notifications.tabUnread"), testId: "notifications-tab-unread" },
        ]}
        activeKey={tab}
        onChange={(key) => setTab(key as "all" | "unread")}
        ariaLabel={t("notifications.title")}
      />

      {visible.length === 0 ? (
        <EmptyState
          title={
            tab === "unread" ? t("notifications.unreadEmptyTitle") : t("notifications.emptyTitle")
          }
          detail={
            tab === "unread" ? t("notifications.unreadEmptyBody") : t("notifications.emptyBody")
          }
        />
      ) : (
        <div className="flex flex-col gap-4">
          {groups.map((group) => (
            <section
              key={group.key}
              className="flex flex-col gap-2"
              aria-label={
                group.key === "today"
                  ? t("notifications.group.today")
                  : group.key === "yesterday"
                    ? t("notifications.group.yesterday")
                    : t("notifications.group.earlier")
              }
            >
              <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
                {group.key === "today"
                  ? t("notifications.group.today")
                  : group.key === "yesterday"
                    ? t("notifications.group.yesterday")
                    : t("notifications.group.earlier")}
              </h2>
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {group.items.map((item) => {
                  const localized = localizePersonalNotification(item, t);
                  const unread = !item.isRead;
                  const destination = resolveNotificationDeepLink(item.relatedType, item.relatedId);
                  const hasActionDestination = destination !== "/personal/notifications";
                  return (
                    <li key={item.id}>
                      <Card
                        className={cn(
                          "flex flex-col gap-2",
                          unread &&
                            "border-[var(--exits-info)] bg-[color-mix(in_srgb,var(--exits-info)_6%,transparent)]",
                        )}
                        data-testid={`notification-row-${item.id}`}
                      >
                        <button
                          type="button"
                          className="flex min-h-[var(--exits-row-min-height)] w-full flex-col items-start gap-1 bg-transparent p-0 text-left text-inherit focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          aria-label={
                            unread
                              ? `${localized.title}. ${t("notifications.unread")}. ${localized.preview}`
                              : `${localized.title}. ${localized.preview}`
                          }
                          onClick={() => {
                            if (!item.isRead) {
                              markRead.mutate(item.id);
                            }
                            navigate(destination);
                          }}
                        >
                          <span className={cn("font-semibold", unread && "font-bold")}>
                            {localized.title}
                          </span>
                          <span className="break-words text-muted">{localized.preview}</span>
                          <span className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)] text-muted">
                            {formatShortDate(item.createdAtUtc)}
                            {unread ? (
                              <span className="inline-flex items-center gap-1 font-semibold text-[var(--exits-info)]">
                                <span
                                  className="size-1.5 rounded-full bg-[var(--exits-info)]"
                                  aria-hidden="true"
                                />
                                {t("notifications.unread")}
                              </span>
                            ) : null}
                          </span>
                        </button>
                        {unread && !hasActionDestination ? (
                          <Button
                            type="button"
                            variant="ghost"
                            disabled={markRead.isPending}
                            onClick={() => void markRead.mutateAsync(item.id)}
                          >
                            {t("notifications.markRead")}
                          </Button>
                        ) : null}
                      </Card>
                    </li>
                  );
                })}
              </ul>
            </section>
          ))}
        </div>
      )}

      <div className="border-t border-border pt-3">
        <Link
          to="/personal/notifications/archived"
          className="inline-flex min-h-[var(--exits-touch-target-min)] items-center text-[length:var(--exits-text-sm)] font-semibold text-[var(--exits-primary)] underline-offset-2 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          data-testid="notifications-view-archived"
        >
          {t("notifications.viewArchived")}
        </Link>
      </div>
    </section>
  );
}
