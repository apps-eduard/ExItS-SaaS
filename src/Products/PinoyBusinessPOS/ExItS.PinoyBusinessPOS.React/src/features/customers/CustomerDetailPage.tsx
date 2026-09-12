import { useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, FileText, Link2, MapPin, NotebookPen, Pencil, Phone, RotateCcw, UserRound, Users, Wallet } from "lucide-react";
import { canEditCustomer, canManageCustomerCreditPolicy, canApproveCustomerCreditPolicy, canRecordRepayment, canViewStatement } from "@/access/pos-capabilities";
import {
  createCustomerLinkRequestForCustomer,
  getCustomerLinkStatus,
  listCustomerLinkRequestHistory,
  remindCustomerLinkRequest,
  revokeCustomerLinkRequest,
} from "@/api/platform/customer-link-status-client";
import { useMutation } from "@tanstack/react-query";
import {
  getOrganizationBusinessCustomer,
  updateBusinessCustomerDeliveryPreferences,
} from "@/api/platform/business-customer-delivery-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  deactivateCustomer,
  getCustomer,
  getCustomerCreditSummary,
  listCustomerCreditEntries,
  listCustomerRepayments,
  reactivateCustomer,
  type PosCustomerListItem,
} from "@/api/pos/pos-customers-client";
import { CustomerDeliveryExceptionSection } from "@/features/customers/CustomerDeliveryExceptionSection";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  customerLinkStatusLabelKey,
  extractPersonalExItsIdFromNotes,
  mapPlatformCustomerLinkStatus,
  resolveDisplayedPersonalExItsId,
  type CustomerLinkUiStatus,
} from "@/features/customers/customer-link-status";
import { ConnectionStatusChip } from "@/features/customer-connection/ConnectionStatusChip";
import {
  mapOrgLinkStatusToRelationship,
} from "@/features/customer-connection/connection-state";
import { CustomerPersonalLinkSection } from "@/features/customers/CustomerPersonalLinkSection";
import { CreditPolicySection } from "@/features/customers/CreditPolicySection";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useI18n } from "@/i18n/I18nProvider";
import {
  cacheCustomer,
  cacheCustomerCreditSummary,
  getCachedCustomer,
  getCachedCustomerCreditSummary,
} from "@/offline/customer-cache";
import { onlineRequiredDetailKey, ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import { useOrganizationOfflineContext } from "@/offline/organization-offline-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

export function CustomerDetailPage() {
  const { t } = useI18n();
  const { customerId } = useParams<{ customerId: string }>();
  const [searchParams] = useSearchParams();
  const pendingLinkHint = searchParams.get("pendingLink") === "1";
  const { sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offlineContext = useOrganizationOfflineContext();
  const [actionError, setActionError] = useState<string | null>(null);
  const [acting, setActing] = useState(false);
  const [afterCreateHintDismissed, setAfterCreateHintDismissed] = useState(false);
  const [cachedCustomer, setCachedCustomer] = useState<PosCustomerListItem | null>(null);
  const [cachedOwed, setCachedOwed] = useState<number | null>(null);

  const allowEdit = canEditCustomer(sessionGrant);
  const allowRepay = canRecordRepayment(sessionGrant);
  const allowStatement = canViewStatement(sessionGrant);
  const allowManageCreditPolicy = canManageCustomerCreditPolicy(sessionGrant);
  const allowApproveCreditPolicy = canApproveCustomerCreditPolicy(sessionGrant);

  const enabledOnline = Boolean(workspace) && Boolean(customerId) && online;

  const customerQuery = useQuery({
    queryKey: ["customers", "detail", workspace?.organizationId, customerId],
    enabled: enabledOnline,
    queryFn: ({ signal }) => getCustomer(workspace!, customerId!, signal),
  });

  const summaryQuery = useQuery({
    queryKey: ["customers", "credit-summary", workspace?.organizationId, customerId],
    enabled: enabledOnline,
    queryFn: ({ signal }) => getCustomerCreditSummary(workspace!, customerId!, signal),
  });

  const platformCustomerId = customerQuery.data?.platformBusinessCustomerId ?? null;
  const linkStatusQuery = useQuery({
    queryKey: [
      "customers",
      "platform-link-status",
      workspace?.organizationId,
      platformCustomerId,
    ],
    enabled: enabledOnline && Boolean(platformCustomerId),
    queryFn: ({ signal }) =>
      getCustomerLinkStatus(workspace!.organizationId, platformCustomerId!, signal),
    refetchOnWindowFocus: true,
  });

  const linkHistoryQuery = useQuery({
    queryKey: [
      "customers",
      "platform-link-history",
      workspace?.organizationId,
      platformCustomerId,
    ],
    enabled: enabledOnline && Boolean(platformCustomerId),
    queryFn: ({ signal }) =>
      listCustomerLinkRequestHistory(workspace!.organizationId, platformCustomerId!, signal),
  });

  const deliveryPrefsQuery = useQuery({
    queryKey: [
      "customers",
      "delivery-preferences",
      workspace?.organizationId,
      platformCustomerId,
    ],
    enabled: enabledOnline && Boolean(platformCustomerId),
    queryFn: ({ signal }) =>
      getOrganizationBusinessCustomer(workspace!.organizationId, platformCustomerId!, signal),
  });

  const deliveryExceptionMutation = useMutation({
    mutationFn: async (next: boolean) => {
      if (!workspace || !platformCustomerId) {
        throw new Error("missing-customer");
      }
      return updateBusinessCustomerDeliveryPreferences(
        workspace.organizationId,
        platformCustomerId,
        next,
      );
    },
    onSuccess: async () => {
      setActionError(null);
      await queryClient.invalidateQueries({
        queryKey: [
          "customers",
          "delivery-preferences",
          workspace?.organizationId,
          platformCustomerId,
        ],
      });
      await queryClient.invalidateQueries({
        queryKey: ["customers", "delivery-exception-ids", workspace?.organizationId],
      });
    },
    onError: (error) => {
      setActionError(
        error instanceof PlatformApiError
          ? (error.problem.detail ?? t("customers.delivery.updateFailed"))
          : t("customers.delivery.updateFailed"),
      );
    },
  });

  async function invalidateLinkQueries() {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: ["customers", "platform-link-status", workspace?.organizationId, platformCustomerId],
      }),
      queryClient.invalidateQueries({
        queryKey: ["customers", "platform-link-history", workspace?.organizationId, platformCustomerId],
      }),
    ]);
  }

  const remindMutation = useMutation({
    mutationFn: async () => {
      const requestId = linkStatusQuery.data?.latestLinkRequestId;
      if (!workspace || !requestId) {
        throw new Error("missing-request");
      }
      return remindCustomerLinkRequest(workspace.organizationId, requestId);
    },
    onSuccess: async () => {
      setActionError(null);
      await invalidateLinkQueries();
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("error.detail"));
    },
  });

  const revokeMutation = useMutation({
    mutationFn: async () => {
      const requestId = linkStatusQuery.data?.latestLinkRequestId;
      if (!workspace || !requestId) {
        throw new Error("missing-request");
      }
      await revokeCustomerLinkRequest(workspace.organizationId, requestId);
    },
    onSuccess: async () => {
      setActionError(null);
      await invalidateLinkQueries();
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("error.detail"));
    },
  });

  const inviteAgainMutation = useMutation({
    mutationFn: async () => {
      const row = customerQuery.data;
      if (!workspace || !row?.platformBusinessCustomerId) {
        throw new Error("missing-customer");
      }
      const publicUserId =
        row.linkedPersonalPublicUserId?.trim() ||
        extractPersonalExItsIdFromNotes(row.notes).exItsId ||
        null;
      await createCustomerLinkRequestForCustomer({
        organizationId: workspace.organizationId,
        platformBusinessCustomerId: row.platformBusinessCustomerId,
        publicUserId,
      });
    },
    onSuccess: async () => {
      setActionError(null);
      await invalidateLinkQueries();
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("error.detail"));
    },
  });

  const creditsQuery = useQuery({
    queryKey: ["customers", "credits", workspace?.organizationId, customerId],
    enabled: enabledOnline,
    queryFn: ({ signal }) =>
      listCustomerCreditEntries(workspace!, customerId!, { pageSize: 20 }, signal),
  });

  const repaymentsQuery = useQuery({
    queryKey: ["customers", "repayments", workspace?.organizationId, customerId],
    enabled: enabledOnline,
    queryFn: ({ signal }) =>
      listCustomerRepayments(workspace!, customerId!, { pageSize: 20 }, signal),
  });

  const repaymentActorIds = (repaymentsQuery.data?.items ?? []).flatMap((payment) => [
    payment.recordedBy,
    payment.reversedBy,
  ]);
  const actors = useActorDirectory(workspace?.organizationId, repaymentActorIds);

  useEffect(() => {
    if (!offlineContext || !online) {
      return;
    }
    if (customerQuery.data) {
      void cacheCustomer(offlineContext.db, offlineContext.scopeBinding, customerQuery.data).catch(
        () => {},
      );
    }
    if (summaryQuery.data) {
      void cacheCustomerCreditSummary(
        offlineContext.db,
        offlineContext.scopeBinding,
        summaryQuery.data,
      ).catch(() => {});
    }
  }, [customerQuery.data, offlineContext, online, summaryQuery.data]);

  useEffect(() => {
    if (!offlineContext || online || !customerId) {
      return;
    }
    let cancelled = false;
    void Promise.all([
      getCachedCustomer(offlineContext.db, offlineContext.scopeBinding, customerId),
      getCachedCustomerCreditSummary(offlineContext.db, offlineContext.scopeBinding, customerId),
    ]).then(([cachedRow, summary]) => {
      if (cancelled) {
        return;
      }
      setCachedCustomer(cachedRow);
      setCachedOwed(summary?.outstandingAmount ?? null);
    });
    return () => {
      cancelled = true;
    };
  }, [customerId, offlineContext, online]);

  if (!workspace || !customerId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (customerQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  const customer = customerQuery.data ?? cachedCustomer;

  if (!customer) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={
          online
            ? ((customerQuery.error as Error | undefined)?.message ?? t("customers.notFound"))
            : t("offline.customerNotCached")
        }
      />
    );
  }

  const usingCachedCustomer = !customerQuery.data;
  const amountOwed = summaryQuery.data?.outstandingAmount ?? cachedOwed ?? 0;
  const isActive = customer.status.toLowerCase() === "active";

  const linkUiStatus: CustomerLinkUiStatus = (() => {
    if (!customer.platformBusinessCustomerId?.trim()) {
      return "NotLinked";
    }
    if (!online) {
      return "Unavailable";
    }
    if (linkStatusQuery.isError) {
      return "Unavailable";
    }
    if (linkStatusQuery.data) {
      return mapPlatformCustomerLinkStatus(linkStatusQuery.data.status);
    }
    // Authoritative Platform status still loading — never invent Linked.
    return "Unavailable";
  })();

  const showAfterCreateHint =
    pendingLinkHint &&
    !afterCreateHintDismissed &&
    (linkUiStatus === "Pending" || (linkStatusQuery.isLoading && !linkStatusQuery.data));
  const linkData = linkStatusQuery.data;
  const showUnavailableBanner =
    online &&
    linkData !== undefined &&
    mapPlatformCustomerLinkStatus(linkData.status) === "Unavailable";
  const linkMeta = linkData;
  const reminderCooldownActive =
    linkUiStatus === "Pending" &&
    Boolean(linkMeta?.nextReminderEligibleAtUtc) &&
    new Date(linkMeta!.nextReminderEligibleAtUtc!).getTime() > Date.now();

  const linkHistoryItems = (linkHistoryQuery.data ?? []).slice(0, 8);

  const personalExItsId = resolveDisplayedPersonalExItsId({
    linkedPersonalPublicUserId: customer.linkedPersonalPublicUserId,
    notes: customer.notes,
  });
  const notesDisplay = extractPersonalExItsIdFromNotes(customer.notes).notesWithoutExItsTag;

  async function toggleStatus() {
    if (!allowEdit || acting || !workspace || !customerId) {
      return;
    }
    if (!online) {
      setActionError(t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.CustomerStatus)));
      return;
    }
    setActing(true);
    setActionError(null);
    try {
      if (isActive) {
        await deactivateCustomer(workspace, customerId);
      } else {
        await reactivateCustomer(workspace, customerId);
      }
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t("error.detail"));
    } finally {
      setActing(false);
    }
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-4" data-testid="customer-detail-page">
      <PageHeader
        title={customer.displayName}
        description={t("customers.detailLede")}
        backTo={pageBackNav.customers.to}
        backLabel={t(pageBackNav.customers.labelKey)}
        backTestId="page-header-back-customers"
      />
      <div className="flex flex-wrap items-center gap-2">
        <StatusChip tone={isActive ? "success" : "warning"}>{customer.status}</StatusChip>
        <span data-testid="customer-link-status">
          <ConnectionStatusChip
            state={mapOrgLinkStatusToRelationship(linkUiStatus)}
            audience="organization"
            testId="customer-connection-status-chip"
          />
        </span>
      </div>

      {linkUiStatus !== "NotLinked" ? (
        <CustomerPersonalLinkSection
          linkUiStatus={linkUiStatus}
          personalExItsId={personalExItsId}
          customerDisplayName={customer.displayName}
          linkMeta={linkMeta}
          linkHistoryItems={linkHistoryItems}
          historyPeer={
            platformCustomerId && online ? (
              <CustomerDeliveryExceptionSection
                allowBeyond={deliveryPrefsQuery.data?.allowDeliveryBeyondNormalDistance ?? false}
                canEdit={allowEdit && !deliveryPrefsQuery.isLoading}
                pending={deliveryExceptionMutation.isPending}
                t={t}
                onToggle={(next) => deliveryExceptionMutation.mutate(next)}
              />
            ) : null
          }
          showAfterCreateHint={showAfterCreateHint}
          afterCreateHintDismissed={afterCreateHintDismissed}
          onDismissAfterCreateHint={() => setAfterCreateHintDismissed(true)}
          online={online}
          allowEdit={allowEdit}
          reminderCooldownActive={reminderCooldownActive}
          remindPending={remindMutation.isPending}
          revokePending={revokeMutation.isPending}
          onRemind={() => remindMutation.mutate()}
          onRevoke={() => revokeMutation.mutate()}
        />
      ) : platformCustomerId && online ? (
        <CustomerDeliveryExceptionSection
          allowBeyond={deliveryPrefsQuery.data?.allowDeliveryBeyondNormalDistance ?? false}
          canEdit={allowEdit && !deliveryPrefsQuery.isLoading}
          pending={deliveryExceptionMutation.isPending}
          t={t}
          onToggle={(next) => deliveryExceptionMutation.mutate(next)}
        />
      ) : null}

      {showUnavailableBanner ? (
        <Card data-testid="customer-link-unavailable-banner">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("customers.linkStatus.unavailable")}
          </p>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.linkConnectionUnavailableDetail")}
          </p>
        </Card>
      ) : null}

      {(linkUiStatus === "Declined" || linkUiStatus === "Expired" || linkUiStatus === "Revoked") &&
      online &&
      allowEdit &&
      customer.platformBusinessCustomerId &&
      (customer.linkedPersonalPublicUserId || extractPersonalExItsIdFromNotes(customer.notes).exItsId) ? (
        <Card data-testid="customer-link-invite-again-card">
          <Button
            type="button"
            data-testid="customer-link-invite-again"
            disabled={inviteAgainMutation.isPending}
            onClick={() => inviteAgainMutation.mutate()}
          >
            {linkUiStatus === "Expired"
              ? t("customers.linkSendNewInvite")
              : t("customers.linkInviteAgain")}
          </Button>
        </Card>
      ) : null}

      {usingCachedCustomer ? (
        <Card data-testid="customer-detail-cached-notice">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("offline.cachedBalanceNotice")}
          </p>
        </Card>
      ) : null}

      {!online && customer.platformBusinessCustomerId?.trim() ? (
        <Card data-testid="customer-link-status-offline">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.linkStatus.unavailableOffline")}
          </p>
        </Card>
      ) : null}

      {actionError ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {actionError}
          </p>
        </Card>
      ) : null}

      <Card className="flex flex-col gap-4 p-4" data-testid="customer-overview-card">
        <div className="customer-detail-owed" data-testid="customer-amount-owed">
          <div className="customer-detail-owed__copy">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("customers.amountOwed")}
            </p>
            <p className="mb-0 mt-1 text-[length:var(--exits-text-xl)] font-semibold tabular-nums">
              <MoneyDisplay amount={amountOwed} testId="customer-amount-owed-value" />
            </p>
          </div>
          <span className="customer-detail-owed__icon" aria-hidden>
            <Wallet className="size-5" />
          </span>
        </div>

        <dl className="branch-mgmt-overview__grid">
          <div className="branch-mgmt-overview__item">
            <dt>
              <UserRound className="branch-mgmt-overview__icon" aria-hidden />
              {t("customers.exItsIdLabel")}
            </dt>
            <dd
              data-testid={linkUiStatus === "Pending" ? undefined : "customer-exits-id"}
            >
              {personalExItsId ?? t("customers.exItsIdNone")}
            </dd>
          </div>
          <div className="branch-mgmt-overview__item">
            <dt>
              <Link2 className="branch-mgmt-overview__icon" aria-hidden />
              {t("customers.linkStatusLabel")}
            </dt>
            <dd className="branch-mgmt-overview__value--status" data-testid="customer-link-status-label">
              {online && !customer.platformBusinessCustomerId?.trim()
                ? t("customers.linkStatus.notLinked")
                : !online && customer.platformBusinessCustomerId?.trim()
                  ? t("customers.linkStatus.unavailableOffline")
                  : t(customerLinkStatusLabelKey(linkUiStatus))}
            </dd>
          </div>
          <div className="branch-mgmt-overview__item">
            <dt>
              <Phone className="branch-mgmt-overview__icon" aria-hidden />
              {t("customers.mobile")}
            </dt>
            <dd>{customer.mobileNumber?.trim() || "—"}</dd>
          </div>
        </dl>

        <dl className="branch-mgmt-overview__grid customer-detail-overview__address-notes">
          <div className="branch-mgmt-overview__item">
            <dt>
              <MapPin className="branch-mgmt-overview__icon" aria-hidden />
              {t("customers.address")}
            </dt>
            <dd>{customer.address?.trim() || "—"}</dd>
          </div>
          <div className="branch-mgmt-overview__item">
            <dt>
              <NotebookPen className="branch-mgmt-overview__icon" aria-hidden />
              {t("customers.notes")}
            </dt>
            <dd className="whitespace-pre-wrap" data-testid="customer-notes-display">
              {notesDisplay || "—"}
            </dd>
          </div>
        </dl>

        <div className="customer-detail-overview__actions">
          {allowEdit ? (
            <Button asChild variant="outline" data-testid="customer-edit">
              <Link to={`/customers/${customerId}/edit`}>
                <Pencil className="size-4 shrink-0" aria-hidden />
                {t("customers.edit")}
              </Link>
            </Button>
          ) : null}
          {allowRepay ? (
            <Button asChild variant="outline" data-testid="customer-repay">
              <Link to={`/customers/${customerId}/repay`}>
                <Wallet className="size-4 shrink-0" aria-hidden />
                {t("customers.recordPayment")}
              </Link>
            </Button>
          ) : null}
          {allowStatement && online ? (
            <Button asChild variant="outline" data-testid="customer-statement">
              <Link to={`/customers/${customerId}/statement`}>
                <FileText className="size-4 shrink-0" aria-hidden />
                {t("customers.viewStatement")}
              </Link>
            </Button>
          ) : null}
          {allowEdit ? (
            <Button
              type="button"
              variant="outline"
              data-testid="customer-toggle-status"
              disabled={acting}
              onClick={() => void toggleStatus()}
            >
              {isActive ? (
                <Ban className="size-4 shrink-0" aria-hidden />
              ) : (
                <RotateCcw className="size-4 shrink-0" aria-hidden />
              )}
              {isActive ? t("customers.deactivate") : t("customers.reactivate")}
            </Button>
          ) : null}
        </div>
      </Card>

      {workspace && customerId ? (
        <CreditPolicySection
          workspace={workspace}
          customerId={customerId}
          online={online}
          canManage={allowManageCreditPolicy}
          canApprove={allowApproveCreditPolicy}
          subjectIdentity={[customer.displayName, personalExItsId]
            .filter((part): part is string => Boolean(part))
            .join(" · ")}
        />
      ) : null}

      <section className="flex flex-col gap-2" data-testid="customer-credits-section">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.creditsTitle")}
        </h2>
        {creditsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        {creditsQuery.isSuccess && creditsQuery.data.items.length === 0 ? (
          <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("customers.creditsEmpty")}
            detail={t("customers.creditsEmptyDetail")}
          />
        ) : null}
        {creditsQuery.data && creditsQuery.data.items.length > 0 ? (
          <Card className="overflow-hidden p-0">
            <div className="min-w-0 overflow-x-auto">
              <table className="customer-ledger-table w-full min-w-[32rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                <thead>
                  <tr className="border-b border-border">
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("expense.amount")}
                    </th>
                    <th className="min-w-[12rem] px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("expense.description")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("customers.statusLabel")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {creditsQuery.data.items.map((entry) => (
                    <tr
                      key={entry.creditEntryId}
                      className="border-b border-border last:border-b-0"
                      data-testid={`customer-credit-${entry.creditEntryId}`}
                    >
                      <td className="whitespace-nowrap px-3 py-2.5 align-middle font-semibold tabular-nums">
                        <MoneyDisplay amount={entry.amount} />
                      </td>
                      <td className="max-w-[24rem] px-3 py-2.5 align-middle text-muted">
                        {entry.sourceSaleId ? (
                          <Link
                            to={`/sell/sales/${entry.sourceSaleId}/summary`}
                            className="line-clamp-2 font-medium text-[var(--exits-primary)] underline-offset-2 hover:underline"
                            data-testid={`customer-credit-sale-link-${entry.creditEntryId}`}
                          >
                            {entry.remarks?.trim() || t("transactions.viewSummary")}
                          </Link>
                        ) : (
                          <span className="line-clamp-2">{entry.remarks || "—"}</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-3 py-2.5 align-middle">
                        <StatusChip
                          tone={entry.status.toLowerCase() === "active" ? "success" : "neutral"}
                        >
                          {entry.status}
                        </StatusChip>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        ) : null}
      </section>

      <section className="flex flex-col gap-2" data-testid="customer-payments-section">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.paymentsTitle")}
        </h2>
        {repaymentsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        {repaymentsQuery.isSuccess && repaymentsQuery.data.items.length === 0 ? (
          <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("customers.paymentsEmpty")}
            detail={t("customers.paymentsEmptyDetail")}
          />
        ) : null}
        {repaymentsQuery.data && repaymentsQuery.data.items.length > 0 ? (
          <Card className="overflow-hidden p-0">
            <div className="min-w-0 overflow-x-auto">
              <table className="customer-ledger-table w-full min-w-[40rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                <thead>
                  <tr className="border-b border-border">
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("expense.amount")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("customers.statusLabel")}
                    </th>
                    <th className="min-w-[10rem] px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("common.recordedBy")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("expense.description")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {repaymentsQuery.data.items.map((payment) => (
                    <tr
                      key={payment.repaymentId}
                      className="border-b border-border last:border-b-0"
                      data-testid={`customer-payment-${payment.repaymentId}`}
                    >
                      <td className="whitespace-nowrap px-3 py-2.5 align-middle font-semibold tabular-nums">
                        <MoneyDisplay amount={payment.amount} />
                      </td>
                      <td className="whitespace-nowrap px-3 py-2.5 align-middle">
                        <StatusChip
                          tone={payment.status.toLowerCase() === "active" ? "success" : "neutral"}
                        >
                          {payment.status}
                        </StatusChip>
                      </td>
                      <td className="px-3 py-2.5 align-middle">
                        <ActorAttribution
                          labelKey="common.recordedBy"
                          actorId={payment.recordedBy}
                          occurredAtUtc={payment.recordedAtUtc}
                          resolved={actors.resolve(payment.recordedBy)}
                          isLoading={actors.isResolving}
                          className="min-h-0"
                          testId={`customer-payment-recorded-by-${payment.repaymentId}`}
                        />
                        {payment.reversedAtUtc || payment.reversedBy ? (
                          <div className="mt-2">
                            <ActorAttribution
                              labelKey="common.reversedBy"
                              actorId={payment.reversedBy}
                              occurredAtUtc={payment.reversedAtUtc}
                              resolved={actors.resolve(payment.reversedBy)}
                              isLoading={actors.isResolving}
                              className="min-h-0"
                              testId={`customer-payment-reversed-by-${payment.repaymentId}`}
                            />
                            {payment.reversalReason ? (
                              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                                {t("common.reason")}: {payment.reversalReason}
                              </p>
                            ) : null}
                          </div>
                        ) : null}
                      </td>
                      <td className="max-w-[16rem] px-3 py-2.5 align-middle text-muted">
                        <span className="line-clamp-2">
                          {payment.remarks?.trim() || "—"}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        ) : null}
      </section>
    </div>
  );
}
