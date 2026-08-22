import { Link, useParams } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import {
  parseSubscriptionId,
  subscriptionsListHref,
} from "@/api/subscriptions/subscription-portfolio-query";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { OrganizationSubscriptionLifecycle } from "@/features/organizations/OrganizationSubscriptionLifecycle";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import { subscriptionPeriodEnd } from "@/features/organizations/subscription-lifecycle";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { SubscriptionNotFoundPage } from "@/features/subscriptions/SubscriptionNotFoundPage";
import { useSubscriptionDetailQuery } from "@/features/subscriptions/use-subscription-portfolio-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

function isNotFound(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 404;
}

function formatInstant(value: string | undefined, language: string): string {
  if (!value) return "—";
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
): string {
  if (value === undefined) return "—";
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

export function SubscriptionDetailPage() {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const params = useParams();
  const subscriptionId = parseSubscriptionId(params.subscriptionId);
  const canView =
    authorization.status === "loaded" &&
    authorization.hasAnyPermission([
      PLATFORM_PERMISSIONS.manageSubscriptions,
      PLATFORM_PERMISSIONS.viewPortfolio,
    ]);

  const query = useSubscriptionDetailQuery(canView ? subscriptionId : null, canView);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) return <ShellNotFoundPage />;
  if (subscriptionId == null) return <SubscriptionNotFoundPage />;

  if (query.isPending) {
    return (
      <section
        className="grid max-w-3xl gap-3"
        role="status"
        aria-busy="true"
        aria-label={t("subscriptions.detail.loading")}
      >
        <DashboardWidgetSkeleton rows={10} />
      </section>
    );
  }

  if (query.isError && isForbidden(query.error)) return <ShellNotFoundPage />;
  if (query.isError && isNotFound(query.error)) return <SubscriptionNotFoundPage />;

  if (query.isError) {
    return (
      <ErrorState
        diagnostic={normalizeDiagnosticError({
          error: query.error,
          operation: "Load subscription",
        })}
        title={t("subscriptions.detail.error")}
        headingLevel="h1"
        onRetry={() => void query.refetch()}
      />
    );
  }

  const subscription = query.data!;

  return (
    <section className="grid max-w-3xl gap-4">
      <p className="text-[length:var(--exits-text-sm)]">
        <Link className="inline-flex items-center gap-1 text-primary hover:underline" to={subscriptionsListHref()}>
          <ArrowLeft aria-hidden className="size-4" />
          {t("subscriptions.detail.back")}
        </Link>
      </p>
      <PageHeader
        title={subscription.productDisplayName || subscription.productCode}
        description={t("subscriptions.detail.description")}
      />

      <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div>
            <p className="text-[length:var(--exits-text-sm)] text-muted">
              {subscription.planDisplayName || subscription.planKey || subscription.planId}
            </p>
          </div>
          <StatusIndicator
            tone={organizationSubscriptionStatusTone(subscription.status)}
            label={organizationSubscriptionStatusLabel(subscription.status, t)}
          />
        </div>
        <dl className="mt-3 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.organization")}</dt>
            <dd>
              <Link
                className="font-medium text-primary hover:underline"
                to={`/admin/organizations/${subscription.organizationId}`}
              >
                {subscription.organizationDisplayName || subscription.organizationId}
              </Link>
            </dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.product")}</dt>
            <dd>{subscription.productDisplayName || subscription.productCode}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.plan")}</dt>
            <dd>{subscription.planDisplayName || subscription.planKey || subscription.planId}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.billingCycle")}</dt>
            <dd>{subscription.billingCycle || "—"}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.agreedPrice")}</dt>
            <dd>
              {formatMoney(subscription.agreedPrice, subscription.currencyCode, language)}
            </dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.periodEnd")}</dt>
            <dd>{formatInstant(subscriptionPeriodEnd(subscription), language)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.trialEnd")}</dt>
            <dd>{formatInstant(subscription.trialEndUtc, language)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.graceEnd")}</dt>
            <dd>{formatInstant(subscription.gracePeriodEndUtc, language)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.pendingPlan")}</dt>
            <dd>
              {subscription.pendingPlanId
                ? `${subscription.pendingPlanId}${
                    subscription.pendingPlanEffectiveAtUtc
                      ? ` · ${formatInstant(subscription.pendingPlanEffectiveAtUtc, language)}`
                      : ""
                  }`
                : t("organization.subscriptions.summary.pendingNone")}
            </dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.version")}</dt>
            <dd>{subscription.version ?? "—"}</dd>
          </div>
        </dl>
        <div className="mt-3 flex flex-wrap gap-2">
          <Button type="button" size="sm" variant="outline" asChild>
            <Link to={`/admin/organizations/${subscription.organizationId}/billing`}>
              {t("subscriptions.detail.link.billing")}
            </Link>
          </Button>
          <Button type="button" size="sm" variant="outline" asChild>
            <Link to={`/admin/organizations/${subscription.organizationId}/entitlements`}>
              {t("subscriptions.detail.link.entitlements")}
            </Link>
          </Button>
        </div>
        <dl className="mt-4 grid gap-1 border-t border-border pt-3 text-[length:var(--exits-text-xs)] sm:grid-cols-2">
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.subscriptionId")}</dt>
            <dd className="break-all font-mono">{subscription.id}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.organizationId")}</dt>
            <dd className="break-all font-mono">{subscription.organizationId}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("subscriptions.detail.field.planId")}</dt>
            <dd className="break-all font-mono">{subscription.planId}</dd>
          </div>
          {subscription.planVersionId ? (
            <div>
              <dt className="text-muted">{t("subscriptions.detail.field.planVersionId")}</dt>
              <dd className="break-all font-mono">{subscription.planVersionId}</dd>
            </div>
          ) : null}
        </dl>
      </div>

      <OrganizationSubscriptionLifecycle
        organizationId={subscription.organizationId}
        subscriptions={[subscription]}
      />
    </section>
  );
}
