import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  CalendarClock,
  Check,
  Hourglass,
  Loader2,
  RefreshCw,
  Store,
  X,
} from "lucide-react";
import {
  acceptCustomerLinkRequest,
  blockBusinessFromCustomerLinkRequest,
  declineCustomerLinkRequest,
  listPendingCustomerLinkRequests,
  listResolvedCustomerLinkRequests,
  type PendingCustomerLinkRequestDto,
} from "@/api/platform/customer-link-requests-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { PERSONAL_NOTIFICATIONS_QUERY_KEY } from "@/features/personal/personal-notifications";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";

type CustomerLinksTab = "pending" | "history";

const PENDING_QUERY_KEY = ["personal", "customer-link-requests"] as const;
const HISTORY_QUERY_KEY = ["personal", "customer-link-requests", "history"] as const;

function statusLabelKey(status: string): MessageKey {
  const normalized = status.trim().toLowerCase();
  switch (normalized) {
    case "active":
    case "accepted":
      return "personal.customerLinks.statusAccepted";
    case "declined":
      return "personal.customerLinks.statusDeclined";
    case "expired":
      return "personal.customerLinks.statusExpired";
    case "revoked":
      return "personal.customerLinks.statusRevoked";
    case "pending":
      return "personal.customerLinks.statusPending";
    default:
      return "personal.customerLinks.statusPending";
  }
}

function statusTone(
  status: string,
): "info" | "success" | "warning" | "danger" {
  const normalized = status.trim().toLowerCase();
  switch (normalized) {
    case "active":
    case "accepted":
      return "success";
    case "declined":
    case "revoked":
      return "danger";
    case "expired":
      return "info";
    case "pending":
    default:
      return "warning";
  }
}

