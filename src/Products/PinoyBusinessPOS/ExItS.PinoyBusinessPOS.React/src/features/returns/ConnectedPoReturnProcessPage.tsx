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
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
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

      <section
        className="exits-animate-panel flex flex-col gap-3"
        data-testid="connected-po-return-summary"
      >
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("returns.connectedPo.summaryTitle")}
        </h2>
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <Card
            className="flex flex-col gap-2 p-3"
            treatment="bordered"
            data-testid="connected-po-return-summary-po"
          >
            <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
              {t("returns.connectedPo.poNumber")}
            </h3>
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {batch.poNumberSnapshot ?? "—"}
            </p>
          </Card>
          <Card
            className="flex flex-col gap-2 p-3"
            treatment="bordered"
            data-testid="connected-po-return-summary-reason"
          >
            <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
              {t("returns.reason")}
            </h3>
            <p className="m-0 break-words text-[length:var(--exits-text-sm)] font-semibold">
              {batch.reason}
            </p>
          </Card>
          <Card
            className="flex flex-col gap-2 p-3"
            treatment="bordered"
            data-testid="connected-po-return-summary-value"
          >
            <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
              {t("returns.connectedPo.returnValue")}
            </h3>
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              <MoneyDisplay amount={batch.acceptedReturnValue} />
            </p>
          </Card>
          <Card
            className="flex flex-col gap-2 p-3"
            treatment="bordered"
            data-testid="connected-po-return-summary-refund"
          >
            <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
              {t("returns.connectedPo.refundDue")}
            </h3>
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              <MoneyDisplay amount={batch.financialSummary.refundRemaining} />
            </p>
          </Card>
        </div>
      </section>

      <Card
        className="exits-animate-panel flex flex-col gap-3 p-3"
        treatment="bordered"
        data-testid="connected-po-return-lines-card"
      >
        <CardHeader>
          <CardTitle as="h2">{t("returns.linesTitle")}</CardTitle>
        </CardHeader>
        <CardContent>
          <ExitsTableContainer data-testid="connected-po-return-process-lines">
            <ExitsTable data-testid="connected-po-return-process-lines-desktop">
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead cellAlign="text">
                    {t("returns.connectedPo.colProduct")}
                  </ExitsTableHead>
                  <ExitsTableHead cellAlign="text">
                    {t("returns.returnedQuantity")}
                  </ExitsTableHead>
                  <ExitsTableHead cellAlign="text">
                    {t("returns.connectedPo.colInspection")}
                  </ExitsTableHead>
                  <ExitsTableHead cellAlign="actions">
                    {t("returns.connectedPo.colActions")}
                  </ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                {batch.lines.map((line) => {
                  const inspectDisabled =
                    awaitingReceipt || batch.status === "Finalized";
                  const inspectionLabel =
                    line.sellableQuantity == null
                      ? t("returns.pendingInspection")
                      : `${t("returns.sellableAgain")}: ${line.sellableQuantity} · ${t("returns.damagedWriteOff")}: ${line.damagedQuantity ?? 0}`;
                  return (
                    <ExitsTableRow
                      key={line.returnBatchLineId}
                      data-testid={`connected-po-return-line-row-${line.returnBatchLineId}`}
                    >
                      <ExitsTableCell cellAlign="text" className="font-medium">
                        {line.productNameSnapshot}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text">
                        {line.acceptedQuantity} {line.unitOfMeasure}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="text" className="text-muted">
                        {inspectionLabel}
                      </ExitsTableCell>
                      <ExitsTableCell cellAlign="actions">
                        <Button
                          type="button"
                          appearance="ghost"
                          className="h-8 px-2"
                          disabled={inspectDisabled}
                          data-testid={`connected-po-return-inspect-${line.returnBatchLineId}`}
                          onClick={() => setInspectionLineId(line.returnBatchLineId)}
                        >
                          {t("returns.connectedPo.inspect")}
                        </Button>
                      </ExitsTableCell>
                    </ExitsTableRow>
                  );
                })}
              </ExitsTableBody>
            </ExitsTable>

            <ExitsTableMobile data-testid="connected-po-return-process-lines-mobile">
              {batch.lines.map((line) => {
                const inspectDisabled =
                  awaitingReceipt || batch.status === "Finalized";
                const inspectionLabel =
                  line.sellableQuantity == null
                    ? t("returns.pendingInspection")
                    : `${t("returns.sellableAgain")}: ${line.sellableQuantity} · ${t("returns.damagedWriteOff")}: ${line.damagedQuantity ?? 0}`;
                return (
                  <ExitsTableMobileRow
                    key={line.returnBatchLineId}
                    data-testid={`connected-po-return-line-mobile-${line.returnBatchLineId}`}
                  >
                    <div className="exits-table-mobile__title-row">
                      <span className="exits-table-mobile__title">
                        {line.productNameSnapshot}
                      </span>
                      <Button
                        type="button"
                        appearance="ghost"
                        className="h-8 px-2"
                        disabled={inspectDisabled}
                        data-testid={`connected-po-return-inspect-${line.returnBatchLineId}`}
                        onClick={() => setInspectionLineId(line.returnBatchLineId)}
                      >
                        {t("returns.connectedPo.inspect")}
                      </Button>
                    </div>
                    <p className="exits-table-mobile__meta m-0">
                      {t("returns.returnedQuantity")}: {line.acceptedQuantity}{" "}
                      {line.unitOfMeasure}
                    </p>
                    <p className="exits-table-mobile__meta m-0">{inspectionLabel}</p>
                  </ExitsTableMobileRow>
                );
              })}
            </ExitsTableMobile>
          </ExitsTableContainer>
        </CardContent>
      </Card>

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
