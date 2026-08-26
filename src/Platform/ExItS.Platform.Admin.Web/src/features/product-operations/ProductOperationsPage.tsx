import { useQueries, useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getProductOverview } from "@/api/admin/admin-portfolio-client";
import { getSystemHealth } from "@/api/ops/system-health-client";
import { listCatalogProductsPage } from "@/api/catalog/product-catalog-client";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";

const SERVICE_BY_PRODUCT: Record<string, string> = {
  "pinoy-business-pos": "pos-api",
};

const SERVICE_LABELS: Record<string, MessageKey> = {
  "platform-api": "systemHealth.service.platformApi",
  "pos-api": "systemHealth.service.posApi",
};

function catalogStatusTone(status: string): "success" | "warning" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Inactive") {
    return "warning";
  }
  return "neutral";
}

export function ProductOperationsPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" && authorization.isPlatformAdministrator;

  const productsQuery = useQuery({
    queryKey: ["product-operations", "catalog-products"],
    enabled: canView,
    queryFn: ({ signal }) =>
      listCatalogProductsPage(env.platformApiBaseUrl, {
        page: 1,
        pageSize: 50,
        signal,
      }),
  });

  const healthQuery = useQuery({
    queryKey: ["product-operations", "system-health"],
    enabled: canView,
    queryFn: ({ signal }) => getSystemHealth(env.platformApiBaseUrl, signal),
  });

  const overviewQueries = useQueries({
    queries: (productsQuery.data?.items ?? []).map((product) => ({
      queryKey: ["product-operations", "overview", product.code],
      enabled: canView && productsQuery.isSuccess,
      queryFn: ({ signal }: { signal?: AbortSignal }) =>
        getProductOverview(env.platformApiBaseUrl, product.code, signal),
    })),
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

  const diagnostic = productsQuery.error
    ? normalizeDiagnosticError({ error: productsQuery.error, operation: "Load product operations" })
    : healthQuery.error
      ? normalizeDiagnosticError({ error: healthQuery.error, operation: "Load system health" })
      : null;

  const rows = (productsQuery.data?.items ?? []).map((product, index) => {
    const overview = overviewQueries[index]?.data;
    const serviceName = SERVICE_BY_PRODUCT[product.code];
    const service = serviceName
      ? healthQuery.data?.services.find((item) => item.name === serviceName)
      : undefined;
    return {
      id: product.id,
      productCode: product.code,
      productLabel: product.displayName,
      catalogStatus: product.status,
      activePlans: overview?.plans.filter((plan) => plan.status === "Active").length ?? null,
      publishedVersions: overview?.publishedPlanVersions.length ?? null,
      featureCount: overview?.features.length ?? null,
      trialCount: overview?.trials.length ?? null,
      relatedHealthStatus: service?.status,
      relatedHealthLabel: serviceName ? t(SERVICE_LABELS[serviceName] ?? "systemHealth.services.unknown") : null,
      overviewError: overviewQueries[index]?.isError ?? false,
    };
  });

  return (
    <section className="grid min-w-0 gap-4">
      <PageHeader
        title={t("productOperations.title")}
        description={t("productOperations.description")}
        actions={
          <Link className="text-[length:var(--exits-text-sm)] text-primary hover:underline" to="/admin/system-health">
            {t("productOperations.link.platformHealth")}
          </Link>
        }
      />

      {productsQuery.isPending || healthQuery.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("productOperations.loading")}>
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          headingLevel="h2"
          onRetry={() => {
            void productsQuery.refetch();
            void healthQuery.refetch();
          }}
        />
      ) : null}

      {productsQuery.data ? (
        <div className="min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <AdminTable
            caption={t("productOperations.table.caption")}
            empty={t("productOperations.table.empty")}
            rows={rows}
            columns={[
              {
                id: "product",
                header: t("productOperations.column.product"),
                cell: (row) => row.productLabel,
              },
              {
                id: "status",
                header: t("productOperations.column.catalogStatus"),
                cell: (row) => (
                  <StatusIndicator
                    tone={catalogStatusTone(row.catalogStatus)}
                    label={row.catalogStatus}
                  />
                ),
              },
              {
                id: "plans",
                header: t("productOperations.column.activePlans"),
                cell: (row) =>
                  row.overviewError
                    ? t("productOperations.value.unavailable")
                    : row.activePlans != null
                      ? String(row.activePlans)
                      : t("productOperations.value.unavailable"),
              },
              {
                id: "features",
                header: t("productOperations.column.features"),
                cell: (row) =>
                  row.overviewError
                    ? t("productOperations.value.unavailable")
                    : row.featureCount != null
                      ? String(row.featureCount)
                      : t("productOperations.value.unavailable"),
              },
              {
                id: "health",
                header: t("productOperations.column.relatedHealth"),
                cell: (row) =>
                  row.relatedHealthLabel && row.relatedHealthStatus ? (
                    <StatusIndicator tone="neutral" label={`${row.relatedHealthLabel}: ${row.relatedHealthStatus}`} />
                  ) : (
                    t("productOperations.value.notConfigured")
                  ),
              },
            ]}
          />
          <p className="mt-3 text-[length:var(--exits-text-sm)] text-muted">
            {t("productOperations.hint.noTelemetry")}
          </p>
        </div>
      ) : null}
    </section>
  );
}