export function PersonalCustomerLinksPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<CustomerLinksTab>("pending");
  const [actionError, setActionError] = useState<string | null>(null);

  const pendingQuery = useQuery({
    queryKey: PENDING_QUERY_KEY,
    queryFn: ({ signal }) => listPendingCustomerLinkRequests(signal),
    enabled: tab === "pending",
  });

  const historyQuery = useQuery({
    queryKey: HISTORY_QUERY_KEY,
    queryFn: ({ signal }) => listResolvedCustomerLinkRequests(signal),
    enabled: tab === "history",
  });

  async function invalidateAfterDecision() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: PENDING_QUERY_KEY }),
      queryClient.invalidateQueries({ queryKey: HISTORY_QUERY_KEY }),
      queryClient.invalidateQueries({ queryKey: ["personal", "linked-merchants"] }),
      queryClient.invalidateQueries({ queryKey: PERSONAL_NOTIFICATIONS_QUERY_KEY }),
    ]);
  }

  const accept = useMutation({
    mutationFn: (id: string) => acceptCustomerLinkRequest(id),
    onSuccess: async () => {
      setActionError(null);
      await invalidateAfterDecision();
    },
    onError: (error) =>
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.customerLinks.acceptFailed"),
      ),
  });

  const decline = useMutation({
    mutationFn: (id: string) => declineCustomerLinkRequest(id),
    onSuccess: async () => {
      setActionError(null);
      await invalidateAfterDecision();
    },
    onError: (error) =>
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.customerLinks.declineFailed"),
      ),
  });

  const blockBusiness = useMutation({
    mutationFn: (id: string) => blockBusinessFromCustomerLinkRequest(id),
    onSuccess: async () => {
      setActionError(null);
      await invalidateAfterDecision();
      await queryClient.invalidateQueries({ queryKey: ["personal", "blocked-businesses"] });
    },
    onError: (error) =>
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.customerLinks.blockFailed"),
      ),
  });

  const busy = accept.isPending || decline.isPending || blockBusiness.isPending;
  const acceptingId = accept.isPending ? accept.variables : null;
  const decliningId = decline.isPending ? decline.variables : null;
  const blockingId = blockBusiness.isPending ? blockBusiness.variables : null;

  const activeQuery = tab === "pending" ? pendingQuery : historyQuery;

  return (
    <div
      className="personal-page customer-links-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-customer-links-page"
    >
      <PageHeader
        title={t("personal.customerLinks.title")}
        description={t("personal.customerLinks.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-customer-links"
      />

      <PersonalCommerceNav active="links" />

      <UnderlineTabBar
        items={[
          {
            key: "pending",
            label: t("personal.customerLinks.listTitle"),
            testId: "customer-links-tab-pending",
          },
          {
            key: "history",
            label: t("personal.customerLinks.historyTitle"),
            testId: "customer-links-tab-history",
          },
        ]}
        activeKey={tab}
        onChange={(key) => {
          setActionError(null);
          setTab(key as CustomerLinksTab);
        }}
        ariaLabel={t("personal.customerLinks.title")}
        testId="customer-links-tabs"
      />

      {actionError ? (
        <p
          className="exits-animate-toolbar m-0 text-[length:var(--exits-text-sm)] text-destructive"
          role="alert"
        >
          {actionError}
        </p>
      ) : null}

      {activeQuery.isPending ? (
        <LoadingSkeleton label={t("loading.label")} />
      ) : activeQuery.isError ? (
        <div className="exits-animate-panel flex flex-col gap-3">
          <ErrorState
            title={t("personal.customerLinks.errorTitle")}
            detail={t("personal.customerLinks.errorDetail")}
          />
          <div className="exits-animate-toolbar">
            <ActionTileGrid
              tiles={[
                {
                  key: "retry",
                  label: t("orders.retry"),
                  icon: RefreshCw,
                  onClick: () => void activeQuery.refetch(),
                },
              ]}
            />
          </div>
        </div>
      ) : tab === "pending" ? (
        <PendingTabContent
          items={pendingQuery.data ?? []}
          busy={busy}
          acceptingId={acceptingId}
          decliningId={decliningId}
          blockingId={blockingId}
          onAccept={(id) => accept.mutate(id)}
          onDecline={(id) => decline.mutate(id)}
          onBlock={(id) => {
            if (window.confirm(t("personal.customerLinks.blockConfirm"))) {
              blockBusiness.mutate(id);
            }
          }}
          t={t}
        />
      ) : (
        <HistoryTabContent items={historyQuery.data ?? []} t={t} />
      )}
    </div>
  );
}

function PendingTabContent({
  items,
  busy,
  acceptingId,
  decliningId,
  blockingId,
  onAccept,
  onDecline,
  onBlock,
  t,
}: {
  items: PendingCustomerLinkRequestDto[];
  busy: boolean;
  acceptingId: string | null;
  decliningId: string | null;
  blockingId: string | null;
  onAccept: (id: string) => void;
  onDecline: (id: string) => void;
  onBlock: (id: string) => void;
  t: (key: MessageKey) => string;
}) {
  if (items.length === 0) {
    return (
      <div className="exits-animate-panel flex flex-col gap-3">
        <EmptyState
          title={t("personal.customerLinks.emptyTitle")}
          detail={t("personal.customerLinks.emptyDetail")}
        />
        <ActionTileGrid
          tiles={[
            {
              key: "stores-empty",
              label: t("personal.merchantsTitle"),
              icon: Store,
              to: "/personal/linked-merchants",
              testId: "open-stores-empty",
              primary: true,
            },
          ]}
        />
      </div>
    );
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section gap-2"
      aria-label={t("personal.customerLinks.listTitle")}
    >
      <h2 className="catalog-form-section__title text-muted">
        {t("personal.customerLinks.listTitle")}
      </h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("personal.customerLinks.acceptHint")}
      </p>
      <ul className="exits-list m-0 grid list-none gap-2 p-0">
        {items.map((request) => {
          const isAccepting = acceptingId === request.id;
          const isDeclining = decliningId === request.id;
          const isBlocking = blockingId === request.id;
          return (
            <li key={request.id}>
              <article
                className="exits-list__card customer-link-card"
                data-testid={`customer-link-request-${request.id}`}
                data-busy={isAccepting || isDeclining || isBlocking ? "true" : "false"}
              >
                <div className="customer-link-card__header">
                  <span className="customer-link-card__avatar" aria-hidden>
                    <Store className="size-5" />
                  </span>
                  <div className="customer-link-card__heading min-w-0 flex-1">
                    <div className="customer-link-card__title-row">
                      <p className="exits-list__name m-0 min-w-0 flex-1 truncate font-semibold">
                        {request.organizationDisplayName}
                      </p>
                      <StatusChip tone="warning">
                        {t("personal.customerLinks.statusPending")}
                      </StatusChip>
                    </div>
                    <p className="customer-link-card__prompt m-0">
                      {t("personal.customerLinks.cardPrompt")}
                    </p>
                  </div>
                </div>

                <div className="customer-link-card__meta">
                  <span className="customer-link-card__meta-item">
                    <CalendarClock className="size-3.5 shrink-0" aria-hidden />
                    <span>
                      {t("personal.customerLinks.requestedAt")}:{" "}
                      {new Date(request.createdAtUtc).toLocaleString()}
                    </span>
                  </span>
                  <span className="customer-link-card__meta-item">
                    <Hourglass className="size-3.5 shrink-0" aria-hidden />
                    <span>
                      {t("personal.customerLinks.expiresAt")}:{" "}
                      {new Date(request.expiresAtUtc).toLocaleString()}
                    </span>
                  </span>
                </div>

                <div className="customer-link-card__actions">
                  <Button
                    type="button"
                    className="min-h-11"
                    disabled={busy}
                    data-testid={`customer-link-accept-${request.id}`}
                    onClick={() => onAccept(request.id)}
                  >
                    {isAccepting ? (
                      <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                    ) : (
                      <Check className="size-4 shrink-0" aria-hidden />
                    )}
                    {t("personal.customerLinks.accept")}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    className="min-h-11"
                    disabled={busy}
                    data-testid={`customer-link-decline-${request.id}`}
                    onClick={() => onDecline(request.id)}
                  >
                    {isDeclining ? (
                      <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                    ) : (
                      <X className="size-4 shrink-0" aria-hidden />
                    )}
                    {t("personal.customerLinks.decline")}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11 text-destructive"
                    disabled={busy}
                    data-testid={`customer-link-block-${request.id}`}
                    onClick={() => onBlock(request.id)}
                  >
                    {isBlocking ? (
                      <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                    ) : null}
                    {t("personal.customerLinks.blockBusiness")}
                  </Button>
                </div>
              </article>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function HistoryTabContent({
  items,
  t,
}: {
  items: PendingCustomerLinkRequestDto[];
  t: (key: MessageKey) => string;
}) {
  if (items.length === 0) {
    return (
      <div className="exits-animate-panel flex flex-col gap-3">
        <EmptyState
          title={t("personal.customerLinks.historyEmptyTitle")}
          detail={t("personal.customerLinks.historyEmptyDetail")}
        />
      </div>
    );
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section gap-2"
      aria-label={t("personal.customerLinks.historyTitle")}
    >
      <h2 className="catalog-form-section__title text-muted">
        {t("personal.customerLinks.historyTitle")}
      </h2>
      <ul className="exits-list m-0 grid list-none gap-2 p-0">
        {items.map((request) => (
          <li key={request.id}>
            <article
              className="exits-list__card customer-link-card"
              data-testid={`customer-link-history-${request.id}`}
            >
              <div className="customer-link-card__header">
                <span className="customer-link-card__avatar" aria-hidden>
                  <Store className="size-5" />
                </span>
                <div className="customer-link-card__heading min-w-0 flex-1">
                  <div className="customer-link-card__title-row">
                    <p className="exits-list__name m-0 min-w-0 flex-1 truncate font-semibold">
                      {request.organizationDisplayName}
                    </p>
                    <StatusChip tone={statusTone(request.status)}>
                      {t(statusLabelKey(request.status))}
                    </StatusChip>
                  </div>
                </div>
              </div>

              <div className="customer-link-card__meta">
                <span className="customer-link-card__meta-item">
                  <CalendarClock className="size-3.5 shrink-0" aria-hidden />
                  <span>
                    {t("personal.customerLinks.requestedAt")}:{" "}
                    {new Date(request.createdAtUtc).toLocaleString()}
                  </span>
                </span>
                <span className="customer-link-card__meta-item">
                  <Hourglass className="size-3.5 shrink-0" aria-hidden />
                  <span>
                    {t("personal.customerLinks.expiresAt")}:{" "}
                    {new Date(request.expiresAtUtc).toLocaleString()}
                  </span>
                </span>
              </div>
            </article>
          </li>
        ))}
      </ul>
    </section>
  );
}
