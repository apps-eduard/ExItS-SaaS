import { useMemo, useState } from "react";
import {
  ArrowLeft,
  CalendarClock,
  Check,
  ChevronRight,
  HandCoins,
  Hourglass,
  Loader2,
  UserRound,
  X,
} from "lucide-react";
import { Link } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import type { PersonalConnectionRequestDto } from "@/api/platform/personal-types";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { StatusChip } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingState } from "@/components/ui/skeleton";
import {
  cascadeResolveSiblingRequests,
  groupByKey,
  siblingRequestIds,
  sortByNewestUtc,
} from "@/features/personal/consent-request-groups";
import {
  useAcceptConnectionMutation,
  useDeclineConnectionMutation,
  usePersonalConnectionRequestsQuery,
  usePersonalContactsQuery,
  useRevokeConnectionMutation,
} from "@/features/personal/people-queries";
import { formatShortDate, isPendingConnectionRequest } from "@/features/personal/people-status";
import { useConsentActionGuard } from "@/features/personal/useConsentActionGuard";
import { useI18n } from "@/i18n/I18nProvider";

type RequestGroup = {
  key: string;
  primary: PersonalConnectionRequestDto;
  items: PersonalConnectionRequestDto[];
  duplicateCount: number;
};

function ConnectionRequestCard({
  request,
  group,
  variant,
  displayName,
  publicUserId,
  personHref,
  actionsDisabled,
  isAccepting,
  isDeclining,
  isRevoking,
  onAccept,
  onDecline,
  onRevoke,
}: {
  request: PersonalConnectionRequestDto;
  group: RequestGroup;
  variant: "received" | "sent";
  displayName: string;
  publicUserId?: string | null;
  personHref?: string | null;
  actionsDisabled: boolean;
  isAccepting: boolean;
  isDeclining: boolean;
  isRevoking: boolean;
  onAccept?: () => void;
  onDecline?: () => void;
  onRevoke?: () => void;
}) {
  const { t } = useI18n();

  return (
    <article
      className="exits-list__card customer-link-card"
      data-testid={`connection-request-${request.id}`}
      data-busy={isAccepting || isDeclining || isRevoking ? "true" : "false"}
    >
      <div className="customer-link-card__header">
        <PersonAvatar name={displayName} className="size-10 shrink-0" />
        <div className="customer-link-card__heading min-w-0 flex-1">
          <div className="customer-link-card__title-row">
            {personHref ? (
              <Link
                to={personHref}
                className="exits-list__name m-0 min-w-0 flex-1 truncate font-semibold text-inherit no-underline hover:text-primary"
              >
                {displayName}
              </Link>
            ) : (
              <p className="exits-list__name m-0 min-w-0 flex-1 truncate font-semibold">
                {displayName}
              </p>
            )}
            <div className="flex flex-wrap items-center gap-1">
              <StatusChip tone={variant === "received" ? "warning" : "info"}>
                {variant === "received"
                  ? t("people.status.requestReceived")
                  : t("people.status.requestSent")}
              </StatusChip>
              {group.duplicateCount > 1 ? (
                <StatusChip tone="neutral">
                  {(variant === "received"
                    ? t("invitations.duplicateCount")
                    : t("invitations.duplicateCountSent")
                  ).replace("{count}", String(group.duplicateCount))}
                </StatusChip>
              ) : null}
            </div>
          </div>
          <p className="customer-link-card__prompt m-0">
            {variant === "received"
              ? t("invitations.connectionRequestBody")
              : t("invitations.waitingResponse")}
          </p>
          {publicUserId ? (
            <p className="m-0 mt-1 truncate text-[length:var(--exits-text-xs)] text-muted">
              {publicUserId}
            </p>
          ) : null}
        </div>
      </div>

      <div className="customer-link-card__meta">
        <span className="customer-link-card__meta-item">
          <CalendarClock className="size-3.5 shrink-0" aria-hidden />
          <span>
            {t("invitations.requestedAt")}: {formatShortDate(request.createdAtUtc)}
          </span>
        </span>
        <span className="customer-link-card__meta-item">
          <Hourglass className="size-3.5 shrink-0" aria-hidden />
          <span>
            {t("invitations.expiresAt")}: {formatShortDate(request.expiresAtUtc)}
          </span>
        </span>
      </div>

      {variant === "received" ? (
        <div className="customer-link-card__actions grid grid-cols-2 gap-2 sm:flex sm:flex-wrap">
          <Button
            type="button"
            variant="outline"
            className="min-h-11 w-full sm:w-auto"
            disabled={actionsDisabled}
            onClick={onDecline}
          >
            {isDeclining ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <X className="size-4 shrink-0" aria-hidden />
            )}
            {t("invitations.decline")}
          </Button>
          <Button
            type="button"
            className="min-h-11 w-full sm:w-auto"
            disabled={actionsDisabled}
            onClick={onAccept}
          >
            {isAccepting ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Check className="size-4 shrink-0" aria-hidden />
            )}
            {t("invitations.accept")}
          </Button>
        </div>
      ) : (
        <div className="customer-link-card__actions">
          <Button
            type="button"
            variant="outline"
            className="min-h-11 w-full sm:w-auto"
            disabled={actionsDisabled}
            onClick={onRevoke}
          >
            {isRevoking ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <X className="size-4 shrink-0" aria-hidden />
            )}
            {t("people.detail.cancelRequest")}
          </Button>
        </div>
      )}
    </article>
  );
}

