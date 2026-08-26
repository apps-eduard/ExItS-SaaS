import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import type { PersonalInAppNotificationDto } from "@/api/platform/personal-types";
import { listArchivedPersonalNotifications } from "@/api/platform/personal-people-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { StatusChip } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingState } from "@/components/ui/skeleton";
import {
  formatNotificationMonthHeading,
  groupNotificationsByMonth,
  resolveArchivedNotificationStatusLabel,
} from "@/features/personal/notification-archive";
import {
  useArchivedPersonalNotificationsQuery,
  useMarkNotificationReadMutation,
  usePersonalConnectionRequestsQuery,
} from "@/features/personal/people-queries";
import {
  localizePersonalNotification,
  resolveNotificationDeepLink,
} from "@/features/personal/personal-notifications";
import { formatShortDate } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { usePreferences } from "@/hooks/usePreferences";

const ARCHIVE_PAGE_SIZE = 30;

export function ArchivedNotificationsPage() {
  const { t } = useI18n();
  const { preferences } = usePreferences();
  const navigate = useNavigate();
  const [tab, setTab] = useState<"all" | "unread">("all");
  const unreadOnly = tab === "unread";
  const archiveQuery = useArchivedPersonalNotificationsQuery(unreadOnly);
  const connectionsQuery = usePersonalConnectionRequestsQuery();
  const markRead = useMarkNotificationReadMutation();
  const [extraItems, setExtraItems] = useState<PersonalInAppNotificationDto[]>([]);
  const [nextPage, setNextPage] = useState(2);
  const [loadingMore, setLoadingMore] = useState(false);
  const [loadMoreError, setLoadMoreError] = useState<string | null>(null);
  const [reachedEnd, setReachedEnd] = useState(false);

  const connectionStatusById = useMemo(() => {
    const map = new Map<string, string>();
    for (const row of connectionsQuery.data ?? []) {
      map.set(row.id, row.status);
    }
    return map;
  }, [connectionsQuery.data]);

  if (archiveQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (archiveQuery.error && extraItems.length === 0) {
    const err = archiveQuery.error;
    return (
      <ErrorState
        title={t("error.title")}
        detail={err instanceof PlatformApiError ? err.message : t("error.body")}
        error={err}
      />
    );
  }

  const page1Items = archiveQuery.data?.items ?? [];
  const totalCount = archiveQuery.data?.totalCount ?? 0;
  const items = dedupeById([...page1Items, ...extraItems]);
  const groups = groupNotificationsByMonth(items);
  const hasMore = !reachedEnd && items.length < totalCount;

  async function loadMore() {
    if (loadingMore || !hasMore) {
      return;
    }
    setLoadingMore(true);
    setLoadMoreError(null);
    try {
      const page = await listArchivedPersonalNotifications(nextPage, ARCHIVE_PAGE_SIZE, {
        unreadOnly,
      });
      setExtraItems((prev) => dedupeById([...prev, ...page.items]));
      setNextPage((p) => p + 1);
      if (page.items.length === 0 || page1Items.length + extraItems.length + page.items.length >= page.totalCount) {
        setReachedEnd(true);
      }
    } catch (err) {
      setLoadMoreError(err instanceof PlatformApiError ? err.message : t("notifications.archiveLoadMoreError"));
    } finally {
      setLoadingMore(false);
    }
  }

  function onTabChange(key: string) {
    setTab(key as "all" | "unread");
    setExtraItems([]);
    setNextPage(2);
    setReachedEnd(false);
    setLoadMoreError(null);
  }

  return (
    <section
      className="mx-auto flex w-full max-w-lg flex-col gap-4"
      data-testid="personal-notifications-archived-page"
    >
      <PageHeader
        title={t("notifications.archiveTitle")}
        subtitle={t("notifications.archiveLede")}
        backTo="/personal/notifications"
        backLabel={t("notifications.title")}
      />

      <UnderlineTabBar
        items={[
          { key: "all", label: t("notifications.tabAll"), testId: "archived-notifications-tab-all" },
          {
            key: "unread",
            label: t("notifications.tabUnread"),
            testId: "archived-notifications-tab-unread",
          },
        ]}
        activeKey={tab}
        onChange={onTabChange}
        ariaLabel={t("notifications.archiveTitle")}
      />

      {items.length === 0 ? (
        <EmptyState
          title={t("notifications.archiveEmptyTitle")}
          detail={t("notifications.archiveEmptyBody")}
        />
      ) : (
        <div className="flex flex-col gap-4">
          {groups.map((group) => (
            <section key={group.key} className="flex flex-col gap-2">
              <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
                {formatNotificationMonthHeading(group, preferences.locale)}
              </h2>
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {group.items.map((item) => {
                  const localized = localizePersonalNotification(item, t);
                  const statusKey = resolveArchivedNotificationStatusLabel(
                    item,
                    item.relatedId ? connectionStatusById.get(item.relatedId) : null,
                  );
                  const destination = resolveNotificationDeepLink(item.relatedType, item.relatedId);
                  const canNavigate = destination !== "/personal/notifications";
                  return (
                    <li key={item.id}>
                      <Card
                        className="flex flex-col gap-2"
                        data-testid={`archived-notification-row-${item.id}`}
                      >
                        <button
                          type="button"
                          className="flex min-h-[var(--exits-touch-target-min)] w-full flex-col items-start gap-1 bg-transparent p-0 text-left text-inherit focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          disabled={!canNavigate}
                          onClick={() => {
                            if (!item.isRead) {
                              markRead.mutate(item.id);
                            }
                            if (canNavigate) {
                              navigate(destination);
                            }
                          }}
                        >
                          <span className="break-words font-semibold">{localized.preview}</span>
                          <span className="inline-flex flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)] text-muted">
                            {formatShortDate(item.createdAtUtc)}
                            <StatusChip
                              tone={
                                statusKey === "connected"
                                  ? "success"
                                  : statusKey === "declined" || statusKey === "revoked"
                                    ? "warning"
                                    : statusKey === "unread"
                                      ? "info"
                                      : "neutral"
                              }
                            >
                              {statusKey === "connected"
                                ? t("notifications.archiveStatus.connected")
                                : statusKey === "declined"
                                  ? t("notifications.archiveStatus.declined")
                                  : statusKey === "revoked"
                                    ? t("notifications.archiveStatus.revoked")
                                    : statusKey === "expired"
                                      ? t("notifications.archiveStatus.expired")
                                      : statusKey === "resolved"
                                        ? t("notifications.archiveStatus.resolved")
                                        : statusKey === "unread"
                                          ? t("notifications.archiveStatus.unread")
                                          : t("notifications.archiveStatus.read")}
                            </StatusChip>
                          </span>
                        </button>
                      </Card>
                    </li>
                  );
                })}
              </ul>
            </section>
          ))}
        </div>
      )}

      {hasMore ? (
        <div className="flex flex-col gap-2">
          {loadMoreError ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" role="alert">
              {loadMoreError}
            </p>
          ) : null}
          <Button
            type="button"
            variant="outline"
            className="min-h-[var(--exits-touch-target-min)]"
            disabled={loadingMore}
            data-testid="archived-notifications-load-more"
            onClick={() => void loadMore()}
          >
            {loadingMore ? t("loading.label") : t("notifications.loadMore")}
          </Button>
        </div>
      ) : null}
    </section>
  );
}

function dedupeById(items: PersonalInAppNotificationDto[]): PersonalInAppNotificationDto[] {
  const seen = new Set<string>();
  const result: PersonalInAppNotificationDto[] = [];
  for (const item of items) {
    if (seen.has(item.id)) {
      continue;
    }
    seen.add(item.id);
    result.push(item);
  }
  return result;
}
