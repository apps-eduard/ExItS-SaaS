import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Ban,
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
} from "@/api/platform/customer-link-requests-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import {
  cascadeResolveSiblingRequests,
  groupByKey,
  siblingRequestIds,
  sortByNewestUtc,
} from "@/features/personal/consent-request-groups";
import { PERSONAL_NOTIFICATIONS_QUERY_KEY } from "@/features/personal/personal-notifications";
import { useConsentActionGuard } from "@/features/personal/useConsentActionGuard";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

export function PersonalCustomerLinksPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["personal", "customer-link-requests"],
    queryFn: ({ signal }) => listPendingCustomerLinkRequests(signal),
  });

  async function invalidateAfterDecision() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["personal", "customer-link-requests"] }),
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
  const { actionsDisabled, cooledDown, noteActionError, noteActionSuccess } =
    useConsentActionGuard(busy);
  const acceptingId = accept.isPending ? accept.variables : null;
  const decliningId = decline.isPending ? decline.variables : null;
  const blockingId = blockBusiness.isPending ? blockBusiness.variables : null;

  const groupedItems = useMemo(() => {
    const items = query.data ?? [];
    return groupByKey(
      items,
      (request) => request.organizationId,
      sortByNewestUtc((request) => request.createdAtUtc),
    );
  }, [query.data]);

  if (query.isPending) {
    return (
      <div className="personal-page customer-links-page exits-page flex min-w-0 flex-col gap-3">
        <PageHeader
          title={t("personal.customerLinks.title")}
          description={t("personal.customerLinks.lede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-customer-links"
        />
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (query.isError) {
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
                onClick: () => void query.refetch(),
              },
            ]}
          />
        </div>
      </div>
    );
  }

  async function onAcceptGroup(group: (typeof groupedItems)[number]) {
    setActionError(null);
    const primaryId = group.primary.id;
    const siblings = siblingRequestIds(primaryId, group.items);

    try {
      await accept.mutateAsync(primaryId);
      await cascadeResolveSiblingRequests(siblings, (requestId) =>
        decline.mutateAsync(requestId),
      );
      noteActionSuccess();
    } catch (error) {
      noteActionError(error);
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.customerLinks.acceptFailed"),
      );
    }
  }

  async function onDeclineGroup(group: (typeof groupedItems)[number]) {
    setActionError(null);

    try {
      for (const request of group.items) {
        await decline.mutateAsync(request.id);
      }
      noteActionSuccess();
    } catch (error) {
      noteActionError(error);
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.customerLinks.declineFailed"),
      );
    }
  }

  async function onBlockGroup(group: (typeof groupedItems)[number]) {
    setActionError(null);
    const primaryId = group.primary.id;
    const siblings = siblingRequestIds(primaryId, group.items);

    try {
      await blockBusiness.mutateAsync(primaryId);
      await cascadeResolveSiblingRequests(siblings, (requestId) =>
        decline.mutateAsync(requestId),
      );
      await queryClient.invalidateQueries({ queryKey: ["personal", "blocked-businesses"] });
      noteActionSuccess();
    } catch (error) {
      noteActionError(error);
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.customerLinks.blockFailed"),
      );
    }
  }

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

      {actionError ? (
        <p
          className="exits-animate-toolbar m-0 text-[length:var(--exits-text-sm)] text-destructive"
          role="alert"
        >
          {actionError}
        </p>
      ) : null}

      {!cooledDown ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" role="status">
          {t("personal.consentAction.waitCooldown")}
        </p>
      ) : null}

      {groupedItems.length === 0 ? (
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
      ) : (
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
            {groupedItems.map((group) => {
              const request = group.primary;
              const isAccepting = group.items.some((item) => acceptingId === item.id);
              const isDeclining = group.items.some((item) => decliningId === item.id);
              const isBlocking = group.items.some((item) => blockingId === item.id);
              return (
                <li key={group.key}>
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
                          <div className="flex flex-wrap items-center gap-1">
                            <StatusChip tone="warning">
                              {t("personal.customerLinks.statusPending")}
                            </StatusChip>
                            {group.duplicateCount > 1 ? (
                              <StatusChip tone="info">
                                {t("personal.customerLinks.duplicateCount").replace(
                                  "{count}",
                                  String(group.duplicateCount),
                                )}
                              </StatusChip>
                            ) : null}
                          </div>
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

                    <div className="customer-link-card__actions flex flex-col gap-2">
                      <div className="grid grid-cols-2 gap-2 sm:flex sm:flex-wrap">
                        <Button
                          type="button"
                          className="min-h-11 w-full sm:w-auto"
                          disabled={actionsDisabled}
                          data-testid={`customer-link-accept-${request.id}`}
                          onClick={() => void onAcceptGroup(group)}
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
                          className="min-h-11 w-full sm:w-auto"
                          disabled={actionsDisabled}
                          data-testid={`customer-link-decline-${request.id}`}
                          onClick={() => void onDeclineGroup(group)}
                        >
                          {isDeclining ? (
                            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                          ) : (
                            <X className="size-4 shrink-0" aria-hidden />
                          )}
                          {t("personal.customerLinks.decline")}
                        </Button>
                      </div>
                      <Button
                        type="button"
                        variant="ghost"
                        className="min-h-11 w-full text-destructive sm:w-auto"
                        disabled={actionsDisabled}
                        data-testid={`customer-link-block-${request.id}`}
                        onClick={() => {
                          if (window.confirm(t("personal.customerLinks.blockConfirm"))) {
                            void onBlockGroup(group);
                          }
                        }}
                      >
                        {isBlocking ? (
                          <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                        ) : (
                          <Ban className="size-4 shrink-0" aria-hidden />
                        )}
                        {t("personal.customerLinks.blockBusiness")}
                      </Button>
                    </div>
                  </article>
                </li>
              );
            })}
          </ul>
        </section>
      )}
    </div>
  );
}
