import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageBranchFulfillment } from "@/access/pos-capabilities";
import { listOrganizationBranchesForFulfillment } from "@/api/platform/branch-fulfillment-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
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
      <div data-testid="branch-fulfillment-denied" className="flex flex-col gap-3">
        <PageHeader title={t("branches.listTitle")} description={t("branches.denied")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/org">{t("branches.backOrg")}</Link>
        </Button>
      </div>
    );
  }

  if (!organizationId || branchesQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (branchesQuery.isError) {
    return (
      <div data-testid="branch-fulfillment-list-error" className="flex flex-col gap-3">
        <PageHeader title={t("branches.listTitle")} description={t("branches.loadError")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/org">{t("branches.backOrg")}</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="branch-fulfillment-list">
      <PageHeader title={t("branches.listTitle")} description={t("branches.listLede")} />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/org">{t("branches.backOrg")}</Link>
      </Button>

      {branches.length === 0 ? (
        <EmptyState title={t("branches.emptyTitle")} detail={t("branches.emptyDetail")} />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-3 p-0">
          {branches.map((branch) => (
            <li key={branch.id}>
              <Card className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <p className="m-0 text-[length:var(--exits-text-md)] font-medium">
                    {branch.name}
                  </p>
                  <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                    {branch.code}
                    {branch.city ? ` · ${branch.city}` : ""}
                  </p>
                  <div className="mt-2 flex flex-wrap gap-2">
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
                <Button asChild className="min-h-11 w-full sm:w-auto">
                  <Link
                    to={`/org/branches/${branch.id}`}
                    data-testid={`open-branch-fulfillment-${branch.id}`}
                  >
                    {t("branches.configure")}
                  </Link>
                </Button>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
