import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getSale } from "@/api/pos/pos-sales-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Transaction Summary — never labeled Invoice.
 * Disclaimer matches SalesDocumentWording / MAUI SalesDocument_DisclaimerBody.
 */
export function TransactionSummaryPage() {
  const { t } = useI18n();
  const { saleId } = useParams<{ saleId: string }>();
  const { boundWorkspace } = useWorkspace();

  const workspaceScope =
    boundWorkspace?.branchId && boundWorkspace.organizationId
      ? {
          organizationId: boundWorkspace.organizationId,
          branchId: boundWorkspace.branchId,
        }
      : null;

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

  return (
    <div data-testid="transaction-summary-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader
        title={t("summary.title")}
        description={`${t("summary.subtitle")} · ${sale.saleNumber}`}
      />

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
              {sale.paymentMethod}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("summary.status")}</dt>
            <dd className="m-0">{sale.status}</dd>
          </div>
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

      <Button asChild className="min-h-11 w-fit" data-testid="summary-new-sale">
        <Link to="/sell">{t("summary.newSale")}</Link>
      </Button>
    </div>
  );
}
