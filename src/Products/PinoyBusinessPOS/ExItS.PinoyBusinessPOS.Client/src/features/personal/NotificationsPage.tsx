import { useNavigate } from "react-router-dom";
import { ApiClientError } from "@/api/http";
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
import { formatShortDate } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { usePreferences } from "@/hooks/usePreferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { cn } from "@/lib/cn";

function invitationDeepLink(relatedType: string, relatedId?: string | null): string {
  const type = relatedType.toLowerCase();
  if (type.includes("invitation") || type.includes("utang")) {
    return relatedId
      ? `/personal/invitations?relatedId=${encodeURIComponent(relatedId)}`
      : "/personal/invitations";
  }
  return "/personal/invitations";
}

export function NotificationsPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { preferences } = usePreferences();
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
        body={err instanceof ApiClientError ? err.message : t("error.body")}
        record={normalizeDiagnosticError(err, {
          locale: preferences.locale,
          theme: preferences.theme,
          pathname: "/personal/notifications",
        })}
      />
    );
  }

  const items = notificationsQuery.data ?? [];

  return (
    <section className="flex flex-col gap-4">
      <PageHeader title={t("notifications.title")} />
      {items.length === 0 ? (
        <EmptyState title={t("notifications.emptyTitle")} body={t("notifications.emptyBody")} />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {items.map((item) => (
            <li key={item.id}>
              <Card
                className={cn(
                  "flex flex-col gap-2",
                  !item.isRead && "border-[var(--exits-info)]",
                )}
              >
                <button
                  type="button"
                  className="flex flex-col items-start gap-1 bg-transparent p-0 text-left text-inherit"
                  onClick={() => {
                    void markRead.mutateAsync(item.id).finally(() => {
                      void navigate(invitationDeepLink(item.relatedType, item.relatedId));
                    });
                  }}
                >
                  <span className="font-semibold">{item.title}</span>
                  <span className="text-muted">{item.preview}</span>
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {formatShortDate(item.createdAtUtc)}
                    {!item.isRead ? ` · ${t("notifications.unread")}` : ""}
                  </span>
                </button>
                {!item.isRead ? (
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => void markRead.mutateAsync(item.id)}
                  >
                    {t("notifications.markRead")}
                  </Button>
                ) : null}
              </Card>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
