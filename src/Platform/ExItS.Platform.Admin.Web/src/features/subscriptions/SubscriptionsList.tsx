import { useMemo, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";
import {
  hasActiveSubscriptionPortfolioFilters,
  parseSubscriptionPortfolioSearchParams,
  SUBSCRIPTION_PORTFOLIO_PAGE_SIZE,
  SUBSCRIPTION_PORTFOLIO_SORT_BY,
  SUBSCRIPTION_PORTFOLIO_STATUSES,
  subscriptionDetailHref,
  subscriptionPortfolioSearchParams,
  type SubscriptionPortfolioUrlState,
} from "@/api/subscriptions/subscription-portfolio-query";
import type { OrganizationSubscriptionSortBy } from "@/api/organizations/subscription-list-query";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import { subscriptionPeriodEnd } from "@/features/organizations/subscription-lifecycle";
import { useSubscriptionPortfolioQuery } from "@/features/subscriptions/use-subscription-portfolio-queries";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<OrganizationSubscriptionSortBy, MessageKey> = {
  UpdatedAtUtc: "organization.subscriptions.sort.updatedAtUtc",
  CreatedAtUtc: "organization.subscriptions.sort.createdAtUtc",
  Status: "organization.subscriptions.sort.status",
  ProductCode: "organization.subscriptions.sort.productCode",
  TrialEndUtc: "organization.subscriptions.sort.trialEndUtc",
  PaidPeriodEndUtc: "organization.subscriptions.sort.paidPeriodEndUtc",
  ProductDisplayName: "organization.subscriptions.sort.productDisplayName",
  PlanDisplayName: "organization.subscriptions.sort.planDisplayName",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
  }).format(date);
}

function productLabel(item: OrganizationSubscription): string {
  return item.productDisplayName || item.productCode;
}

function planLabel(item: OrganizationSubscription): string {
  return item.planDisplayName || item.planKey || item.planId;
}

function organizationLabel(item: OrganizationSubscription): string {
  return item.organizationDisplayName || item.organizationId;
}

