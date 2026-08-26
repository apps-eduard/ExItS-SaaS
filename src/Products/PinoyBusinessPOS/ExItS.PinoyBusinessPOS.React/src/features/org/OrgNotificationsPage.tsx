import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { X } from "lucide-react";
import {
  listOrganizationNotifications,
  markOrganizationNotificationRead,
} from "@/api/platform/organization-notifications-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import {
  organizationNotificationsQueryKey,
  resolveOrganizationNotificationHref,
} from "@/features/org/org-notifications";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type OrgNotificationsLocationState = {
  returnTo?: string;
};

function isSafeOrgReturnPath(path: string | null | undefined): path is string {
  if (!path || !path.startsWith("/") || path.startsWith("//")) {
    return false;
  }
  if (path === "/org/notifications" || path.startsWith("/org/notifications?")) {
    return false;
  }
  if (path.startsWith("/personal")) {
    return false;
  }
  return true;
}

export function OrgNotificationsPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const [tab, setTab] = useState<"unread" | "all">("unread");

  const returnToFromState =
    location.state &&
    typeof location.state === "object" &&
    "returnTo" in location.state &&
    typeof (location.state as OrgNotificationsLocationState).returnTo === "string"
      ? (location.state as OrgNotificationsLocationState).returnTo
      : null;
  const closeTo = isSafeOrgReturnPath(returnToFromState)
    ? returnToFromState
    : pageBackNav.org.to;

  const query = useQuery({
    queryKey: organizationId
      ? organizationNotificationsQueryKey(organizationId)
      : ["organization", "notifications", "none"],
    enabled: organizationId !== null,
    queryFn: ({ signal }) => listOrganizationNotifications(organizationId!, signal),
  });

  const markRead = useMutation({
    mutationFn: (id: string) => markOrganizationNotificationRead(organizationId!, id),
    onSuccess: async () => {
      if (!organizationId) return;
      await queryClient.invalidateQueries({
        queryKey: organizationNotificationsQueryKey(organizationId),
      });
    },
  });

  function leaveNotifications() {
    navigate(closeTo, { replace: true });
  }

  async function openNotification(id: string, isRead: boolean, href: string | null) {
    if (!organizationId) return;
    if (!isRead) {
      try {
        await markRead.mutateAsync(id);
      } catch {
        return;
      }
    }
    if (href) {
      navigate(href);
    }
  }

  const closeButton = (
    <button
      type="button"
      className="page-header__info notifications-page__close"
      data-testid="org-notifications-close"
      aria-label={t("org.notifications.close")}
      onClick={leaveNotifications}
    >
      <X className="size-4 shrink-0" aria-hidden />
    </button>
  );

  if (!organizationId) {
    return (
      <div className="notifications-page exits-page flex min-w-0 flex-col gap-3">
        <PageHeader
          title={t("org.notifications.title")}
          description={t("org.notifications.lede")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          trailing={closeButton}
        />
        <ErrorState
          title={t("org.notifications.loadErrorTitle")}
          detail={t("org.businessQr.noOrg")}
        />
      </div>
    );
  }

  if (query.isPending) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (query.isError) {
    return (
      <div className="notifications-page exits-page flex min-w-0 flex-col gap-3">
        <PageHeader
          title={t("org.notifications.title")}
          description={t("org.notifications.lede")}
          backTo={closeTo}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org-notifications"
          trailing={closeButton}
        />
        <ErrorState
          title={t("org.notifications.loadErrorTitle")}
          detail={t("org.notifications.loadErrorDetail")}
        />
      </div>
    );
  }

  const items = query.data;
  const visible = tab === "unread" ? items.filter((item) => !item.isRead) : items;

  return (
    <div
      className="notifications-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="org-notifications-page"
    >
      <PageHeader
        title={t("org.notifications.title")}
        description={t("org.notifications.lede")}
        backTo={closeTo}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org-notifications"
        trailing={closeButton}
      />

      <UnderlineTabBar
        items={[
          {
            key: "unread",
            label: t("org.notifications.tabUnread"),
            testId: "org-notifications-tab-unread",
          },
          {
            key: "all",
            label: t("org.notifications.tabAll"),
            testId: "org-notifications-tab-all",
          },
        ]}
        activeKey={tab}
        onChange={(key) => setTab(key as "unread" | "all")}
        ariaLabel={t("org.notifications.title")}
      />

      {visible.length === 0 ? (
        <EmptyState
          title={
            tab === "unread"
              ? t("org.notifications.unreadEmptyTitle")
              : t("org.notifications.emptyTitle")
          }
          detail={
            tab === "unread"
              ? t("org.notifications.unreadEmptyDetail")
              : t("org.notifications.emptyDetail")
          }
        />
      ) : (
        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="org-notifications-list">
          {visible.map((item) => {
            const href = resolveOrganizationNotificationHref(item);
            return (
              <li key={item.id}>
                <article
                  data-testid={`org-notification-row-${item.id}`}
                  data-read={item.isRead ? "true" : "false"}
                  className={cn(
                    "exits-list__card notification-row",
                    !item.isRead && "notification-row--unread",
                  )}
                >
                  <div className="notification-row__main min-w-0">
                    <div className="notification-row__title-row">
                      <strong className="exits-list__name block min-w-0 truncate font-semibold">
                        {item.title}
                      </strong>
                      {!item.isRead ? (
                        <StatusChip tone="warning">{t("org.notifications.unread")}</StatusChip>
                      ) : null}
                    </div>
                    <p className="notification-row__preview mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {item.preview}
                    </p>
                    <p className="notification-row__meta mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                      {new Date(item.createdAtUtc).toLocaleString()}
                    </p>
                  </div>
                  <div className="notification-row__actions">
                    {href ? (
                      <Button
                        type="button"
                        className="min-h-11"
                        data-testid={`org-notification-open-${item.id}`}
                        disabled={markRead.isPending}
                        onClick={() => void openNotification(item.id, item.isRead, href)}
                      >
                        {t("org.notifications.open")}
                      </Button>
                    ) : null}
                    {!item.isRead ? (
                      <Button
                        type="button"
                        variant="outline"
                        className="min-h-11"
                        data-testid={`org-notification-mark-read-${item.id}`}
                        disabled={markRead.isPending}
                        onClick={() => markRead.mutate(item.id)}
                      >
                        {t("org.notifications.markRead")}
                      </Button>
                    ) : null}
                  </div>
                </article>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
