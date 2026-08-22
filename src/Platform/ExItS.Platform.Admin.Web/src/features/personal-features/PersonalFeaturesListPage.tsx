import { Link } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import type { PersonalFeatureDefinition } from "@/api/personal-features/personal-features-client";
import { AdminTable } from "@/components/exits/AdminTable";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Alert } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { ForbiddenState } from "@/features/overview/ForbiddenState";
import { usePersonalFeaturesListQuery } from "@/features/personal-features/use-personal-features-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function formatPrice(
  feature: PersonalFeatureDefinition,
  t: (key: "personalFeatures.notRedeemable") => string,
): string {
  if (feature.rewardPointsPrice == null) {
    return t("personalFeatures.notRedeemable");
  }
  return String(feature.rewardPointsPrice);
}

function formatDuration(
  feature: PersonalFeatureDefinition,
  t: (key: "personalFeatures.indefinite") => string,
): string {
  if (feature.defaultEntitlementDurationDays == null) {
    return t("personalFeatures.indefinite");
  }
  return String(feature.defaultEntitlementDurationDays);
}

export function PersonalFeaturesListPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const showTable = useMediaQuery("(min-width: 768px)");
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewPortfolio);
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageCatalog);

  const query = usePersonalFeaturesListQuery(canView);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.viewPortfolio} />;
  }

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.viewPortfolio} />;
  }

  return (
    <section className="grid gap-4" data-testid="personal-features-list-page">
      <PageHeader
        title={t("nav.personalFeatures")}
        description={t("personalFeatures.description")}
      />
      <Alert title={t("personalFeatures.economicsNote")} tone="info" />

      {query.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("personalFeatures.loading")}>
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {query.isError ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: query.error,
            operation: "Load personal features",
          })}
          title={t("personalFeatures.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data && query.data.length === 0 ? (
        <EmptyState title={t("personalFeatures.empty")} />
      ) : null}

      {query.data && query.data.length > 0 ? (
        showTable ? (
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
            <AdminTable
              caption={t("personalFeatures.caption")}
              empty={t("personalFeatures.empty")}
              columns={[
                {
                  id: "code",
                  header: t("personalFeatures.featureCode"),
                  cell: (row) => (
                    <span className="font-mono text-[length:var(--exits-text-xs)]">
                      {row.featureCode}
                    </span>
                  ),
                },
                {
                  id: "name",
                  header: t("personalFeatures.displayName"),
                  cell: (row) => row.displayName,
                },
                {
                  id: "status",
                  header: t("personalFeatures.status"),
                  cell: (row) => (
                    <StatusIndicator
                      tone={row.isActive ? "success" : "neutral"}
                      label={row.isActive ? t("personalFeatures.active") : t("personalFeatures.inactive")}
                    />
                  ),
                },
                {
                  id: "price",
                  header: t("personalFeatures.rewardPoints"),
                  cell: (row) => formatPrice(row, t),
                },
                {
                  id: "duration",
                  header: t("personalFeatures.durationDays"),
                  cell: (row) => formatDuration(row, t),
                },
                {
                  id: "actions",
                  header: t("personalFeatures.actions"),
                  cell: (row) => (
                    <Link
                      className="text-primary hover:underline"
                      to={`/admin/personal-features/${encodeURIComponent(row.featureCode)}`}
                    >
                      {canManage ? t("personalFeatures.edit") : t("personalFeatures.view")}
                    </Link>
                  ),
                },
              ]}
              rows={query.data.map((row) => ({ ...row, id: row.featureCode }))}
            />
          </div>
        ) : (
          <ul className="grid gap-2">
            {query.data.map((row) => (
              <li
                key={row.featureCode}
                className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
              >
                <p className="font-medium">{row.displayName}</p>
                <p className="mt-0.5 font-mono text-[length:var(--exits-text-xs)] text-muted">
                  {row.featureCode}
                </p>
                <div className="mt-1.5 flex flex-wrap items-center gap-2">
                  <StatusIndicator
                    tone={row.isActive ? "success" : "neutral"}
                    label={row.isActive ? t("personalFeatures.active") : t("personalFeatures.inactive")}
                  />
                  <Link
                    className="text-primary hover:underline"
                    to={`/admin/personal-features/${encodeURIComponent(row.featureCode)}`}
                  >
                    {canManage ? t("personalFeatures.edit") : t("personalFeatures.view")}
                  </Link>
                </div>
              </li>
            ))}
          </ul>
        )
      ) : null}
    </section>
  );
}
