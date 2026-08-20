import { Link, useParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { parsePlanId } from "@/api/catalog/plan-id";
import { plansListHref } from "@/api/catalog/plan-list-query";
import { productDetailHref } from "@/api/catalog/product-id";
import { PlatformApiError } from "@/api/platform-http";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Skeleton } from "@/components/ui/skeleton";
import { useCatalogPlanDetailQuery } from "@/features/catalog/use-catalog-detail-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
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

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
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

function formatBoolean(value: boolean | undefined, language: string): string | null {
  if (value === undefined) return null;
  return value ? (language === "fil-PH" ? "Oo" : "Yes") : language === "fil-PH" ? "Hindi" : "No";
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

function isNotFound(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 404;
}

function DetailField({ label, value }: { label: string; value: string | null | undefined }) {
  if (value == null || value.length === 0) return null;
  return (
    <div className="min-w-0">
      <dt className="text-[length:var(--exits-text-xs)] text-muted">{label}</dt>
      <dd className="break-words text-[length:var(--exits-text-sm)]">{value}</dd>
    </div>
  );
}

export function PlanDetailPage() {
  const { t, language } = usePreferences();
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
        className="grid max-w-3xl gap-3"
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
    <section className="grid max-w-3xl gap-4">
      <p className="text-[length:var(--exits-text-sm)]">
        <Link className="text-primary hover:underline" to={plansListHref()}>
          {t("plans.detail.back")}
        </Link>
      </p>
      <PageHeader
        title={plan.displayName}
        description={plan.code}
        actions={
          <StatusIndicator
            tone={statusTone(plan.status)}
            label={STATUS_LABELS[plan.status] ? t(STATUS_LABELS[plan.status]!) : plan.status}
          />
        }
      />
      <DashboardSection title={t("plans.detail.identity")}>
        <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <DetailField label={t("plans.column.code")} value={plan.code} />
          <DetailField label={t("plans.detail.field.id")} value={plan.id} />
          <DetailField label={t("plans.detail.field.planKey")} value={plan.planKey} />
          <DetailField label={t("plans.detail.field.productCode")} value={plan.productCode} />
          <DetailField
            label={t("plans.detail.field.product")}
            value={plan.productDisplayName ?? plan.productCode}
          />
          {plan.productId ? (
            <div className="min-w-0">
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("plans.detail.field.productLink")}
              </dt>
              <dd>
                <Link
                  className="text-primary hover:underline"
                  to={productDetailHref(plan.productId)}
                >
                  {plan.productDisplayName ?? plan.productCode}
                </Link>
              </dd>
            </div>
          ) : null}
          <DetailField label={t("plans.detail.field.description")} value={plan.description} />
          <DetailField
            label={t("plans.column.created")}
            value={formatInstant(plan.createdAtUtc, language)}
          />
          <DetailField
            label={t("plans.column.updated")}
            value={formatInstant(plan.updatedAtUtc, language)}
          />
        </dl>
      </DashboardSection>
      <DashboardSection
        title={t("plans.detail.pricing")}
        description={t("plans.detail.pricing.hint")}
      >
        <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <DetailField
            label={t("plans.column.monthlyPrice")}
            value={formatMoney(plan.monthlyPrice, plan.currencyCode, language)}
          />
          <DetailField
            label={t("plans.detail.field.annualPrice")}
            value={formatMoney(plan.annualPrice, plan.currencyCode, language)}
          />
          <DetailField label={t("plans.detail.field.currency")} value={plan.currencyCode} />
        </dl>
      </DashboardSection>
      <DashboardSection title={t("plans.detail.limits")}>
        <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <DetailField
            label={t("plans.detail.field.maxBranches")}
            value={plan.maxBranches != null ? String(plan.maxBranches) : null}
          />
          <DetailField
            label={t("plans.detail.field.maxActiveStaff")}
            value={plan.maxActiveStaff != null ? String(plan.maxActiveStaff) : null}
          />
          <DetailField
            label={t("plans.detail.field.maxActivePosDevices")}
            value={plan.maxActivePosDevices != null ? String(plan.maxActivePosDevices) : null}
          />
          <DetailField
            label={t("plans.detail.field.maxActiveBusinessTypes")}
            value={plan.maxActiveBusinessTypes != null ? String(plan.maxActiveBusinessTypes) : null}
          />
          <DetailField
            label={t("plans.detail.field.sortOrder")}
            value={plan.sortOrder != null ? String(plan.sortOrder) : null}
          />
        </dl>
      </DashboardSection>
      <DashboardSection title={t("plans.detail.features")}>
        <dl className="grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <DetailField
            label={t("plans.detail.field.customerCreditEnabled")}
            value={formatBoolean(plan.customerCreditEnabled, language)}
          />
          <DetailField
            label={t("plans.detail.field.advancedReportsEnabled")}
            value={formatBoolean(plan.advancedReportsEnabled, language)}
          />
          <DetailField
            label={t("plans.detail.field.exportEnabled")}
            value={formatBoolean(plan.exportEnabled, language)}
          />
          <DetailField
            label={t("plans.detail.field.trialAllowed")}
            value={formatBoolean(plan.trialAllowed, language)}
          />
          <DetailField
            label={t("plans.detail.field.defaultTrialDays")}
            value={plan.defaultTrialDays != null ? String(plan.defaultTrialDays) : null}
          />
        </dl>
      </DashboardSection>
    </section>
  );
}
