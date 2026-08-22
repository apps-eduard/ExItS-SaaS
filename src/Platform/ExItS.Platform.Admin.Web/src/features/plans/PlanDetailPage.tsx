import { Link, useParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { parsePlanId } from "@/api/catalog/plan-id";
import { plansListHref } from "@/api/catalog/plan-list-query";
import { PlatformApiError } from "@/api/platform-http";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Skeleton } from "@/components/ui/skeleton";
import { useCatalogPlanDetailQuery } from "@/features/catalog/use-catalog-detail-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { PlanCommercialOperator } from "@/features/plans/PlanCommercialOperator";
import { PlanNotFoundPage } from "@/features/plans/PlanNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Inactive: "plans.status.Inactive",
  Retired: "plans.status.Retired",
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

export function PlanDetailPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const params = useParams();
  const planId = parsePlanId(params.planId);
  const canView =
    authorization.status === "loaded" &&
    authorization.hasAnyPermission([PLATFORM_PERMISSIONS.viewPortfolio]);

  const planQuery = useCatalogPlanDetailQuery(canView ? planId : null);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) return <ShellNotFoundPage />;
  if (planId == null) return <PlanNotFoundPage />;

  if (planQuery.isPending) {
    return (
      <section
        className="grid max-w-4xl gap-3"
        role="status"
        aria-busy="true"
        aria-label={t("plans.detail.loading")}
      >
        <DashboardWidgetSkeleton rows={10} />
      </section>
    );
  }

  if (planQuery.isError && isForbidden(planQuery.error)) return <ShellNotFoundPage />;
  if (planQuery.isError && isNotFound(planQuery.error)) return <PlanNotFoundPage />;

  if (planQuery.isError) {
    return (
      <ErrorState
        diagnostic={normalizeDiagnosticError({ error: planQuery.error, operation: "Load plan" })}
        title={t("plans.detail.error")}
        headingLevel="h1"
        onRetry={() => void planQuery.refetch()}
      />
    );
  }

  const plan = planQuery.data;
  if (!plan) return <PlanNotFoundPage />;

  return (
    <section className="grid max-w-4xl gap-4">
      <p className="text-[length:var(--exits-text-sm)]">
        <Link className="text-primary hover:underline" to={plansListHref()}>
          {t("plans.detail.back")}
        </Link>
      </p>
      <PageHeader
        title={plan.displayName}
        description={t("plans.editor.pageDescription").replace("{code}", plan.code)}
        actions={
          <StatusIndicator
            tone={statusTone(plan.status)}
            label={STATUS_LABELS[plan.status] ? t(STATUS_LABELS[plan.status]!) : plan.status}
          />
        }
      />
      <PlanCommercialOperator plan={plan} />
    </section>
  );
}
