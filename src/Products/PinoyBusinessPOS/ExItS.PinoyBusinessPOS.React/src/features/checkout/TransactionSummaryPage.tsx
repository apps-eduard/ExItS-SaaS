import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, ChevronDown, Plus, RotateCcw } from "lucide-react";
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
import { BottomSheet } from "@/components/exits/SheetDialog";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { describeCheckoutSaleError } from "@/features/checkout/checkout-sale-errors";
import { invalidatePosStockQueries } from "@/features/catalog/invalidate-pos-stock-queries";
import { productionCostStatusLabelKey } from "@/features/inventory/production-labels";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
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
  const { saleId } = useParams<{ saleId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const [voidReason, setVoidReason] = useState("");
  const [voidError, setVoidError] = useState<string | null>(null);
  const [voiding, setVoiding] = useState(false);
  const [voidSheetOpen, setVoidSheetOpen] = useState(false);
  const [disclaimerOpen, setDisclaimerOpen] = useState(true);

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
        <Button asChild variant="ghost" className="min-h-11 w-fit">
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
        <Button asChild className="min-h-11 w-fit">
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

  const showReturnAction = !isVoided && allowProcessReturn;

  return (
    <div data-testid="transaction-summary-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader
        title={t("summary.title")}
        description={`${t("summary.subtitle")} · ${sale.saleNumber}`}
        trailing={
          !isVoided && allowVoid ? (
            <Button
              type="button"
              variant="destructive"
              className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
              data-testid="summary-void-trigger"
              aria-haspopup="dialog"
              onClick={() => setVoidSheetOpen(true)}
            >
              <Ban className="size-4 shrink-0" aria-hidden />
              {t("summary.voidSection")}
            </Button>
          ) : undefined
        }
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

      <Card data-testid="transaction-summary-disclaimer" className="flex flex-col gap-2">
        <button
          type="button"
          className="flex w-full min-h-11 items-center justify-between gap-3 border-0 bg-transparent p-0 text-left font-semibold text-[length:var(--exits-text-sm)] text-foreground"
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

      <Card>
        <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.saleNumber")}</dt>
            <dd className="m-0 font-semibold" data-testid="summary-sale-number">
              {sale.saleNumber}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.dateTime")}</dt>
            <dd className="m-0" data-testid="summary-date-time">
              {new Date(sale.recordedAtUtc).toLocaleString()}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.paymentMethod")}</dt>
            <dd className="m-0" data-testid="summary-payment-method">
              {paymentLabel}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.status")}</dt>
            <dd className="m-0" data-testid="summary-status">
              {sale.status}
            </dd>
          </div>
          {sale.customerDisplayName ? (
            <div className="flex justify-between gap-2">
              <dt className="text-muted">{t("summary.customer")}</dt>
              <dd className="m-0" data-testid="summary-customer">
                {sale.customerDisplayName}
              </dd>
            </div>
          ) : null}
          {sale.gCashReference ? (
            <div className="flex justify-between gap-2">
              <dt className="text-muted">{t("summary.gcashReference")}</dt>
              <dd className="m-0" data-testid="summary-gcash-reference">
                {sale.gCashReference}
              </dd>
            </div>
          ) : null}
          {sale.shiftNumber ? (
            <div className="flex justify-between gap-2">
              <dt className="text-muted">{t("summary.shift")}</dt>
              <dd className="m-0">{sale.shiftNumber}</dd>
            </div>
          ) : null}
        </dl>

        <div
          className="mt-3 flex flex-col gap-3 border-t border-border pt-3"
          data-testid="summary-actor-attribution"
        >
          <ActorAttribution
            labelKey="common.soldBy"
            actorId={sale.recordedBy}
            occurredAtUtc={sale.recordedAtUtc}
            resolved={saleActors.resolve(sale.recordedBy)}
            isLoading={saleActors.isResolving}
            testId="summary-sold-by"
          />
          {isVoided ? (
            <ActorAttribution
              labelKey="common.voidedBy"
              actorId={sale.voidedBy}
              occurredAtUtc={sale.voidedAtUtc}
              resolved={saleActors.resolve(sale.voidedBy)}
              isLoading={saleActors.isResolving}
              testId="summary-voided-by"
            />
          ) : null}
        </div>

        <ul className="mb-0 mt-4 list-none space-y-2 border-t border-border pt-3 p-0">
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

        <div className="mt-4 space-y-1 border-t border-border pt-3 text-[length:var(--exits-text-sm)]">
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

        {showCostSection ? (
          <div
            className="mt-4 space-y-1 border-t border-border pt-3 text-[length:var(--exits-text-sm)]"
            data-testid="summary-cost-section"
          >
            <p className="m-0 font-semibold">{t("summary.costSection")}</p>
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
        ) : null}
      </Card>

      {!isVoided && allowVoid ? (
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
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
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
            className="mt-3 min-h-11 w-full"
            data-testid="summary-void-confirm"
            disabled={voiding}
            onClick={() => void onVoid()}
          >
            {voiding ? t("summary.voiding") : t("summary.voidConfirm")}
          </Button>
        </BottomSheet>
      ) : null}

      {!isVoided && !allowVoid ? (
        <p
          data-testid="summary-void-denied"
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        >
          {t("summary.voidDenied")}
        </p>
      ) : null}

      <div
        className={cn("grid gap-2", showReturnAction ? "grid-cols-2" : "grid-cols-1")}
        data-testid="summary-footer-actions"
      >
        <Button asChild className="min-h-11 w-full gap-2" data-testid="summary-new-sale">
          <Link to="/sell">
            <Plus className="size-4 shrink-0" aria-hidden />
            {t("summary.newSale")}
          </Link>
        </Button>
        {showReturnAction ? (
          <Button
            asChild
            variant="outline"
            className="min-h-11 w-full gap-2"
            data-testid="summary-return-items"
          >
            <Link to={`/returns/sale/${sale.saleId}`}>
              <RotateCcw className="size-4 shrink-0" aria-hidden />
              {t("returns.returnItems")}
            </Link>
          </Button>
        ) : null}
      </div>
    </div>
  );
}
