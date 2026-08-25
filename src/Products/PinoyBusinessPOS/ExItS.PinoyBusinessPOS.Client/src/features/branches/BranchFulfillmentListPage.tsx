import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight } from "lucide-react";
import { canManageBranchFulfillment } from "@/access/pos-capabilities";
import { listOrganizationBranchesForFulfillment } from "@/api/platform/branch-fulfillment-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function BranchFulfillmentListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageBranchFulfillment(sessionGrant);
  const organizationId = boundWorkspace?.organizationId;

  const branchesQuery = useQuery({
    queryKey: ["branch-fulfillment-list", organizationId],
    enabled: Boolean(organizationId && canManage),
    queryFn: ({ signal }) => listOrganizationBranchesForFulfillment(organizationId!, signal),
  });

  const branches = useMemo(() => {
    const items = branchesQuery.data ?? [];
    return [...items].sort((a, b) => {
      if (a.isPrimary !== b.isPrimary) {
        return a.isPrimary ? -1 : 1;
      }
      return a.name.localeCompare(b.name);
    });
  }, [branchesQuery.data]);

  if (!canManage) {
    return (
      <div
        data-testid="branch-fulfillment-denied"
        className="branch-fulfillment-page exits-page flex min-w-0 flex-col gap-3"
      >
        <PageHeader
          title={t("branches.listTitle")}
          description={t("branches.denied")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
      </div>
    );
  }

  if (!organizationId || branchesQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (branchesQuery.isError) {
    return (
      <div
        data-testid="branch-fulfillment-list-error"
        className="branch-fulfillment-page exits-page flex min-w-0 flex-col gap-3"
      >
        <PageHeader
          title={t("branches.listTitle")}
          description={t("branches.listLede")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
        <ErrorState title={t("branches.loadError")} detail={t("branches.listLede")} />
      </div>
    );
  }

  return (
    <div
      className="branch-fulfillment-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="branch-fulfillment-list"
    >
      <PageHeader
        title={t("branches.listTitle")}
        description={t("branches.listLede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />

      {branches.length === 0 ? (
        <EmptyState title={t("branches.emptyTitle")} detail={t("branches.emptyDetail")} />
      ) : (
        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="branch-fulfillment-items">
          {branches.map((branch) => {
            const meta = [branch.code, branch.city].filter(Boolean).join(" · ");
            return (
              <li key={branch.id}>
                <Link
                  className="exits-list__card branch-row block min-w-0 text-foreground no-underline"
                  to={`/org/branches/${branch.id}`}
                  data-testid={`open-branch-fulfillment-${branch.id}`}
                >
                  <div className="branch-row__main min-w-0">
                    <strong className="exits-list__name block truncate font-semibold">
                      {branch.name}
                    </strong>
                    {meta ? (
                      <p className="branch-row__meta mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                        {meta}
                      </p>
                    ) : null}
                    <div className="branch-row__chips mt-2 flex flex-wrap gap-1.5">
                      <StatusChip tone={branch.status === "Active" ? "success" : "warning"}>
                        {branch.status}
                      </StatusChip>
                      <StatusChip tone={branch.pickupEnabled ? "success" : "info"}>
                        {branch.pickupEnabled
                          ? t("branches.pickupEnabled")
                          : t("branches.pickupDisabled")}
                      </StatusChip>
                      <StatusChip tone={branch.deliveryEnabled ? "success" : "info"}>
                        {branch.deliveryEnabled
                          ? t("branches.deliveryEnabled")
                          : t("branches.deliveryDisabled")}
                      </StatusChip>
                    </div>
                  </div>
                  <span className="branch-row__aside">
                    <span className="sr-only">{t("branches.configure")}</span>
                    <ChevronRight
                      className="branch-row__chevron size-4 shrink-0 text-muted"
                      aria-hidden
                    />
                  </span>
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
