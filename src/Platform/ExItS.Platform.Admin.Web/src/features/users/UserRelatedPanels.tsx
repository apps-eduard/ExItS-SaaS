import { Link } from "react-router-dom";
import { AdminTable } from "@/components/exits/AdminTable";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Alert } from "@/components/ui/alert";
import {
  usePlatformUserMembershipsQuery,
  usePlatformUserProductAccessQuery,
} from "@/features/users/use-user-detail-queries";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Suspended" || status === "Pending") {
    return "warning";
  }
  if (status === "Revoked" || status === "Deactivated") {
    return "danger";
  }
  return "neutral";
}

export function UserMembershipsPanel({ userId }: { userId: string }) {
  const { t } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const query = usePlatformUserMembershipsQuery(userId);

  return (
    <div data-testid="users-memberships-panel">
      <DashboardSection title={t("users.memberships.title")}>
        {query.isPending ? (
          <div role="status" aria-busy="true" aria-label={t("users.memberships.loading")}>
            <DashboardWidgetSkeleton rows={3} />
          </div>
        ) : null}
        {query.isError ? (
          <ErrorState
            diagnostic={normalizeDiagnosticError({
              error: query.error,
              operation: "Load memberships",
            })}
            title={t("users.memberships.error")}
            headingLevel="h2"
            onRetry={() => void query.refetch()}
          />
        ) : null}
        {query.data && query.data.items.length === 0 ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
            {t("users.memberships.empty")}
          </p>
        ) : null}
        {query.data && query.data.items.length > 0 ? (
          showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("users.memberships.caption")}
                empty={t("users.memberships.empty")}
                columns={[
                  {
                    id: "organization",
                    header: t("users.memberships.organization"),
                    cell: (row) => (
                      <Link
                        className="font-mono text-[length:var(--exits-text-xs)] text-primary hover:underline"
                        to={`/admin/organizations/${row.organizationId}`}
                      >
                        {row.organizationId}
                      </Link>
                    ),
                  },
                  {
                    id: "role",
                    header: t("users.memberships.role"),
                    cell: (row) => row.roleDisplay ?? row.role,
                  },
                  {
                    id: "status",
                    header: t("users.memberships.status"),
                    cell: (row) => (
                      <StatusIndicator tone={statusTone(row.status)} label={row.status} />
                    ),
                  },
                ]}
                rows={query.data.items}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {query.data.items.map((row) => (
                <li
                  key={row.id}
                  className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-sm)]"
                >
                  <Link
                    className="font-mono text-[length:var(--exits-text-xs)] text-primary hover:underline"
                    to={`/admin/organizations/${row.organizationId}`}
                  >
                    {row.organizationId}
                  </Link>
                  <p className="mt-1 text-muted">{row.roleDisplay ?? row.role}</p>
                  <StatusIndicator tone={statusTone(row.status)} label={row.status} />
                </li>
              ))}
            </ul>
          )
        ) : null}
      </DashboardSection>
    </div>
  );
}

export function UserProductAccessPanel({ userId }: { userId: string }) {
  const { t } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const query = usePlatformUserProductAccessQuery(userId);

  return (
    <div data-testid="users-product-access-panel">
      <DashboardSection title={t("users.productAccess.title")}>
        <Alert title={t("users.productAccess.warning")} tone="info" />
        {query.isPending ? (
          <div role="status" aria-busy="true" aria-label={t("users.productAccess.loading")}>
            <DashboardWidgetSkeleton rows={3} />
          </div>
        ) : null}
        {query.isError ? (
          <ErrorState
            diagnostic={normalizeDiagnosticError({
              error: query.error,
              operation: "Load product access",
            })}
            title={t("users.productAccess.error")}
            headingLevel="h2"
            onRetry={() => void query.refetch()}
          />
        ) : null}
        {query.data && query.data.items.length === 0 ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
            {t("users.productAccess.empty")}
          </p>
        ) : null}
        {query.data && query.data.items.length > 0 ? (
          showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("users.productAccess.caption")}
                empty={t("users.productAccess.empty")}
                columns={[
                  {
                    id: "product",
                    header: t("users.productAccess.product"),
                    cell: (row) => row.productCode,
                  },
                  {
                    id: "organization",
                    header: t("users.productAccess.organization"),
                    cell: (row) => (
                      <Link
                        className="font-mono text-[length:var(--exits-text-xs)] text-primary hover:underline"
                        to={`/admin/organizations/${row.organizationId}/products`}
                      >
                        {row.organizationId}
                      </Link>
                    ),
                  },
                  {
                    id: "status",
                    header: t("users.productAccess.status"),
                    cell: (row) => (
                      <StatusIndicator tone={statusTone(row.status)} label={row.status} />
                    ),
                  },
                ]}
                rows={query.data.items}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {query.data.items.map((row) => (
                <li
                  key={row.id}
                  className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-sm)]"
                >
                  <p className="font-medium">{row.productCode}</p>
                  <Link
                    className="font-mono text-[length:var(--exits-text-xs)] text-primary hover:underline"
                    to={`/admin/organizations/${row.organizationId}/products`}
                  >
                    {row.organizationId}
                  </Link>
                  <div className="mt-1">
                    <StatusIndicator tone={statusTone(row.status)} label={row.status} />
                  </div>
                </li>
              ))}
            </ul>
          )
        ) : null}
      </DashboardSection>
    </div>
  );
}
