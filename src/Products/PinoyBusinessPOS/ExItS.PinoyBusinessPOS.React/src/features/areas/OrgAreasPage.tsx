import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { canInviteOrganizationStaff, canManageStoreAreas } from "@/access/pos-capabilities";
import {
  createOrganizationArea,
  listOrganizationAreas,
} from "@/api/platform/organization-areas-client";
import { listBranchManagementSummaries } from "@/api/platform/organization-branches-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function locationBreakdown(
  branches: Array<{ branchType?: string | null }>,
): { total: number; retail: number; warehouse: number } {
  const retail = branches.filter((branch) => !isWarehouseBranch(branch.branchType)).length;
  const warehouse = branches.filter((branch) => isWarehouseBranch(branch.branchType)).length;
  return { total: branches.length, retail, warehouse };
}

export function OrgAreasPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManageStaff = canInviteOrganizationStaff(sessionGrant);
  const areasEntitled = canManageStoreAreas(sessionGrant);
  const canManage = canManageStaff && areasEntitled;
  const organizationId = boundWorkspace?.organizationId ?? null;

  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const areasQuery = useQuery({
    queryKey: ["organization-areas", organizationId],
    enabled: Boolean(organizationId && canManage),
    queryFn: async ({ signal }) => {
      const result = await listOrganizationAreas(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("areas.loadError"));
      }
      return result.value;
    },
  });

  const branchesQuery = useQuery({
    queryKey: ["branch-management-summary", organizationId],
    enabled: Boolean(organizationId && canManage),
    queryFn: async ({ signal }) => {
      const result = await listBranchManagementSummaries(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.mgmt.loadError"));
      }
      return result.value;
    },
  });

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId) {
        throw new Error(t("areas.loadError"));
      }
      const trimmed = name.trim();
      if (!trimmed) {
        throw new Error(t("areas.nameRequired"));
      }
      const result = await createOrganizationArea(organizationId, {
        name: trimmed,
        code: code.trim() || null,
      });
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("areas.createError"));
      }
      return result.value;
    },
    onSuccess: async () => {
      setName("");
      setCode("");
      setFormOpen(false);
      setSubmitError(null);
      await queryClient.invalidateQueries({ queryKey: ["organization-areas", organizationId] });
    },
    onError: (error: Error) => setSubmitError(error.message),
  });

  const data = areasQuery.data;
  const liveBranches = useMemo(
    () =>
      (branchesQuery.data ?? []).filter(
        (branch) => normalizeBranchStatusFilter(branch.status) !== "Archived",
      ),
    [branchesQuery.data],
  );
  const unassigned = useMemo(
    () => liveBranches.filter((branch) => !branch.areaId),
    [liveBranches],
  );
  const unassignedBreakdown = locationBreakdown(unassigned);
  const activeAreas = useMemo(
    () => (data?.areas ?? []).filter((area) => area.status === "Active"),
    [data],
  );
  const archivedAreas = useMemo(
    () => (data?.areas ?? []).filter((area) => area.status === "Archived"),
    [data],
  );
  const atLimit = data != null && data.maxAreas > 0 && data.activeAreaCount >= data.maxAreas;

  function areaBreakdown(areaId: string) {
    return locationBreakdown(liveBranches.filter((branch) => branch.areaId === areaId));
  }

  function closeForm() {
    setFormOpen(false);
    setSubmitError(null);
  }

  const addAreaControl = (
    <Button
      type="button"
      className="branch-mgmt-add"
      disabled={atLimit || createMutation.isPending}
      data-testid="org-areas-add"
      onClick={() => {
        setFormOpen(true);
        setSubmitError(null);
      }}
    >
      <Plus className="size-4 shrink-0" aria-hidden />
      <span>{t("areas.add")}</span>
    </Button>
  );

  if (!canManage) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="org-areas-denied">
        <PageHeader
          title={t("areas.title")}
          description={
            canManageStaff && !areasEntitled
              ? t("areas.entitlementRequired")
              : t("areas.denied")
          }
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
      </div>
    );
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="org-areas-page">
      <PageHeader
        title={t("areas.title")}
        description={t("areas.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
        trailing={addAreaControl}
      />

      {data ? (
        <div className="branch-mgmt-capacity" data-testid="org-areas-capacity">
          <p className="branch-mgmt-capacity__label m-0">{t("areas.capacity")}</p>
          <p className="branch-mgmt-capacity__value m-0" data-testid="org-areas-capacity-value">
            {t("areas.capacityOf")
              .replace("{used}", String(data.activeAreaCount))
              .replace("{allowed}", String(data.maxAreas))}
          </p>
          {atLimit ? (
            <p className="branch-mgmt-capacity__limit m-0" data-testid="org-areas-capacity-limit">
              {t("areas.capacityLimit").replace("{allowed}", String(data.maxAreas))}
            </p>
          ) : null}
        </div>
      ) : null}

      {areasQuery.isLoading || branchesQuery.isLoading ? (
        <LoadingSkeleton count={3} label={t("loading.label")} />
      ) : null}

      {areasQuery.isError || branchesQuery.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            areasQuery.error instanceof Error
              ? areasQuery.error.message
              : branchesQuery.error instanceof Error
                ? branchesQuery.error.message
                : t("areas.loadError")
          }
        />
      ) : null}

      {areasQuery.isSuccess && data ? (
        <>
          {data.areas.length === 0 ? (
            <EmptyState title={t("areas.emptyTitle")} detail={t("areas.emptyDetail")} />
          ) : (
            <ul className="m-0 grid list-none gap-2 p-0" data-testid="org-areas-items">
              {[...activeAreas, ...archivedAreas].map((area) => {
                const breakdown = areaBreakdown(area.id);
                return (
                  <li key={area.id}>
                    <article
                      className="exits-entity-card exits-entity-card--interactive min-w-0"
                      data-testid={`org-area-card-${area.id}`}
                    >
                      <div className="exits-entity-card__header">
                        <div className="exits-entity-card__identity">
                          <div className="exits-entity-card__title-row">
                            <h2 className="exits-entity-card__title">{area.name}</h2>
                            {area.code ? (
                              <>
                                <span className="exits-entity-card__code-sep" aria-hidden>
                                  ·
                                </span>
                                <p className="exits-entity-card__code">{area.code}</p>
                              </>
                            ) : null}
                          </div>
                        </div>
                        <div className="exits-entity-card__badges">
                          <StatusChip tone={area.status === "Active" ? "success" : "info"}>
                            {area.status === "Active"
                              ? t("areas.status.active")
                              : t("areas.status.archived")}
                          </StatusChip>
                        </div>
                      </div>
                      <div className="exits-entity-card__meta">
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)]"
                          data-testid={`org-area-branch-count-${area.id}`}
                        >
                          {t("areas.locationCount").replace("{count}", String(breakdown.total))}
                        </p>
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                          data-testid={`org-area-type-breakdown-${area.id}`}
                        >
                          {t("areas.locationBreakdown")
                            .replace("{retail}", String(breakdown.retail))
                            .replace("{warehouse}", String(breakdown.warehouse))}
                        </p>
                      </div>
                      <div className="exits-entity-card__actions">
                        <Button asChild variant="outline" data-testid={`org-area-open-${area.id}`}>
                          <Link to={`/org/areas/${area.id}`}>
                            {t("areas.open")}
                            <span aria-hidden> →</span>
                          </Link>
                        </Button>
                      </div>
                    </article>
                  </li>
                );
              })}
            </ul>
          )}

          {unassignedBreakdown.total > 0 ? (
            <div className="branch-mgmt-capacity" data-testid="org-areas-unassigned">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
                {t("areas.unassignedCount").replace(
                  "{count}",
                  String(unassignedBreakdown.total),
                )}
              </p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("areas.locationBreakdown")
                  .replace("{retail}", String(unassignedBreakdown.retail))
                  .replace("{warehouse}", String(unassignedBreakdown.warehouse))}
              </p>
            </div>
          ) : (
            <p className="m-0 text-muted" data-testid="org-areas-unassigned">
              {t("areas.unassignedCount").replace("{count}", "0")}
            </p>
          )}
        </>
      ) : null}

      <BottomSheet
        open={formOpen}
        onClose={closeForm}
        panelId="org-areas-create-panel"
        testId="org-areas-form"
        title={t("areas.create.title")}
        closeLabel={t("areas.cancel")}
        panelClassName="sm:inset-x-auto sm:bottom-auto sm:left-1/2 sm:top-1/2 sm:w-full sm:max-w-md sm:-translate-x-1/2 sm:-translate-y-1/2 sm:rounded-[var(--exits-radius-lg)]"
      >
        <div className="flex flex-col gap-3">
          <label className="flex flex-col gap-1">
            <span className="text-[length:var(--exits-text-sm)] font-medium">
              {t("areas.create.name")}
            </span>
            <input
              className="exits-input"
              value={name}
              maxLength={100}
              data-testid="org-areas-name"
              onChange={(event) => {
                setName(event.target.value);
                setSubmitError(null);
              }}
            />
          </label>
          <label className="flex flex-col gap-1">
            <span className="text-[length:var(--exits-text-sm)] font-medium">
              {t("areas.create.code")}
            </span>
            <input
              className="exits-input"
              value={code}
              maxLength={32}
              data-testid="org-areas-code"
              onChange={(event) => {
                setCode(event.target.value);
                setSubmitError(null);
              }}
            />
          </label>
          {submitError ? (
            <div className="exits-alert exits-alert--error" role="alert">
              <p className="m-0 text-[length:var(--exits-text-sm)]">{submitError}</p>
            </div>
          ) : null}
          <div className="flex flex-wrap justify-end gap-2">
            <Button type="button" variant="outline" data-testid="org-areas-cancel" onClick={closeForm}>
              {t("areas.cancel")}
            </Button>
            <Button
              type="button"
              disabled={createMutation.isPending || !name.trim()}
              data-testid="org-areas-submit"
              onClick={() => createMutation.mutate()}
            >
              {createMutation.isPending ? t("areas.saving") : t("areas.create.submit")}
            </Button>
          </div>
        </div>
      </BottomSheet>
    </div>
  );
}
