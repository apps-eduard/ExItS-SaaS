import { useMemo, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  ORGANIZATION_PAYMENT_STATUSES,
  type OrganizationPayment,
  type OrganizationPaymentStatus,
} from "@/api/organizations/billing-list-query";
import {
  hasActivePaymentPortfolioFilters,
  parsePaymentPortfolioSearchParams,
  PAYMENT_PORTFOLIO_PAGE_SIZE,
  paymentDetailHref,
  paymentPortfolioSearchParams,
  type PaymentPortfolioUrlState,
} from "@/api/payments/payment-client";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import { MANUAL_PAYMENT_METHODS } from "@/features/organizations/billing-lifecycle";
import { usePaymentPortfolioQuery } from "@/features/payments/use-payment-portfolio-queries";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<string, MessageKey> = {
  PendingConfirmation: "organization.billing.status.PendingConfirmation",
  Confirmed: "organization.billing.status.Confirmed",
  Rejected: "organization.billing.status.Rejected",
  Voided: "organization.billing.status.Voided",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Confirmed") return "success";
  if (status === "PendingConfirmation") return "warning";
  if (status === "Rejected" || status === "Voided") return "danger";
  return "neutral";
}

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function formatAmount(item: OrganizationPayment): string {
  return `${item.amount} ${item.currencyCode}`;
}

export function PaymentsList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parsePaymentPortfolioSearchParams(searchParams), [searchParams]);
  const productsQuery = useAuthorizedCatalogProductsQuery();
  const query = usePaymentPortfolioQuery(state, enabled);

  function replaceState(patch: Partial<PaymentPortfolioUrlState>) {
    const current = parsePaymentPortfolioSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(paymentPortfolioSearchParams({ ...current, ...patch }), { replace: true });
  }

  function resetFilters() {
    replaceState({ page: 1, status: "", productCode: "", method: "" });
  }

  function onFilterSubmit(event: FormEvent) {
    event.preventDefault();
  }

  const filtersActive = hasActivePaymentPortfolioFilters(state);
  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / PAYMENT_PORTFOLIO_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load payment portfolio" })
    : null;

  return (
    <div className="grid gap-3">
      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-3"
        onSubmit={onFilterSubmit}
      >
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("payments.portfolio.status")}
          <select
            className={controlClass}
            value={state.status}
            aria-label={t("payments.portfolio.status")}
            onChange={(event) =>
              replaceState({
                status: event.target.value as OrganizationPaymentStatus | "",
                page: 1,
              })
            }
          >
            <option value="">{t("payments.portfolio.status.all")}</option>
            {ORGANIZATION_PAYMENT_STATUSES.map((status) => (
              <option key={status} value={status}>
                {STATUS_LABELS[status] ? t(STATUS_LABELS[status]!) : status}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("payments.portfolio.product")}
          <select
            className={controlClass}
            value={state.productCode}
            aria-label={t("payments.portfolio.product")}
            disabled={productsQuery.isPending || productsQuery.isError}
            onChange={(event) => replaceState({ productCode: event.target.value, page: 1 })}
          >
            <option value="">{t("payments.portfolio.product.all")}</option>
            {productsQuery.data?.items.map((product) => (
              <option key={product.code} value={product.code}>
                {product.displayName}
              </option>
            ))}
          </select>
          {productsQuery.isPending ? (
            <span className="font-normal text-muted">{t("commercial.productCatalog.loading")}</span>
          ) : null}
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("payments.portfolio.method")}
          <select
            className={controlClass}
            value={state.method}
            aria-label={t("payments.portfolio.method")}
            onChange={(event) => replaceState({ method: event.target.value, page: 1 })}
          >
            <option value="">{t("payments.portfolio.method.all")}</option>
            {MANUAL_PAYMENT_METHODS.map((method) => (
              <option key={method} value={method}>
                {method}
              </option>
            ))}
          </select>
        </label>
        {filtersActive ? (
          <div className="md:col-span-3">
            <Button type="button" size="sm" variant="outline" onClick={resetFilters}>
              {t("payments.portfolio.reset")}
            </Button>
          </div>
        ) : null}
      </form>

      {productsQuery.isError ? (
        <Alert title={t("commercial.catalogUnavailable")} tone="danger">
          {t("organizations.product.catalogUnavailable")}
        </Alert>
      ) : null}

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("payments.portfolio.loading")}
        >
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("payments.portfolio.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <>
          {query.data.items.length === 0 ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
                {filtersActive
                  ? t("payments.portfolio.zeroResult")
                  : t("payments.portfolio.empty")}
              </p>
              {filtersActive ? (
                <Button type="button" size="sm" variant="outline" className="mt-2" onClick={resetFilters}>
                  {t("payments.portfolio.reset")}
                </Button>
              ) : null}
            </div>
          ) : showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("payments.portfolio.caption")}
                empty={t("payments.portfolio.empty")}
                columns={[
                  {
                    id: "organization",
                    header: t("payments.portfolio.column.organization"),
                    cell: (item) => (
                      <Link
                        className="font-medium text-primary hover:underline"
                        to={`/admin/organizations/${item.organizationId}`}
                      >
                        {item.organizationId}
                      </Link>
                    ),
                  },
                  {
                    id: "product",
                    header: t("payments.portfolio.column.product"),
                    cell: (item) => (
                      <Link
                        className="font-medium text-primary hover:underline"
                        to={paymentDetailHref(item.id)}
                      >
                        {item.productCode}
                      </Link>
                    ),
                  },
                  {
                    id: "amount",
                    header: t("payments.portfolio.column.amount"),
                    cell: (item) => formatAmount(item),
                  },
                  {
                    id: "method",
                    header: t("payments.portfolio.column.method"),
                    cell: (item) => item.method,
                  },
                  {
                    id: "status",
                    header: t("payments.portfolio.column.status"),
                    cell: (item) => (
                      <StatusIndicator
                        tone={statusTone(item.status)}
                        label={
                          STATUS_LABELS[item.status] ? t(STATUS_LABELS[item.status]!) : item.status
                        }
                      />
                    ),
                  },
                  {
                    id: "paid",
                    header: t("payments.portfolio.column.paid"),
                    cell: (item) => formatInstant(item.paidAtUtc, language) || "—",
                  },
                ]}
                rows={query.data.items}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {query.data.items.map((item) => (
                <li
                  key={item.id}
                  className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                >
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={paymentDetailHref(item.id)}
                  >
                    {formatAmount(item)}
                  </Link>
                  <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                    {item.productCode} · {item.method}
                  </p>
                  <div className="mt-1.5">
                    <StatusIndicator
                      tone={statusTone(item.status)}
                      label={
                        STATUS_LABELS[item.status] ? t(STATUS_LABELS[item.status]!) : item.status
                      }
                    />
                  </div>
                </li>
              ))}
            </ul>
          )}
          {totalPages > 1 ? (
            <div className="flex flex-wrap items-center gap-2">
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={state.page <= 1}
                onClick={() => replaceState({ page: state.page - 1 })}
              >
                {t("payments.portfolio.previous")}
              </Button>
              <p className="text-[length:var(--exits-text-xs)] text-muted">
                {t("payments.portfolio.page")} {state.page} / {totalPages}
              </p>
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={state.page >= totalPages}
                onClick={() => replaceState({ page: state.page + 1 })}
              >
                {t("payments.portfolio.next")}
              </Button>
            </div>
          ) : null}
        </>
      ) : null}
    </div>
  );
}
