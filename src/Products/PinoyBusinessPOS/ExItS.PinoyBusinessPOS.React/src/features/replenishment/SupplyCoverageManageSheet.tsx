import { useEffect, useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  upsertSupplyCoverageBySource,
  type SupplyRouteDto,
} from "@/api/pos/pos-supply-routes-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  areaRetailMembers,
  areaTriState,
  connectedDestinationIds,
  filterLocationsBySearch,
  otherWarehouses,
  preferredSourceByDestination,
  retailDestinations,
  toggleAreaSelection,
  type CoverageLocation,
} from "@/features/replenishment/supply-coverage-helpers";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function plural(
  t: (key: MessageKey) => string,
  count: number,
  oneKey: MessageKey,
  manyKey: MessageKey,
): string {
  return (count === 1 ? t(oneKey) : t(manyKey)).replace("{count}", String(count));
}

export type SupplyCoverageArea = {
  id: string;
  name: string;
};

export function SupplyCoverageManageSheet({
  open,
  onClose,
  workspace,
  organizationId,
  sourceLocationId,
  sourceName,
  locations,
  routes,
  areas,
  allowManage,
}: {
  open: boolean;
  onClose: () => void;
  workspace: PosWorkspaceScope | null;
  organizationId: string | null;
  sourceLocationId: string | null;
  sourceName: string;
  locations: CoverageLocation[];
  routes: SupplyRouteDto[];
  areas: SupplyCoverageArea[];
  allowManage: boolean;
}) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [selectedDestinations, setSelectedDestinations] = useState<Set<string>>(new Set());
  const [preferredOverride, setPreferredOverride] = useState<Set<string>>(new Set());
  const [coverageSearch, setCoverageSearch] = useState("");

  useEffect(() => {
    if (!open || !sourceLocationId) {
      return;
    }
    setSelectedDestinations(connectedDestinationIds(routes, sourceLocationId));
    setPreferredOverride(new Set());
    setCoverageSearch("");
  }, [open, sourceLocationId, routes]);

  const nameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const loc of locations) map.set(loc.id, loc.name);
    return map;
  }, [locations]);

  const preferredByDest = useMemo(() => preferredSourceByDestination(routes), [routes]);

  const coverageRetail = retailDestinations(locations);
  const coverageOtherWarehouses = sourceLocationId
    ? otherWarehouses(locations, sourceLocationId)
    : [];
  const visibleRetail = filterLocationsBySearch(coverageRetail, coverageSearch);
  const visibleOtherWh = filterLocationsBySearch(coverageOtherWarehouses, coverageSearch);
  const areaIdsOrdered = [
    ...areas.map((a) => a.id),
    ...(coverageRetail.some((r) => !r.areaId) ? [null as string | null] : []),
  ];

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !sourceLocationId) return;
      await upsertSupplyCoverageBySource(
        workspace,
        sourceLocationId,
        [...selectedDestinations],
        [...preferredOverride],
      );
    },
    onSuccess: async () => {
      onClose();
      await queryClient.invalidateQueries({ queryKey: ["supply-routes", organizationId] });
    },
  });

  return (
    <BottomSheet
      open={open && Boolean(sourceLocationId)}
      onClose={onClose}
      panelId="supply-coverage-panel"
      testId="supply-coverage-panel"
      presentation="sheet-mobile-dialog-desktop"
      panelClassName="md:max-w-[720px] md:max-h-[80vh] md:flex md:flex-col"
      title={t("supplyRoutes.coverageTitle").replace("{name}", sourceName)}
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
                        ? sourceName
                        : preferredId
                          ? nameById.get(preferredId)
                          : null;
                      const canSetPreferred =
                        checked &&
                        allowManage &&
                        sourceLocationId &&
                        preferredId !== sourceLocationId &&
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
          <Button type="button" variant="outline" onClick={onClose}>
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
  );
}
