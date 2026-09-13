import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronDown, ChevronRight, MapPinned, Save } from "lucide-react";
import {
  grantCustomerBranchAccess,
  listCustomerBranchAccess,
  revokeCustomerBranchAccess,
} from "@/api/pos/pos-customer-branch-access-client";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import { listBranchManagementSummaries } from "@/api/platform/organization-branches-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError } from "@/api/pos/pos-http";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { CountBadge } from "@/components/exits/CountChip";
import { LoadingState } from "@/components/exits/LoadingState";
import {
  applyVisibilityMode,
  areaCheckboxState,
  areaSelectedCount,
  diffBranchAccess,
  groupBranchesByArea,
  resolveVisibilityMode,
  toggleAreaSelection,
  toggleBranchSelection,
  withHomeBranchLocked,
  type CustomerVisibilityMode,
  type VisibilityBranch,
} from "@/features/customers/customer-branch-visibility";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type CustomerBranchVisibilitySectionProps = {
  workspace: PosWorkspaceScope;
  organizationId: string;
  customerId: string;
  online: boolean;
  canManage: boolean;
};

function AreaIndeterminateCheckbox({
  state,
  disabled,
  onToggle,
  label,
  testId,
}: {
  state: "unchecked" | "checked" | "indeterminate";
  disabled?: boolean;
  onToggle: () => void;
  label: string;
  testId: string;
}) {
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (ref.current) {
      ref.current.indeterminate = state === "indeterminate";
    }
  }, [state]);

  return (
    <label className="inline-flex min-w-0 cursor-pointer items-center gap-2">
      <input
        ref={ref}
        type="checkbox"
        className="size-4 accent-[var(--exits-primary)]"
        checked={state === "checked"}
        disabled={disabled}
        data-testid={testId}
        aria-checked={state === "indeterminate" ? "mixed" : state === "checked"}
        onChange={onToggle}
      />
      <span className="min-w-0 truncate font-medium">{label}</span>
    </label>
  );
}

