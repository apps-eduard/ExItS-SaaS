import { useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { receiveConnectedPoReturnBatch } from "@/api/pos/pos-connected-po-returns-client";
import {
  getReturnBatch,
  recordReturnBatchRefund,
  type ReturnBatchDto,
} from "@/api/pos/pos-return-batches-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { ReturnInspectionDialog } from "@/features/returns/ReturnInspectionDialog";
import { ReturnReviewDialog } from "@/features/returns/ReturnReviewDialog";
import { describeReturnError } from "@/features/returns/return-errors";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ConnectedPoReturnProcessPage() {
  const { t } = useI18n();
  const { returnBatchId } = useParams<{ returnBatchId: string }>();
  const { boundWorkspace } = useWorkspace();
  const queryClient = useQueryClient();

  const [inspectionLineId, setInspectionLineId] = useState<string | null>(null);
  const [reviewOpen, setReviewOpen] = useState(false);
  const [refundAmount, setRefundAmount] = useState("");
  const [refundBusy, setRefundBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [receiving, setReceiving] = useState(false);

  const workspace =
    boundWorkspace?.branchId && boundWorkspace.organizationId
      ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
      : null;

  const headerBack = {
    backTo: pageBackNav.returns.to,
    backLabel: t(pageBackNav.returns.labelKey),
    backTestId: "page-header-back-returns",
  };

  const batchQuery = useQuery({
    queryKey: [
      "connected-po-return-batch",
      workspace?.organizationId,
      workspace?.branchId,
      returnBatchId,
    ],
    enabled: Boolean(workspace && returnBatchId),
    queryFn: ({ signal }) => getReturnBatch(workspace!, returnBatchId!, signal),
  });

  function applyUpdate(updated: ReturnBatchDto) {
    queryClient.setQueryData(
      [
        "connected-po-return-batch",
        workspace?.organizationId,
        workspace?.branchId,
        returnBatchId,
      ],
      updated,
    );
    void queryClient.invalidateQueries({ queryKey: ["connected-po-return-seller-inbox"] });
  }

  async function onReceive() {
    if (!workspace || !batch || receiving) {
      return;
    }
    setReceiving(true);
    setActionError(null);
    try {
      applyUpdate(
        await receiveConnectedPoReturnBatch(workspace, batch.returnBatchId, batch.updatedAtUtc),
      );
    } catch (err) {
      setActionError(describeReturnError(err, t));
    } finally {
      setReceiving(false);
    }
  }

  async function onRecordRefund() {
    if (!workspace || !batch || refundBusy) {
      return;
    }
    const amount = Number(refundAmount);
    if (!Number.isFinite(amount) || amount <= 0) {
      setActionError(t("returns.connectedPo.refundAmountInvalid"));
      return;
    }
    setRefundBusy(true);
    setActionError(null);
    try {
      applyUpdate(
        await recordReturnBatchRefund(workspace, batch.returnBatchId, {
          amount,
          method: "Cash",
        }),
      );
      setRefundAmount("");
    } catch (err) {
      setActionError(describeReturnError(err, t));
    } finally {
      setRefundBusy(false);
    }
  }

  if (!returnBatchId) {
    return (
      <div className="flex flex-col gap-3" data-testid="connected-po-return-process-missing">
        <PageHeader
          title={t("returns.connectedPo.processTitle")}
          description={t("returns.connectedPo.loadError")}
          {...headerBack}
        />
      </div>
    );
  }

  if (!workspace || batchQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  const batch = batchQuery.data ?? null;

  if (batchQuery.isError || !batch) {
    return (
      <div className="flex flex-col gap-3" data-testid="connected-po-return-process-error">
        <PageHeader
          title={t("returns.connectedPo.processTitle")}
          description={t("returns.connectedPo.loadError")}
          {...headerBack}
        />
        <ErrorState title={t("error.title")} detail={t("returns.errorNotFound")} />
      </div>
    );
  }

  const awaitingReceipt = batch.status === "AwaitingSellerReceipt";
  const allClassified = batch.lines.every(
    (line) => line.sellableQuantity != null && line.damagedQuantity != null,
  );
  const inspectionLine =
    batch.lines.find((line) => line.returnBatchLineId === inspectionLineId) ?? null;
  const canRefund =
    batch.status === "Finalized" &&
    batch.refundStatus === "RefundDue" &&
    batch.financialSummary.refundRemaining > 0;

  return (
    <div
      className="exits-page flex min-w-0 flex-col gap-3"
      data-testid="connected-po-return-process-page"
    >
      <PageHeader
        title={t("returns.connectedPo.processTitle")}
        description={`${batch.batchNumber} · ${batch.poNumberSnapshot ?? ""}`}
        {...headerBack}
      />

      <section className="catalog-form-section exits-animate-panel gap-0">
        <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("returns.connectedPo.poNumber")}</dt>
            <dd className="m-0 font-semibold">{batch.poNumberSnapshot ?? "—"}</dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("returns.reason")}</dt>
            <dd className="m-0">{batch.reason}</dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("returns.connectedPo.returnValue")}</dt>
            <dd className="m-0">
              <MoneyDisplay amount={batch.acceptedReturnValue} />
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("returns.connectedPo.refundDue")}</dt>
            <dd className="m-0">
              <MoneyDisplay amount={batch.financialSummary.refundRemaining} />
            </dd>
          </div>
        </dl>
      </section>

      <section className="catalog-form-section exits-animate-panel gap-0">
        <h2 className="catalog-form-section__title">{t("returns.linesTitle")}</h2>
        <ul className="mb-0 mt-3 list-none space-y-2 p-0" data-testid="connected-po-return-process-lines">
          {batch.lines.map((line) => (
            <li
              key={line.returnBatchLineId}
              className="flex flex-wrap items-center justify-between gap-2 text-[length:var(--exits-text-sm)]"
            >
              <span className="min-w-0">
                <span className="font-semibold">{line.productNameSnapshot}</span>
                <span className="text-muted">
                  {" "}
                  · {t("returns.returnedQuantity")}: {line.acceptedQuantity} {line.unitOfMeasure}
                </span>
              </span>
              <span className="flex items-center gap-2">
                <span className="text-muted">
                  {line.sellableQuantity == null
                    ? t("returns.pendingInspection")
                    : `${t("returns.sellableAgain")}: ${line.sellableQuantity} · ${t("returns.damagedWriteOff")}: ${line.damagedQuantity ?? 0}`}
                </span>
                <Button
                  type="button"
                  variant="ghost"
                  className="h-8 px-2"
                  disabled={awaitingReceipt || batch.status === "Finalized"}
                  data-testid={`connected-po-return-inspect-${line.returnBatchLineId}`}
                  onClick={() => setInspectionLineId(line.returnBatchLineId)}
                >
                  {t("returns.connectedPo.inspect")}
                </Button>
              </span>
            </li>
          ))}
        </ul>
      </section>

      {actionError ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid="connected-po-return-process-error-message"
        >
          {actionError}
        </p>
      ) : null}

      <div className="flex flex-wrap justify-end gap-2">
        {awaitingReceipt ? (
          <Button
            type="button"
            disabled={receiving}
            data-testid="connected-po-return-receive"
            onClick={() => void onReceive()}
          >
            {receiving ? t("returns.submitting") : t("returns.connectedPo.markReceived")}
          </Button>
        ) : null}
        {!awaitingReceipt && batch.status !== "Finalized" && allClassified ? (
          <Button
            type="button"
            variant="outline"
            data-testid="connected-po-return-review"
            onClick={() => setReviewOpen(true)}
          >
            {t("returns.connectedPo.reviewAndFinalize")}
          </Button>
        ) : null}
      </div>

      {canRefund ? (
        <section className="catalog-form-section exits-animate-panel gap-0">
          <h2 className="catalog-form-section__title">{t("returns.connectedPo.recordRefund")}</h2>
          <label
            className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor="connected-po-return-refund-amount"
          >
            {t("returns.refundAmount")}
            <input
              id="connected-po-return-refund-amount"
              data-testid="connected-po-return-refund-amount"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={refundAmount}
              placeholder={batch.financialSummary.refundRemaining.toFixed(2)}
              className="w-40 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              onChange={(event) => setRefundAmount(event.target.value)}
            />
          </label>
          <div className="mt-3 flex justify-end">
            <Button
              type="button"
              disabled={refundBusy}
              data-testid="connected-po-return-refund-submit"
              onClick={() => void onRecordRefund()}
            >
              {refundBusy ? t("returns.submitting") : t("returns.connectedPo.recordRefund")}
            </Button>
          </div>
        </section>
      ) : null}

      <ReturnInspectionDialog
        open={Boolean(inspectionLine)}
        workspace={workspace}
        batch={batch}
        line={inspectionLine}
        onClose={() => setInspectionLineId(null)}
        onSaved={(updated) => {
          applyUpdate(updated);
          setInspectionLineId(null);
        }}
      />

      <ReturnReviewDialog
        open={reviewOpen}
        workspace={workspace}
        batch={batch}
        onClose={() => setReviewOpen(false)}
        onBackToEdit={() => setReviewOpen(false)}
        onFinalized={(updated) => {
          applyUpdate(updated);
          setReviewOpen(false);
        }}
      />
    </div>
  );
}
