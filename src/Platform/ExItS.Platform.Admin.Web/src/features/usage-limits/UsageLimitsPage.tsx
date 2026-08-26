import { useMemo, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { listOrganizations } from "@/api/organizations/organization-client";
import { organizationWorkspaceHref } from "@/api/organizations/organization-id";
import {
  listUsageLimits,
  USAGE_LIMITS_PAGE_SIZE,
  type UsageLimitApiRow,
} from "@/api/ops/usage-limits-client";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import {
  parseUsageLimitsSearchParams,
  usageLimitsSearchParams,
  type UsageLimitsUrlState,
} from "@/features/usage-limits/usage-limits-query";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import { isGuid } from "@/api/support/support-identity-client";
import type { MessageKey } from "@/lib/i18n/messages";

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

const USAGE_STATUS_LABELS: Record<UsageLimitApiRow["usageStatus"], MessageKey> = {
  Measured: "usageLimits.status.measured",
  NotInstrumented: "usageLimits.status.notInstrumented",
  Unavailable: "usageLimits.status.unavailable",
};

function limitLabel(row: UsageLimitApiRow, t: (key: MessageKey) => string): string {
  if (!row.entitlementEnabled) {
    return t("usageLimits.value.disabled");
  }
  if (row.unlimited) {
    return t("usageLimits.value.unlimited");
  }
  if (row.numericLimit != null) {
    return String(row.numericLimit);
  }
  return t("usageLimits.status.featureOnly");
}

function usageLabel(row: UsageLimitApiRow, t: (key: MessageKey) => string): string {
  if (row.usageStatus === "NotInstrumented") {
    return t("usageLimits.value.notInstrumented");
  }
  if (row.usageStatus === "Unavailable" || row.usage == null) {
    return t("usageLimits.value.unavailable");
  }
  if (row.usagePercent != null) {
    return `${row.usage} (${row.usagePercent}%)`;
  }
  return String(row.usage);
}

export function UsageLimitsPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseUsageLimitsSearchParams(searchParams), [searchParams]);
  const [organizationDraft, setOrganizationDraft] = useState(state.organizationId);
  const [productDraft, setProductDraft] = useState(state.productCode);
  const [appliedOrganization, setAppliedOrganization] = useState(state.organizationId);
  const [appliedProduct, setAppliedProduct] = useState(state.productCode);
  if (state.organizationId !== appliedOrganization) {
    setAppliedOrganization(state.organizationId);
    setOrganizationDraft(state.organizationId);
  }
  if (state.productCode !== appliedProduct) {
    setAppliedProduct(state.productCode);
    setProductDraft(state.productCode);
  }
  const canView =
    authorization.status === "loaded" &&
    authorization.hasAnyPermission([PLATFORM_PERMISSIONS.viewPortfolio]);

  const productsQuery = useAuthorizedCatalogProductsQuery();

  const usageQuery = useQuery({
    queryKey: ["usage-limits", state.organizationId, state.productCode, state.page],
    enabled: canView,
    queryFn: ({ signal }) =>
      listUsageLimits(env.platformApiBaseUrl, {
        organizationId: state.organizationId || undefined,
        productCode: state.productCode || undefined,
        page: state.page,
        pageSize: USAGE_LIMITS_PAGE_SIZE,
        signal,
      }),
  });

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <DashboardWidgetSkeleton rows={4} />
      </section>
    );
  }

  if (!canView) {
    return <ShellNotFoundPage />;
  }

  function replaceState(patch: Partial<UsageLimitsUrlState>) {
    const current = parseUsageLimitsSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(usageLimitsSearchParams({ ...current, ...patch }), { replace: true });
  }

  function onFilterSubmit(event: FormEvent) {
    event.preventDefault();
    void (async () => {
      let organizationId = organizationDraft.trim();
      if (organizationId && !isGuid(organizationId)) {
        const page = await listOrganizations(env.platformApiBaseUrl, {
          search: organizationId,
          page: 1,
          pageSize: 1,
        });
        organizationId = page.items[0]?.id ?? "";
      }
      replaceState({
        organizationId,
        productCode: productDraft.trim(),
        page: 1,
      });
      setOrganizationDraft(organizationId);
    })();
  }

  const diagnostic = usageQuery.error
    ? normalizeDiagnosticError({ error: usageQuery.error, operation: "Load usage limits" })
    : null;
  const totalPages = usageQuery.data
    ? Math.max(1, Math.ceil(usageQuery.data.totalCount / USAGE_LIMITS_PAGE_SIZE))
    : 1;

  return (
    <section className="grid min-w-0 gap-4">
      <PageHeader
        title={t("usageLimits.title")}
        description={t("usageLimits.description")}
      />

      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-2"
        onSubmit={onFilterSubmit}
      >
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="usage-limits-organization"
        >
          {t("usageLimits.filters.organization")}
          <Input
            id="usage-limits-organization"
            value={organizationDraft}
            onChange={(event) => setOrganizationDraft(event.target.value)}
            placeholder={t("usageLimits.filters.organizationPlaceholder")}
            autoComplete="off"
          />
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="usage-limits-product"
        >
          {t("usageLimits.filters.product")}
          <select
            id="usage-limits-product"
            className={controlClass}
            value={productDraft}
            disabled={productsQuery.isPending || productsQuery.isError}
            onChange={(event) => setProductDraft(event.target.value)}
          >
            <option value="">{t("usageLimits.filters.product.all")}</option>
            {productsQuery.data?.items.map((product) => (
              <option key={product.code} value={product.code}>
                {product.displayName}
              </option>
            ))}
          </select>
        </label>
        <div className="flex flex-wrap items-end gap-2 md:col-span-2">
          <Button type="submit">{t("usageLimits.filters.apply")}</Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => {
              setOrganizationDraft("");
              setProductDraft("");
              replaceState({ organizationId: "", productCode: "", page: 1 });
            }}
          >
            {t("usageLimits.filters.reset")}
          </Button>
        </div>
      </form>

      {usageQuery.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("usageLimits.loading")}>
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {usageQuery.isError && diagnostic ? (
        <ErrorState diagnostic={diagnostic} headingLevel="h2" onRetry={() => void usageQuery.refetch()} />
      ) : null}

      {usageQuery.isSuccess ? (
        <div className="min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <AdminTable
            caption={t("usageLimits.table.caption")}
            empty={t("usageLimits.table.empty")}
            rows={usageQuery.data.items.map((row) => ({
              ...row,
              id: `${row.subscriptionId}:${row.featureCode}`,
            }))}
            columns={[
              {
                id: "organization",
                header: t("usageLimits.column.organization"),
                cell: (row) => row.organizationDisplayName || row.organizationId,
              },
              {
                id: "product",
                header: t("usageLimits.column.product"),
                cell: (row) => row.productDisplayName || row.productCode,
              },
              {
                id: "plan",
                header: t("usageLimits.column.plan"),
                cell: (row) => row.planDisplayName || row.planKey || t("usageLimits.value.unavailable"),
              },
              {
                id: "feature",
                header: t("usageLimits.column.feature"),
                cell: (row) => (
                  <span className="break-all font-mono text-[length:var(--exits-text-xs)]">
                    {row.featureCode}
                  </span>
                ),
              },
              {
                id: "limit",
                header: t("usageLimits.column.limit"),
                cell: (row) => limitLabel(row, t),
              },
              {
                id: "usage",
                header: t("usageLimits.column.usage"),
                cell: (row) => usageLabel(row, t),
              },
              {
                id: "status",
                header: t("usageLimits.column.status"),
                cell: (row) => (
                  <div className="grid gap-1">
                    <StatusIndicator
                      tone={organizationSubscriptionStatusTone(row.subscriptionStatus)}
                      label={organizationSubscriptionStatusLabel(row.subscriptionStatus, t)}
                    />
                    <span className="text-[length:var(--exits-text-xs)] text-muted">
                      {t(USAGE_STATUS_LABELS[row.usageStatus])}
                    </span>
                  </div>
                ),
              },
            ]}
          />
          {totalPages > 1 ? (
            <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
              <p className="text-[length:var(--exits-text-sm)] text-muted">
                {t("usageLimits.pagination.page")
                  .replace("{page}", String(state.page))
                  .replace("{totalPages}", String(totalPages))}
              </p>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={state.page <= 1}
                  onClick={() => replaceState({ page: state.page - 1 })}
                >
                  {t("usageLimits.pagination.previous")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  disabled={state.page >= totalPages}
                  onClick={() => replaceState({ page: state.page + 1 })}
                >
                  {t("usageLimits.pagination.next")}
                </Button>
              </div>
            </div>
          ) : null}
          {state.organizationId ? (
            <p className="mt-3 text-[length:var(--exits-text-sm)]">
              <Link
                className="text-primary hover:underline"
                to={organizationWorkspaceHref(state.organizationId)}
              >
                {t("usageLimits.link.organizationWorkspace")}
              </Link>
            </p>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