export function CustomerBranchVisibilitySection({
  workspace,
  organizationId,
  customerId,
  online,
  canManage,
}: CustomerBranchVisibilitySectionProps) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [mode, setMode] = useState<CustomerVisibilityMode>("this_branch");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [error, setError] = useState<string | null>(null);
  const [hydrated, setHydrated] = useState(false);

  const accessQuery = useQuery({
    queryKey: ["customer-branch-access", organizationId, customerId],
    enabled: online && Boolean(customerId) && canManage,
    queryFn: ({ signal }) => listCustomerBranchAccess(workspace, customerId, signal),
  });

  const branchesQuery = useQuery({
    queryKey: ["customer-branch-visibility-branches", organizationId],
    enabled: online && canManage,
    queryFn: async ({ signal }) => {
      const result = await listBranchManagementSummaries(organizationId, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("customers.branchAccess.loadError"));
      }
      return result.value;
    },
  });

  const areasQuery = useQuery({
    queryKey: ["customer-branch-visibility-areas", organizationId],
    enabled: online && canManage,
    queryFn: async ({ signal }) => {
      const result = await listOrganizationAreas(organizationId, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("customers.branchAccess.loadError"));
      }
      return result.value;
    },
  });

  const visibilityBranches: VisibilityBranch[] = useMemo(() => {
    const rows = branchesQuery.data ?? [];
    return rows.map((row) => ({
      id: row.id,
      name: row.name,
      areaId: row.areaId,
      areaName: row.areaName,
      status: row.status,
    }));
  }, [branchesQuery.data]);

  const activeAreas = useMemo(
    () =>
      (areasQuery.data?.areas ?? []).filter(
        (area) => area.status === "Active" && area.name.trim().length > 0,
      ),
    [areasQuery.data],
  );

  const hasAreas = activeAreas.length > 0;
  const eligibleIds = useMemo(
    () =>
      visibilityBranches
        .filter((b) => {
          const status = (b.status ?? "Active").trim().toLowerCase();
          return status === "" || status === "active";
        })
        .map((b) => b.id),
    [visibilityBranches],
  );

  const homeBranchId = accessQuery.data?.homeBranchId ?? eligibleIds[0] ?? null;
  const homeBranchName =
    visibilityBranches.find((b) => b.id === homeBranchId)?.name ??
    t("customers.branchAccess.homeUnknown");

  const groups = useMemo(
    () =>
      groupBranchesByArea(
        visibilityBranches,
        hasAreas ? activeAreas.map((a) => ({ id: a.id, name: a.name })) : [],
        hasAreas
          ? t("customers.branchAccess.unassigned")
          : t("customers.branchAccess.branches"),
      ),
    [visibilityBranches, hasAreas, activeAreas, t],
  );

  useEffect(() => {
    if (!accessQuery.isSuccess || hydrated) {
      return;
    }
    const currentIds = [
      ...new Set(accessQuery.data.items.map((item) => item.branchId).filter(Boolean)),
    ];
    const next = withHomeBranchLocked(currentIds, accessQuery.data.homeBranchId ?? homeBranchId);
    setSelected(next);
    setMode(resolveVisibilityMode(next, eligibleIds, accessQuery.data.homeBranchId ?? homeBranchId));
    const initialExpanded: Record<string, boolean> = {};
    for (const group of groups) {
      initialExpanded[group.key] = areaSelectedCount(group, next).selected > 0 || groups.length <= 2;
    }
    setExpanded(initialExpanded);
    setHydrated(true);
  }, [accessQuery.isSuccess, accessQuery.data, hydrated, eligibleIds, homeBranchId, groups]);

  const currentAccessIds = useMemo(
    () => [...new Set((accessQuery.data?.items ?? []).map((i) => i.branchId))],
    [accessQuery.data],
  );

  const dirty = useMemo(() => {
    if (!hydrated) {
      return false;
    }
    const desired = withHomeBranchLocked(selected, homeBranchId);
    if (desired.size !== currentAccessIds.length) {
      return true;
    }
    return currentAccessIds.some((id) => !desired.has(id));
  }, [hydrated, selected, homeBranchId, currentAccessIds]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      const desired = withHomeBranchLocked(selected, homeBranchId);
      const { toGrant, toRevoke } = diffBranchAccess({
        currentBranchIds: currentAccessIds,
        desiredBranchIds: desired,
        homeBranchId,
      });
      for (const branchId of toGrant) {
        await grantCustomerBranchAccess(workspace, customerId, branchId);
      }
      for (const branchId of toRevoke) {
        await revokeCustomerBranchAccess(workspace, customerId, branchId);
      }
    },
    onSuccess: async () => {
      setError(null);
      setHydrated(false);
      await queryClient.invalidateQueries({
        queryKey: ["customer-branch-access", organizationId, customerId],
      });
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError
          ? err.problem?.detail ?? t("customers.branchAccess.saveError")
          : err instanceof Error
            ? err.message
            : t("customers.branchAccess.saveError"),
      );
    },
  });

  if (!canManage) {
    return null;
  }

  if (!online) {
    return (
      <Card className="flex flex-col gap-2 p-3" data-testid="customer-branch-access-offline">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.branchAccess.title")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.branchAccess.offline")}
        </p>
      </Card>
    );
  }

  const loading =
    accessQuery.isLoading || branchesQuery.isLoading || areasQuery.isLoading || !hydrated;

  return (
    <Card className="flex flex-col gap-3 p-3" data-testid="customer-branch-access">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("customers.branchAccess.title")}
          </h2>
          <p className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("customers.branchAccess.help")}
          </p>
        </div>
        <MapPinned className="size-4 shrink-0 text-muted" aria-hidden />
      </div>

      {loading ? <LoadingState label={t("loading.label")} /> : null}

      {!loading ? (
        <>
          <div data-testid="customer-branch-access-home">
            <p className="m-0 text-[length:var(--exits-text-xs)] font-medium text-muted">
              {t("customers.branchAccess.homeBranch")}
            </p>
            <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] font-semibold">
              {homeBranchName}
            </p>
          </div>

          <fieldset className="m-0 border-0 p-0">
            <legend className="mb-1.5 text-[length:var(--exits-text-sm)] font-medium">
              {t("customers.branchAccess.visibility")}
            </legend>
            <div className="flex flex-col gap-1.5" role="radiogroup">
              {(
                [
                  ["this_branch", t("customers.branchAccess.modeThisBranch")],
                  ["selected", t("customers.branchAccess.modeSelected")],
                  ["all", t("customers.branchAccess.modeAll")],
                ] as const
              ).map(([value, label]) => (
                <label
                  key={value}
                  className="inline-flex cursor-pointer items-center gap-2 text-[length:var(--exits-text-sm)]"
                >
                  <input
                    type="radio"
                    name="customer-branch-visibility-mode"
                    checked={mode === value}
                    data-testid={`customer-branch-access-mode-${value}`}
                    onChange={() => {
                      setMode(value);
                      if (value === "selected") {
                        setSelected((prev) => withHomeBranchLocked(prev, homeBranchId));
                        return;
                      }
                      setSelected(applyVisibilityMode(value, eligibleIds, homeBranchId));
                    }}
                  />
                  {label}
                </label>
              ))}
            </div>
          </fieldset>

          {mode !== "this_branch" ? (
            <div className="flex flex-col gap-2" data-testid="customer-branch-access-checklist">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
                {t("customers.branchAccess.selectedBranches")}
              </p>
              {groups.map((group) => {
                const state = areaCheckboxState(group, selected);
                const counts = areaSelectedCount(group, selected);
                const isOpen = expanded[group.key] ?? true;
                const showAreaHelper = hasAreas && group.branches.length > 0;
                return (
                  <div
                    key={group.key}
                    className="rounded-[var(--exits-radius-md)] border border-border px-2 py-1.5"
                    data-testid={`customer-branch-access-group-${group.key}`}
                  >
                    <div className="flex min-w-0 items-center gap-2">
                      {showAreaHelper ? (
                        <AreaIndeterminateCheckbox
                          state={state}
                          label={group.name}
                          testId={`customer-branch-access-area-${group.key}`}
                          onToggle={() => {
                            setMode("selected");
                            setSelected(
                              toggleAreaSelection({
                                group,
                                selected,
                                homeBranchId,
                              }),
                            );
                            setExpanded((prev) => ({ ...prev, [group.key]: true }));
                          }}
                        />
                      ) : (
                        <span className="min-w-0 truncate text-[length:var(--exits-text-sm)] font-medium">
                          {group.name}
                        </span>
                      )}
                      <CountBadge
                        count={`${counts.selected} of ${counts.total}`}
                        tone="neutral"
                        className="ms-auto shrink-0"
                      />
                      <button
                        type="button"
                        className="inline-flex size-7 items-center justify-center rounded-md text-muted hover:bg-[var(--exits-surface-muted)]"
                        aria-expanded={isOpen}
                        data-testid={`customer-branch-access-expand-${group.key}`}
                        onClick={() =>
                          setExpanded((prev) => ({ ...prev, [group.key]: !isOpen }))
                        }
                      >
                        {isOpen ? (
                          <ChevronDown className="size-4" aria-hidden />
                        ) : (
                          <ChevronRight className="size-4" aria-hidden />
                        )}
                      </button>
                    </div>
                    {isOpen ? (
                      <ul className="m-0 mt-1 list-none space-y-1 border-t border-border pt-1.5 ps-1">
                        {group.branches.map((branch) => {
                          const isHome = homeBranchId === branch.id;
                          const checked = selected.has(branch.id);
                          return (
                            <li key={branch.id}>
                              <label
                                className={cn(
                                  "flex min-w-0 items-center gap-2 text-[length:var(--exits-text-sm)]",
                                  isHome && "opacity-90",
                                )}
                              >
                                <input
                                  type="checkbox"
                                  className="size-4 accent-[var(--exits-primary)]"
                                  checked={checked}
                                  disabled={isHome}
                                  data-testid={`customer-branch-access-branch-${branch.id}`}
                                  onChange={() => {
                                    setMode("selected");
                                    setSelected(
                                      toggleBranchSelection({
                                        branchId: branch.id,
                                        selected,
                                        homeBranchId,
                                      }),
                                    );
                                  }}
                                />
                                <span className="min-w-0 truncate">{branch.name}</span>
                                {isHome ? (
                                  <span className="ms-auto shrink-0 text-[length:var(--exits-text-xs)] text-muted">
                                    {t("customers.branchAccess.homeBadge")}
                                  </span>
                                ) : null}
                              </label>
                            </li>
                          );
                        })}
                      </ul>
                    ) : null}
                  </div>
                );
              })}
            </div>
          ) : null}

          {error ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
              {error}
            </p>
          ) : null}

          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              disabled={!dirty || saveMutation.isPending}
              data-testid="customer-branch-access-save"
              onClick={() => saveMutation.mutate()}
            >
              <Save className={`size-4 shrink-0 ${buttonIconMotion.add}`} aria-hidden />
              {saveMutation.isPending
                ? t("customers.branchAccess.saving")
                : t("customers.branchAccess.save")}
            </Button>
          </div>
        </>
      ) : null}
    </Card>
  );
}
