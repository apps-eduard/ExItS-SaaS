import { useEffect, useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManagePurchasing, canViewPurchasing } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getSupplierPayableSummary,
  listSupplierPayablePayments,
  listSupplierPayables,
  recordSupplierPayablePayment,
  SUPPLIER_PAYABLE_PAYMENT_METHODS,
  SUPPLIER_PAYABLE_PAYMENT_NOTES_MAX,
  SUPPLIER_PAYABLE_PAYMENT_REFERENCE_MAX,
  type PosSupplierPayableDto,
  type PosSupplierPayablePaymentDto,
  type SupplierPayablePaymentMethodCode,
} from "@/api/pos/pos-supplier-payables-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { laterPaymentsAmount, parseMoneyInput, remainingCredit } from "@/features/purchasing/receive-payment";
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

function canRecordPayment(payable: PosSupplierPayableDto): boolean {
  return (
    (payable.status === "Open" || payable.status === "PartiallyPaid") && payable.balance > 0
  );
}

type SupplierCreditSectionProps = {
  supplierId: string;
};

export function SupplierCreditSection({ supplierId }: SupplierCreditSectionProps) {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const allowView = canViewPurchasing(sessionGrant);
  const allowManage = canManagePurchasing(sessionGrant);

  const [paymentTarget, setPaymentTarget] = useState<PosSupplierPayableDto | null>(null);
  const [detailTarget, setDetailTarget] = useState<PosSupplierPayableDto | null>(null);
  const [amountText, setAmountText] = useState("");
  const [paymentMethod, setPaymentMethod] =
    useState<SupplierPayablePaymentMethodCode>("Cash");
  const [reference, setReference] = useState("");
  const [notes, setNotes] = useState("");
  const [recording, setRecording] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

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

  const historyPayableId = detailTarget?.payableId ?? paymentTarget?.payableId;

  const paymentsQuery = useQuery({
    queryKey: ["supplier-payable-payments", workspace?.organizationId, historyPayableId],
    enabled: Boolean(workspace) && allowView && online && Boolean(historyPayableId),
    queryFn: ({ signal }) => listSupplierPayablePayments(workspace!, historyPayableId!, signal),
  });

  useEffect(() => {
    if (!paymentTarget) {
      return;
    }
    setAmountText(String(paymentTarget.balance));
    setPaymentMethod("Cash");
    setReference("");
    setNotes("");
    setFormError(null);
  }, [paymentTarget]);

  if (!allowView) {
    return null;
  }

  const payables = listQuery.data?.items ?? [];
  const summary = summaryQuery.data;
  const paidCount = payables.filter((p) => p.status === "Paid").length;
  const paymentAmount = parseMoneyInput(amountText);
  const remainingAfterPayment =
    paymentTarget && paymentAmount !== null
      ? remainingCredit(paymentTarget.balance, paymentAmount)
      : paymentTarget?.balance ?? 0;

  async function onRecordPayment() {
    if (!workspace || !paymentTarget || !allowManage || !online || recording) {
      return;
    }
    if (paymentAmount === null || paymentAmount <= 0) {
      setFormError(t("supplierPayables.amountRequired"));
      return;
    }
    if (paymentAmount > paymentTarget.balance) {
      setFormError(t("supplierPayables.overpay"));
      return;
    }
    setRecording(true);
    setFormError(null);
    try {
      await recordSupplierPayablePayment(workspace, paymentTarget.payableId, {
        amount: paymentAmount,
        paymentMethod,
        reference: reference.trim() || null,
        notes: notes.trim() || null,
      });
      setPaymentTarget(null);
      await queryClient.invalidateQueries({ queryKey: ["supplier-payable-summary"] });
      await queryClient.invalidateQueries({ queryKey: ["supplier-payables"] });
      await queryClient.invalidateQueries({ queryKey: ["supplier-payable-payments"] });
    } catch (err) {
      setFormError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("supplierPayables.recordFailed"))
          : t("supplierPayables.recordFailed"),
      );
    } finally {
      setRecording(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-3" data-testid="supplier-credit-section">
      <Card>
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-base)] font-semibold">
          {t("supplierPayables.title")}
        </h2>
        <p className="m-0 mb-3 text-[length:var(--exits-text-sm)] text-muted">
          {t("supplierPayables.summaryLede")}
        </p>
        {summaryQuery.isLoading || listQuery.isLoading ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
        ) : summaryQuery.isError || listQuery.isError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("supplierPayables.loadFailed")}
          </p>
        ) : (
          <dl className="m-0 grid gap-2 sm:grid-cols-2 lg:grid-cols-4 text-[length:var(--exits-text-sm)]">
            <div>
              <dt className="text-muted">{t("supplierPayables.outstanding")}</dt>
              <dd className="m-0" data-testid="supplier-credit-outstanding">
                <MoneyDisplay amount={summary?.outstandingTotal ?? 0} />
              </dd>
            </div>
            <div>
              <dt className="text-muted">{t("supplierPayables.overdue")}</dt>
              <dd className="m-0" data-testid="supplier-credit-overdue">
                <MoneyDisplay amount={summary?.overdueTotal ?? 0} />
              </dd>
            </div>
            <div>
              <dt className="text-muted">{t("supplierPayables.openCount")}</dt>
              <dd className="m-0" data-testid="supplier-credit-open-count">
                {summary?.openCount ?? 0}
              </dd>
            </div>
            <div>
              <dt className="text-muted">{t("supplierPayables.paidCount")}</dt>
              <dd className="m-0" data-testid="supplier-credit-paid-count">
                {paidCount}
              </dd>
            </div>
          </dl>
        )}
      </Card>

      <Card>
        <h3 className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
          {t("supplierPayables.listTitle")}
        </h3>
        {payables.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="supplier-credit-empty">
            {t("supplierPayables.empty")}
          </p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-3 p-0" data-testid="supplier-credit-list">
            {payables.map((payable) => {
              const later = laterPaymentsAmount(payable.paidAmount, payable.paidAtReceiptAmount);
              const showPay = allowManage && online && canRecordPayment(payable);
              return (
                <li
                  key={payable.payableId}
                  className="rounded-md border border-border p-3"
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
                      <dd className="m-0">
                        <MoneyDisplay amount={payable.originalAmount} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.paidAtReceipt")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={payable.paidAtReceiptAmount} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.laterPayments")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={later} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.totalPaid")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={payable.paidAmount} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.balance")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={payable.balance} />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("supplierPayables.dueDate")}</dt>
                      <dd className="m-0">{payable.dueDate?.trim() || "—"}</dd>
                    </div>
                  </dl>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {showPay ? (
                      <Button
                        type="button"
                        className="min-h-11"
                        data-testid={`supplier-payable-record-${payable.payableId}`}
                        onClick={() => setPaymentTarget(payable)}
                      >
                        {t("supplierPayables.recordPayment")}
                      </Button>
                    ) : null}
                    <Button
                      type="button"
                      variant="ghost"
                      className="min-h-11"
                      data-testid={`supplier-payable-detail-${payable.payableId}`}
                      onClick={() => setDetailTarget(payable)}
                    >
                      {t("supplierPayables.viewDetails")}
                    </Button>
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </Card>

      {paymentTarget ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="supplier-payment-dialog-title"
          data-testid="supplier-payment-dialog"
        >
          <Card className="w-full max-w-md">
            <h2
              id="supplier-payment-dialog-title"
              className="m-0 mb-2 text-[length:var(--exits-text-base)] font-semibold"
            >
              {t("supplierPayables.recordPayment")}
            </h2>
            <dl
              className="m-0 mb-3 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-3"
              data-testid="supplier-payment-preview"
            >
              <div>
                <dt className="text-muted">{t("supplierPayables.balance")}</dt>
                <dd className="m-0">
                  <MoneyDisplay amount={paymentTarget.balance} />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("supplierPayables.amount")}</dt>
                <dd className="m-0">
                  <MoneyDisplay amount={paymentAmount ?? 0} />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("supplierPayables.remainingBalance")}</dt>
                <dd className="m-0" data-testid="supplier-payment-remaining">
                  <MoneyDisplay amount={remainingAfterPayment} />
                </dd>
              </div>
            </dl>
            {formError ? (
              <p
                className="mb-3 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
                data-testid="supplier-payment-error"
              >
                {formError}
              </p>
            ) : null}
            <div className="grid gap-3">
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("supplierPayables.amount")}
                <input
                  type="text"
                  inputMode="decimal"
                  className="min-h-11 rounded-md border border-border bg-background px-3"
                  value={amountText}
                  onChange={(e) => setAmountText(e.target.value)}
                  data-testid="supplier-payment-amount"
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("supplierPayables.paymentMethod")}
                <select
                  className="min-h-11 rounded-md border border-border bg-background px-3"
                  value={paymentMethod}
                  onChange={(e) =>
                    setPaymentMethod(e.target.value as SupplierPayablePaymentMethodCode)
                  }
                  data-testid="supplier-payment-method"
                >
                  {SUPPLIER_PAYABLE_PAYMENT_METHODS.map((method) => (
                    <option key={method} value={method}>
                      {t(methodLabelKey(method))}
                    </option>
                  ))}
                </select>
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("supplierPayables.reference")}
                <input
                  className="min-h-11 rounded-md border border-border bg-background px-3"
                  value={reference}
                  maxLength={SUPPLIER_PAYABLE_PAYMENT_REFERENCE_MAX}
                  onChange={(e) => setReference(e.target.value)}
                  data-testid="supplier-payment-reference"
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("supplierPayables.notes")}
                <textarea
                  className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
                  value={notes}
                  maxLength={SUPPLIER_PAYABLE_PAYMENT_NOTES_MAX}
                  onChange={(e) => setNotes(e.target.value)}
                  data-testid="supplier-payment-notes"
                />
              </label>
            </div>
            <div className="mt-4 flex flex-wrap gap-2">
              <Button
                type="button"
                variant="ghost"
                className="min-h-11"
                disabled={recording}
                onClick={() => setPaymentTarget(null)}
                data-testid="supplier-payment-cancel"
              >
                {t("supplierPayables.cancel")}
              </Button>
              <Button
                type="button"
                className="min-h-11"
                disabled={recording}
                onClick={() => void onRecordPayment()}
                data-testid="supplier-payment-confirm"
              >
                {recording
                  ? t("supplierPayables.recording")
                  : t("supplierPayables.confirmPayment")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}

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
              {allowManage && online && canRecordPayment(detailTarget) ? (
                <Button
                  type="button"
                  className="min-h-11"
                  data-testid="supplier-payable-detail-record"
                  onClick={() => {
                    setPaymentTarget(detailTarget);
                    setDetailTarget(null);
                  }}
                >
                  {t("supplierPayables.recordPayment")}
                </Button>
              ) : null}
              <Button
                type="button"
                variant="ghost"
                className="min-h-11"
                onClick={() => setDetailTarget(null)}
                data-testid="supplier-payable-detail-close"
              >
                {t("supplierPayables.cancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
