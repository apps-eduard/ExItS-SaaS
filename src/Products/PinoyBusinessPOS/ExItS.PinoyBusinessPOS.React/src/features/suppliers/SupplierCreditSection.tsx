import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  BadgeCheck,
  CheckCircle2,
  CircleDollarSign,
  ClipboardList,
  Eye,
  FileText,
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
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { laterPaymentsAmount } from "@/features/purchasing/receive-payment";
import { resolveBuyerSupplierPaymentCta } from "@/features/suppliers/buyer-supplier-payment-gate";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
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
  return sourceType === "DirectPurchaseReceipt"
    ? "supplierPayables.source.directPurchase"
    : "supplierPayables.source.goodsReceipt";
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

  const workspace = useMemo(
    () =>
      boundWorkspace
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const summaryQuery = useQuery({
    queryKey: ["supplier-payable-summary", workspace?.organizationId, supplierId],
    enabled: Boolean(workspace) && allowView && online,
    queryFn: ({ signal }) => getSupplierPayableSummary(workspace!, supplierId, signal),
  });

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

  const listQuery = useQuery({
    queryKey: ["supplier-payables", workspace?.organizationId, supplierId],
    enabled: Boolean(workspace) && allowView && online,
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
  const paidCount = payables.filter((p) => p.status === "Paid").length;
  const creditPolicy = creditPolicyQuery.data;
  const approvedCreditLimit =
    creditPolicy?.status === "Approved" && creditPolicy.creditLimit != null
      ? creditPolicy.creditLimit
      : null;
  const showApprovedCreditLimit = Boolean(connectedRelationshipId);

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
            <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
              {t("supplierPayables.title")}
            </h2>
            <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
              {t("supplierPayables.summaryLede")}
            </p>
          </div>
        </div>
        {summaryQuery.isLoading || listQuery.isLoading ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
        ) : summaryQuery.isError || listQuery.isError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("supplierPayables.loadFailed")}
          </p>
        ) : (
          <dl
            className={
              showApprovedCreditLimit
                ? "supplier-credit-stats supplier-credit-stats--with-limit m-0"
                : "supplier-credit-stats m-0"
            }
          >
            {showApprovedCreditLimit ? (
              <div className="supplier-credit-stat supplier-credit-stat--limit">
                <dt>
                  <BadgeCheck className="supplier-credit-stat__icon" aria-hidden />
                  {t("supplierPayables.approvedCreditLimit")}
                </dt>
                <dd className="m-0 tabular-nums" data-testid="supplier-credit-approved-limit">
                  {creditPolicyQuery.isLoading ? (
                    t("loading.label")
                  ) : approvedCreditLimit != null ? (
                    <MoneyDisplay amount={approvedCreditLimit} />
                  ) : (
                    "—"
                  )}
                </dd>
              </div>
            ) : null}
            <div className="supplier-credit-stat supplier-credit-stat--outstanding">
              <dt>
                <CircleDollarSign className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.outstanding")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-outstanding">
                <MoneyDisplay amount={summary?.outstandingTotal ?? 0} />
              </dd>
            </div>
            <div className="supplier-credit-stat supplier-credit-stat--overdue">
              <dt>
                <AlertTriangle className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.overdue")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-overdue">
                <MoneyDisplay amount={summary?.overdueTotal ?? 0} />
              </dd>
            </div>
            <div className="supplier-credit-stat supplier-credit-stat--open">
              <dt>
                <ClipboardList className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.openCount")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-open-count">
                {summary?.openCount ?? 0}
              </dd>
            </div>
            <div className="supplier-credit-stat supplier-credit-stat--paid">
              <dt>
                <CheckCircle2 className="supplier-credit-stat__icon" aria-hidden />
                {t("supplierPayables.paidCount")}
              </dt>
              <dd className="m-0 tabular-nums" data-testid="supplier-credit-paid-count">
                {paidCount}
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
        {payables.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="supplier-credit-empty">
            {t("supplierPayables.empty")}
          </p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-3 p-0" data-testid="supplier-credit-list">
            {payables.map((payable) => {
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
                    {t(sourceLabelKey(payable.sourceType))}
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
                <dd className="m-0">{t(sourceLabelKey(detailTarget.sourceType))}</dd>
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
