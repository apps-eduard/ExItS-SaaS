import { useMemo, useState } from "react";
import { CalendarClock, Check, HandCoins, Hourglass, Loader2, UserRound, Users, X, type LucideIcon } from "lucide-react";
import { Link } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import type { PersonalConnectionRequestDto } from "@/api/platform/personal-types";
import { EmptyState } from "@/components/exits/EmptyState";
import { Notice } from "@/components/exits/Notice";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageSkeleton } from "@/components/exits/loading/PageSkeleton";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import {
  cascadeResolveSiblingRequests,
  groupByKey,
  type ConsentRequestGroup,
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

type RequestGroup = ConsentRequestGroup<PersonalConnectionRequestDto>;

function RibbonPennant({ icon: Icon }: { icon: LucideIcon }) {
  return (
    <span className="personal-tracker-card__pennant" aria-hidden>
      <Icon className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
    </span>
  );
}

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
    <Card
      className="person-ribbon-card invitations-glass invitations-glass--people flex flex-col gap-3"
      data-shell="soft-glass"
      data-testid={`connection-request-${request.id}`}
      data-busy={isAccepting || isDeclining || isRevoking ? "true" : "false"}
    >
      <h3 className="personal-tracker-card__ribbon">
        <RibbonPennant icon={UserRound} />
        {personHref ? (
          <Link to={personHref} className="min-w-0 truncate text-inherit no-underline">
            {displayName}
          </Link>
        ) : (
          <span className="min-w-0 truncate">{displayName}</span>
        )}
      </h3>
      <div className="flex flex-wrap items-center gap-1">
        <StatusChip tone="neutral">{t("invitations.connectionRequests")}</StatusChip>
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
      <p className="m-0 text-[length:var(--exits-text-sm)]">
        {variant === "received"
          ? t("invitations.connectionRequestBody")
          : t("invitations.waitingResponse")}
      </p>
      {publicUserId ? (
        <p className="m-0 truncate text-[length:var(--exits-text-xs)] text-muted">
          {publicUserId}
        </p>
      ) : null}

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
        <div className="customer-link-card__actions invitations-glass__actions grid grid-cols-2 gap-2 sm:flex sm:flex-wrap">
          <Button
            type="button"
            intent="danger"
            appearance="glossy"
            emphasis="strong"
            className="invitations-glass__btn w-full sm:w-auto"
            disabled={actionsDisabled}
            onClick={onDecline}
          >
            {isDeclining ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : null}
            {t("invitations.decline")}
          </Button>
          <Button
            type="button"
            intent="primary"
            appearance="glossy"
            className="invitations-glass__btn w-full sm:w-auto"
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
        <div className="customer-link-card__actions invitations-glass__actions">
          <Button
            type="button"
            intent="danger"
            appearance="glossy"
            emphasis="strong"
            className="invitations-glass__btn w-full sm:w-auto"
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
    </Card>
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

  if (connectionsQuery.isLoading && !connectionsQuery.data) {
    return (
      <div className="personal-page invitations-page exits-page flex w-full min-w-0 flex-col gap-3">
        <PageHeader
          title={t("invitations.title")}
          description={t("invitations.lede")}
          backTo="/personal/people"
          backLabel={t("people.backToList")}
          backTestId="page-header-back-invitations"
        />
        <PageSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (connectionsQuery.error) {
    const err = connectionsQuery.error;
    return (
      <div className="personal-page invitations-page exits-page flex w-full min-w-0 flex-col gap-3">
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
      className="personal-page invitations-page exits-page flex w-full min-w-0 flex-col gap-4"
      data-testid="invitations-page"
    >
      <PageHeader
        title={t("invitations.title")}
        description={t("invitations.lede")}
        titleTestId="invitations-page-title"
        backTo="/personal/people"
        backLabel={t("people.backToList")}
        backTestId="page-header-back-invitations"
      />

      {(connectionsQuery.isFetching || contactsQuery.isFetching) && connectionsQuery.data ? (
        <BackgroundRefreshIndicator active label={t("loading.updating")} />
      ) : null}

      <div
        className={pendingTotal === 0 ? "invitations-top invitations-top--pair" : "invitations-top"}
        data-testid="invitations-top"
      >
        <Card
          className="person-ribbon-card invitations-glass invitations-glass--lent flex flex-col gap-3"
          data-shell="soft-glass"
          data-testid="invitations-utang-card"
        >
          <h2 className="personal-tracker-card__ribbon">
            <RibbonPennant icon={HandCoins} />
            {t("invitations.utangCardTitle")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("invitations.utangCardHelp")}
          </p>
          <div className="invitations-glass__actions flex justify-end">
            <Button
              asChild
              intent="success"
              appearance="glossy"
              className="invitations-glass__btn w-full sm:w-auto"
            >
              <Link to="/personal/utang/invitations" data-testid="invitations-open-utang">
                <HandCoins className="size-4 shrink-0" aria-hidden />
                {t("invitations.openUtangInvites")}
              </Link>
            </Button>
          </div>
        </Card>

        {pendingTotal === 0 ? (
          <Card
            className="person-ribbon-card invitations-glass invitations-glass--people flex flex-col gap-3"
            data-shell="soft-glass"
            data-testid="invitations-empty-hub"
          >
            <h2 className="personal-tracker-card__ribbon">
              <RibbonPennant icon={UserRound} />
              {t("invitations.emptyHubTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("invitations.emptyHubBody")}
            </p>
            <div className="invitations-glass__actions flex justify-end">
              <Button
                asChild
                intent="primary"
                appearance="glossy"
                className="invitations-glass__btn w-full sm:w-auto"
              >
                <Link to="/personal/people" data-testid="invitations-open-people">
                  <Users className="size-4 shrink-0" aria-hidden />
                  {t("invitations.openPeople")}
                </Link>
              </Button>
            </div>
          </Card>
        ) : null}
      </div>

      {!cooledDown ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" role="status">
          {t("personal.consentAction.waitCooldown")}
        </p>
      ) : null}

      {actionError ? (
        <Notice tone="danger">{actionError}</Notice>
      ) : null}

      <div className="invitations-top invitations-top--pair">
      <section
        className="person-ribbon-card invitations-glass invitations-glass--people catalog-form-section exits-animate-panel personal-section flex flex-col gap-3"
        data-shell="soft-glass"
        aria-label={t("invitations.received")}
        data-testid="invitations-received"
      >
        <h2 className="personal-tracker-card__ribbon">
          <RibbonPennant icon={Users} />
          {t("invitations.received")}
        </h2>

        {receivedGroups.length === 0 ? (
          <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />} title={t("invitations.emptyTitle")} detail={t("invitations.emptyBody")} />
        ) : (
          <ul className="exits-list m-0 grid list-none gap-4 p-0">
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
        className="person-ribbon-card invitations-glass invitations-glass--info catalog-form-section exits-animate-panel personal-section flex flex-col gap-3"
        data-shell="soft-glass"
        aria-label={t("invitations.sent")}
        data-testid="invitations-sent"
      >
        <h2 className="personal-tracker-card__ribbon">
          <RibbonPennant icon={Hourglass} />
          {t("invitations.sent")}
        </h2>

        {sentGroups.length === 0 ? (
          <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />} title={t("invitations.sentEmptyTitle")} detail={t("invitations.sentEmpty")} />
        ) : (
          <ul className="exits-list m-0 grid list-none gap-4 p-0">
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
      </div>
    </section>
  );
}
