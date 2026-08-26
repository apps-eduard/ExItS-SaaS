import { useEffect } from "react";
import { Outlet, useParams } from "react-router-dom";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import { PlatformApiError } from "@/api/platform-http";
import { ErrorState } from "@/components/exits/ErrorState";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { OrganizationNotFoundPage } from "@/features/organizations/OrganizationNotFoundPage";
import { OrganizationWorkspaceNav } from "@/features/organizations/OrganizationWorkspaceNav";
import { useOrganizationWorkspaceIdentity } from "@/features/organizations/organization-workspace-context";
import { useOrganizationDetailQuery } from "@/features/organizations/use-organization-workspace-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

function isNotFound(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 404;
}

export function OrganizationWorkspaceLayout() {
  const { t } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const workspace = useOrganizationWorkspaceIdentity();
  const query = useOrganizationDetailQuery(organizationId);
  const setIdentity = workspace?.setIdentity;

  useEffect(() => {
    if (!setIdentity) {
      return;
    }
    if (query.data) {
      setIdentity({ id: query.data.id, displayName: query.data.displayName });
    } else {
      setIdentity(null);
    }
    return () => setIdentity(null);
  }, [query.data, setIdentity]);

  if (organizationId == null) {
    return <OrganizationNotFoundPage />;
  }

  if (query.isPending) {
    return (
      <section
        className="grid max-w-3xl gap-3"
        role="status"
        aria-busy="true"
        aria-label={t("organization.workspace.loading")}
      >
        <DashboardWidgetSkeleton rows={8} />
      </section>
    );
  }

  if (query.isError && isForbidden(query.error)) {
    return <ShellNotFoundPage />;
  }

  if (query.isError && isNotFound(query.error)) {
    return <OrganizationNotFoundPage />;
  }

  if (query.isError) {
    const diagnostic = normalizeDiagnosticError({
      error: query.error,
      operation: "Load organization",
    });
    return (
      <ErrorState
        diagnostic={diagnostic}
        title={t("organization.error")}
        headingLevel="h1"
        onRetry={() => void query.refetch()}
      />
    );
  }

  if (!query.data) {
    return <OrganizationNotFoundPage />;
  }

  return (
    <div className="grid min-w-0 gap-4">
      <OrganizationWorkspaceNav />
      <Outlet />
    </div>
  );
}
