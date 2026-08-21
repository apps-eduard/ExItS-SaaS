import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canVoidSale } from "@/access/pos-capabilities";
import {
  formatPaymentMethodLabel,
  getSale,
  VOID_REASON_MAX_LENGTH,
  voidSale,
} from "@/api/pos/pos-sales-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { describeCheckoutSaleError } from "@/features/checkout/checkout-sale-errors";
import { useI18n } from "@/i18n/I18nProvider";
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

  const workspaceScope =
    boundWorkspace?.branchId && boundWorkspace.organizationId
      ? {
          organizationId: boundWorkspace.organizationId,
          branchId: boundWorkspace.branchId,
        }
      : null;

  const allowVoid = canVoidSale(sessionGrant);

  const saleQuery = useQuery({
    queryKey: ["pos-sale", workspaceScope?.organizationId, workspaceScope?.branchId, saleId],
    enabled: Boolean(workspaceScope && saleId),
    queryFn: ({ signal }) => getSale(workspaceScope!, saleId!, signal),
  });

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
      setVoidReason("");
    } catch (error) {
      setVoidError(describeCheckoutSaleError(error, t));
    } finally {
      setVoiding(false);
    }
  }

  return (
    <div data-testid="transaction-summary-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader
        title={t("summary.title")}
        description={`${t("summary.subtitle")} · ${sale.saleNumber}`}
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

      <Card data-testid="transaction-summary-disclaimer">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("summary.disclaimerTitle")}
        </h2>
        <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
          {t("summary.disclaimerBody")}
        </p>
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

        <ul className="mb-0 mt-4 list-none space-y-2 border-t border-border pt-3 p-0">
          {sale.lines.map((line) => (
            <li
              key={line.saleLineId}
              className="flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]"
            >
              <span className="min-w-0 truncate">
                {line.name} × {line.quantity} {line.unitOfMeasure}
              </span>
              <MoneyDisplay amount={line.lineTotal} />
            </li>
          ))}
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
      </Card>

      {!isVoided && allowVoid ? (
        <Card data-testid="summary-void-panel">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("summary.voidSection")}
          </h2>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("summary.voidLede")}
          </p>
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
            className="mt-3 min-h-11"
            data-testid="summary-void-confirm"
            disabled={voiding}
            onClick={() => void onVoid()}
          >
            {voiding ? t("summary.voiding") : t("summary.voidConfirm")}
          </Button>
        </Card>
      ) : null}

      {!isVoided && !allowVoid ? (
        <p
          data-testid="summary-void-denied"
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        >
          {t("summary.voidDenied")}
        </p>
      ) : null}

      <Button asChild className="min-h-11 w-fit" data-testid="summary-new-sale">
        <Link to="/sell">{t("summary.newSale")}</Link>
      </Button>
    </div>
  );
}
