import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  BadgeCheck,
  CircleDollarSign,
  ClipboardList,
  Eye,
  FileText,
  Lock,
  Wallet,
  X,
} from "lucide-react";
import { canManagePurchasing, canViewPurchasing } from "@/access/pos-capabilities";
import { getOrganizationOnlineSupplierPaymentsCapability } from "@/api/platform/organization-online-supplier-payments-client";
import { getBusinessCustomerCreditPolicy } from "@/api/pos/pos-business-credit-policy-client";
import { listPaymentMethods } from "@/api/pos/pos-payment-methods-client";
import {
  getSupplierPayableSummary,
  listSupplierPayablePayments,
  listSupplierPayables,
  type PosSupplierPayableDto,
  type PosSupplierPayablePaymentDto,
} from "@/api/pos/pos-supplier-payables-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ExitsChipBar, type ExitsChipItem } from "@/components/exits/ExitsChipBar";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { laterPaymentsAmount } from "@/features/purchasing/receive-payment";
import {
  computeSupplierCreditExposure,
  countSupplierPayablesByFilter,
  filterSupplierPayables,
  formatUtilizationPercent,
  type SupplierPayableListFilter,
} from "@/features/suppliers/supplier-credit-exposure";
import { resolveBuyerSupplierPaymentCta } from "@/features/suppliers/buyer-supplier-payment-gate";
import {
  buyerCreditStatusLabelKey,
  creditPolicyStatusTone,
  resolveBuyerCreditDisplayStatus,
} from "@/features/customers/credit-policy";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function payableStatusTone(
  status: string,
  isOverdue: boolean,
): "success" | "warning" | "info" | "danger" {
  if (status === "Paid") {
    return "success";
  }
  if (status === "Voided") {
    return "warning";
  }
  if (isOverdue) {
    return "danger";
  }
  if (status === "PartiallyPaid") {
    return "info";
  }
  return "warning";
}

function statusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Open":
      return "supplierPayables.status.open";
    case "PartiallyPaid":
      return "supplierPayables.status.partiallyPaid";
    case "Paid":
      return "supplierPayables.status.paid";
    case "Voided":
      return "supplierPayables.status.voided";
    default:
      return "supplierPayables.status.open";
  }
}

function methodLabelKey(method: string): MessageKey {
  switch (method) {
    case "BankTransfer":
      return "supplierPayables.method.bankTransfer";
    case "GCash":
      return "supplierPayables.method.gcash";
    case "Other":
      return "supplierPayables.method.other";
    default:
      return "supplierPayables.method.cash";
  }
}

function sourceLabelKey(sourceType: string): MessageKey {
  if (sourceType === "DirectPurchaseReceipt") {
    return "supplierPayables.source.directPurchase";
  }
  if (sourceType === "Sale") {
    return "supplierPayables.source.sale";
  }
  return "supplierPayables.source.goodsReceipt";
}

