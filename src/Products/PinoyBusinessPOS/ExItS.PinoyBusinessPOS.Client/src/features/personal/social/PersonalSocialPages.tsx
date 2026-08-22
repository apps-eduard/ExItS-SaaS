import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acceptPersonalUtangInvitation,
  cancelPersonalReminder,
  createPersonalUtangInvitation,
  createRelationshipReminder,
  declinePersonalUtangInvitation,
  listPersonalNotifications,
  listPersonalUtangInvitations,
  listRelationshipReminders,
  markPersonalNotificationRead,
  resendPersonalUtangInvitation,
  revokePersonalUtangInvitation,
} from "@/api/platform/personal-social-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { PERSONAL_NOTIFICATIONS_QUERY_KEY } from "@/features/personal/personal-notifications";
import { useI18n } from "@/i18n/I18nProvider";

export function PersonalInvitationsPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["personal", "utang", "invitations"],
    queryFn: ({ signal }) => listPersonalUtangInvitations(signal),
  });

  const resend = useMutation({
    mutationFn: (id: string) => resendPersonalUtangInvitation(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["personal", "utang", "invitations"] });
    },
  });
  const revoke = useMutation({
    mutationFn: (id: string) => revokePersonalUtangInvitation(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["personal", "utang", "invitations"] });
    },
  });

  if (query.isPending) return <LoadingSkeleton />;
  if (query.isError) {
    return (
      <ErrorState
        title={t("personal.social.loadErrorTitle")}
        detail={t("personal.social.loadErrorDetail")}
      />
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-invitations-page">
      <PageHeader
        title={t("personal.social.invitationsTitle")}
        description={t("personal.social.invitationsLede")}
      />
      {query.data.length === 0 ? (
        <EmptyState
          title={t("personal.social.invitationsEmptyTitle")}
          detail={t("personal.social.invitationsEmptyDetail")}
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {query.data.map((invite) => (
            <li
              key={invite.id}
              className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3"
              data-testid={`utang-invite-${invite.id}`}
            >
              <p className="m-0 font-semibold">{invite.status}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {invite.inviteTargetEmailMasked || t("personal.social.inviteNoEmail")}
              </p>
              {invite.status === "Pending" ? (
                <div className="mt-2 flex flex-wrap gap-2">
                  <Button
                    type="button"
                    className="min-h-11"
                    disabled={resend.isPending}
                    onClick={() => resend.mutate(invite.id)}
                  >
                    {t("personal.social.resend")}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11"
                    disabled={revoke.isPending}
                    onClick={() => revoke.mutate(invite.id)}
                  >
                    {t("personal.social.revoke")}
                  </Button>
                </div>
              ) : null}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export function PersonalUtangInviteAcceptPage() {
  const { t } = useI18n();
  const [params] = useSearchParams();
  const token = params.get("token")?.trim() ?? "";
  const [message, setMessage] = useState<string | null>(null);

  const accept = useMutation({
    mutationFn: () => acceptPersonalUtangInvitation(token),
    onSuccess: () => setMessage(t("personal.social.acceptSuccess")),
    onError: (error) =>
      setMessage(
        error instanceof PlatformApiError ? error.message : t("personal.social.acceptFailed"),
      ),
  });
  const decline = useMutation({
    mutationFn: () => declinePersonalUtangInvitation(token),
    onSuccess: () => setMessage(t("personal.social.declineSuccess")),
    onError: (error) =>
      setMessage(
        error instanceof PlatformApiError ? error.message : t("personal.social.declineFailed"),
      ),
  });

  return (
    <div
      className="mx-auto flex min-h-[100dvh] max-w-lg flex-col gap-4 p-4"
      data-testid="utang-invite-accept"
    >
      <PageHeader
        title={t("personal.social.acceptTitle")}
        description={t("personal.social.acceptLede")}
      />
      {!token ? (
        <ErrorState
          title={t("personal.social.missingTokenTitle")}
          detail={t("personal.social.missingTokenDetail")}
        />
      ) : (
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            className="min-h-11"
            disabled={accept.isPending || decline.isPending}
            onClick={() => accept.mutate()}
            data-testid="utang-invite-accept-btn"
          >
            {t("personal.social.accept")}
          </Button>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={accept.isPending || decline.isPending}
            onClick={() => decline.mutate()}
            data-testid="utang-invite-decline-btn"
          >
            {t("personal.social.decline")}
          </Button>
        </div>
      )}
      {message ? <p role="status">{message}</p> : null}
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal">{t("personal.nav.home")}</Link>
      </Button>
    </div>
  );
}

export function PersonalNotificationsPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<"unread" | "all">("unread");
  const query = useQuery({
    queryKey: PERSONAL_NOTIFICATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalNotifications(signal),
  });
  const markRead = useMutation({
    mutationFn: (id: string) => markPersonalNotificationRead(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: PERSONAL_NOTIFICATIONS_QUERY_KEY });
    },
  });

  if (query.isPending) return <LoadingSkeleton />;
  if (query.isError) {
    return (
      <ErrorState
        title={t("personal.social.loadErrorTitle")}
        detail={t("personal.social.loadErrorDetail")}
      />
    );
  }

  const items = query.data;
  const visible = tab === "unread" ? items.filter((item) => !item.isRead) : items;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-notifications-page">
      <PageHeader
        title={t("personal.social.notificationsTitle")}
        description={t("personal.social.notificationsLede")}
      />
      <UnderlineTabBar
        items={[
          {
            key: "unread",
            label: t("personal.social.tabUnread"),
            testId: "notifications-tab-unread",
          },
          {
            key: "all",
            label: t("personal.social.tabAll"),
            testId: "notifications-tab-all",
          },
        ]}
        activeKey={tab}
        onChange={(key) => setTab(key as "unread" | "all")}
        ariaLabel={t("personal.social.notificationsTitle")}
      />

      {visible.length === 0 ? (
        <EmptyState
          title={
            tab === "unread"
              ? t("personal.social.unreadEmptyTitle")
              : t("personal.social.notificationsEmptyTitle")
          }
          detail={
            tab === "unread"
              ? t("personal.social.unreadEmptyDetail")
              : t("personal.social.notificationsEmptyDetail")
          }
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="notifications-list">
          {visible.map((item) => {
            const isCustomerLink =
              item.relatedType.localeCompare("CustomerLinkRequest", undefined, {
                sensitivity: "accent",
              }) === 0;
            return (
              <li
                key={item.id}
                data-testid={`notification-row-${item.id}`}
                data-read={item.isRead ? "true" : "false"}
                className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3"
              >
                <p className="m-0 font-semibold">{item.title}</p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{item.preview}</p>
                <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {new Date(item.createdAtUtc).toLocaleString()}
                  {item.isRead ? "" : ` · ${t("personal.social.unread")}`}
                </p>
                <div className="mt-2 flex flex-wrap gap-2">
                  {isCustomerLink ? (
                    <Button
                      asChild
                      className="min-h-11"
                      data-testid="notification-open-customer-links"
                    >
                      <Link to="/personal/customer-links">{t("personal.customerLinks.title")}</Link>
                    </Button>
                  ) : null}
                  {!item.isRead ? (
                    <Button
                      type="button"
                      variant="ghost"
                      className="min-h-11"
                      data-testid={`notification-mark-read-${item.id}`}
                      onClick={() => markRead.mutate(item.id)}
                    >
                      {t("personal.social.markRead")}
                    </Button>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

export { PersonalMyQrPage } from "@/features/personal/social/PersonalMyQrPage";

export function RelationshipInviteReminderPanel({
  relationshipId,
  inviteeContactId,
}: {
  relationshipId: string;
  inviteeContactId: string | null;
}) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);
  const [scheduledFor, setScheduledFor] = useState("");
  const remindersQuery = useQuery({
    queryKey: ["personal", "utang", "reminders", relationshipId],
    queryFn: ({ signal }) => listRelationshipReminders(relationshipId, signal),
  });

  const invite = useMutation({
    mutationFn: () => {
      if (!inviteeContactId) throw new Error("no contact");
      return createPersonalUtangInvitation(relationshipId, inviteeContactId);
    },
    onSuccess: async () => {
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ["personal", "utang", "invitations"] });
    },
    onError: (err) =>
      setError(err instanceof PlatformApiError ? err.message : t("personal.social.inviteFailed")),
  });

  const createReminder = useMutation({
    mutationFn: () =>
      createRelationshipReminder(relationshipId, {
        scheduleType: "OneTime",
        scheduledForUtc: new Date(scheduledFor).toISOString(),
        message: null,
      }),
    onSuccess: async () => {
      setScheduledFor("");
      setError(null);
      await queryClient.invalidateQueries({
        queryKey: ["personal", "utang", "reminders", relationshipId],
      });
    },
    onError: (err) =>
      setError(err instanceof PlatformApiError ? err.message : t("personal.social.reminderFailed")),
  });

  const cancel = useMutation({
    mutationFn: (id: string) => cancelPersonalReminder(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["personal", "utang", "reminders", relationshipId],
      });
    },
  });

  return (
    <section className="flex flex-col gap-3" data-testid="utang-invite-reminder-panel">
      <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
        {t("personal.social.inviteAndRemind")}
      </h2>
      {inviteeContactId ? (
        <Button
          type="button"
          className="min-h-11 w-fit"
          disabled={invite.isPending}
          onClick={() => invite.mutate()}
          data-testid="utang-invite-create"
        >
          {t("personal.social.inviteToExits")}
        </Button>
      ) : null}
      <form
        className="flex flex-col gap-2"
        onSubmit={(event) => {
          event.preventDefault();
          if (!scheduledFor) {
            setError(t("personal.social.reminderDateRequired"));
            return;
          }
          createReminder.mutate();
        }}
      >
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.social.reminderWhen")}
          <input
            type="datetime-local"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={scheduledFor}
            onChange={(e) => setScheduledFor(e.target.value)}
            data-testid="utang-reminder-when"
          />
        </label>
        <Button type="submit" className="min-h-11 w-fit" disabled={createReminder.isPending}>
          {t("personal.social.addReminder")}
        </Button>
      </form>
      {error ? (
        <p
          role="alert"
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
        >
          {error}
        </p>
      ) : null}
      {remindersQuery.data?.length ? (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {remindersQuery.data.map((reminder) => (
            <li
              key={reminder.id}
              className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-2"
            >
              <div className="min-w-0">
                <p className="m-0 text-[length:var(--exits-text-sm)]">{reminder.scheduleType}</p>
                <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {new Date(reminder.scheduledForUtc).toLocaleString()} · {reminder.status}
                </p>
              </div>
              {reminder.status !== "Cancelled" ? (
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  onClick={() => cancel.mutate(reminder.id)}
                >
                  {t("personal.social.cancelReminder")}
                </Button>
              ) : null}
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
