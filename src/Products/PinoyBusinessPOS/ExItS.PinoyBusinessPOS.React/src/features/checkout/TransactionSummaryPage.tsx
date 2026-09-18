import { Ban, Eye, Plus, Printer, RotateCcw } from "lucide-react";
import { canProcessReturn, canVoidSale } from "@/access/pos-capabilities";
import {
  listReturnBatches,
  recordReturnBatchRefund,
  type ReturnBatchDto,
  type ReturnBatchLineDto,
} from "@/api/pos/pos-return-batches-client";
import {
  formatPaymentMethodLabel,
  getSale,
  VOID_REASON_MAX_LENGTH,
  voidSale,
} from "@/api/pos/pos-sales-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { BottomSheet, ConfirmationDialog } from "@/components/exits/SheetDialog";
import { LoadingSkeleton, StickyActionBar } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatQuantityDisplay } from "@/cart/sell-cart-helpers";
import { describeCheckoutSaleError } from "@/features/checkout/checkout-sale-errors";
import { invalidatePosStockQueries } from "@/features/catalog/invalidate-pos-stock-queries";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { BusinessDocumentPreview } from "@/features/documents/BusinessDocumentPreview";
import { customerPurchaseSummaryFromPosSale } from "@/features/documents/customer-purchase-summary-view";
import { CustomerPurchaseSummaryDocument } from "@/features/documents/SaleBusinessDocument";
import { printBusinessDocument } from "@/features/documents/print-business-document";
import { useBusinessDocumentIdentity } from "@/features/documents/use-business-document-identity";
import { ReturnInspectionDialog } from "@/features/returns/ReturnInspectionDialog";
import { ReturnReviewDialog } from "@/features/returns/ReturnReviewDialog";
import { useOrganizationDocumentSettings } from "@/features/documents/use-organization-document-settings";
import { useI18n } from "@/i18n/I18nProvider";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";

function saleStatusTone(status: string, voided: boolean): "danger" | "success" | "info" {
  if (voided) {
    return "danger";
  }
  if (status === "Completed") {
    return "success";
  }
  return "info";
}

function lineInspectionStatus(line: ReturnBatchLineDto): "pending" | "classified" {
  return line.classifiedAtUtc ? "classified" : "pending";
}

/**
 * Transaction Detail — operational staff UI for a completed sale.
 * Customer Purchase Summary is a separate printable document opened via Preview / Print / PDF.
 */
