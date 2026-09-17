import { useCallback, useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { BarChart3, Check, CreditCard, FileText, Minus, Receipt } from "lucide-react";
import { canInviteOrganizationStaff, canManageStoreAreas } from "@/access/pos-capabilities";
import { getBranchCapacity } from "@/api/platform/organization-branches-client";
import type { OrganizationCurrentPlanDto } from "@/api/platform/organization-current-plan-client";
import { getOrganizationCurrentPlan } from "@/api/platform/organization-current-plan-client";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import { listOrganizationMembers } from "@/api/platform/organization-members-client";
import { listOrganizationSaasPayments } from "@/api/platform/organization-saas-payments-client";
import { getPosDeviceCapacity } from "@/api/platform/pos-devices-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsTabs, type ExitsTabItem } from "@/components/exits/ExitsTabs";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { AdminUsageMeter } from "@/features/admin/AdminUsageMeter";
import { PlanSubscriptionChip } from "@/features/admin/PlanSubscriptionChip";
import { OrgPlanChangePanel } from "@/features/organization/subscription/OrgPlanChangePanel";
import { isSimulatedOrganizationBilling } from "@/features/organization/subscription/organization-billing-mode";
import {
  CAPACITY_LABEL_KEYS,
  PLAN_FEATURE_LABEL_KEYS,
  SUBSCRIPTION_TABS,
  SUBSCRIPTION_TAB_LABEL_KEYS,
  buildCapacityUsage,
  buildPlanFeatureRows,
  formatSubscriptionDate,
  parseSubscriptionTab,
  resolveNextPaymentDate,
  resolveSubscriptionStatusTone,
  type SubscriptionTabId,
} from "@/features/organization/subscription/subscription-billing-view";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const COMMERCIAL_STALE_TIME = 60_000;

/**
 * Organization Subscription & Billing (Owner-only, `/org/subscription`).
 * Plan changes use Platform commercial APIs; payment execution is Simulated
 * via Local Validation — subscription and entitlement domain stay real.
 */
export function OrgSubscriptionBillingPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant, refreshSessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const canManage = canInviteOrganizationStaff(sessionGrant);
  const areasEntitled = canManageStoreAreas(sessionGrant);
  const simulatedBilling = isSimulatedOrganizationBilling();

  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = parseSubscriptionTab(searchParams.get("tab"));

  const goToTab = useCallback(
    (tab: SubscriptionTabId) => {
      const next = new URLSearchParams(searchParams);
      if (tab === "overview") {
        next.delete("tab");
      } else {
        next.set("tab", tab);
      }
      setSearchParams(next, { replace: true });
    },
    [searchParams, setSearchParams],
  );

  const enabled = Boolean(organizationId && canManage);

  const currentPlanQuery = useQuery({
    queryKey: ["org-subscription", "current-plan", organizationId],
    enabled,
    staleTime: COMMERCIAL_STALE_TIME,
    queryFn: async ({ signal }) => {
      const result = await getOrganizationCurrentPlan(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? "current-plan");
      }
      return result.value;
    },
  });

  const branchCapacityQuery = useQuery({
    queryKey: ["org-subscription", "branch-capacity", organizationId],
    enabled,
    staleTime: COMMERCIAL_STALE_TIME,
    queryFn: async ({ signal }) => {
      const result = await getBranchCapacity(organizationId!, signal);
      return result.ok ? result.value : null;
    },
  });

  const deviceCapacityQuery = useQuery({
    queryKey: ["org-subscription", "device-capacity", organizationId],
    enabled,
    staleTime: COMMERCIAL_STALE_TIME,
    queryFn: async ({ signal }) => {
      const result = await getPosDeviceCapacity(organizationId!, signal);
      return result.ok ? result.value : null;
    },
  });

  const areaCapacityQuery = useQuery({
    queryKey: ["org-subscription", "area-capacity", organizationId],
    enabled: enabled && areasEntitled,
    staleTime: COMMERCIAL_STALE_TIME,
    queryFn: async ({ signal }) => {
      const result = await listOrganizationAreas(organizationId!, signal);
      return result.ok ? result.value : null;
    },
  });

  const activeStaffQuery = useQuery({
    queryKey: ["org-subscription", "active-staff", organizationId],
    enabled,
    staleTime: COMMERCIAL_STALE_TIME,
    queryFn: async () => {
      const result = await listOrganizationMembers(organizationId!, "Active");
      return result.ok ? result.members.length : null;
    },
  });

  const paymentsQuery = useQuery({
    queryKey: ["org-subscription", "saas-payments", organizationId],
    enabled: enabled && (activeTab === "invoices" || activeTab === "billing"),
    staleTime: 0,
    queryFn: async ({ signal }) => {
      const result = await listOrganizationSaasPayments(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? "saas-payments");
      }
      return result.value;
    },
  });

  const data: OrganizationCurrentPlanDto | undefined = currentPlanQuery.data;
  const subscription = data?.currentSubscription ?? null;
  const currentPlan = data?.currentPlan ?? null;
  const pendingChange = data?.pendingPlanChange ?? null;

  const capacityRows = useMemo(() => {
    const areas = areaCapacityQuery.data;
    return buildCapacityUsage({
      branches: branchCapacityQuery.data ?? null,
      staff:
        activeStaffQuery.data != null && currentPlan
          ? { used: activeStaffQuery.data, allowed: currentPlan.maxActiveStaff }
          : null,
      devices: deviceCapacityQuery.data ?? null,
      areas: areas ? { used: areas.activeAreaCount, allowed: areas.maxAreas } : null,
    });
  }, [
    activeStaffQuery.data,
    areaCapacityQuery.data,
    branchCapacityQuery.data,
    currentPlan,
    deviceCapacityQuery.data,
  ]);

  const atLimitRows = capacityRows.filter((row) => row.atLimit);
  const nearLimitRows = capacityRows.filter((row) => row.nearLimit);

  async function handlePlanChangeCompleted() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["org-subscription"] }),
      refreshSessionGrant(),
    ]);
  }

  if (!canManage) {
    return (
      <div className="exits-page mx-auto flex w-full max-w-[1100px] flex-col gap-4">
        <PageHeader title={t("orgSubscription.title")} description={t("orgSubscription.lede")} />
        <Notice tone="warning" title={t("orgSubscription.deniedTitle")} testId="org-subscription-denied">
          {t("orgSubscription.deniedDetail")}
        </Notice>
      </div>
    );
  }

  const statusLabel = subscription?.status ?? t("orgSubscription.notAvailable");
  const nextPaymentDate = formatSubscriptionDate(resolveNextPaymentDate(subscription));
  const cycleLabel = subscription?.billingCycle ?? t("orgSubscription.notAvailable");

  const summaryRows = (
    <dl className="grid gap-3 sm:grid-cols-2" data-testid="org-subscription-summary">
      <div className="flex flex-col gap-1">
        <dt className="text-[length:var(--exits-text-xs)] text-muted">
          {t("orgSubscription.plan")}
        </dt>
        <dd className="m-0" data-testid="org-subscription-plan">
          {currentPlan?.displayName ? (
            <PlanSubscriptionChip
              planKey={currentPlan.planKey}
              planDisplayName={currentPlan.displayName}
            />
          ) : (
            t("orgSubscription.notAvailable")
          )}
        </dd>
      </div>
      <div className="flex flex-col gap-1">
        <dt className="text-[length:var(--exits-text-xs)] text-muted">
          {t("orgSubscription.status")}
        </dt>
        <dd className="m-0">
          <StatusChip
            tone={resolveSubscriptionStatusTone(subscription?.status)}
            shape="auto"
            data-testid="org-subscription-status"
          >
            {statusLabel}
          </StatusChip>
        </dd>
      </div>
      <div className="flex flex-col gap-1">
        <dt className="text-[length:var(--exits-text-xs)] text-muted">
          {t("orgSubscription.billingCycle")}
        </dt>
        <dd className="m-0" data-testid="org-subscription-cycle">
          {cycleLabel}
        </dd>
      </div>
      <div className="flex flex-col gap-1">
        <dt className="text-[length:var(--exits-text-xs)] text-muted">
          {t("orgSubscription.nextPayment")}
        </dt>
        <dd className="m-0" data-testid="org-subscription-next-payment">
          {nextPaymentDate ?? t("orgSubscription.notAvailable")}
        </dd>
      </div>
      {subscription?.agreedPrice != null ? (
        <div className="flex flex-col gap-1">
          <dt className="text-[length:var(--exits-text-xs)] text-muted">
            {t("orgSubscription.amount")}
          </dt>
          <dd className="m-0">
            <MoneyDisplay amount={subscription.agreedPrice} testId="org-subscription-amount" />
          </dd>
        </div>
      ) : null}
      {simulatedBilling ? (
        <div className="flex flex-col gap-1">
          <dt className="text-[length:var(--exits-text-xs)] text-muted">
            {t("orgSubscription.billingModeLabel")}
          </dt>
          <dd className="m-0" data-testid="org-subscription-billing-mode-summary">
            {t("orgSubscription.billingModeSimulated")}
          </dd>
        </div>
      ) : null}
    </dl>
  );

  const pendingChangeNotice = pendingChange ? (
    <Notice tone="info" title={t("orgSubscription.pendingChange")} testId="org-subscription-pending-change">
      {(pendingChange.effectiveAtUtc
        ? t("orgSubscription.pendingChangeDetail").replace(
            "{date}",
            formatSubscriptionDate(pendingChange.effectiveAtUtc) ?? "",
          )
        : t("orgSubscription.pendingChangeDetailNoDate")
      ).replace("{plan}", pendingChange.displayName ?? pendingChange.planKey ?? "")}
    </Notice>
  ) : null;

  const usageMeters =
    capacityRows.length > 0 ? (
      <ul className="admin-plan-usage__meters m-0 list-none p-0" data-testid="org-subscription-usage">
        {capacityRows.map((row) => (
          <AdminUsageMeter
            key={row.dimension}
            label={t(CAPACITY_LABEL_KEYS[row.dimension])}
            used={row.used}
            allowed={row.allowed}
            testId={`org-subscription-usage-${row.dimension}`}
          />
        ))}
      </ul>
    ) : null;

  const overviewPanel = (
    <div className="flex flex-col gap-4">
      {pendingChangeNotice}
      {summaryRows}
      {usageMeters ? (
        <section className="flex flex-col gap-2">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("orgSubscription.usageTitle")}
          </h3>
          {usageMeters}
        </section>
      ) : null}
      <div className="flex flex-wrap gap-2">
        <Button
          intent="primary"
          onClick={() => goToTab("plan")}
          data-testid="org-subscription-goto-plan"
        >
          {t("orgSubscription.action.changePlan")}
        </Button>
        <Button
          intent="neutral"
          appearance="outline"
          onClick={() => goToTab("billing")}
          data-testid="org-subscription-goto-billing"
        >
          {t("orgSubscription.action.manageBilling")}
        </Button>
      </div>
    </div>
  );

  const featureRows = buildPlanFeatureRows(currentPlan);

  const planPanel = (
    <div className="flex flex-col gap-4">
      {atLimitRows.length > 0 ? (
        <Notice
          tone="warning"
          title={t("orgSubscription.atLimitTitle")}
          testId="org-subscription-at-limit"
        >
          {t("orgSubscription.atLimitDetail")}
        </Notice>
      ) : nearLimitRows.length > 0 ? (
        <Notice
          tone="info"
          title={t("orgSubscription.nearLimitTitle")}
          testId="org-subscription-near-limit"
        >
          {t("orgSubscription.nearLimitDetail")}
        </Notice>
      ) : null}

      {featureRows.length > 0 ? (
        <section className="flex flex-col gap-2">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("orgSubscription.featuresTitle")}
          </h3>
          <ul className="m-0 flex list-none flex-col gap-1 p-0" data-testid="org-subscription-features">
            {featureRows.map((row) => (
              <li
                key={row.id}
                className="flex items-center gap-2 text-[length:var(--exits-text-sm)]"
                data-testid={`org-subscription-feature-${row.id}`}
                data-included={row.included ? "true" : "false"}
              >
                {row.included ? (
                  <Check className="size-4 shrink-0 text-[var(--exits-success)]" aria-hidden />
                ) : (
                  <Minus className="size-4 shrink-0 text-muted" aria-hidden />
                )}
                <span className={row.included ? undefined : "text-muted"}>
                  {t(PLAN_FEATURE_LABEL_KEYS[row.id])}
                </span>
                <span className="text-[length:var(--exits-text-xs)] text-muted">
                  {row.included
                    ? t("orgSubscription.featureIncluded")
                    : t("orgSubscription.featureUnavailable")}
                </span>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {usageMeters ? (
        <section className="flex flex-col gap-2">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("orgSubscription.limitsTitle")}
          </h3>
          {usageMeters}
        </section>
      ) : null}

      {organizationId && subscription ? (
        <OrgPlanChangePanel
          organizationId={organizationId}
          subscription={subscription}
          currentPlan={currentPlan}
          availablePlans={data?.availablePlans ?? []}
          onCompleted={handlePlanChangeCompleted}
        />
      ) : (
        <Notice tone="info">{t("orgSubscription.noSubscriptionDetail")}</Notice>
      )}
    </div>
  );

  const billingPanel = (
    <div className="flex flex-col gap-4">
      {summaryRows}
      <Notice
        tone="info"
        title={t("orgSubscription.billingManagedTitle")}
        testId="org-subscription-billing-managed"
      >
        {t("orgSubscription.billingManagedDetail")}
      </Notice>
      {paymentsQuery.isLoading ? <LoadingState label={t("orgSubscription.loading")} /> : null}
      {paymentsQuery.data && paymentsQuery.data.items.length > 0 ? (
        <section className="flex flex-col gap-2" data-testid="org-subscription-billing-history">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("orgSubscription.historyTitle")}
          </h3>
          <PaymentHistoryList items={paymentsQuery.data.items} />
        </section>
      ) : null}
    </div>
  );

  const invoicesPanel = (
    <div className="flex flex-col gap-4">
      {paymentsQuery.isLoading ? <LoadingState label={t("orgSubscription.loading")} /> : null}
      {paymentsQuery.isError ? (
        <ErrorState
          title={t("orgSubscription.loadError")}
          detail={t("orgSubscription.loadErrorDetail")}
        />
      ) : null}
      {paymentsQuery.data && paymentsQuery.data.items.length === 0 ? (
        <EmptyState
          title={t("orgSubscription.invoicesEmptyTitle")}
          detail={t("orgSubscription.invoicesEmptyDetail")}
          icon={<Receipt className="size-5" aria-hidden />}
          testId="org-subscription-invoices-empty"
        />
      ) : null}
      {paymentsQuery.data && paymentsQuery.data.items.length > 0 ? (
        <section className="flex flex-col gap-2" data-testid="org-subscription-invoices">
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("orgSubscription.invoicesTitle")}
          </h3>
          <PaymentHistoryList items={paymentsQuery.data.items} />
        </section>
      ) : null}
    </div>
  );

  const tabItems: ExitsTabItem[] = SUBSCRIPTION_TABS.map((tab) => ({
    key: tab,
    label: t(SUBSCRIPTION_TAB_LABEL_KEYS[tab]),
    icon:
      tab === "overview"
        ? CreditCard
        : tab === "plan"
          ? BarChart3
          : tab === "billing"
            ? Receipt
            : FileText,
    testId: `org-subscription-tab-${tab}`,
  }));

  return (
    <div
      className="exits-page mx-auto flex w-full max-w-[1100px] min-w-0 flex-col gap-4"
      data-testid="org-subscription-page"
    >
      <PageHeader
        title={t("orgSubscription.title")}
        description={t("orgSubscription.lede")}
        subtitle={boundWorkspace?.organizationDisplayName}
      />

      {currentPlanQuery.isLoading ? <LoadingState label={t("orgSubscription.loading")} /> : null}
      {currentPlanQuery.isError ? (
        <ErrorState
          title={t("orgSubscription.loadError")}
          detail={t("orgSubscription.loadErrorDetail")}
        />
      ) : null}

      {data && !subscription && !currentPlan ? (
        <EmptyState
          title={t("orgSubscription.noSubscription")}
          detail={t("orgSubscription.noSubscriptionDetail")}
          testId="org-subscription-none"
        />
      ) : null}

      {data && (subscription || currentPlan) ? (
        <ExitsTabs
          variant="underline"
          items={tabItems}
          value={activeTab}
          onValueChange={(key) => goToTab(parseSubscriptionTab(key))}
          ariaLabel={t("orgSubscription.tabsAria")}
          testId="org-subscription-tabs"
          panels={{
            overview: overviewPanel,
            plan: planPanel,
            billing: billingPanel,
            invoices: invoicesPanel,
          }}
        />
      ) : null}
    </div>
  );
}

