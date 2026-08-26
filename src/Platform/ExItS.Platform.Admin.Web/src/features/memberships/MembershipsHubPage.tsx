import { Link, useSearchParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import { ORGANIZATION_LIST_PAGE_SIZE } from "@/api/organizations/organization-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ForbiddenState } from "@/features/overview/ForbiddenState";
import { useOrganizationListQuery } from "@/features/organizations/use-organization-list-query";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Suspended") {
    return "warning";
  }
  if (status === "Closed") {
    return "danger";
  }
  return "neutral";
}

export function MembershipsHubPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Math.max(1, Number.parseInt(searchParams.get("page") ?? "1", 10) || 1);

  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageMemberships);

  const query = useOrganizationListQuery(
    {
      page,
      pageSize: ORGANIZATION_LIST_PAGE_SIZE,
      sortBy: "DisplayName",
      sortDesc: false,
    },
    canManage,
  );

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canManage) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.manageMemberships} />;
  }

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.manageMemberships} />;
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ORGANIZATION_LIST_PAGE_SIZE))
    : 1;

  return (
    <section className="grid gap-4" data-testid="memberships-hub-page">
      <PageHeader title={t("nav.memberships")} description={t("membershipsHub.description")} />
      <Alert title={t("membershipsHub.hint")} tone="info" />

      {query.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("membershipsHub.loading")}>
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: query.error,
            operation: "Load organizations for memberships",
          })}
          title={t("membershipsHub.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data && query.data.items.length === 0 ? (
        <EmptyState
          title={t("membershipsHub.empty")}
          description={t("membershipsHub.emptyBody")}
        />
      ) : null}

      {query.data && query.data.items.length > 0 ? (
        <>
          {showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("membershipsHub.caption")}
                empty={t("membershipsHub.empty")}
                columns={[
                  {
                    id: "name",
                    header: t("organizations.column.organization"),
                    cell: (org) => (
                      <Link
                        className="font-medium text-primary hover:underline"
                        to={`/admin/organizations/${org.id}/people`}
                      >
                        {org.displayName}
                      </Link>
                    ),
                  },
                  {
                    id: "slug",
                    header: t("organizations.column.identifier"),
                    cell: (org) => (
                      <span className="font-mono text-[length:var(--exits-text-xs)] text-muted">
                        {org.slug}
                      </span>
                    ),
                  },
                  {
                    id: "status",
                    header: t("organizations.column.status"),
                    cell: (org) => (
                      <StatusIndicator tone={statusTone(org.status)} label={org.status} />
                    ),
                  },
                  {
                    id: "open",
                    header: t("membershipsHub.openMembers"),
                    cell: (org) => (
                      <Link
                        className="text-primary hover:underline"
                        to={`/admin/organizations/${org.id}/people`}
                      >
                        {t("membershipsHub.members")}
                      </Link>
                    ),
                  },
                ]}
                rows={query.data.items}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {query.data.items.map((org) => (
                <li
                  key={org.id}
                  className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                >
                  <p className="font-medium">{org.displayName}</p>
                  <p className="mt-0.5 font-mono text-[length:var(--exits-text-xs)] text-muted">
                    {org.slug}
                  </p>
                  <div className="mt-1.5 flex flex-wrap items-center gap-2">
                    <StatusIndicator tone={statusTone(org.status)} label={org.status} />
                    <Link
                      className="text-primary hover:underline"
                      to={`/admin/organizations/${org.id}/people`}
                    >
                      {t("membershipsHub.members")}
                    </Link>
                  </div>
                </li>
              ))}
            </ul>
          )}

          {query.data.totalCount > ORGANIZATION_LIST_PAGE_SIZE ? (
            <div className="flex flex-wrap items-center gap-2">
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={page <= 1}
                onClick={() =>
                  setSearchParams(page <= 2 ? {} : { page: String(page - 1) }, { replace: true })
                }
              >
                {t("users.previous")}
              </Button>
              <p className="text-[length:var(--exits-text-sm)] text-muted">
                {t("users.page")} {page} / {totalPages}
              </p>
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={page >= totalPages}
                onClick={() =>
                  setSearchParams({ page: String(page + 1) }, { replace: true })
                }
              >
                {t("users.next")}
              </Button>
            </div>
          ) : null}
        </>
      ) : null}
    </section>
  );
}
