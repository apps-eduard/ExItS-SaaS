import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  BadgeCheck,
  CircleDollarSign,
  ClipboardList,
  Lock,
  Wallet,
} from "lucide-react";
import { canManagePurchasing, canViewPurchasing } from "@/access/pos-capabilities";
import { getOrganizationOnlineSupplierPaymentsCapability } from "@/api/platform/organization-online-supplier-payments-client";
import { getBusinessCustomerCreditPolicy } from "@/api/pos/pos-business-credit-policy-client";
import { listPaymentMethods } from "@/api/pos/pos-payment-methods-client";
import {
  getSupplierPayableSummary,
  listSupplierPayables,
  type PosSupplierPayableDto,
} from "@/api/pos/pos-supplier-payables-client";
import { Card } from "@/components/ui/card";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { B2bObligationsView } from "@/features/b2b-obligations/B2bObligationsView";
import {
  mapPayableToObligation,
  type B2bObligationItem,
  type B2bObligationListFilter,
} from "@/features/b2b-obligations/b2b-obligations-model";
import {
  computeSupplierCreditExposure,
  filterSupplierPayables,
  formatUtilizationPercent,
} from "@/features/suppliers/supplier-credit-exposure";
import { resolveBuyerSupplierPaymentCta } from "@/features/suppliers/buyer-supplier-payment-gate";
import {
  buyerCreditStatusLabelKey,
  creditPolicyStatusTone,
  resolveBuyerCreditDisplayStatus,
} from "@/features/customers/credit-policy";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function canPayPayable(payable: PosSupplierPayableDto): boolean {
  return (
    (payable.status === "Open" || payable.status === "PartiallyPaid") && payable.balance > 0
  );
}

type SupplierCreditSectionProps = {
  supplierId: string;
  /** Connected B2B relationship — enables approved credit limit from seller policy. */
  connectedRelationshipId?: string | null;
};