export function InvitationsPage() {
  const { t } = useI18n();
  const [actionError, setActionError] = useState<string | null>(null);

  const connectionsQuery = usePersonalConnectionRequestsQuery();
  const contactsQuery = usePersonalContactsQuery();
  const acceptMutation = useAcceptConnectionMutation();
  const declineMutation = useDeclineConnectionMutation();
  const revokeMutation = useRevokeConnectionMutation();

  const busy =
    acceptMutation.isPending || declineMutation.isPending || revokeMutation.isPending;
  const { actionsDisabled, cooledDown, noteActionError, noteActionSuccess } =
    useConsentActionGuard(busy);

  const acceptingId = acceptMutation.isPending ? acceptMutation.variables : null;
  const decliningId = declineMutation.isPending ? declineMutation.variables : null;
  const revokingId = revokeMutation.isPending ? revokeMutation.variables : null;

  const contactsByIdentity = useMemo(() => {
    const map = new Map<string, { displayName: string; id: string; publicUserId?: string | null }>();
    for (const contact of contactsQuery.data ?? []) {
      if (contact.resolvedUserIdentityId) {
        map.set(contact.resolvedUserIdentityId, {
          id: contact.id,
          displayName: contact.displayName,
          publicUserId: contact.resolvedPublicUserId,
        });
      }
    }
    return map;
  }, [contactsQuery.data]);

  const { receivedGroups, sentGroups } = useMemo(() => {
    const list = connectionsQuery.data ?? [];
    const pending = list.filter(isPendingConnectionRequest);
    const received = pending.filter((item) => item.direction.toLowerCase() === "received");
    const sent = pending.filter((item) => item.direction.toLowerCase() === "sent");
    const sortNewest = sortByNewestUtc(
      (item: (typeof pending)[number]) => item.createdAtUtc,
    );

    return {
      receivedGroups: groupByKey(received, (item) => item.requesterUserIdentityId, sortNewest),
      sentGroups: groupByKey(sent, (item) => item.targetUserIdentityId, sortNewest),
    };
  }, [connectionsQuery.data]);

  async function onAcceptGroup(group: RequestGroup) {
    setActionError(null);
    const primaryId = group.primary.id;
    const siblings = siblingRequestIds(primaryId, group.items);

    try {
      await acceptMutation.mutateAsync(primaryId);
      await cascadeResolveSiblingRequests(siblings, (requestId) =>
        declineMutation.mutateAsync(requestId),
      );
      noteActionSuccess();
    } catch (err) {
      noteActionError(err);
      setActionError(err instanceof Error ? err.message : t("error.body"));
    }
  }

  async function onDeclineGroup(group: RequestGroup) {
    setActionError(null);

    try {
      for (const request of group.items) {
        await declineMutation.mutateAsync(request.id);
      }
      noteActionSuccess();
    } catch (err) {
      noteActionError(err);
      setActionError(err instanceof Error ? err.message : t("error.body"));
    }
  }

  async function onRevokeGroup(group: RequestGroup) {
    setActionError(null);

    try {
      for (const request of group.items) {
        await revokeMutation.mutateAsync(request.id);
      }
      noteActionSuccess();
    } catch (err) {
      noteActionError(err);
      setActionError(err instanceof Error ? err.message : t("error.body"));
    }
  }

  if (connectionsQuery.isLoading || contactsQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (connectionsQuery.error) {
    const err = connectionsQuery.error;
    return (
      <div className="personal-page invitations-page exits-page mx-auto flex w-full max-w-3xl min-w-0 flex-col gap-3">
        <PageHeader
          title={t("invitations.title")}
          description={t("invitations.lede")}
          backTo="/personal/people"
          backLabel={t("people.backToList")}
          backTestId="page-header-back-invitations"
        />
        <ErrorState
          title={t("error.title")}
          detail={err instanceof PlatformApiError ? err.message : t("error.body")}
          error={err}
        />
      </div>
    );
  }

  const pendingTotal = receivedGroups.length + sentGroups.length;

  return (
    <section
      className="personal-page invitations-page exits-page mx-auto flex w-full max-w-3xl min-w-0 flex-col gap-4"
      data-testid="invitations-page"
    >
      <header className="flex items-center gap-2">
        <Button asChild variant="ghost" size="icon" className="shrink-0" aria-label={t("shell.back")}>
          <Link to="/personal/people">
            <ArrowLeft className="size-5" aria-hidden="true" />
          </Link>
        </Button>
        <h1
          className="m-0 min-w-0 flex-1 text-[length:var(--exits-text-2xl)] font-bold tracking-tight"
          data-testid="invitations-page-title"
        >
          {t("invitations.title")}
        </h1>
      </header>

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("invitations.lede")}</p>

      <div className="flex flex-wrap gap-2" data-testid="invitations-summary">
        <StatusChip tone={receivedGroups.length > 0 ? "warning" : "neutral"}>
          {t("invitations.receivedCount").replace("{count}", String(receivedGroups.length))}
        </StatusChip>
        <StatusChip tone={sentGroups.length > 0 ? "info" : "neutral"}>
          {t("invitations.sentCount").replace("{count}", String(sentGroups.length))}
        </StatusChip>
      </div>

      <Card className="flex flex-col gap-2">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {t("invitations.utangCardTitle")}
            </h2>
            <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
              {t("invitations.utangCardHelp")}
            </p>
          </div>
          <HandCoins className="size-6 shrink-0 text-primary" aria-hidden="true" />
        </div>
        <Button asChild variant="outline" className="min-h-[var(--exits-touch-target-min)] justify-between">
          <Link to="/personal/utang/invitations" data-testid="invitations-open-utang">
            <span>{t("invitations.openUtangInvites")}</span>
            <ChevronRight className="size-4" aria-hidden="true" />
          </Link>
        </Button>
      </Card>

      {!cooledDown ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" role="status">
          {t("personal.consentAction.waitCooldown")}
        </p>
      ) : null}

      {actionError ? (
        <div className="exits-alert exits-alert--error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{actionError}</p>
        </div>
      ) : null}

      <section
        className="catalog-form-section exits-animate-panel personal-section flex flex-col gap-3"
        aria-label={t("invitations.received")}
      >
        <div className="flex items-center justify-between gap-2">
          <h2 className="catalog-form-section__title m-0">{t("invitations.received")}</h2>
          {receivedGroups.length > 0 ? (
            <StatusChip tone="warning">{String(receivedGroups.length)}</StatusChip>
          ) : null}
        </div>

        {receivedGroups.length === 0 ? (
          <EmptyState title={t("invitations.emptyTitle")} detail={t("invitations.emptyBody")} />
        ) : (
          <ul className="exits-list m-0 grid list-none gap-2 p-0">
            {receivedGroups.map((group) => {
              const request = group.primary;
              const isAccepting = group.items.some((item) => acceptingId === item.id);
              const isDeclining = group.items.some((item) => decliningId === item.id);
              return (
                <li key={group.key}>
                  <ConnectionRequestCard
                    request={request}
                    group={group}
                    variant="received"
                    displayName={request.requesterDisplayName}
                    publicUserId={request.requesterPublicUserId}
                    personHref={
                      request.requesterContactId
                        ? `/personal/people/${request.requesterContactId}`
                        : null
                    }
                    actionsDisabled={actionsDisabled}
                    isAccepting={isAccepting}
                    isDeclining={isDeclining}
                    isRevoking={false}
                    onAccept={() => void onAcceptGroup(group)}
                    onDecline={() => void onDeclineGroup(group)}
                  />
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section flex flex-col gap-3"
        aria-label={t("invitations.sent")}
      >
        <div className="flex items-center justify-between gap-2">
          <h2 className="catalog-form-section__title m-0">{t("invitations.sent")}</h2>
          {sentGroups.length > 0 ? (
            <StatusChip tone="info">{String(sentGroups.length)}</StatusChip>
          ) : null}
        </div>

        {sentGroups.length === 0 ? (
          <EmptyState title={t("invitations.sentEmptyTitle")} detail={t("invitations.sentEmpty")} />
        ) : (
          <ul className="exits-list m-0 grid list-none gap-2 p-0">
            {sentGroups.map((group) => {
              const request = group.primary;
              const targetContact = contactsByIdentity.get(request.targetUserIdentityId);
              const isRevoking = group.items.some((item) => revokingId === item.id);
              return (
                <li key={group.key}>
                  <ConnectionRequestCard
                    request={request}
                    group={group}
                    variant="sent"
                    displayName={
                      targetContact?.displayName ??
                      request.targetPublicUserId ??
                      t("invitations.someone")
                    }
                    publicUserId={
                      targetContact?.publicUserId ?? request.targetPublicUserId ?? null
                    }
                    personHref={targetContact ? `/personal/people/${targetContact.id}` : null}
                    actionsDisabled={actionsDisabled}
                    isAccepting={false}
                    isDeclining={false}
                    isRevoking={isRevoking}
                    onRevoke={() => void onRevokeGroup(group)}
                  />
                </li>
              );
            })}
          </ul>
        )}
      </section>

      {pendingTotal === 0 ? (
        <Card className="flex flex-col gap-3 border-dashed bg-[var(--exits-surface-muted)]/40">
          <div className="flex items-start gap-3">
            <span className="inline-flex size-10 shrink-0 items-center justify-center rounded-full bg-surface text-primary">
              <UserRound className="size-5" aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <p className="m-0 font-semibold">{t("invitations.emptyHubTitle")}</p>
              <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("invitations.emptyHubBody")}
              </p>
            </div>
          </div>
          <Button asChild className="min-h-11 w-full sm:w-auto">
            <Link to="/personal/people">{t("invitations.openPeople")}</Link>
          </Button>
        </Card>
      ) : null}
    </section>
  );
}
