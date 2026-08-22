import { Link, useParams } from "react-router-dom";
import { planDetailHref } from "@/api/catalog/plan-list-query";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { parseProductId, productsListHref } from "@/api/catalog/product-id";
import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useCatalogProductDetailQuery,
  useCatalogProductPlansQuery,
} from "@/features/catalog/use-catalog-detail-queries";
import { ProductLifecycleOperator } from "@/features/products/ProductLifecycleOperator";
import { ProductNotFoundPage } from "@/features/products/ProductNotFoundPage";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Inactive: "products.status.Inactive",
  Retired: "products.status.Retired",
};

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") return "success";
  if (status === "Inactive") return "warning";
  if (status === "Retired") return "danger";
  return "neutral";
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

function isNotFound(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 404;
}

function formatMoney(
  value: number | undefined,
  currency: string | undefined,
  language: string,
): string | null {
  if (value === undefined) return null;
  const code = currency && currency.length > 0 ? currency : "PHP";
  try {
    return new Intl.NumberFormat(language === "fil-PH" ? "fil-PH" : "en-PH", {
      style: "currency",
      currency: code,
    }).format(value);
  } catch {
    return `${value} ${code}`;
  }
}

export function ProductDetailPage() {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const params = useParams();
  const productId = parseProductId(params.productId);
  const showTable = useMediaQuery("(min-width: 768px)");
  const canView =
    authorization.status === "loaded" &&
    authorization.hasAnyPermission([PLATFORM_PERMISSIONS.viewPortfolio]);

  const productQuery = useCatalogProductDetailQuery(canView ? productId : null);
  const plansQuery = useCatalogProductPlansQuery(
    canView && productQuery.data ? productQuery.data.code : null,
  );

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) return <ShellNotFoundPage />;
  if (productId == null) return <ProductNotFoundPage />;

  if (productQuery.isPending) {
    return (
      <section
        className="grid max-w-3xl gap-3"
        role="status"
        aria-busy="true"
        aria-label={t("products.detail.loading")}
      >
        <DashboardWidgetSkeleton rows={8} />
      </section>
    );
  }

  if (productQuery.isError && isForbidden(productQuery.error)) return <ShellNotFoundPage />;
  if (productQuery.isError && isNotFound(productQuery.error)) return <ProductNotFoundPage />;

  if (productQuery.isError) {
    return (
      <ErrorState
        diagnostic={normalizeDiagnosticError({
          error: productQuery.error,
          operation: "Load product",
        })}
        title={t("products.detail.error")}
        headingLevel="h1"
        onRetry={() => void productQuery.refetch()}
      />
    );
  }

  const product = productQuery.data;
  if (!product) return <ProductNotFoundPage />;

  return (
    <section className="grid max-w-3xl gap-4">
      <p className="text-[length:var(--exits-text-sm)]">
        <Link className="text-primary hover:underline" to={productsListHref()}>
          {t("products.detail.back")}
        </Link>
      </p>
      <PageHeader
        title={product.displayName}
        description={t("products.editor.pageDescription").replace("{code}", product.code)}
        actions={
          <StatusIndicator
            tone={statusTone(product.status)}
            label={
              STATUS_LABELS[product.status] ? t(STATUS_LABELS[product.status]!) : product.status
            }
          />
        }
      />
      <ProductLifecycleOperator product={product} />
      <DashboardSection
        title={t("products.detail.plans")}
        description={t("products.detail.plans.hint")}
      >
        {plansQuery.isPending ? (
          <div role="status" aria-busy="true">
            <DashboardWidgetSkeleton rows={4} />
          </div>
        ) : null}
        {plansQuery.isError ? (
          <ErrorState
            diagnostic={normalizeDiagnosticError({
              error: plansQuery.error,
              operation: "Load product plans",
            })}
            title={t("products.detail.plans.error")}
            headingLevel="h2"
            onRetry={() => void plansQuery.refetch()}
          />
        ) : null}
        {plansQuery.data && plansQuery.data.length === 0 ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
            {t("products.detail.plans.empty")}
          </p>
        ) : null}
        {plansQuery.data && plansQuery.data.length > 0 ? (
          <ProductPlansList items={plansQuery.data} showTable={showTable} language={language} />
        ) : null}
      </DashboardSection>
    </section>
  );
}

function ProductPlansList({
  items,
  showTable,
  language,
}: {
  items: CatalogPlan[];
  showTable: boolean;
  language: string;
}) {
  const { t } = usePreferences();
  if (showTable) {
    return (
      <AdminTable
        caption={t("products.detail.plans.caption")}
        empty={t("products.detail.plans.empty")}
        columns={[
          {
            id: "name",
            header: t("plans.column.displayName"),
            cell: (plan) => (
              <Link
                className="font-medium text-primary hover:underline"
                to={planDetailHref(plan.id)}
              >
                {plan.displayName}
              </Link>
            ),
          },
          {
            id: "code",
            header: t("plans.column.code"),
            cell: (plan) => (
              <span className="font-mono text-[length:var(--exits-text-xs)]">{plan.code}</span>
            ),
          },
          {
            id: "status",
            header: t("plans.column.status"),
            cell: (plan) => (
              <StatusIndicator
                tone={statusTone(plan.status)}
                label={STATUS_LABELS[plan.status] ? t(STATUS_LABELS[plan.status]!) : plan.status}
              />
            ),
          },
          {
            id: "price",
            header: t("plans.column.monthlyPrice"),
            cell: (plan) => (
              <span className="tabular-nums text-muted">
                {formatMoney(plan.monthlyPrice, plan.currencyCode, language) ?? "—"}
              </span>
            ),
          },
        ]}
        rows={items}
      />
    );
  }
  return (
    <ul className="grid gap-2">
      {items.map((plan) => (
        <li
          key={plan.id}
          className="rounded-[var(--exits-density-radius)] border border-border px-3 py-2"
        >
          <Link className="font-medium text-primary hover:underline" to={planDetailHref(plan.id)}>
            {plan.displayName}
          </Link>
          <p className="font-mono text-[length:var(--exits-text-xs)] text-muted">{plan.code}</p>
        </li>
      ))}
    </ul>
  );
}