export function SubscriptionsList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(
    () => parseSubscriptionPortfolioSearchParams(searchParams),
    [searchParams],
  );
  const [searchDraft, setSearchDraft] = useState(state.search);
  const [productDraft, setProductDraft] = useState(state.productCode);
  const [appliedSearch, setAppliedSearch] = useState(state.search);
  const [appliedProduct, setAppliedProduct] = useState(state.productCode);
  if (state.search !== appliedSearch) {
    setAppliedSearch(state.search);
    setSearchDraft(state.search);
  }
  if (state.productCode !== appliedProduct) {
    setAppliedProduct(state.productCode);
    setProductDraft(state.productCode);
  }

  const productsQuery = useAuthorizedCatalogProductsQuery();
  const query = useSubscriptionPortfolioQuery(state, enabled);

  function replaceState(patch: Partial<SubscriptionPortfolioUrlState>) {
    const current = parseSubscriptionPortfolioSearchParams(
      new URLSearchParams(window.location.search),
    );
    setSearchParams(subscriptionPortfolioSearchParams({ ...current, ...patch }), { replace: true });
  }

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    replaceState({
      search: searchDraft.trim(),
      productCode: productDraft.trim(),
      page: 1,
    });
  }

  function resetFilters() {
    setSearchDraft("");
    setProductDraft("");
    replaceState({
      page: 1,
      search: "",
      status: "",
      isTrial: "",
      productCode: "",
      planId: "",
      sortBy: "UpdatedAtUtc",
      sortDesc: true,
    });
  }

  const filtersActive = hasActiveSubscriptionPortfolioFilters(state);
  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / SUBSCRIPTION_PORTFOLIO_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load subscription portfolio",
      })
    : null;

  return (
    <div className="grid gap-3">
      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-2"
        onSubmit={onSearchSubmit}
      >
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="sub-portfolio-search"
        >
          {t("subscriptions.portfolio.search")}
          <Input
            id="sub-portfolio-search"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            placeholder={t("subscriptions.portfolio.searchPlaceholder")}
            name="search"
            autoComplete="off"
          />
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="sub-portfolio-product"
        >
          {t("subscriptions.portfolio.product")}
          <select
            id="sub-portfolio-product"
            className={controlClass}
            value={productDraft}
            disabled={productsQuery.isPending || productsQuery.isError}
            onChange={(event) => setProductDraft(event.target.value)}
          >
            <option value="">{t("subscriptions.portfolio.product.all")}</option>
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
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="sub-portfolio-status"
        >
          {t("subscriptions.portfolio.status")}
          <select
            id="sub-portfolio-status"
            className={controlClass}
            value={state.status}
            onChange={(event) =>
              replaceState({
                status: event.target.value as SubscriptionPortfolioUrlState["status"],
                page: 1,
              })
            }
          >
            <option value="">{t("subscriptions.portfolio.status.all")}</option>
            {SUBSCRIPTION_PORTFOLIO_STATUSES.map((status) => (
              <option key={status} value={status}>
                {organizationSubscriptionStatusLabel(status, t)}
              </option>
            ))}
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="sub-portfolio-trial"
        >
          {t("subscriptions.portfolio.trial")}
          <select
            id="sub-portfolio-trial"
            className={controlClass}
            value={state.isTrial}
            onChange={(event) =>
              replaceState({
                isTrial: event.target.value as SubscriptionPortfolioUrlState["isTrial"],
                page: 1,
              })
            }
          >
            <option value="">{t("subscriptions.portfolio.trial.all")}</option>
            <option value="true">{t("subscriptions.portfolio.trial.yes")}</option>
            <option value="false">{t("subscriptions.portfolio.trial.no")}</option>
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="sub-portfolio-sort"
        >
          {t("subscriptions.portfolio.sort")}
          <select
            id="sub-portfolio-sort"
            className={controlClass}
            value={state.sortBy}
            onChange={(event) =>
              replaceState({
                sortBy: event.target.value as OrganizationSubscriptionSortBy,
                page: 1,
              })
            }
          >
            {SUBSCRIPTION_PORTFOLIO_SORT_BY.map((sortBy) => (
              <option key={sortBy} value={sortBy}>
                {t(SORT_LABELS[sortBy])}
              </option>
            ))}
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="sub-portfolio-order"
        >
          {t("subscriptions.portfolio.sort.direction")}
          <select
            id="sub-portfolio-order"
            className={controlClass}
            value={state.sortDesc ? "desc" : "asc"}
            onChange={(event) =>
              replaceState({
                sortDesc: event.target.value === "desc",
                page: 1,
              })
            }
          >
            <option value="desc">{t("organizations.sort.desc")}</option>
            <option value="asc">{t("organizations.sort.asc")}</option>
          </select>
        </label>
        <div className="flex flex-wrap gap-2 md:col-span-2">
          <Button type="submit" size="sm">
            {t("subscriptions.portfolio.searchSubmit")}
          </Button>
          {filtersActive ? (
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={resetFilters}
            >
              {t("subscriptions.portfolio.reset")}
            </Button>
          ) : null}
        </div>
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
          aria-label={t("subscriptions.portfolio.loading")}
        >
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("subscriptions.portfolio.error")}
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
                  ? t("subscriptions.portfolio.zeroResult")
                  : t("subscriptions.portfolio.empty")}
              </p>
              {filtersActive ? (
                <Button type="button" size="sm" variant="outline" className="mt-2" onClick={resetFilters}>
                  {t("subscriptions.portfolio.reset")}
                </Button>
              ) : null}
            </div>
          ) : showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("subscriptions.portfolio.caption")}
                empty={t("subscriptions.portfolio.empty")}
                columns={[
                  {
                    id: "organization",
                    header: t("subscriptions.portfolio.column.organization"),
                    cell: (item) => (
                      <Link
                        className="font-medium text-primary hover:underline"
                        to={`/admin/organizations/${item.organizationId}`}
                      >
                        {organizationLabel(item)}
                      </Link>
                    ),
                  },
                  {
                    id: "product",
                    header: t("subscriptions.portfolio.column.product"),
                    cell: (item) => (
                      <Link
                        className="font-medium text-primary hover:underline"
                        to={subscriptionDetailHref(item.id)}
                      >
                        {productLabel(item)}
                      </Link>
                    ),
                  },
                  {
                    id: "plan",
                    header: t("subscriptions.portfolio.column.plan"),
                    cell: (item) => (
                      <span className="break-words text-muted">{planLabel(item)}</span>
                    ),
                  },
                  {
                    id: "status",
                    header: t("subscriptions.portfolio.column.status"),
                    cell: (item) => (
                      <StatusIndicator
                        tone={organizationSubscriptionStatusTone(item.status)}
                        label={organizationSubscriptionStatusLabel(item.status, t)}
                      />
                    ),
                  },
                  {
                    id: "trial",
                    header: t("subscriptions.portfolio.column.trial"),
                    cell: (item) => formatInstant(item.trialEndUtc, language) || "—",
                  },
                  {
                    id: "period",
                    header: t("subscriptions.portfolio.column.period"),
                    cell: (item) =>
                      formatInstant(subscriptionPeriodEnd(item), language) || "—",
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
                    to={subscriptionDetailHref(item.id)}
                  >
                    {productLabel(item)}
                  </Link>
                  <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                    {organizationLabel(item)} · {planLabel(item)}
                  </p>
                  <div className="mt-1.5">
                    <StatusIndicator
                      tone={organizationSubscriptionStatusTone(item.status)}
                      label={organizationSubscriptionStatusLabel(item.status, t)}
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
                {t("subscriptions.portfolio.previous")}
              </Button>
              <p className="text-[length:var(--exits-text-xs)] text-muted">
                {t("subscriptions.portfolio.page")} {state.page} / {totalPages}
              </p>
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={state.page >= totalPages}
                onClick={() => replaceState({ page: state.page + 1 })}
              >
                {t("subscriptions.portfolio.next")}
              </Button>
            </div>
          ) : null}
        </>
      ) : null}
    </div>
  );
}