function formatPayableSourceLabel(
  payable: Pick<PosSupplierPayableDto, "sourceType" | "sourceReference">,
  t: (key: MessageKey) => string,
): string {
  const reference = payable.sourceReference?.trim();
  if (reference) {
    return reference;
  }
  return t(sourceLabelKey(payable.sourceType));
}

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

  const [detailTarget, setDetailTarget] = useState<PosSupplierPayableDto | null>(null);
  const [payableFilter, setPayableFilter] = useState<SupplierPayableListFilter>("open");

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

  // Connected buyers: wait for credit-policy GET (heals missing Direct Purchase payables) before listing.
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
        { supplierId, page: 1, pageSize: 50 },
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

  const historyPayableId = detailTarget?.payableId;

  const paymentsQuery = useQuery({
    queryKey: ["supplier-payable-payments", workspace?.organizationId, historyPayableId],
    enabled: Boolean(workspace) && allowView && online && Boolean(historyPayableId),
    queryFn: ({ signal }) => listSupplierPayablePayments(workspace!, historyPayableId!, signal),
  });

  if (!allowView) {
    return null;
  }

  const payables = listQuery.data?.items ?? [];
  const summary = summaryQuery.data;
  const creditPolicy = creditPolicyQuery.data;
  const approvedCreditLimit =
    creditPolicy?.status === "Approved" && creditPolicy.creditLimit != null
      ? creditPolicy.creditLimit
      : null;
  const isConnected = Boolean(connectedRelationshipId);
  // Prefer canonical business credit outstanding when connected; payable summary otherwise.
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
  // Prefer server available when approved; fall back to derived exposure.
  const availableDisplay =
    approvedCreditLimit != null && typeof creditPolicy?.availableCredit === "number"
      ? creditPolicy.availableCredit
      : exposure.availableCredit;
  const overdueTotal = summary?.overdueTotal ?? 0;
  const openCount = summary?.openCount ?? filterSupplierPayables(payables, "open").length;
  const filterCounts = countSupplierPayablesByFilter(payables);
  const filteredPayables = filterSupplierPayables(payables, payableFilter);

  const availableToneClass =
    availableDisplay == null
      ? ""
      : availableDisplay < -1e-9 || exposure.isOverLimit
        ? "supplier-credit-stat--available-danger"
        : "supplier-credit-stat--available";

  const payableFilterChips: ExitsChipItem[] = [
    {
      key: "open",
      label: t("supplierPayables.filter.open"),
      count: filterCounts.open,
      state: payableFilter === "open" ? "active" : "idle",
      testId: "supplier-credit-filter-open",
      onSelect: () => setPayableFilter("open"),
    },
    {
      key: "overdue",
      label: t("supplierPayables.filter.overdue"),
      count: filterCounts.overdue,
      state: payableFilter === "overdue" ? "active" : "idle",
      testId: "supplier-credit-filter-overdue",
      onSelect: () => setPayableFilter("overdue"),
    },
    {
      key: "paid",
      label: t("supplierPayables.filter.paid"),
      count: filterCounts.paid,
      state: payableFilter === "paid" ? "active" : "idle",
      testId: "supplier-credit-filter-paid",
      onSelect: () => setPayableFilter("paid"),
    },
    {
      key: "all",
      label: t("supplierPayables.filter.all"),
      count: filterCounts.all,
      state: payableFilter === "all" ? "active" : "idle",
      testId: "supplier-credit-filter-all",
      onSelect: () => setPayableFilter("all"),
    },
  ];

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

  function paymentCtaFor(payable: PosSupplierPayableDto) {
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

      <Card className="supplier-credit-card">
        <div className="supplier-credit-card__header">
          <span className="supplier-credit-card__header-icon" aria-hidden>
            <FileText className="size-4" />
          </span>
          <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("supplierPayables.listTitle")}
          </h3>
        </div>
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("supplierPayables.filterAria")}
          testId="supplier-credit-payable-filters"
          items={payableFilterChips}
        />
        {filteredPayables.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="supplier-credit-empty">
            {payableFilter === "open"
              ? t("supplierPayables.emptyOpen")
              : t("supplierPayables.empty")}
          </p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-3 p-0" data-testid="supplier-credit-list">
            {filteredPayables.map((payable) => {
              const later = laterPaymentsAmount(payable.paidAmount, payable.paidAtReceiptAmount);
              const cta = paymentCtaFor(payable);
              return (
                <li
                  key={payable.payableId}
                  className="supplier-payable-item"
                  data-testid={`supplier-payable-${payable.payableId}`}
                  data-status={payable.status}
                >
                  <div className="flex flex-wrap items-center gap-2">
                    <StatusChip tone={payableStatusTone(payable.status, payable.isOverdue)}>
                      {t(statusLabelKey(payable.status))}
                    </StatusChip>
                    {payable.isOverdue && payable.status !== "Voided" && payable.status !== "Paid" ? (
                      <StatusChip tone="danger">{t("supplierPayables.overdue")}</StatusChip>
                    ) : null}
                  </div>
                  <p className="mt-2 mb-1 text-[length:var(--exits-text-sm)] text-muted">
                    {formatPayableSourceLabel(payable, t)}
                    {payable.createdAtUtc
                      ? ` · ${new Date(payable.createdAtUtc).toLocaleDateString()}`
                      : ""}
                  </p>
                  <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2 lg:grid-cols-3">
                    <div>
                      <dt className="text-muted">{t("supplierPayables.originalAmount")}</dt>
                      <dd className="m-0 tabular-nums">
                        <MoneyDisplay amount={payable.originalAmount} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.paidAtReceipt")}</dt>
                      <dd className="m-0 tabular-nums">
                        <MoneyDisplay amount={payable.paidAtReceiptAmount} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.laterPayments")}</dt>
                      <dd className="m-0 tabular-nums">
                        <MoneyDisplay amount={later} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.totalPaid")}</dt>
                      <dd className="m-0 tabular-nums">
                        <MoneyDisplay amount={payable.paidAmount} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.balance")}</dt>
                      <dd className="m-0 font-semibold tabular-nums">
                        <MoneyDisplay amount={payable.balance} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.dueDate")}</dt>
                      <dd className="m-0">{payable.dueDate?.trim() || "—"}</dd>
                    </div>
                  </dl>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {cta === "pay_now" ? (
                      <Button
                        type="button"
                        className="supplier-detail-action-btn"
                        data-testid={`supplier-payable-pay-now-${payable.payableId}`}
                        disabled
                        title={t("supplierPayables.payNowNotReady")}
                      >
                        <Wallet className="size-4 shrink-0" aria-hidden />
                        {t("supplierPayables.payNow")}
                      </Button>
                    ) : null}
                    <Button
                      type="button"
                      variant="outline"
                      className="supplier-detail-action-btn"
                      data-testid={`supplier-payable-detail-${payable.payableId}`}
                      onClick={() => setDetailTarget(payable)}
                    >
                      <Eye className="size-4 shrink-0" aria-hidden />
                      {t("supplierPayables.viewDetails")}
                    </Button>
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </Card>

      {detailTarget ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="supplier-payable-detail-title"
          data-testid="supplier-payable-detail-dialog"
        >
          <Card className="max-h-[90vh] w-full max-w-lg overflow-y-auto">
            <h2
              id="supplier-payable-detail-title"
              className="m-0 mb-3 text-[length:var(--exits-text-base)] font-semibold"
            >
              {t("supplierPayables.detailTitle")}
            </h2>
            <div className="mb-3 flex flex-wrap gap-2">
              <StatusChip tone={payableStatusTone(detailTarget.status, detailTarget.isOverdue)}>
                {t(statusLabelKey(detailTarget.status))}
              </StatusChip>
            </div>
            <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
              <div>
                <dt className="text-muted">{t("supplierPayables.source")}</dt>
                <dd className="m-0">{formatPayableSourceLabel(detailTarget, t)}</dd>
              </div>
              <div>
                <dt className="text-muted">{t("supplierPayables.receiptDate")}</dt>
                <dd className="m-0">
                  {new Date(detailTarget.createdAtUtc).toLocaleDateString()}
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("supplierPayables.originalAmount")}</dt>
                <dd className="m-0">
                  <MoneyDisplay amount={detailTarget.originalAmount} />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("supplierPayables.paidAtReceipt")}</dt>
                <dd className="m-0">
                  <MoneyDisplay amount={detailTarget.paidAtReceiptAmount} />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("supplierPayables.laterPayments")}</dt>
                <dd className="m-0">
                  <MoneyDisplay
                    amount={laterPaymentsAmount(
                      detailTarget.paidAmount,
                      detailTarget.paidAtReceiptAmount,
                    )}
                  />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("supplierPayables.balance")}</dt>
                <dd className="m-0">
                  <MoneyDisplay amount={detailTarget.balance} />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("supplierPayables.dueDate")}</dt>
                <dd className="m-0">{detailTarget.dueDate?.trim() || "—"}</dd>
              </div>
              {detailTarget.voidReason?.trim() ? (
                <div className="sm:col-span-2">
                  <dt className="text-muted">{t("supplierPayables.voidReason")}</dt>
                  <dd className="m-0">{detailTarget.voidReason}</dd>
                </div>
              ) : null}
            </dl>

            <h3 className="mb-2 mt-4 text-[length:var(--exits-text-sm)] font-semibold">
              {t("supplierPayables.paymentHistory")}
            </h3>
            {paymentsQuery.isLoading ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("loading.label")}
              </p>
            ) : (paymentsQuery.data?.length ?? 0) === 0 ? (
              <p
                className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="supplier-payable-no-payments"
              >
                {t("supplierPayables.noPayments")}
              </p>
            ) : (
              <ul
                className="m-0 flex list-none flex-col gap-2 p-0"
                data-testid="supplier-payable-payment-history"
              >
                {(paymentsQuery.data as PosSupplierPayablePaymentDto[]).map((payment) => (
                  <li
                    key={payment.paymentId}
                    className="rounded-md border border-border p-2 text-[length:var(--exits-text-sm)]"
                    data-testid={`supplier-payment-row-${payment.paymentId}`}
                  >
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <MoneyDisplay amount={payment.amount} />
                      <span className="text-muted">{t(methodLabelKey(payment.paymentMethod))}</span>
                    </div>
                    <p className="m-0 mt-1 text-muted">
                      {new Date(payment.paidAtUtc).toLocaleString()}
                    </p>
                    {payment.reference?.trim() ? (
                      <p className="m-0 mt-1">{payment.reference}</p>
                    ) : null}
                    {payment.notes?.trim() ? (
                      <p className="m-0 mt-1 text-muted">{payment.notes}</p>
                    ) : null}
                  </li>
                ))}
              </ul>
            )}
            <div className="mt-4 flex flex-wrap gap-2">
              {paymentCtaFor(detailTarget) === "pay_now" ? (
                <Button
                  type="button"
                  className="supplier-detail-action-btn"
                  data-testid="supplier-payable-detail-pay-now"
                  disabled
                  title={t("supplierPayables.payNowNotReady")}
                >
                  <Wallet className="size-4 shrink-0" aria-hidden />
                  {t("supplierPayables.payNow")}
                </Button>
              ) : null}
              <Button
                type="button"
                variant="ghost"
                onClick={() => setDetailTarget(null)}
                data-testid="supplier-payable-detail-close"
              >
                <X className="size-4 shrink-0" aria-hidden />
                {t("supplierPayables.cancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
