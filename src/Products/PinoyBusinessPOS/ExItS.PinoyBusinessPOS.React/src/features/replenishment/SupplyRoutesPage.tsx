import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listBranchManagementSummaries } from "@/api/platform/organization-branches-client";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import {
  listSupplyRoutes,
  upsertSupplyRoutesForDestination,
  type SupplyRouteDto,
} from "@/api/pos/pos-supply-routes-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type AreaFilter = "all" | "unassigned" | string;

export function SupplyRoutesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const [areaFilter, setAreaFilter] = useState<AreaFilter>("all");
  const [typeFilter, setTypeFilter] = useState<"all" | "retail" | "warehouse">("all");
  const [search, setSearch] = useState("");
  const [manageDestinationId, setManageDestinationId] = useState<string | null>(null);
  const [selectedSources, setSelectedSources] = useState<Record<string, boolean>>({});
  const [preferredSourceId, setPreferredSourceId] = useState<string | null>(null);

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

  const destinations = useMemo(() => {
    const branches = branchesQuery.data ?? [];
    const q = search.trim().toLowerCase();
    return branches.filter((b) => {
      if (normalizeBranchStatusFilter(b.status) !== "Active") return false;
      if (typeFilter === "warehouse" && !isWarehouseBranch(b.branchType)) return false;
      if (typeFilter === "retail" && isWarehouseBranch(b.branchType)) return false;
      if (areaFilter === "unassigned" && b.areaId) return false;
      if (areaFilter !== "all" && areaFilter !== "unassigned" && b.areaId !== areaFilter) return false;
      if (q && !b.name.toLowerCase().includes(q)) return false;
      return true;
    });
  }, [branchesQuery.data, areaFilter, typeFilter, search]);

  const routesByDestination = useMemo(() => {
    const map = new Map<string, SupplyRouteDto[]>();
    for (const route of routesQuery.data ?? []) {
      const list = map.get(route.destinationLocationId) ?? [];
      list.push(route);
      map.set(route.destinationLocationId, list);
    }
    return map;
  }, [routesQuery.data]);

  const nameById = useMemo(() => {
    const map = new Map<string, { name: string; warehouse: boolean }>();
    for (const b of branchesQuery.data ?? []) {
      map.set(b.id, { name: b.name, warehouse: isWarehouseBranch(b.branchType) });
    }
    return map;
  }, [branchesQuery.data]);

  const manageDestination = destinations.find((d) => d.id === manageDestinationId) ?? null;

  const openManage = (destinationId: string) => {
    const existing = routesByDestination.get(destinationId) ?? [];
    const selected: Record<string, boolean> = {};
    for (const route of existing.filter((r) => r.isActive)) {
      selected[route.sourceLocationId] = true;
    }
    setSelectedSources(selected);
    setPreferredSourceId(existing.find((r) => r.isPreferred && r.isActive)?.sourceLocationId ?? null);
    setManageDestinationId(destinationId);
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !manageDestinationId) return;
      const sourceIds = Object.entries(selectedSources)
        .filter(([, on]) => on)
        .map(([id]) => id);
      const routes = sourceIds.map((sourceLocationId) => ({
        sourceLocationId,
        isActive: true,
        isPreferred: preferredSourceId === sourceLocationId,
      }));
      await upsertSupplyRoutesForDestination(workspace, manageDestinationId, routes);
    },
    onSuccess: async () => {
      setManageDestinationId(null);
      await queryClient.invalidateQueries({ queryKey: ["supply-routes", orgId] });
    },
  });

  if (!orgId) {
    return <EmptyState title={t("supplyRoutes.needOrg")} detail={t("supplyRoutes.needOrgDetail")} />;
  }

  if (branchesQuery.isLoading || routesQuery.isLoading) {
    return <LoadingState label={t("supplyRoutes.loading")} />;
  }

  if (branchesQuery.isError || routesQuery.isError) {
    return (
      <ErrorState
        title={t("supplyRoutes.loadError")}
        detail={t("supplyRoutes.loadError")}
      />
    );
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="supply-routes-page">
      <PageHeader
        title={t("supplyRoutes.title")}
        description={t("supplyRoutes.lede")}
        backTo={pageBackNav.orgBranches.to}
        backLabel={t(pageBackNav.orgBranches.labelKey)}
        backTestId="page-header-back-branches"
      />

      <ExitsChipBar
        ariaLabel={t("supplyRoutes.filter.area")}
        variant="filter"
        items={[
          {
            key: "all",
            label: t("supplyRoutes.filter.all"),
            state: areaFilter === "all" ? "active" : "idle",
            onSelect: () => setAreaFilter("all"),
          },
          ...(areasQuery.data ?? []).map((a) => ({
            key: a.id,
            label: a.name,
            state: (areaFilter === a.id ? "active" : "idle") as "active" | "idle",
            onSelect: () => setAreaFilter(a.id),
          })),
          {
            key: "unassigned",
            label: t("supplyRoutes.filter.unassigned"),
            state: areaFilter === "unassigned" ? "active" : "idle",
            onSelect: () => setAreaFilter("unassigned"),
          },
        ]}
      />

      <ExitsChipBar
        ariaLabel={t("supplyRoutes.filter.type")}
        variant="filter"
        items={[
          {
            key: "all-types",
            label: t("supplyRoutes.filter.allTypes"),
            state: typeFilter === "all" ? "active" : "idle",
            onSelect: () => setTypeFilter("all"),
          },
          {
            key: "retail",
            label: t("supplyRoutes.filter.retail"),
            state: typeFilter === "retail" ? "active" : "idle",
            onSelect: () => setTypeFilter("retail"),
          },
          {
            key: "warehouse",
            label: t("supplyRoutes.filter.warehouse"),
            state: typeFilter === "warehouse" ? "active" : "idle",
            onSelect: () => setTypeFilter("warehouse"),
          },
        ]}
      />

      <input
        className="exits-input"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder={t("supplyRoutes.search")}
        data-testid="supply-routes-search"
      />

      {destinations.length === 0 ? (
        <EmptyState title={t("supplyRoutes.empty")} detail={t("supplyRoutes.emptyDetail")} />
      ) : (
        <ul className="flex flex-col gap-2">
          {destinations.map((dest) => {
            const routes = (routesByDestination.get(dest.id) ?? []).filter((r) => r.isActive);
            const preferred = routes.find((r) => r.isPreferred);
            const others = routes.filter((r) => !r.isPreferred);
            return (
              <li
                key={dest.id}
                className="rounded-[var(--exits-radius-md)] border border-[var(--exits-border)] p-3"
                data-testid={`supply-route-card-${dest.id}`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <div className="font-medium">{dest.name}</div>
                    <StatusChip tone="info">
                      {isWarehouseBranch(dest.branchType)
                        ? t("supplyRoutes.type.warehouse")
                        : t("supplyRoutes.type.retail")}
                    </StatusChip>
                  </div>
                  {allowManage ? (
                    <Button type="button" variant="outline" size="sm" onClick={() => openManage(dest.id)}>
                      {t("supplyRoutes.manageSources")}
                    </Button>
                  ) : null}
                </div>
                <div className="mt-2 text-[length:var(--exits-text-sm)] text-muted">
                  <div>{t("supplyRoutes.preferred")}</div>
                  <div className="text-foreground">
                    {preferred
                      ? `${nameById.get(preferred.sourceLocationId)?.name ?? preferred.sourceLocationId} [${
                          nameById.get(preferred.sourceLocationId)?.warehouse
                            ? t("supplyRoutes.type.warehouse")
                            : t("supplyRoutes.type.retail")
                        }]`
                      : t("supplyRoutes.none")}
                  </div>
                  {others.length > 0 ? (
                    <>
                      <div className="mt-1">{t("supplyRoutes.otherSources")}</div>
                      <ul>
                        {others.map((r) => (
                          <li key={r.routeId}>
                            {nameById.get(r.sourceLocationId)?.name ?? r.sourceLocationId}
                          </li>
                        ))}
                      </ul>
                    </>
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

      <BottomSheet
        open={manageDestination !== null}
        onClose={() => setManageDestinationId(null)}
        panelId="supply-routes-manage-panel"
        testId="supply-routes-manage-panel"
        title={t("supplyRoutes.manageTitle").replace("{name}", manageDestination?.name ?? "")}
        closeLabel={t("branches.cancel")}
      >
        <div className="flex flex-col gap-2" data-testid="supply-routes-manage">
          {(branchesQuery.data ?? [])
            .filter(
              (b) =>
                b.id !== manageDestinationId && normalizeBranchStatusFilter(b.status) === "Active",
            )
            .map((source) => {
              const checked = Boolean(selectedSources[source.id]);
              return (
                <label key={source.id} className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={(e) =>
                      setSelectedSources((prev) => ({ ...prev, [source.id]: e.target.checked }))
                    }
                  />
                  <span className="flex-1">
                    {source.name} [
                    {isWarehouseBranch(source.branchType)
                      ? t("supplyRoutes.type.warehouse")
                      : t("supplyRoutes.type.retail")}
                    ]
                  </span>
                  <button
                    type="button"
                    className="text-[length:var(--exits-text-xs)] underline disabled:opacity-40"
                    disabled={!checked}
                    onClick={() => setPreferredSourceId(source.id)}
                  >
                    {preferredSourceId === source.id
                      ? t("supplyRoutes.preferredBadge")
                      : t("supplyRoutes.setPreferred")}
                  </button>
                </label>
              );
            })}
          <Button
            type="button"
            disabled={saveMutation.isPending || !allowManage}
            onClick={() => saveMutation.mutate()}
            data-testid="supply-routes-save"
          >
            {t("expense.save")}
          </Button>
          {saveMutation.isError ? (
            <p className="text-danger text-[length:var(--exits-text-sm)]">{t("supplyRoutes.saveError")}</p>
          ) : null}
        </div>
      </BottomSheet>
    </div>
  );
}
