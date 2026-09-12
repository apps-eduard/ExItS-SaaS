import { ArrowLeftRight } from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listBranchManagementSummaries } from "@/api/platform/organization-branches-client";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import {
  listSupplyRoutes,
  upsertSupplyCoverageBySource,
  type SupplyRouteDto,
} from "@/api/pos/pos-supply-routes-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { BottomSheet } from "@/components/exits/SheetDialog";
import {
  areaRetailMembers,
  areaTriState,
  connectedDestinationIds,
  filterLocationsBySearch,
  otherWarehouses,
  preferredSourceByDestination,
  retailDestinations,
  toggleAreaSelection,
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
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const [searchWarehouses, setSearchWarehouses] = useState("");
  const [manageSourceId, setManageSourceId] = useState<string | null>(null);
  const [selectedDestinations, setSelectedDestinations] = useState<Set<string>>(new Set());
  const [preferredOverride, setPreferredOverride] = useState<Set<string>>(new Set());
  const [coverageSearch, setCoverageSearch] = useState("");

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

  const nameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const loc of locations) map.set(loc.id, loc.name);
    return map;
  }, [locations]);

  const preferredByDest = useMemo(() => preferredSourceByDestination(routes), [routes]);

  const manageWarehouse = locations.find((l) => l.id === manageSourceId) ?? null;

  const openManage = (sourceId: string) => {
    setSelectedDestinations(connectedDestinationIds(routes, sourceId));
    setPreferredOverride(new Set());
    setCoverageSearch("");
    setManageSourceId(sourceId);
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !manageSourceId) return;
      await upsertSupplyCoverageBySource(
        workspace,
        manageSourceId,
        [...selectedDestinations],
        [...preferredOverride],
      );
    },
    onSuccess: async () => {
      setManageSourceId(null);
      await queryClient.invalidateQueries({ queryKey: ["supply-routes", orgId] });
    },
  });

  if (!orgId) {
    return <EmptyState
              align="center"
              icon={<ArrowLeftRight className="size-5" strokeWidth={1.75} />} title={t("supplyRoutes.needOrg")} detail={t("supplyRoutes.needOrgDetail")} />;
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
  const coverageRetail = retailDestinations(locations);
  const coverageOtherWarehouses = manageSourceId
    ? otherWarehouses(locations, manageSourceId)
    : [];
  const visibleRetail = filterLocationsBySearch(coverageRetail, coverageSearch);
  const visibleOtherWh = filterLocationsBySearch(coverageOtherWarehouses, coverageSearch);
  const areaIdsOrdered = [
    ...areas.map((a) => a.id),
    ...(coverageRetail.some((r) => !r.areaId) ? [null as string | null] : []),
  ];

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
                      onClick={() => openManage(wh.id)}
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

      <BottomSheet
        open={manageWarehouse !== null}
        onClose={() => setManageSourceId(null)}
        panelId="supply-coverage-panel"
        testId="supply-coverage-panel"
        presentation="sheet-mobile-dialog-desktop"
        panelClassName="md:max-w-[720px] md:max-h-[80vh] md:flex md:flex-col"
        title={t("supplyRoutes.coverageTitle").replace("{name}", manageWarehouse?.name ?? "")}
        closeLabel={t("branches.cancel")}
      >
        <div
          className="flex min-h-0 flex-1 flex-col gap-3 md:overflow-hidden"
          data-testid="supply-coverage-manage"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("supplyRoutes.coverageLede")}
          </p>
          <input
            className="exits-input"
            value={coverageSearch}
            onChange={(e) => setCoverageSearch(e.target.value)}
            placeholder={t("supplyRoutes.searchLocations")}
            data-testid="supply-coverage-search"
          />

          <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto md:pr-1">
            <section>
              <h3 className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
                {t("supplyRoutes.areasHeading")}
              </h3>
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {areas.map((area) => {
                  const members = areaRetailMembers(locations, area.id);
                  if (members.length === 0) return null;
                  const memberIds = members.map((m) => m.id);
                  const state = areaTriState(memberIds, selectedDestinations);
                  const selectedCount = memberIds.filter((id) => selectedDestinations.has(id)).length;
                  return (
                    <li key={area.id}>
                      <label className="flex cursor-pointer items-start gap-2 text-[length:var(--exits-text-sm)]">
                        <input
                          type="checkbox"
                          className="mt-0.5"
                          checked={state === "checked"}
                          ref={(el) => {
                            if (el) el.indeterminate = state === "partial";
                          }}
                          onChange={() =>
                            setSelectedDestinations((prev) => toggleAreaSelection(memberIds, prev))
                          }
                          data-testid={`supply-area-${area.id}`}
                        />
                        <span>
                          <span className="font-medium text-foreground">{area.name}</span>
                          <span className="block text-muted">
                            {state === "partial"
                              ? t("supplyRoutes.areaPartialCount")
                                  .replace("{selected}", String(selectedCount))
                                  .replace("{total}", String(members.length))
                              : plural(
                                  t,
                                  members.length,
                                  "supplyRoutes.count.retailOne",
                                  "supplyRoutes.count.retailMany",
                                )}
                          </span>
                        </span>
                      </label>
                    </li>
                  );
                })}
              </ul>
            </section>

            <section>
              <h3 className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
                {t("supplyRoutes.retailLocations")}
              </h3>
              {areaIdsOrdered.map((areaId) => {
                const members = areaRetailMembers(locations, areaId);
                const visible = members.filter((m) => visibleRetail.some((v) => v.id === m.id));
                if (visible.length === 0) return null;
                const heading =
                  areaId === null
                    ? t("supplyRoutes.unassigned")
                    : areas.find((a) => a.id === areaId)?.name ?? t("supplyRoutes.unassigned");
                return (
                  <div key={areaId ?? "unassigned"} className="mb-3">
                    <div className="mb-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {heading}
                    </div>
                    <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
                      {visible.map((loc) => {
                        const checked = selectedDestinations.has(loc.id);
                        const preferredId = preferredByDest.get(loc.id);
                        const pendingPreferred = preferredOverride.has(loc.id);
                        const preferredName = pendingPreferred
                          ? manageWarehouse?.name
                          : preferredId
                            ? nameById.get(preferredId)
                            : null;
                        const canSetPreferred =
                          checked &&
                          allowManage &&
                          manageSourceId &&
                          preferredId !== manageSourceId &&
                          !pendingPreferred;
                        return (
                          <li key={loc.id} className="flex flex-col gap-0.5">
                            <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                              <input
                                type="checkbox"
                                checked={checked}
                                onChange={(e) => {
                                  const checkedNow = e.target.checked;
                                  setSelectedDestinations((prev) => {
                                    const next = new Set(prev);
                                    if (checkedNow) next.add(loc.id);
                                    else next.delete(loc.id);
                                    return next;
                                  });
                                  if (!checkedNow) {
                                    setPreferredOverride((p) => {
                                      const cleared = new Set(p);
                                      cleared.delete(loc.id);
                                      return cleared;
                                    });
                                  }
                                }}
                                data-testid={`supply-dest-${loc.id}`}
                              />
                              <span className="flex-1 truncate">{loc.name}</span>
                              <StatusChip tone="neutral">{t("supplyRoutes.type.retail")}</StatusChip>
                            </label>
                            {checked && preferredName ? (
                              <div className="pl-6 text-[length:var(--exits-text-xs)] text-muted">
                                {t("supplyRoutes.preferredLabel")}: {preferredName}
                              </div>
                            ) : null}
                            {canSetPreferred ? (
                              <button
                                type="button"
                                className="pl-6 text-left text-[length:var(--exits-text-xs)] underline"
                                onClick={() =>
                                  setPreferredOverride((prev) => new Set(prev).add(loc.id))
                                }
                                data-testid={`supply-set-preferred-${loc.id}`}
                              >
                                {t("supplyRoutes.setThisWarehousePreferred")}
                              </button>
                            ) : null}
                          </li>
                        );
                      })}
                    </ul>
                  </div>
                );
              })}
            </section>

            <details className="rounded-[var(--exits-radius-sm)] border border-[var(--exits-border)] p-2">
              <summary className="cursor-pointer text-[length:var(--exits-text-sm)] font-semibold">
                {t("supplyRoutes.otherWarehouses")}
              </summary>
              <ul className="mt-2 m-0 flex list-none flex-col gap-1.5 p-0">
                {visibleOtherWh.length === 0 ? (
                  <li className="text-[length:var(--exits-text-sm)] text-muted">
                    {t("supplyRoutes.otherWarehousesEmpty")}
                  </li>
                ) : (
                  visibleOtherWh.map((loc) => (
                    <li key={loc.id}>
                      <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                        <input
                          type="checkbox"
                          checked={selectedDestinations.has(loc.id)}
                          onChange={(e) => {
                            setSelectedDestinations((prev) => {
                              const next = new Set(prev);
                              if (e.target.checked) next.add(loc.id);
                              else next.delete(loc.id);
                              return next;
                            });
                          }}
                          data-testid={`supply-wh-dest-${loc.id}`}
                        />
                        <span className="flex-1 truncate">{loc.name}</span>
                        <StatusChip tone="info">{t("supplyRoutes.type.warehouse")}</StatusChip>
                      </label>
                    </li>
                  ))
                )}
              </ul>
            </details>
          </div>

          <div className="flex flex-wrap justify-end gap-2 border-t border-[var(--exits-border)] pt-3">
            <Button type="button" variant="outline" onClick={() => setManageSourceId(null)}>
              {t("branches.cancel")}
            </Button>
            <Button
              type="button"
              disabled={saveMutation.isPending || !allowManage}
              onClick={() => saveMutation.mutate()}
              data-testid="supply-coverage-save"
            >
              {t("supplyRoutes.saveChanges")}
            </Button>
          </div>
          {saveMutation.isError ? (
            <p className="text-danger text-[length:var(--exits-text-sm)]">{t("supplyRoutes.saveError")}</p>
          ) : null}
        </div>
      </BottomSheet>
    </div>
  );
}
