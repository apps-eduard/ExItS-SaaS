import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, ChevronDown, Plus, Printer, RotateCcw } from "lucide-react";
import { canProcessReturn, canVoidSale, canViewReports } from "@/access/pos-capabilities";
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
import { describeCheckoutSaleError } from "@/features/checkout/checkout-sale-errors";
import { invalidatePosStockQueries } from "@/features/catalog/invalidate-pos-stock-queries";
import { productionCostStatusLabelKey } from "@/features/inventory/production-labels";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Transaction Summary — never labeled Invoice.
 * Disclaimer matches SalesDocumentWording / MAUI SalesDocument_DisclaimerBody.
 * Void for Owner/Admin/Manager (RMAP-12).
 */
export function TransactionSummaryPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { saleId } = useParams<{ saleId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const [voidReason, setVoidReason] = useState("");
  const [voidError, setVoidError] = useState<string | null>(null);
  const [voiding, setVoiding] = useState(false);
  const [voidConfirmOpen, setVoidConfirmOpen] = useState(false);
  const [voidSheetOpen, setVoidSheetOpen] = useState(false);
  const [returnConfirmOpen, setReturnConfirmOpen] = useState(false);
  const [disclaimerOpen, setDisclaimerOpen] = useState(false);

  const workspaceScope =
    boundWorkspace?.branchId && boundWorkspace.organizationId
      ? {
          organizationId: boundWorkspace.organizationId,
          branchId: boundWorkspace.branchId,
        }
      : null;

  const allowVoid = canVoidSale(sessionGrant);
  const allowProcessReturn = canProcessReturn(sessionGrant);
  const allowViewCost = canViewReports(sessionGrant);

  const saleQuery = useQuery({
    queryKey: ["pos-sale", workspaceScope?.organizationId, workspaceScope?.branchId, saleId],
    enabled: Boolean(workspaceScope && saleId),
    queryFn: ({ signal }) => getSale(workspaceScope!, saleId!, signal),
  });

  const saleActors = useActorDirectory(workspaceScope?.organizationId, [
    saleQuery.data?.recordedBy,
    saleQuery.data?.voidedBy,
  ]);

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
  const costComplete = sale.costStatus === "Complete";
  const showCostSection =
    allowViewCost &&
    (sale.costStatus != null ||
      sale.totalCostSnapshot != null ||
      sale.grossProfit != null ||
      sale.lines.some((line) => line.lineCostSnapshot != null || line.unitCostSnapshot != null));
  const showReturnAction = !isVoided && allowProcessReturn;
  const showVoidAction = !isVoided && allowVoid;

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
        data-testid="summary-print"
        onClick={() => window.print()}
      >
        <Printer className="size-4 shrink-0" aria-hidden />
        {t("summary.printSummary")}
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
        backTo="/sell"
        backLabel={t("summary.backToSell")}
        backTestId="summary-back-to-sell"
        trailing={headerActions}
      />

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

      <Card data-testid="summary-details-section">
        <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold uppercase tracking-wide text-muted">
          {t("summary.sectionDetails")}
        </h2>
        <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.saleNumber")}</dt>
            <dd className="m-0 text-right font-semibold" data-testid="summary-sale-number">
              {sale.saleNumber}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.dateTime")}</dt>
            <dd className="m-0 text-right" data-testid="summary-date-time">
              {new Date(sale.recordedAtUtc).toLocaleString()}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.paymentMethod")}</dt>
            <dd className="m-0 text-right" data-testid="summary-payment-method">
              {paymentLabel}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.status")}</dt>
            <dd className="m-0 text-right" data-testid="summary-status">
              {sale.status}
            </dd>
          </div>
          {sale.shiftNumber ? (
            <div className="flex justify-between gap-2">
              <dt className="text-muted">{t("summary.shift")}</dt>
              <dd className="m-0 text-right" data-testid="summary-shift">
                {sale.shiftNumber}
              </dd>
            </div>
          ) : null}
          <div className="flex justify-between gap-2" data-testid="summary-actor-attribution">
            <dt className="text-muted">{t("common.soldBy")}</dt>
            <dd className="m-0 text-right font-medium" data-testid="summary-sold-by">
              {soldByLabel}
            </dd>
          </div>
          {isVoided ? (
            <div className="flex justify-between gap-2">
              <dt className="text-muted">{t("common.voidedBy")}</dt>
              <dd className="m-0 text-right font-medium" data-testid="summary-voided-by">
                {voidedByLabel}
              </dd>
            </div>
          ) : null}
          {sale.customerDisplayName ? (
            <div className="flex justify-between gap-2">
              <dt className="text-muted">{t("summary.customer")}</dt>
              <dd className="m-0 text-right" data-testid="summary-customer">
                {sale.customerDisplayName}
              </dd>
            </div>
          ) : null}
          {sale.gCashReference ? (
            <div className="flex justify-between gap-2">
              <dt className="text-muted">{t("summary.gcashReference")}</dt>
              <dd className="m-0 text-right" data-testid="summary-gcash-reference">
                {sale.gCashReference}
              </dd>
            </div>
          ) : null}
        </dl>
      </Card>

      <Card data-testid="summary-items-section">
        <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold uppercase tracking-wide text-muted">
          {t("summary.sectionItems")}
        </h2>
        <ul className="m-0 list-none space-y-2 p-0">
          {sale.lines.map((line) => {
            const override = sale.priceOverrides?.find(
              (item) => item.lineNumber === line.lineNumber,
            );
            return (
              <li
                key={line.saleLineId}
                className="flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]"
                data-testid={`summary-line-${line.lineNumber}`}
              >
                <span className="min-w-0">
                  <span className="truncate">
                    {line.name} × {line.quantity} {line.unitOfMeasure}
                  </span>
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
                  {allowViewCost && line.lineCostSnapshot != null ? (
                    <span
                      className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted"
                      data-testid={`summary-line-cost-${line.lineNumber}`}
                    >
                      {t("summary.lineCost")}: <MoneyDisplay amount={line.lineCostSnapshot} />
                    </span>
                  ) : null}
                </span>
                <MoneyDisplay amount={line.lineTotal} />
              </li>
            );
          })}
        </ul>
      </Card>

      <Card data-testid="summary-totals-section">
        <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold uppercase tracking-wide text-muted">
          {t("summary.sectionTotals")}
        </h2>
        <div className="space-y-1 text-[length:var(--exits-text-sm)]">
          <p className="m-0 flex justify-between gap-2">
            <span className="text-muted">{t("summary.subtotal")}</span>
            <MoneyDisplay amount={sale.subtotal} />
          </p>
          <p
            className="m-0 flex justify-between gap-2 text-[length:var(--exits-text-md)] font-semibold"
            data-testid="summary-total"
          >
            <span>{t("summary.total")}</span>
            <MoneyDisplay amount={sale.total} />
          </p>
          {sale.amountTendered != null ? (
            <p className="m-0 flex justify-between gap-2" data-testid="summary-tendered">
              <span className="text-muted">{t("summary.cashReceived")}</span>
              <MoneyDisplay amount={sale.amountTendered} />
            </p>
          ) : null}
          {sale.changeAmount != null ? (
            <p className="m-0 flex justify-between gap-2" data-testid="summary-change">
              <span className="text-muted">{t("summary.change")}</span>
              <MoneyDisplay amount={sale.changeAmount} />
            </p>
          ) : null}
        </div>
      </Card>

      {showCostSection ? (
        <Card data-testid="summary-cost-section">
          <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold uppercase tracking-wide text-muted">
            {t("summary.costSection")}
          </h2>
          <div className="space-y-1 text-[length:var(--exits-text-sm)]">
            {sale.costStatus ? (
              <p className="m-0 flex justify-between gap-2">
                <span className="text-muted">{t("summary.costStatus")}</span>
                <span data-testid="summary-cost-status">
                  {t(productionCostStatusLabelKey(sale.costStatus))}
                </span>
              </p>
            ) : null}
            {costComplete && sale.totalCostSnapshot != null ? (
              <p className="m-0 flex justify-between gap-2" data-testid="summary-total-cost">
                <span className="text-muted">{t("summary.totalCost")}</span>
                <MoneyDisplay amount={sale.totalCostSnapshot} />
              </p>
            ) : sale.totalCostSnapshot != null ? (
              <p className="m-0 flex justify-between gap-2" data-testid="summary-known-cost">
                <span className="text-muted">{t("summary.knownCost")}</span>
                <MoneyDisplay amount={sale.totalCostSnapshot} />
              </p>
            ) : null}
            {costComplete && sale.grossProfit != null ? (
              <p className="m-0 flex justify-between gap-2" data-testid="summary-gross-profit">
                <span className="text-muted">{t("summary.grossProfit")}</span>
                <MoneyDisplay amount={sale.grossProfit} />
              </p>
            ) : null}
            {costComplete && sale.grossMarginPercent != null ? (
              <p className="m-0 flex justify-between gap-2" data-testid="summary-gross-margin">
                <span className="text-muted">{t("summary.grossMargin")}</span>
                <span>{sale.grossMarginPercent.toFixed(1)}%</span>
              </p>
            ) : null}
            {!costComplete && sale.costStatus ? (
              <p className="m-0 text-muted" data-testid="summary-cost-incomplete">
                {sale.costStatus === "Partial"
                  ? t("summary.costIncompletePartial")
                  : t("summary.costIncompleteUnavailable")}
              </p>
            ) : null}
          </div>
        </Card>
      ) : null}

      <Card data-testid="transaction-summary-disclaimer" className="flex flex-col gap-2 print:hidden">
        <button
          type="button"
          className="flex w-full items-center justify-between gap-3 border-0 bg-transparent p-0 text-left font-semibold text-[length:var(--exits-text-sm)] text-foreground"
          aria-expanded={disclaimerOpen}
          data-testid="transaction-summary-disclaimer-toggle"
          onClick={() => setDisclaimerOpen((open) => !open)}
        >
          <span>{t("summary.disclaimerTitle")}</span>
          <ChevronDown
            className={cn(
              "size-4 shrink-0 transition-transform duration-150",
              disclaimerOpen && "rotate-180",
            )}
            aria-hidden
          />
        </button>
        {disclaimerOpen ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="transaction-summary-disclaimer-body"
          >
            {t("summary.disclaimerBody")}
          </p>
        ) : null}
      </Card>

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
          className="m-0 text-[length:var(--exits-text-sm)] text-muted print:hidden"
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
            onClick={() => window.print()}
          >
            <Printer className="size-4 shrink-0" aria-hidden />
            {t("summary.printSummary")}
          </Button>
        </div>
      </StickyActionBar>
    </div>
  );
}