function PaymentHistoryList({
  items,
}: {
  items: ReadonlyArray<{
    id: string | null;
    amount: number | null;
    status: string | null;
    method: string | null;
    externalReference: string | null;
    paidAtUtc: string | null;
    createdAtUtc: string | null;
  }>;
}) {
  const { t } = useI18n();
  return (
    <ul className="m-0 flex list-none flex-col gap-2 p-0">
      {items.map((item) => (
        <li
          key={item.id ?? item.externalReference ?? `${item.createdAtUtc}-${item.amount}`}
          className="flex flex-wrap items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-2"
          data-testid="org-subscription-invoice-row"
        >
          <div className="flex min-w-0 flex-col gap-1">
            <span className="font-medium">
              {item.amount != null ? (
                <MoneyDisplay amount={item.amount} />
              ) : (
                t("orgSubscription.notAvailable")
              )}
            </span>
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {formatSubscriptionDate(item.paidAtUtc ?? item.createdAtUtc) ??
                t("orgSubscription.notAvailable")}
              {item.externalReference ? ` · ${item.externalReference}` : null}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <StatusChip tone="info" shape="auto">
              {t("orgSubscription.invoiceSimulated")}
            </StatusChip>
            <StatusChip
              tone={item.status?.toLowerCase() === "confirmed" ? "success" : "neutral"}
              shape="auto"
            >
              {item.status ?? t("orgSubscription.notAvailable")}
            </StatusChip>
          </div>
        </li>
      ))}
    </ul>
  );
}
