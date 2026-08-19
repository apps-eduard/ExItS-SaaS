import { parseOrganizationId } from "@/api/organizations/organization-id";
import { PlatformApiError } from "@/api/platform-http";
import type { OrganizationBranch } from "@/api/organizations/organization-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { useOrganizationBranchesQuery } from "@/features/organizations/use-organization-workspace-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";
import { useParams } from "react-router-dom";

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "organization.branch.status.Active",
  Inactive: "organization.branch.status.Inactive",
  Archived: "organization.branch.status.Archived",
};

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Inactive") {
    return "warning";
  }
  if (status === "Archived") {
    return "danger";
  }
  return "neutral";
}

function formatBranchLocation(branch: OrganizationBranch): string {
  return [
    branch.addressLine1,
    branch.addressLine2,
    branch.city,
    branch.region,
    branch.postalCode,
    branch.countryCode,
  ]
    .filter((value): value is string => Boolean(value))
    .join(", ");
}

export function OrganizationBranchesPage() {
  const { t } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const query = useOrganizationBranchesQuery(organizationId);
  const showTable = useMediaQuery("(min-width: 768px)");
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load organization branches",
      })
    : null;

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.branches.title")}
        description={t("organization.branches.description")}
      />

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.branches.loading")}
        >
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.branches.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        showTable ? (
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
            <AdminTable
              caption={t("organization.branches.caption")}
              empty={t("organization.branches.empty")}
              columns={[
                {
                  id: "name",
                  header: t("organization.branches.column.branch"),
                  cell: (branch) => <span className="font-medium">{branch.name}</span>,
                },
                {
                  id: "code",
                  header: t("organization.branches.column.code"),
                  cell: (branch) => (
                    <span className="font-mono text-[length:var(--exits-text-xs)] text-muted">
                      {branch.code}
                    </span>
                  ),
                },
                {
                  id: "location",
                  header: t("organization.branches.column.location"),
                  cell: (branch) => (
                    <span className="break-words text-muted">
                      {formatBranchLocation(branch) || "—"}
                    </span>
                  ),
                },
                {
                  id: "status",
                  header: t("organization.branches.column.status"),
                  cell: (branch) => (
                    <StatusIndicator
                      tone={statusTone(branch.status)}
                      label={
                        STATUS_LABELS[branch.status]
                          ? t(STATUS_LABELS[branch.status]!)
                          : branch.status
                      }
                    />
                  ),
                },
                {
                  id: "type",
                  header: t("organization.branches.column.type"),
                  cell: (branch) =>
                    branch.isPrimary
                      ? t("organization.branches.type.primary")
                      : t("organization.branches.type.branch"),
                },
              ]}
              rows={query.data}
            />
            {query.data.length === 0 ? (
              <p className="mt-2 text-[length:var(--exits-text-xs)] text-muted">
                {t("organization.branches.empty.hint")}
              </p>
            ) : null}
          </div>
        ) : (
          <ul className="grid gap-2">
            {query.data.length === 0 ? (
              <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
                <p>{t("organization.branches.empty")}</p>
                <p className="mt-1 text-[length:var(--exits-text-xs)]">
                  {t("organization.branches.empty.hint")}
                </p>
              </li>
            ) : (
              query.data.map((branch) => (
                <li
                  key={branch.id}
                  className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                >
                  <p className="font-medium">{branch.name}</p>
                  <p className="mt-0.5 font-mono text-[length:var(--exits-text-xs)] text-muted">
                    {branch.code}
                  </p>
                  <div className="mt-1.5 flex flex-wrap items-center gap-2">
                    <StatusIndicator
                      tone={statusTone(branch.status)}
                      label={
                        STATUS_LABELS[branch.status]
                          ? t(STATUS_LABELS[branch.status]!)
                          : branch.status
                      }
                    />
                    {branch.isPrimary ? (
                      <span className="text-[length:var(--exits-text-xs)] font-medium">
                        {t("organization.branches.type.primary")}
                      </span>
                    ) : null}
                  </div>
                  {formatBranchLocation(branch) ? (
                    <p className="mt-1 break-words text-[length:var(--exits-text-xs)] text-muted">
                      {formatBranchLocation(branch)}
                    </p>
                  ) : null}
                  {branch.contactPhone || branch.timeZoneId ? (
                    <p className="mt-1 break-words text-[length:var(--exits-text-xs)] text-muted">
                      {[branch.contactPhone, branch.timeZoneId].filter(Boolean).join(" · ")}
                    </p>
                  ) : null}
                </li>
              ))
            )}
          </ul>
        )
      ) : null}
    </section>
  );
}