export function SupplierCreditSection({
  supplierId,
  connectedRelationshipId = null,
}: SupplierCreditSectionProps) {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowView = canViewPurchasing(sessionGrant);
  const allowManage = canManagePurchasing(sessionGrant);

  const [payableFilter, setPayableFilter] = useState<B2bObligationListFilter>("open");

  const workspace = useMemo(
    () =>
      boundWorkspace
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const creditPolicyQuery = useQuery({
    queryKey: [
      "connected-suppliers",
      "buyer-credit-policy",
      workspace?.organizationId,
      connectedRelationshipId,
    ],
    enabled:
      Boolean(workspace) && allowView && online && Boolean(connectedRelationshipId),
    queryFn: ({ signal }) =>
      getBusinessCustomerCreditPolicy(workspace!, connectedRelationshipId!, signal),
  });

  const payablesReady =
    Boolean(workspace) &&
    allowView &&
    online &&
    (!connectedRelationshipId || creditPolicyQuery.isSuccess);

  const summaryQuery = useQuery({
    queryKey: ["supplier-payable-summary", workspace?.organizationId, supplierId],
    enabled: payablesReady,
    queryFn: ({ signal }) => getSupplierPayableSummary(workspace!, supplierId, signal),
  });

  const listQuery = useQuery({
    queryKey: ["supplier-payables", workspace?.organizationId, supplierId],
    enabled: payablesReady,
    queryFn: ({ signal }) =>
      listSupplierPayables(
        workspace!,
        { supplierId, page: 1, pageSize: 100 },
        signal,
      ),
  });

  const platformPaymentsQuery = useQuery({
    queryKey: ["online-supplier-payments", workspace?.organizationId],
    enabled: Boolean(workspace) && allowView && online,
    queryFn: ({ signal }) =>
      getOrganizationOnlineSupplierPaymentsCapability(workspace!.organizationId, signal),
    staleTime: 60_000,
  });

  const paymentMethodsQuery = useQuery({
    queryKey: ["payment-methods", workspace?.organizationId],
    enabled: Boolean(workspace) && allowView && online,
    queryFn: ({ signal }) => listPaymentMethods(workspace!, signal),
    staleTime: 60_000,
  });

  if (!allowView) {
    return null;
  }

  const payables = listQuery.data?.items ?? [];
  const obligations = payables.map(mapPayableToObligation);
  const summary = summaryQuery.data;
  const creditPolicy = creditPolicyQuery.data;
  const approvedCreditLimit =
    creditPolicy?.status === "Approved" && creditPolicy.creditLimit != null
      ? creditPolicy.creditLimit
      : null;
  const isConnected = Boolean(connectedRelationshipId);
  const outstanding =
    isConnected && typeof creditPolicy?.outstandingAmount === "number"
      ? creditPolicy.outstandingAmount
      : (summary?.outstandingTotal ?? 0);
  const reservedByActivePos =
    isConnected && typeof creditPolicy?.reservedByActivePos === "number"
      ? creditPolicy.reservedByActivePos
      : 0;
  const exposure = computeSupplierCreditExposure({
    approvedCreditLimit,
    outstanding,
    reservedByActivePos,
  });
  const availableDisplay =
    approvedCreditLimit != null && typeof creditPolicy?.availableCredit === "number"
      ? creditPolicy.availableCredit
      : exposure.availableCredit;
  const overdueTotal = summary?.overdueTotal ?? 0;
  const openCount = summary?.openCount ?? filterSupplierPayables(payables, "open").length;

  const availableToneClass =
    availableDisplay == null
      ? ""
      : availableDisplay < -1e-9 || exposure.isOverLimit
        ? "supplier-credit-stat--available-danger"
        : "supplier-credit-stat--available";

  const utilizationCaption =
    exposure.hasApprovedLimit &&
    exposure.utilizationPercent != null &&
    approvedCreditLimit != null
      ? t("supplierPayables.usedOfLimit")
          .replace("{percent}", formatUtilizationPercent(exposure.utilizationPercent))
          .replace("{limit}", formatPeso(approvedCreditLimit))
      : t("supplierPayables.utilizationUnavailable");

  const buyerCreditDisplayStatus = isConnected
    ? resolveBuyerCreditDisplayStatus({
        status: creditPolicy?.status,
        hasEverBeenApproved: creditPolicy?.hasEverBeenApproved,
        buyerDisplayStatus: creditPolicy?.buyerDisplayStatus,
      })
    : "Unavailable";

  const platformCapability = platformPaymentsQuery.data;
  const paymentMethods = paymentMethodsQuery.data;
  const showOnlineUnavailableBanner =
    allowManage &&
    online &&
    platformCapability?.status === "Available" &&
    resolveBuyerSupplierPaymentCta({
      platformCapability,
      paymentMethods,
      payableEligible: true,
      allowManage,
      online,
    }) === "unavailable";

  const payableById = new Map(payables.map((p) => [p.payableId, p]));

  function paymentCtaFor(item: B2bObligationItem) {
    const payable = payableById.get(item.id);
    if (!payable) {
      return "hidden" as const;
    }
    return resolveBuyerSupplierPaymentCta({
      platformCapability,
      paymentMethods,
      payableEligible: canPayPayable(payable),
      allowManage,
      online,
    });
  }

  return (
    <div className="flex min-w-0 flex-col gap-3" data-testid="supplier-credit-section">
      <Card className="supplier-credit-card">
        <div className="supplier-credit-card__header">
          <span className="supplier-credit-card__header-icon" aria-hidden>
            <Wallet className="size-4" />
          </span>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
                {t("supplierPayables.title")}
              </h2>
              {isConnected ? (
                <span data-testid="supplier-credit-buyer-status">
                  <StatusChip tone={creditPolicyStatusTone(buyerCreditDisplayStatus)}>
                    {t(buyerCreditStatusLabelKey(buyerCreditDisplayStatus))}
                  </StatusChip>
                </span>
              ) : null}
            </div>
            <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
              {t("supplierPayables.summaryLede")}
            </p>
          </div>
        </div>
        {summaryQuery.isLoading || listQuery.isLoading || (isConnected && creditPolicyQuery.isLoading) ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
        ) : summaryQuery.isError || listQuery.isError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("supplierPayables.loadFailed")}
          </p>
        ) : (
          <dl className="supplier-credit-stats supplier-credit-stats--v2 m-0">
            <div className="supplier-credit-stat supplier-credit-stat--limit">
              <dt>
                <BadgeCheck className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.approvedCreditLimit")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-approved-limit">
                {approvedCreditLimit != null ? (
                  <MoneyDisplay amount={approvedCreditLimit} />
                ) : (
                  "—"
                )}
              </dd>
            </div>
            <div className="supplier-credit-stat supplier-credit-stat--outstanding">
              <dt>
                <CircleDollarSign className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.outstanding")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-outstanding">
                <MoneyDisplay amount={outstanding} />
              </dd>
            </div>
            <div className="supplier-credit-stat supplier-credit-stat--overdue">
              <dt>
                <AlertTriangle className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.overdue")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-overdue">
                <MoneyDisplay amount={overdueTotal} />
              </dd>
            </div>
            <div className="supplier-credit-stat supplier-credit-stat--reserved">
              <dt>
                <Lock className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.reservedActivePos")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-reserved">
                {isConnected ? <MoneyDisplay amount={reservedByActivePos} /> : "—"}
              </dd>
            </div>
            <div
              className={`supplier-credit-stat supplier-credit-stat--available-card ${availableToneClass}`}
            >
              <dt>
                <Wallet className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.availableCredit")}
              </dt>
              <div className="supplier-credit-available__amount-row">
                <dd className="m-0 tabular-nums" data-testid="supplier-credit-available">
                  {availableDisplay != null ? <MoneyDisplay amount={availableDisplay} /> : "—"}
                </dd>
                <p
                  className="supplier-credit-available__caption m-0"
                  data-testid="supplier-credit-utilization-caption"
                >
                  {utilizationCaption}
                </p>
              </div>
              {exposure.progressPercent != null ? (
                <div
                  className="supplier-credit-available__track"
                  role="progressbar"
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-valuenow={Math.round(exposure.progressPercent)}
                  aria-label={t("supplierPayables.utilizationProgress")}
                  data-testid="supplier-credit-utilization-bar"
                  data-over-limit={exposure.isOverLimit ? "true" : "false"}
                >
                  <span
                    className="supplier-credit-available__fill"
                    style={{ width: `${exposure.progressPercent}%` }}
                  />
                </div>
              ) : null}
            </div>
            <div className="supplier-credit-stat supplier-credit-stat--open">
              <dt>
                <ClipboardList className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.openCount")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-open-count">
                {openCount}
              </dd>
            </div>
          </dl>
        )}
      </Card>

      {showOnlineUnavailableBanner ? (
        <p
          className="m-0 rounded-md border border-border px-3 py-2 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="supplier-credit-online-unavailable"
        >
          {t("supplierPayables.onlinePaymentUnavailable")}
        </p>
      ) : null}

      <B2bObligationsView
        perspective="payable"
        items={obligations}
        isLoading={listQuery.isLoading}
        isError={listQuery.isError}
        filter={payableFilter}
        onFilterChange={setPayableFilter}
        resolvePayNow={paymentCtaFor}
        testIdPrefix="supplier-credit"
      />
    </div>
  );
}
