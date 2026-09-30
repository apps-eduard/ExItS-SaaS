import { ArrowLeftRight } from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listBranchManagementSummaries } from "@/api/platform/organization-branches-client";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import { listSupplyRoutes, type SupplyRouteDto } from "@/api/pos/pos-supply-routes-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { SupplyCoverageManageSheet } from "@/features/replenishment/SupplyCoverageManageSheet";
import {
  warehouseCoverageSummary,
  warehouseSources,
  type CoverageLocation,
} from "@/features/replenishment/supply-coverage-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function plural(
  t: (key: MessageKey) => string,
  count: number,
  oneKey: MessageKey,
  manyKey: MessageKey,
): string {
  return (count === 1 ? t(oneKey) : t(manyKey)).replace("{count}", String(count));
}

export function SupplyRoutesPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const [searchWarehouses, setSearchWarehouses] = useState("");
  const [manageSourceId, setManageSourceId] = useState<string | null>(null);

  const orgId = boundWorkspace?.organizationId;
  const workspace = useMemo(
    () => (orgId ? { organizationId: orgId, branchId: boundWorkspace?.branchId ?? null } : null),
    [orgId, boundWorkspace?.branchId],
  );

  const branchesQuery = useQuery({
    queryKey: ["branch-mgmt-summaries", orgId],
    enabled: Boolean(orgId),
    queryFn: async ({ signal }) => {
      const result = await listBranchManagementSummaries(orgId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? t("supplyRoutes.loadError"));
      return result.value;
    },
  });

  const areasQuery = useQuery({
    queryKey: ["org-areas", orgId],
    enabled: Boolean(orgId),
    queryFn: async ({ signal }) => {
      const result = await listOrganizationAreas(orgId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? t("supplyRoutes.loadError"));
      return result.value.areas;
    },
  });

  const routesQuery = useQuery({
    queryKey: ["supply-routes", orgId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => listSupplyRoutes(workspace!, signal),
  });

  const locations: CoverageLocation[] = useMemo(
    () =>
      (branchesQuery.data ?? []).map((b) => ({
        id: b.id,
        name: b.name,
        code: b.code,
        branchType: b.branchType,
        status: b.status,
        areaId: b.areaId,
        areaName: b.areaName,
      })),
    [branchesQuery.data],
  );

  const routes: SupplyRouteDto[] = useMemo(() => routesQuery.data ?? [], [routesQuery.data]);
  const warehouses = useMemo(() => {
    const list = warehouseSources(locations);
    const q = searchWarehouses.trim().toLowerCase();
    if (!q) return list;
    return list.filter(
      (w) => w.name.toLowerCase().includes(q) || (w.code ?? "").toLowerCase().includes(q),
    );
  }, [locations, searchWarehouses]);

  const manageWarehouse = locations.find((l) => l.id === manageSourceId) ?? null;

  if (!orgId) {
    return (
      <EmptyState
        align="center"
        icon={<ArrowLeftRight className="size-5" strokeWidth={1.75} />}
        title={t("supplyRoutes.needOrg")}
        detail={t("supplyRoutes.needOrgDetail")}
      />
    );
  }

  if (branchesQuery.isLoading || routesQuery.isLoading) {
    return <LoadingState label={t("supplyRoutes.loading")} />;
  }

  if (branchesQuery.isError || routesQuery.isError) {
    const detail =
      (routesQuery.error instanceof Error && routesQuery.error.message) ||
      (branchesQuery.error instanceof Error && branchesQuery.error.message) ||
      t("supplyRoutes.loadError");
    return <ErrorState title={t("supplyRoutes.loadError")} detail={detail} />;
  }

  const areas = areasQuery.data ?? [];

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="supply-routes-page">
      <PageHeader
        title={t("supplyRoutes.title")}
        description={t("supplyRoutes.ledeWarehouseFirst")}
        backTo={pageBackNav.orgBranches.to}
        backLabel={t(pageBackNav.orgBranches.labelKey)}
        backTestId="page-header-back-branches"
      />

      <input
        className="exits-input"
        value={searchWarehouses}
        onChange={(e) => setSearchWarehouses(e.target.value)}
        placeholder={t("supplyRoutes.searchWarehouses")}
        data-testid="supply-routes-search-warehouses"
      />

      {warehouses.length === 0 ? (
        <EmptyState
          align="center"
          icon={<ArrowLeftRight className="size-5" strokeWidth={1.75} />}
          title={t("supplyRoutes.noWarehouses")}
          detail={t("supplyRoutes.noWarehousesDetail")}
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="supply-routes-warehouse-list">
          {warehouses.map((wh) => {
            const summary = warehouseCoverageSummary(locations, routes, wh.id);
            return (
              <li
                key={wh.id}
                className="rounded-[var(--exits-radius-md)] border border-[var(--exits-border)] p-3"
                data-testid={`supply-warehouse-card-${wh.id}`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <div className="font-medium">{wh.name}</div>
                    <StatusChip tone="info">{t("supplyRoutes.type.warehouse")}</StatusChip>
                  </div>
                  {allowManage ? (
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => setManageSourceId(wh.id)}
                      data-testid={`supply-manage-coverage-${wh.id}`}
                    >
                      {t("supplyRoutes.manageCoverage")}
                    </Button>
                  ) : null}
                </div>
                <div className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
                  <div className="font-medium text-foreground">{t("supplyRoutes.coverage")}</div>
                  <div>
                    {plural(t, summary.retailCount, "supplyRoutes.count.retailOne", "supplyRoutes.count.retailMany")}
                    {summary.warehouseCount > 0
                      ? ` · ${plural(t, summary.warehouseCount, "supplyRoutes.count.warehouseOne", "supplyRoutes.count.warehouseMany")}`
                      : ""}
                  </div>
                  {summary.fullAreaNames.length > 0 ? (
                    <div className="mt-1">
                      {t("supplyRoutes.areasFullyCovered")}: {summary.fullAreaNames.join(", ")}
                    </div>
                  ) : null}
                  {summary.partialAreas.length > 0 ? (
                    <div className="mt-1">
                      {t("supplyRoutes.areasPartial")}:{" "}
                      {summary.partialAreas
                        .map((a) => `${a.name} · ${a.selected} of ${a.total}`)
                        .join("; ")}
                    </div>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <p className="text-[length:var(--exits-text-xs)] text-muted">
        <Link to="/org/branches" className="underline">
          {t("supplyRoutes.backBranches")}
        </Link>
      </p>

      <SupplyCoverageManageSheet
        open={manageWarehouse !== null}
        onClose={() => setManageSourceId(null)}
        workspace={workspace}
        organizationId={orgId}
        sourceLocationId={manageSourceId}
        sourceName={manageWarehouse?.name ?? ""}
        locations={locations}
        routes={routes}
        areas={areas}
        allowManage={allowManage}
      />
    </div>
  );
}
