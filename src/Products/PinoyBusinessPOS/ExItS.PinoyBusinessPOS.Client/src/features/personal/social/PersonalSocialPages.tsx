import { useState } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Check, CheckCheck, ChevronRight, Send, Store, UserRoundCheck, X } from "lucide-react";
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
import { listPendingCustomerLinkRequests } from "@/api/platform/customer-link-requests-client";
import { listLinkedMerchants } from "@/api/platform/linked-merchants-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import {
  localizePersonalNotification,
  PERSONAL_NOTIFICATIONS_QUERY_KEY,
  resolveCustomerLinkNotificationState,
} from "@/features/personal/personal-notifications";
import {
  peekNotificationsReturnTo,
  resolveNotificationsReturnTo,
} from "@/features/personal/notifications-return";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { personalPageBackNav } from "@/navigation/page-back-nav";

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
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-invitations-page">
      <PageHeader
        title={t("personal.social.invitationsTitle")}
        description={t("personal.social.invitationsLede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-invitations"
      />
      {query.data.length === 0 ? (
        <EmptyState
          title={t("personal.social.invitationsEmptyTitle")}
          detail={t("personal.social.invitationsEmptyDetail")}
        />
      ) : (
        <ul className="exits-list m-0 grid list-none gap-2 p-0">
          {query.data.map((invite) => (
            <li key={invite.id}>
              <div
                className="exits-list__card"
                data-testid={`utang-invite-${invite.id}`}
              >
              <p className="exits-list__name m-0 font-semibold">{invite.status}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {invite.inviteTargetEmailMasked || t("personal.social.inviteNoEmail")}
              </p>
              {invite.status === "Pending" ? (
                <div className="invitation-card__actions mt-2 grid grid-cols-2 gap-2">
                  <Button
                    type="button"
                    className="min-h-11 w-full gap-2"
                    disabled={resend.isPending}
                    data-testid={`utang-invite-resend-${invite.id}`}
                    onClick={() => resend.mutate(invite.id)}
                  >
                    <Send className="size-4 shrink-0" aria-hidden />
                    {t("personal.social.resend")}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11 w-full gap-2"
                    disabled={revoke.isPending}
                    data-testid={`utang-invite-revoke-${invite.id}`}
                    onClick={() => revoke.mutate(invite.id)}
                  >
                    <Ban className="size-4 shrink-0" aria-hidden />
                    {t("personal.social.revoke")}
                  </Button>
                </div>
              ) : null}
              </div>
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
      className="personal-page exits-page mx-auto flex min-h-[100dvh] max-w-lg flex-col gap-4 p-4"
      data-testid="utang-invite-accept"
    >
      <PageHeader
        title={t("personal.social.acceptTitle")}
        description={t("personal.social.acceptLede")}
        backTo={personalPageBackNav.home.to}
        backLabel={t(personalPageBackNav.home.labelKey)}
        backTestId="page-header-back-utang-invite-accept"
      />
      {!token ? (
        <ErrorState
          title={t("personal.social.missingTokenTitle")}
          detail={t("personal.social.missingTokenDetail")}
        />
      ) : (
        <section className="catalog-form-section exits-animate-panel personal-section gap-3">
          <h2 className="catalog-form-section__title">{t("personal.social.acceptTitle")}</h2>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              className="min-h-11"
              disabled={accept.isPending || decline.isPending}
              onClick={() => accept.mutate()}
              data-testid="utang-invite-accept-btn"
            >
              <Check className="size-4 shrink-0" aria-hidden />
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
              <X className="size-4 shrink-0" aria-hidden />
              {t("personal.social.decline")}
            </Button>
          </div>
        </section>
      )}
      {message ? <p role="status">{message}</p> : null}
    </div>
  );
}

export function PersonalNotificationsPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<"unread" | "all">("unread");
  const [leaving, setLeaving] = useState(false);
  const query = useQuery({
    queryKey: PERSONAL_NOTIFICATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listPersonalNotifications(signal),
  });
  const pendingLinksQuery = useQuery({
    queryKey: ["personal", "customer-link-requests"],
    queryFn: ({ signal }) => listPendingCustomerLinkRequests(signal),
  });
  const linkedMerchantsQuery = useQuery({
    queryKey: ["personal", "linked-merchants"],
    queryFn: ({ signal }) => listLinkedMerchants(1, 50, signal),
  });
  const markRead = useMutation({
    mutationFn: (id: string) => markPersonalNotificationRead(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: PERSONAL_NOTIFICATIONS_QUERY_KEY });
    },
  });

  function leaveNotifications() {
    if (leaving) {
      return;
    }
    setLeaving(true);
    try {
      const returnContext = resolveNotificationsReturnTo(location.state);
      const returnTo = returnContext?.returnTo;
      if (returnTo?.startsWith("/personal")) {
        navigate(returnTo, { replace: true });
        return;
      }
      navigate("/personal", { replace: true });
    } finally {
      setLeaving(false);
    }
  }

  const returnContext = peekNotificationsReturnTo(location.state);
  const backTo =
    returnContext?.returnTo?.startsWith("/personal") === true
      ? returnContext.returnTo
      : "/personal";

  const closeButton = (
    <button
      type="button"
      className="page-header__info notifications-page__close"
      data-testid="notifications-close"
      aria-label={t("personal.social.notificationsClose")}
      disabled={leaving}
      onClick={leaveNotifications}
    >
      <X className="size-4 shrink-0" aria-hidden />
    </button>
  );

  if (query.isPending) {
    return <LoadingState label={t("loading.label")} />;
  }
  if (query.isError) {
    return (
      <div className="personal-page notifications-page exits-page flex min-w-0 flex-col gap-3">
        <PageHeader
          title={t("personal.social.notificationsTitle")}
          description={t("personal.social.notificationsLede")}
          backTo={backTo}
          backLabel={t("personal.nav.home")}
          backTestId="page-header-back-notifications"
          trailing={closeButton}
        />
        <ErrorState
          title={t("personal.social.loadErrorTitle")}
          detail={t("personal.social.loadErrorDetail")}
        />
      </div>
    );
  }

  const items = query.data;
  const visible = tab === "unread" ? items.filter((item) => !item.isRead) : items;
  const pendingLinks = pendingLinksQuery.data ?? [];
  const linkedMerchants = linkedMerchantsQuery.data?.items ?? [];

  return (
    <div
      className="personal-page notifications-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-notifications-page"
    >
      <PageHeader
        title={t("personal.social.notificationsTitle")}
        description={t("personal.social.notificationsLede")}
        backTo={backTo}
        backLabel={t("personal.nav.home")}
        backTestId="page-header-back-notifications"
        trailing={closeButton}
      />

      <div className="exits-animate-toolbar">
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
      </div>

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
        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="notifications-list">
          {visible.map((item) => {
            const localized = localizePersonalNotification(item, t);
            const isCustomerLink =
              item.relatedType.localeCompare("CustomerLinkRequest", undefined, {
                sensitivity: "accent",
              }) === 0;
            const isTodo =
              item.relatedType.localeCompare("PersonalTodo", undefined, {
                sensitivity: "accent",
              }) === 0
              && Boolean(item.relatedId);
            const isUtang =
              item.relatedType.localeCompare("PersonalDebtRelationship", undefined, {
                sensitivity: "accent",
              }) === 0
              && Boolean(item.relatedId);
            const customerLinkState = isCustomerLink
              ? resolveCustomerLinkNotificationState(
                  item.relatedId,
                  item.preview,
                  pendingLinks,
                  linkedMerchants,
                )
              : null;
            return (
              <li key={item.id}>
                <article
                  data-testid={`notification-row-${item.id}`}
                  data-read={item.isRead ? "true" : "false"}
                  data-customer-link-state={customerLinkState ?? undefined}
                  className={cn(
                    "exits-list__card notification-row",
                    !item.isRead && "notification-row--unread",
                  )}
                >
                  <div className="notification-row__main min-w-0">
                    <div className="notification-row__title-row">
                      <strong className="exits-list__name block min-w-0 truncate font-semibold">
                        {localized.title}
                      </strong>
                      {!item.isRead ? (
                        <StatusChip tone="warning">{t("personal.social.unread")}</StatusChip>
                      ) : null}
                      {customerLinkState === "pending" ? (
                        <StatusChip tone="warning">
                          {t("personal.customerLinks.statusPending")}
                        </StatusChip>
                      ) : null}
                      {customerLinkState === "accepted" ? (
                        <StatusChip tone="success">
                          {t("personal.customerLinks.statusAccepted")}
                        </StatusChip>
                      ) : null}
                      {customerLinkState === "declined" ? (
                        <StatusChip tone="danger">
                          {t("personal.customerLinks.statusDeclined")}
                        </StatusChip>
                      ) : null}
                    </div>
                    <p className="notification-row__preview mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {localized.preview}
                    </p>
                    <p className="notification-row__meta mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                      {new Date(item.createdAtUtc).toLocaleString()}
                    </p>
                  </div>
                  <div className="notification-row__actions">
                    {isCustomerLink && customerLinkState === "pending" ? (
                      <div className="flex w-full min-w-0 flex-col gap-1.5">
                        <Button
                          asChild
                          className="min-h-11 w-full"
                          data-testid="notification-open-customer-links"
                        >
                          <Link
                            to="/personal/customer-links"
                            onClick={() => {
                              if (!item.isRead) {
                                markRead.mutate(item.id);
                              }
                            }}
                          >
                            <UserRoundCheck className="size-4 shrink-0" aria-hidden />
                            {t("personal.social.openCustomerLink")}
                          </Link>
                        </Button>
                        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                          {t("personal.social.openCustomerLinkHint")}
                        </p>
                      </div>
                    ) : null}
                    {isCustomerLink && customerLinkState === "accepted" ? (
                      <div className="flex w-full min-w-0 flex-col gap-1.5">
                        <Button asChild className="min-h-11 w-full" data-testid="notification-open-linked-stores">
                          <Link
                            to="/personal/linked-merchants"
                            onClick={() => {
                              if (!item.isRead) {
                                markRead.mutate(item.id);
                              }
                            }}
                          >
                            <Store className="size-4 shrink-0" aria-hidden />
                            {t("personal.social.openLinkedStores")}
                          </Link>
                        </Button>
                        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                          {t("personal.social.customerLinkAcceptedHint")}
                        </p>
                      </div>
                    ) : null}
                    {isCustomerLink && customerLinkState === "declined" ? (
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("personal.social.customerLinkDeclinedHint")}
                      </p>
                    ) : null}
                    {isCustomerLink && customerLinkState === "unknown" ? (
                      <div className="flex w-full min-w-0 flex-col gap-1.5">
                        <Button
                          asChild
                          className="min-h-11 w-full"
                          data-testid="notification-open-customer-links"
                        >
                          <Link
                            to="/personal/customer-links"
                            onClick={() => {
                              if (!item.isRead) {
                                markRead.mutate(item.id);
                              }
                            }}
                          >
                            <UserRoundCheck className="size-4 shrink-0" aria-hidden />
                            {t("personal.social.openCustomerLink")}
                          </Link>
                        </Button>
                      </div>
                    ) : null}
                    {isTodo ? (
                      <Button
                        asChild
                        className="min-h-11"
                        data-testid="notification-open-todo"
                      >
                        <Link
                          to={`/personal/todo/${item.relatedId}`}
                          onClick={() => {
                            if (!item.isRead) {
                              markRead.mutate(item.id);
                            }
                          }}
                        >
                          <ChevronRight className="size-4 shrink-0" aria-hidden />
                          {t("personal.social.openTodo")}
                        </Link>
                      </Button>
                    ) : null}
                    {isUtang ? (
                      <Button
                        asChild
                        className="min-h-11"
                        data-testid="notification-open-utang"
                      >
                        <Link
                          to={`/personal/utang/relationships/${item.relatedId}`}
                          onClick={() => {
                            if (!item.isRead) {
                              markRead.mutate(item.id);
                            }
                          }}
                        >
                          <ChevronRight className="size-4 shrink-0" aria-hidden />
                          {t("personal.social.openUtang")}
                        </Link>
                      </Button>
                    ) : null}
                    {!item.isRead ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="min-h-11"
                        data-testid={`notification-mark-read-${item.id}`}
                        disabled={markRead.isPending}
                        onClick={() => markRead.mutate(item.id)}
                      >
                        <CheckCheck className="size-4 shrink-0" aria-hidden />
                        {t("personal.social.markRead")}
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

  function toLocalInputValue(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  function setPreset(kind: "hour" | "tomorrow") {
    const next = new Date();
    if (kind === "hour") {
      next.setHours(next.getHours() + 1);
    } else {
      next.setDate(next.getDate() + 1);
      next.setHours(9, 0, 0, 0);
    }
    setScheduledFor(toLocalInputValue(next));
    setError(null);
  }

  return (
    <section className="catalog-form-section exits-animate-panel personal-section flex flex-col gap-3" data-testid="utang-invite-reminder-panel">
      <h2 className="catalog-form-section__title">
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
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            data-testid="utang-reminder-preset-hour"
            onClick={() => setPreset("hour")}
          >
            {t("personal.social.reminderInOneHour")}
          </Button>
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            data-testid="utang-reminder-preset-tomorrow"
            onClick={() => setPreset("tomorrow")}
          >
            {t("personal.social.reminderTomorrow")}
          </Button>
        </div>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("personal.social.reminderServerHint")}
        </p>
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
        <ul className="exits-list m-0 grid list-none gap-2 p-0">
          {remindersQuery.data.map((reminder) => (
            <li key={reminder.id}>
              <div className="exits-list__card flex items-center justify-between gap-2">
                <div className="min-w-0">
                  <p className="exits-list__name m-0 text-[length:var(--exits-text-sm)]">
                    {reminder.scheduleType}
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {new Date(reminder.scheduledForUtc).toLocaleString()} · {reminder.status}
                  </p>
                </div>
                {reminder.status === "Scheduled" ? (
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11"
                    onClick={() => cancel.mutate(reminder.id)}
                  >
                    {t("personal.social.cancelReminder")}
                  </Button>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
