import { useNavigate } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingState } from "@/components/ui/skeleton";
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

  return (
    <section className="mx-auto flex w-full max-w-lg flex-col gap-4">
      <PageHeader title={t("notifications.title")} subtitle={t("notifications.lede")} />
      {items.length === 0 ? (
        <EmptyState title={t("notifications.emptyTitle")} detail={t("notifications.emptyBody")} />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {items.map((item) => {
            const localized = localizePersonalNotification(item, t);
            const unread = !item.isRead;
            const destination = resolveNotificationDeepLink(item.relatedType);
            const hasActionDestination = destination !== "/personal/notifications";
            return (
              <li key={item.id}>
                <Card
                  className={cn(
                    "flex flex-col gap-2",
                    unread && "border-[var(--exits-info)] bg-[color-mix(in_srgb,var(--exits-info)_6%,transparent)]",
                  )}
                >
                  <button
                    type="button"
                    className="flex min-h-[var(--exits-touch-target-min)] w-full flex-col items-start gap-1 bg-transparent p-0 text-left text-inherit focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
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
                    <span className={cn("font-semibold", unread && "font-bold")}>{localized.title}</span>
                    <span className="text-muted">{localized.preview}</span>
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
      )}
    </section>
  );
}
