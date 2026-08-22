import { useMemo } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  entitlementPortfolioSearchParams,
  parseEntitlementPortfolioSearchParams,
  ENTITLEMENT_PORTFOLIO_PAGE_SIZE,
  type EntitlementLatestSummary,
} from "@/api/entitlements/entitlement-portfolio-client";
import { subscriptionDetailHref } from "@/api/subscriptions/subscription-portfolio-query";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useEntitlementPortfolioQuery } from "@/features/entitlements/use-entitlement-portfolio-queries";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function productLabel(item: EntitlementLatestSummary): string {
  return item.productDisplayName || item.productCode;
}

function organizationLabel(item: EntitlementLatestSummary): string {
  return item.organizationDisplayName || item.organizationId;
}

export function EntitlementsPortfolioPage() {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(
    () => parseEntitlementPortfolioSearchParams(searchParams),
    [searchParams],
  );
  const canList =
    authorization.status === "loaded" &&
    authorization.hasAnyPermission([
      PLATFORM_PERMISSIONS.manageEntitlementOverrides,
      PLATFORM_PERMISSIONS.manageSubscriptions,
      PLATFORM_PERMISSIONS.viewPortfolio,
    ]);

  const query = useEntitlementPortfolioQuery(state, canList);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canList) return <ShellNotFoundPage />;

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ENTITLEMENT_PORTFOLIO_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load entitlement portfolio" })
    : null;

  function replacePage(page: number) {
    const current = parseEntitlementPortfolioSearchParams(
      new URLSearchParams(window.location.search),
    );
    setSearchParams(entitlementPortfolioSearchParams({ ...current, page }), { replace: true });
  }

  return (
    <section className="grid gap-4">
      <PageHeader
        title={t("entitlements.portfolio.title")}
        description={t("entitlements.portfolio.description")}
      />

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("entitlements.portfolio.loading")}
        >
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("entitlements.portfolio.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <>
          {query.data.items.length === 0 ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
                {t("entitlements.portfolio.empty")}
              </p>
            </div>
          ) : showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("entitlements.portfolio.caption")}
                empty={t("entitlements.portfolio.empty")}
                columns={[
                  {
                    id: "organization",
                    header: t("entitlements.portfolio.column.organization"),
                    cell: (item) => (
                      <Link
                        className="font-medium text-primary hover:underline"
                        to={`/admin/organizations/${item.organizationId}/entitlements`}
                      >
                        {organizationLabel(item)}
                      </Link>
                    ),
                  },
                  {
                    id: "product",
                    header: t("entitlements.portfolio.column.product"),
                    cell: (item) => productLabel(item),
                  },
                  {
                    id: "subscriptionStatus",
                    header: t("entitlements.portfolio.column.subscriptionStatus"),
                    cell: (item) => (
                      <StatusIndicator
                        tone={organizationSubscriptionStatusTone(item.subscriptionStatus)}
                        label={organizationSubscriptionStatusLabel(item.subscriptionStatus, t)}
                      />
                    ),
                  },
                  {
                    id: "generated",
                    header: t("entitlements.portfolio.column.generated"),
                    cell: (item) => formatInstant(item.generatedAtUtc, language) || "—",
                  },
                  {
                    id: "subscription",
                    header: t("entitlements.portfolio.column.subscription"),
                    cell: (item) => (
                      <Link
                        className="font-mono text-[length:var(--exits-text-xs)] text-primary hover:underline"
                        to={subscriptionDetailHref(item.subscriptionId)}
                      >
                        {t("entitlements.portfolio.viewSubscription")}
                      </Link>
                    ),
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
                    to={`/admin/organizations/${item.organizationId}/entitlements`}
                  >
                    {organizationLabel(item)}
                  </Link>
                  <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                    {productLabel(item)}
                  </p>
                </li>
              ))}
            </ul>
          )}
          {query.data.totalCount > ENTITLEMENT_PORTFOLIO_PAGE_SIZE ? (
            <div className="flex flex-wrap items-center gap-2">
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={state.page <= 1}
                onClick={() => replacePage(state.page - 1)}
              >
                {t("entitlements.portfolio.previous")}
              </Button>
              <p className="text-[length:var(--exits-text-xs)] text-muted">
                {t("entitlements.portfolio.page")} {state.page} / {totalPages}
              </p>
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={state.page >= totalPages}
                onClick={() => replacePage(state.page + 1)}
              >
                {t("entitlements.portfolio.next")}
              </Button>
            </div>
          ) : null}
        </>
      ) : null}
    </section>
  );
}