export function TransactionSummaryPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { saleId } = useParams<{ saleId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const smartBack = usePageSmartBack({
    fallback: "sell",
    backLabel: t("summary.backToSell"),
    backTestId: "summary-back-to-sell",
  });
  const organizationId = boundWorkspace?.organizationId ?? null;
  const { settings: documentSettings } = useOrganizationDocumentSettings(organizationId);
  const { identity, headerVisibility } = useBusinessDocumentIdentity(organizationId);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [voidReason, setVoidReason] = useState("");
  const [voidError, setVoidError] = useState<string | null>(null);
  const [voiding, setVoiding] = useState(false);
  const [voidConfirmOpen, setVoidConfirmOpen] = useState(false);
  const [voidSheetOpen, setVoidSheetOpen] = useState(false);
  const [returnConfirmOpen, setReturnConfirmOpen] = useState(false);
  const [inspectionTarget, setInspectionTarget] = useState<{
    returnBatchId: string;
    returnBatchLineId: string;
  } | null>(null);
  const [reviewBatchId, setReviewBatchId] = useState<string | null>(null);
  const [stickyVisible, setStickyVisible] = useState(false);
  const [refundBatchId, setRefundBatchId] = useState<string | null>(null);
  const [refundAmount, setRefundAmount] = useState("");
  const [refundMethod, setRefundMethod] = useState("Cash");
  const [refundReference, setRefundReference] = useState("");
  const [refundNote, setRefundNote] = useState("");
  const [refundError, setRefundError] = useState<string | null>(null);
  const [recordingRefund, setRecordingRefund] = useState(false);
  const stickyGateRef = useRef<HTMLDivElement | null>(null);

  const workspaceScope =
    boundWorkspace?.branchId && boundWorkspace.organizationId
      ? {
          organizationId: boundWorkspace.organizationId,
          branchId: boundWorkspace.branchId,
        }
      : null;

  const allowVoid = canVoidSale(sessionGrant);
  const allowProcessReturn = canProcessReturn(sessionGrant);

  const saleQuery = useQuery({
    queryKey: ["pos-sale", workspaceScope?.organizationId, workspaceScope?.branchId, saleId],
    enabled: Boolean(workspaceScope && saleId),
    queryFn: ({ signal }) => getSale(workspaceScope!, saleId!, signal),
  });

  const returnBatchesQuery = useQuery({
    queryKey: ["return-batches", workspaceScope?.organizationId, workspaceScope?.branchId, saleId],
    enabled: Boolean(workspaceScope && saleId),
    queryFn: ({ signal }) => listReturnBatches(workspaceScope!, { saleId: saleId! }, signal),
  });

  const saleActors = useActorDirectory(workspaceScope?.organizationId, [
    saleQuery.data?.recordedBy,
    saleQuery.data?.voidedBy,
  ]);

  useEffect(() => {
    if (!saleId || saleQuery.isLoading || saleQuery.isError || !saleQuery.data) {
      return;
    }
    const gate = stickyGateRef.current;
    if (!gate || typeof IntersectionObserver === "undefined") {
      setStickyVisible(true);
      return;
    }
    const observer = new IntersectionObserver(
      ([entry]) => {
        setStickyVisible(!(entry?.isIntersecting ?? true));
      },
      { threshold: 0 },
    );
    observer.observe(gate);
    return () => observer.disconnect();
  }, [saleId, saleQuery.isLoading, saleQuery.isError, saleQuery.data]);

  if (!saleId) {
    return (
      <div data-testid="transaction-summary-missing" className="flex flex-col gap-3">
        <PageHeader title={t("summary.title")} description={t("summary.missingSale")} />
        <Button asChild variant="ghost" className="w-fit">
          <Link to="/sell">{t("summary.newSale")}</Link>
        </Button>
      </div>
    );
  }

  if (saleQuery.isLoading || !workspaceScope) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (saleQuery.isError || !saleQuery.data) {
    return (
      <div data-testid="transaction-summary-error" className="flex flex-col gap-3">
        <PageHeader title={t("summary.title")} description={t("summary.loadError")} />
        <Button asChild className="w-fit">
          <Link to="/sell">{t("summary.newSale")}</Link>
        </Button>
      </div>
    );
  }

  const sale = saleQuery.data;
  const isVoided = sale.status === "Voided" || Boolean(sale.voidedAtUtc);
  const paymentLabel = formatPaymentMethodLabel(sale.paymentMethod);
  const returnBatches = returnBatchesQuery.data ?? [];
  const pendingReturnBatches = returnBatches.filter((batch) => batch.status !== "Finalized");
  const hasPendingReturns = pendingReturnBatches.length > 0;
  const selectedRefundBatch = refundBatchId
    ? returnBatches.find((batch) => batch.returnBatchId === refundBatchId) ?? null
    : null;
  const hasFinalizedReturns = returnBatches.some((batch) => batch.status === "Finalized");
  const saleStatusLabel =
    sale.status === "Completed" && hasPendingReturns
      ? t("returns.statusCompletedReturnsPending")
      : sale.status === "Completed" && hasFinalizedReturns
        ? t("returns.statusCompletedReturnsProcessed")
        : sale.status;
  const showReturnAction = !isVoided && allowProcessReturn;
  const showVoidAction = !isVoided && allowVoid;
  const isUtang = sale.paymentMethod.trim().toLowerCase() === "utang";
  const discountTotal =
    sale.discountTotal ??
    sale.lines.reduce(
      (sum, line) =>
        sum + (line.lineDiscountAmount ?? 0) + (line.saleDiscountAllocatedAmount ?? 0),
      0,
    );

  const soldBy = saleActors.resolve(sale.recordedBy);
  const voidedBy = saleActors.resolve(sale.voidedBy);
  const soldByLabel =
    saleActors.isResolving && !soldBy
      ? "\u00a0"
      : soldBy?.displayName?.trim() || t("common.notAvailable");
  const voidedByLabel =
    saleActors.isResolving && !voidedBy
      ? "\u00a0"
      : voidedBy?.displayName?.trim() || t("common.notAvailable");
  const inspectionBatch = inspectionTarget
    ? returnBatches.find((item) => item.returnBatchId === inspectionTarget.returnBatchId) ?? null
    : null;
  const inspectionLine: ReturnBatchLineDto | null =
    inspectionBatch && inspectionTarget
      ? inspectionBatch.lines.find((line) => line.returnBatchLineId === inspectionTarget.returnBatchLineId) ?? null
      : null;
  const reviewBatch = reviewBatchId
    ? returnBatches.find((item) => item.returnBatchId === reviewBatchId) ?? null
    : null;

  const documentNode = (
    <CustomerPurchaseSummaryDocument
      view={customerPurchaseSummaryFromPosSale(sale, paymentLabel)}
      settings={documentSettings}
      identity={identity}
      headerVisibility={headerVisibility(documentSettings.header)}
      cashierLabel={soldByLabel}
      paymentLabel={paymentLabel}
      audience="Seller"
      preview={previewOpen}
    />
  );

  async function onVoid() {
    if (!workspaceScope || !saleId || voiding || isVoided) {
      return;
    }
    const reason = voidReason.trim();
    if (!reason) {
      setVoidError(t("summary.voidReasonRequired"));
      return;
    }
    setVoiding(true);
    setVoidError(null);
    try {
      const updated = await voidSale(workspaceScope, saleId, {
        reason: reason.slice(0, VOID_REASON_MAX_LENGTH),
      });
      await queryClient.setQueryData(
        ["pos-sale", workspaceScope.organizationId, workspaceScope.branchId, saleId],
        updated,
      );
      await invalidatePosStockQueries(queryClient);
      setVoidReason("");
      setVoidSheetOpen(false);
    } catch (error) {
      setVoidError(describeCheckoutSaleError(error, t));
    } finally {
      setVoiding(false);
    }
  }

  function upsertReturnBatch(updated: ReturnBatchDto) {
    if (!workspaceScope || !saleId) {
      return;
    }
    queryClient.setQueryData<ReturnBatchDto[]>(
      ["return-batches", workspaceScope.organizationId, workspaceScope.branchId, saleId],
      (current) => {
        const items = current ?? [];
        const index = items.findIndex((entry) => entry.returnBatchId === updated.returnBatchId);
        if (index < 0) {
          return [updated, ...items];
        }
        const next = items.slice();
        next[index] = updated;
        return next;
      },
    );
  }

  const headerActions = (
    <div
      className="flex min-w-0 flex-wrap items-center gap-2 print:hidden"
      data-testid="summary-header-actions"
    >
      <Button
        asChild
        className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
        data-testid="summary-new-sale"
      >
        <Link to="/sell">
          <Plus className="size-4 shrink-0" aria-hidden />
          {t("summary.newSale")}
        </Link>
      </Button>
      <Button
        type="button"
        variant="outline"
        className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
        data-testid="summary-preview"
        onClick={() => setPreviewOpen(true)}
      >
        <Eye className="size-4 shrink-0" aria-hidden />
        {t("summary.preview")}
      </Button>
      <Button
        type="button"
        variant="outline"
        className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
        data-testid="summary-print"
        onClick={() => printBusinessDocument()}
      >
        <Printer className="size-4 shrink-0" aria-hidden />
        {t("summary.print")}
      </Button>
      {showReturnAction ? (
        <Button
          type="button"
          variant="outline"
          className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
          data-testid="summary-return-items"
          aria-haspopup="dialog"
          onClick={() => setReturnConfirmOpen(true)}
        >
          <RotateCcw className="size-4 shrink-0" aria-hidden />
          {t("returns.returnItems")}
        </Button>
      ) : null}
      {showVoidAction ? (
        <Button
          type="button"
          variant="outline"
          className="h-9 min-h-9 shrink-0 gap-1.5 border-destructive/40 px-2.5 text-destructive hover:border-destructive/55 hover:bg-[var(--exits-danger-soft)]"
          data-testid="summary-void-trigger"
          aria-haspopup="dialog"
          onClick={() => setVoidConfirmOpen(true)}
        >
          <Ban className="size-4 shrink-0" aria-hidden />
          {t("summary.voidSection")}
        </Button>
      ) : null}
    </div>
  );

  return (
    <div
      data-testid="transaction-summary-page"
      className="flex min-w-0 flex-col gap-4 pb-[calc(5.5rem+env(safe-area-inset-bottom,0px))] print:pb-0"
    >
      <PageHeader
        title={t("summary.title")}
        description={`${t("summary.subtitle")} · ${sale.saleNumber}`}
        {...smartBack}
        trailing={headerActions}
      />
      <div ref={stickyGateRef} className="h-px w-full shrink-0" aria-hidden data-testid="summary-sticky-gate" />

      {isVoided ? (
        <Card data-testid="summary-voided-banner">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-[var(--exits-danger)]">
            {t("summary.voidedBanner")}
          </p>
          {sale.voidReason ? (
            <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)]">
              {t("summary.voidReasonLabel")}: {sale.voidReason}
            </p>
          ) : null}
        </Card>
      ) : null}

      <Card
        data-testid="summary-body-card"
        className="mx-auto flex w-full max-w-3xl flex-col gap-4 print:hidden"
      >
        <section data-testid="summary-details-section">
          <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold uppercase tracking-wide text-muted">
            {t("summary.sectionDetails")}
          </h2>
          <dl className="m-0 grid gap-x-6 gap-y-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
            <div className="flex justify-between gap-2 sm:block">
              <dt className="text-muted">{t("summary.saleNumber")}</dt>
              <dd className="m-0 text-right font-semibold sm:mt-0.5 sm:text-left" data-testid="summary-sale-number">
                {sale.saleNumber}
              </dd>
            </div>
            <div className="flex justify-between gap-2 sm:block">
              <dt className="text-muted">{t("summary.dateTime")}</dt>
              <dd className="m-0 text-right sm:mt-0.5 sm:text-left" data-testid="summary-date-time">
                {new Date(sale.recordedAtUtc).toLocaleString()}
              </dd>
            </div>
            <div className="flex justify-between gap-2 sm:block">
              <dt className="text-muted">{t("summary.paymentMethod")}</dt>
              <dd className="m-0 text-right sm:mt-0.5 sm:text-left" data-testid="summary-payment-method">
                {paymentLabel}
              </dd>
            </div>
            <div className="flex items-center justify-between gap-2 sm:block">
              <dt className="text-muted">{t("summary.status")}</dt>
              <dd className="m-0 flex justify-end sm:mt-0.5 sm:justify-start" data-testid="summary-status">
                <StatusChip tone={saleStatusTone(sale.status, isVoided)}>{saleStatusLabel}</StatusChip>
              </dd>
            </div>
            {sale.shiftNumber ? (
              <div className="flex justify-between gap-2 sm:block">
                <dt className="text-muted">{t("summary.shift")}</dt>
                <dd className="m-0 text-right sm:mt-0.5 sm:text-left" data-testid="summary-shift">
                  {sale.shiftNumber}
                </dd>
              </div>
            ) : null}
            <div className="flex justify-between gap-2 sm:block" data-testid="summary-actor-attribution">
              <dt className="text-muted">{t("common.soldBy")}</dt>
              <dd className="m-0 text-right font-medium sm:mt-0.5 sm:text-left" data-testid="summary-sold-by">
                {soldByLabel}
              </dd>
            </div>
            {isVoided ? (
              <div className="flex justify-between gap-2 sm:block">
                <dt className="text-muted">{t("common.voidedBy")}</dt>
                <dd className="m-0 text-right font-medium sm:mt-0.5 sm:text-left" data-testid="summary-voided-by">
                  {voidedByLabel}
                </dd>
              </div>
            ) : null}
            {sale.customerDisplayName ? (
              <div className="flex justify-between gap-2 sm:block sm:col-span-2">
                <dt className="text-muted">{t("summary.customer")}</dt>
                <dd className="m-0 text-right sm:mt-0.5 sm:text-left" data-testid="summary-customer">
                  {sale.customerDisplayName}
                </dd>
              </div>
            ) : null}
            {sale.gCashReference ? (
              <div className="flex justify-between gap-2 sm:block sm:col-span-2">
                <dt className="text-muted">{t("summary.gcashReference")}</dt>
                <dd className="m-0 text-right sm:mt-0.5 sm:text-left" data-testid="summary-gcash-reference">
                  {sale.gCashReference}
                </dd>
              </div>
            ) : null}
          </dl>
        </section>

        <section
          data-testid="summary-items-section"
          className="border-t border-border pt-4"
        >
          <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold uppercase tracking-wide text-muted">
            {t("summary.sectionItems")}
          </h2>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[20rem] border-collapse text-[length:var(--exits-text-sm)]">
              <thead>
                <tr className="border-b border-border text-left text-muted">
                  <th className="pb-2 pr-3 font-medium">{t("summary.itemDescription")}</th>
                  <th className="pb-2 pr-3 text-right font-medium">{t("summary.itemQty")}</th>
                  <th className="pb-2 pr-3 text-right font-medium">{t("summary.itemUnitPrice")}</th>
                  <th className="pb-2 text-right font-medium">{t("summary.itemLineTotal")}</th>
                </tr>
              </thead>
              <tbody>
                {sale.lines.map((line) => {
                  const override = sale.priceOverrides?.find(
                    (item) => item.lineNumber === line.lineNumber,
                  );
                  return (
                    <tr
                      key={line.saleLineId}
                      className="border-b border-border/60 align-top last:border-b-0"
                      data-testid={`summary-line-${line.lineNumber}`}
                    >
                      <td className="py-2 pr-3">
                        <span className="font-medium">{line.name}</span>
                        {override ? (
                          <span
                            className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted"
                            data-testid={`summary-line-price-changed-${line.lineNumber}`}
                          >
                            {t("sell.priceChanged")} · {t("summary.regularPrice")}: ₱
                            {override.baselineUnitPrice.toFixed(2)} · {t("summary.sellingPrice")}: ₱
                            {override.appliedUnitPrice.toFixed(2)}
                            {override.reason
                              ? ` · ${t("summary.priceOverrideReason")}: ${override.reason}`
                              : null}
                          </span>
                        ) : null}
                      </td>
                      <td className="py-2 pr-3 text-right whitespace-nowrap">
                        {line.quantity} {line.unitOfMeasure}
                      </td>
                      <td className="py-2 pr-3 text-right">
                        <MoneyDisplay amount={line.unitPrice} />
                      </td>
                      <td className="py-2 text-right">
                        <MoneyDisplay amount={line.lineTotal} />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>

        <section data-testid="summary-returned-items-section" className="border-t border-border pt-4">
          <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold uppercase tracking-wide text-muted">
            {t("returns.returnedItems")}
          </h2>
          {returnBatchesQuery.isLoading ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
          ) : null}
          {!returnBatchesQuery.isLoading && returnBatches.length === 0 ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{saleStatusLabel}</p>
          ) : null}
          <div className="space-y-2">
            {returnBatches.map((batch) => (
              <div
                key={batch.returnBatchId}
                className="rounded-[var(--exits-radius-md)] border border-border p-3"
                data-testid={`summary-return-batch-${batch.returnBatchId}`}
              >
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {batch.batchNumber}
                </p>
                <ul className="mb-0 mt-2 list-none space-y-1 p-0">
                  {batch.lines.map((line) => {
                    const lineStatus = lineInspectionStatus(line);
                    return (
                      <li
                        key={line.returnBatchLineId}
                        className="flex flex-wrap items-center justify-between gap-2 text-[length:var(--exits-text-sm)]"
                      >
                        <span>
                          {line.productNameSnapshot} · {t("returns.returnedQuantity")}:{" "}
                          {formatQuantityDisplay(line.acceptedQuantity)}
                        </span>
                        <span className="flex items-center gap-2">
                          <span className="text-muted">
                            {lineStatus === "pending"
                              ? t("returns.pendingInspection")
                              : t("returns.classified")}
                          </span>
                          <Button
                            type="button"
                            variant="ghost"
                            className="h-8 px-2"
                            data-testid={`summary-return-process-${line.returnBatchLineId}`}
                            onClick={() =>
                              setInspectionTarget({
                                returnBatchId: batch.returnBatchId,
                                returnBatchLineId: line.returnBatchLineId,
                              })
                            }
                          >
                            {t("returns.process")}
                          </Button>
                        </span>
                      </li>
                    );
                  })}
                </ul>
                <div className="mt-2 grid grid-cols-1 gap-1 text-[length:var(--exits-text-xs)] text-muted sm:grid-cols-2">
                  <p className="m-0">{t("returns.originalSaleTotal")}: {batch.financialSummary.originalSaleTotal.toFixed(2)}</p>
                  <p className="m-0">{t("returns.amountPreviouslyPaid")}: {batch.financialSummary.amountPreviouslyPaid.toFixed(2)}</p>
                  <p className="m-0">{t("returns.remainingAmountDue")}: {batch.financialSummary.remainingAmountDue.toFixed(2)}</p>
                  <p className="m-0">{t("returns.refundRemaining")}: {batch.financialSummary.refundRemaining.toFixed(2)}</p>
                </div>
                {batch.status === "ReadyForFinalize" ? (
                  <div className="mt-3 flex justify-end">
                    <Button
                      type="button"
                      variant="outline"
                      data-testid={`summary-return-review-${batch.returnBatchId}`}
                      onClick={() => setReviewBatchId(batch.returnBatchId)}
                    >
                      {t("returns.reviewReturn")}
                    </Button>
                  </div>
                ) : null}
                {batch.status === "Finalized" && batch.refundStatus === "RefundDue" && batch.financialSummary.refundRemaining > 0 ? (
                  <div className="mt-3 flex justify-end">
                    <Button
                      type="button"
                      variant="outline"
                      data-testid={`summary-return-refund-${batch.returnBatchId}`}
                      onClick={() => {
                        setRefundBatchId(batch.returnBatchId);
                        setRefundAmount(batch.financialSummary.refundRemaining.toFixed(2));
                        setRefundMethod("Cash");
                        setRefundReference("");
                        setRefundNote("");
                        setRefundError(null);
                      }}
                    >
                      {t("returns.recordRefund")}
                    </Button>
                  </div>
                ) : null}
              </div>
            ))}
          </div>
        </section>

        <section
          data-testid="summary-totals-section"
          className="border-t border-border pt-4"
        >
          <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold uppercase tracking-wide text-muted">
            {t("summary.sectionTotals")}
          </h2>
          <div className="ml-auto w-full max-w-xs space-y-1 text-[length:var(--exits-text-sm)]">
            <p className="m-0 flex justify-between gap-2">
              <span className="text-muted">{t("summary.subtotal")}</span>
              <MoneyDisplay amount={sale.subtotal} />
            </p>
            {discountTotal > 0 ? (
              <p className="m-0 flex justify-between gap-2" data-testid="summary-discount">
                <span className="text-muted">{t("summary.discount")}</span>
                <MoneyDisplay amount={discountTotal} />
              </p>
            ) : null}
            <p
              className="m-0 flex justify-between gap-2 text-[length:var(--exits-text-md)] font-semibold"
              data-testid="summary-total"
            >
              <span>{t("summary.total")}</span>
              <MoneyDisplay amount={sale.total} />
            </p>
            {!isUtang && sale.amountTendered != null ? (
              <p className="m-0 flex justify-between gap-2" data-testid="summary-tendered">
                <span className="text-muted">{t("summary.cashReceived")}</span>
                <MoneyDisplay amount={sale.amountTendered} />
              </p>
            ) : null}
            {!isUtang && sale.changeAmount != null ? (
              <p className="m-0 flex justify-between gap-2" data-testid="summary-change">
                <span className="text-muted">{t("summary.change")}</span>
                <MoneyDisplay amount={sale.changeAmount} />
              </p>
            ) : null}
            {isUtang ? (
              <p className="m-0 flex justify-between gap-2" data-testid="summary-utang-balance">
                <span className="text-muted">{t("summary.utangBalance")}</span>
                <MoneyDisplay amount={sale.total} />
              </p>
            ) : null}
          </div>
        </section>
      </Card>

      {/* Single canonical document mount: preview dialog OR off-screen print host */}
      {previewOpen ? (
        <BusinessDocumentPreview
          open={previewOpen}
          onClose={() => setPreviewOpen(false)}
          title={documentSettings.sales.title || t("summary.customerPurchaseSummary")}
          closeLabel={t("summary.closePreview")}
          printLabel={t("summary.print")}
          pdfLabel={t("summary.exportPdf")}
          testId="summary-document-preview"
        >
          {documentNode}
        </BusinessDocumentPreview>
      ) : (
        <div className="exits-bizdoc-print-host" aria-hidden data-testid="summary-print-host">
          {documentNode}
        </div>
      )}

      {showVoidAction ? (
        <>
          <ConfirmationDialog
            open={voidConfirmOpen}
            title={t("summary.voidConfirmTitle")}
            detail={t("summary.voidConfirmDetail")}
            confirmLabel={t("summary.voidConfirmContinue")}
            cancelLabel={t("sell.cancel")}
            confirmTone="danger"
            confirmIcon={<Ban className="size-4 shrink-0" aria-hidden />}
            testId="summary-void-confirm-dialog"
            onCancel={() => setVoidConfirmOpen(false)}
            onConfirm={() => {
              setVoidConfirmOpen(false);
              setVoidError(null);
              setVoidSheetOpen(true);
            }}
          />
          <BottomSheet
            open={voidSheetOpen}
            onClose={() => {
              if (voiding) {
                return;
              }
              setVoidSheetOpen(false);
              setVoidError(null);
            }}
            title={t("summary.voidSection")}
            panelId="summary-void-sheet"
            testId="summary-void-panel"
            closeLabel={t("sell.cancel")}
          >
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("summary.voidLede")}</p>
            <label
              className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
              htmlFor="summary-void-reason"
            >
              {t("summary.voidReason")}
              <input
                id="summary-void-reason"
                data-testid="summary-void-reason"
                type="text"
                maxLength={VOID_REASON_MAX_LENGTH}
                value={voidReason}
                disabled={voiding}
                className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                onChange={(event) => setVoidReason(event.target.value)}
              />
            </label>
            {voidError ? (
              <p
                data-testid="summary-void-error"
                className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
              >
                {voidError}
              </p>
            ) : null}
            <Button
              type="button"
              variant="destructive"
              className="mt-3 w-full"
              data-testid="summary-void-confirm"
              disabled={voiding}
              onClick={() => void onVoid()}
            >
              {voiding ? t("summary.voiding") : t("summary.voidConfirm")}
            </Button>
          </BottomSheet>
        </>
      ) : null}

      {!isVoided && !allowVoid ? (
        <p
          data-testid="summary-void-denied"
          className="mx-auto m-0 w-full max-w-3xl text-[length:var(--exits-text-sm)] text-muted print:hidden"
        >
          {t("summary.voidDenied")}
        </p>
      ) : null}

      {showReturnAction ? (
        <ConfirmationDialog
          open={returnConfirmOpen}
          title={t("summary.returnConfirmTitle")}
          detail={t("summary.returnConfirmDetail")}
          confirmLabel={t("returns.returnItems")}
          cancelLabel={t("sell.cancel")}
          confirmIcon={<RotateCcw className="size-4 shrink-0" aria-hidden />}
          testId="summary-return-confirm-dialog"
          onCancel={() => setReturnConfirmOpen(false)}
          onConfirm={() => {
            setReturnConfirmOpen(false);
            navigate(`/returns/sale/${sale.saleId}`);
          }}
        />
      ) : null}

      {workspaceScope && inspectionBatch ? (
        <ReturnInspectionDialog
          open={Boolean(inspectionTarget)}
          workspace={workspaceScope}
          batch={inspectionBatch}
          line={inspectionLine}
          onClose={() => setInspectionTarget(null)}
          onSaved={(updated) => {
            upsertReturnBatch(updated);
            setInspectionTarget(null);
          }}
        />
      ) : null}

      {workspaceScope && reviewBatch ? (
        <ReturnReviewDialog
          open={Boolean(reviewBatchId)}
          workspace={workspaceScope}
          batch={reviewBatch}
          onClose={() => setReviewBatchId(null)}
          onBackToEdit={() => {
            setReviewBatchId(null);
            if (reviewBatch.lines.length > 0) {
              setInspectionTarget({
                returnBatchId: reviewBatch.returnBatchId,
                returnBatchLineId: reviewBatch.lines[0]!.returnBatchLineId,
              });
            }
          }}
          onFinalized={(updated) => {
            upsertReturnBatch(updated);
            setReviewBatchId(null);
          }}
        />
      ) : null}

      <BottomSheet
        open={Boolean(selectedRefundBatch)}
        onClose={() => {
          if (!recordingRefund) {
            setRefundBatchId(null);
            setRefundError(null);
          }
        }}
        panelId="return-refund-dialog"
        testId="return-refund-dialog"
        title={t("returns.recordRefund")}
        closeLabel={t("sell.cancel")}
      >
        {selectedRefundBatch ? (
          <div className="flex min-w-0 flex-col gap-2">
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("returns.refundAmount")}
              <input
                type="number"
                min={0.01}
                step={0.01}
                value={refundAmount}
                onChange={(event) => setRefundAmount(event.target.value)}
                className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("returns.refundMethod")}
              <select
                value={refundMethod}
                onChange={(event) => setRefundMethod(event.target.value)}
                className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              >
                <option value="Cash">Cash</option>
                <option value="ManualGCash">GCash</option>
                <option value="BankTransfer">Bank transfer / deposit</option>
              </select>
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("returns.refundReference")}
              <input
                type="text"
                value={refundReference}
                onChange={(event) => setRefundReference(event.target.value)}
                className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("returns.notes")}
              <input
                type="text"
                value={refundNote}
                onChange={(event) => setRefundNote(event.target.value)}
                className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              />
            </label>
            {refundError ? <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">{refundError}</p> : null}
            <Button
              type="button"
              disabled={recordingRefund}
              onClick={async () => {
                if (!workspaceScope || !selectedRefundBatch) {
                  return;
                }
                const numericAmount = Number(refundAmount);
                if (!Number.isFinite(numericAmount) || numericAmount <= 0) {
                  setRefundError(t("returns.refundAmount"));
                  return;
                }
                setRecordingRefund(true);
                setRefundError(null);
                try {
                  const updated = await recordReturnBatchRefund(workspaceScope, selectedRefundBatch.returnBatchId, {
                    amount: numericAmount,
                    method: refundMethod,
                    reference: refundReference || undefined,
                    note: refundNote || undefined,
                  });
                  upsertReturnBatch(updated);
                  setRefundBatchId(null);
                } catch (error) {
                  setRefundError(describeCheckoutSaleError(error, t));
                } finally {
                  setRecordingRefund(false);
                }
              }}
            >
              {recordingRefund ? t("returns.submitting") : t("returns.recordRefund")}
            </Button>
          </div>
        ) : null}
      </BottomSheet>

      {stickyVisible ? (
        <StickyActionBar className="print:hidden justify-stretch gap-2 sm:justify-end">
          <div
            className="flex w-full min-w-0 flex-wrap items-stretch gap-2 sm:w-auto sm:justify-end"
            data-testid="summary-postpay-actions"
          >
            <Button
              asChild
              className="min-w-0 flex-1 gap-2 sm:flex-none"
              data-testid="summary-new-sale-sticky"
            >
              <Link to="/sell">
                <Plus className="size-4 shrink-0" aria-hidden />
                {t("summary.newSale")}
              </Link>
            </Button>
            <Button
              type="button"
              variant="outline"
              className="min-w-0 flex-1 gap-2 sm:flex-none"
              data-testid="summary-print-sticky"
              onClick={() => printBusinessDocument()}
            >
              <Printer className="size-4 shrink-0" aria-hidden />
              {t("summary.print")}
            </Button>
          </div>
        </StickyActionBar>
      ) : null}
    </div>
  );
}
