import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowRightLeft, Plus, X } from "lucide-react";
import { canInviteOrganizationStaff } from "@/access/pos-capabilities";
import {
  archiveOrganizationArea,
  listOrganizationAreas,
  setBranchArea,
  updateOrganizationArea,
} from "@/api/platform/organization-areas-client";
import {
  listBranchManagementSummaries,
  type BranchManagementSummaryItemDto,
} from "@/api/platform/organization-branches-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type PendingLocationAction =
  | { kind: "assign" | "transfer"; branch: BranchManagementSummaryItemDto }
  | { kind: "remove"; branch: BranchManagementSummaryItemDto }
  | null;

export function OrgAreaDetailPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { areaId } = useParams<{ areaId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canInviteOrganizationStaff(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;

  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [formReady, setFormReady] = useState(false);
  const [editing, setEditing] = useState(false);
  const [archiveOpen, setArchiveOpen] = useState(false);
  const [pendingAction, setPendingAction] = useState<PendingLocationAction>(null);
  const [actionError, setActionError] = useState<string | null>(null);

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

  const area = useMemo(
    () => areasQuery.data?.areas.find((item) => item.id === areaId) ?? null,
    [areasQuery.data, areaId],
  );

  useEffect(() => {
    if (!area || formReady) {
      return;
    }
    setName(area.name);
    setCode(area.code ?? "");
    setFormReady(true);
  }, [area, formReady]);

  const liveBranches = useMemo(
    () =>
      (branchesQuery.data ?? []).filter(
        (branch) => normalizeBranchStatusFilter(branch.status) !== "Archived",
      ),
    [branchesQuery.data],
  );
  const inArea = liveBranches.filter((branch) => branch.areaId === areaId);
  const outsideArea = liveBranches.filter((branch) => branch.areaId !== areaId);

  async function refresh(): Promise<void> {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["organization-areas", organizationId] }),
      queryClient.invalidateQueries({ queryKey: ["branch-management-summary", organizationId] }),
    ]);
  }

  const renameMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId || !areaId) {
        throw new Error(t("areas.loadError"));
      }
      const trimmed = name.trim();
      if (!trimmed) {
        throw new Error(t("areas.nameRequired"));
      }
      const result = await updateOrganizationArea(organizationId, areaId, {
        name: trimmed,
        code: code.trim() || null,
      });
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("areas.saveError"));
      }
    },
    onSuccess: async () => {
      setActionError(null);
      setEditing(false);
      setFormReady(false);
      await refresh();
    },
    onError: (error: Error) => setActionError(error.message),
  });

  const branchMutation = useMutation({
    mutationFn: async (input: { branchId: string; target: string | null }) => {
      if (!organizationId) {
        throw new Error(t("areas.loadError"));
      }
      const result = await setBranchArea(organizationId, input.branchId, input.target);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("areas.saveError"));
      }
    },
    onSuccess: async () => {
      setPendingAction(null);
      setActionError(null);
      await refresh();
    },
    onError: (error: Error) => {
      setPendingAction(null);
      setActionError(error.message);
    },
  });

  const archiveMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId || !areaId) {
        throw new Error(t("areas.loadError"));
      }
      const result = await archiveOrganizationArea(organizationId, areaId);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("areas.archiveError"));
      }
    },
    onSuccess: async () => {
      setArchiveOpen(false);
      setActionError(null);
      await refresh();
      navigate("/org/areas", { replace: true });
    },
    onError: (error: Error) => {
      setArchiveOpen(false);
      setActionError(error.message);
    },
  });

  function confirmPendingAction() {
    if (!pendingAction) {
      return;
    }
    if (pendingAction.kind === "remove") {
      branchMutation.mutate({ branchId: pendingAction.branch.id, target: null });
      return;
    }
    branchMutation.mutate({
      branchId: pendingAction.branch.id,
      target: areaId ?? null,
    });
  }

  const pendingConfirm =
    pendingAction == null || !area
      ? null
      : pendingAction.kind === "remove"
        ? {
            title: t("areas.detail.removeConfirmTitle"),
            detail: t("areas.detail.removeConfirmDetail")
              .replace("{location}", pendingAction.branch.name)
              .replace("{area}", area.name),
            confirmLabel: t("areas.detail.removeConfirmAction"),
            testId: "org-area-remove-confirm",
          }
        : pendingAction.kind === "transfer"
          ? {
              title: t("areas.detail.transferConfirmTitle"),
              detail: t("areas.detail.transferConfirmDetail")
                .replace("{location}", pendingAction.branch.name)
                .replace("{fromArea}", pendingAction.branch.areaName ?? t("areas.unassigned"))
                .replace("{area}", area.name),
              confirmLabel: t("areas.detail.transferConfirmAction"),
              testId: "org-area-transfer-confirm",
            }
          : {
              title: t("areas.detail.assignConfirmTitle"),
              detail: t("areas.detail.assignConfirmDetail")
                .replace("{location}", pendingAction.branch.name)
                .replace("{area}", area.name),
              confirmLabel: t("areas.detail.assignConfirmAction"),
              testId: "org-area-assign-confirm",
            };

  if (!canManage) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="org-area-detail-denied">
        <PageHeader
          title={t("areas.detail.title")}
          description={t("areas.denied")}
          backTo="/org/areas"
          backLabel={t("areas.title")}
          backTestId="page-header-back-areas"
        />
      </div>
    );
  }

  const loading = areasQuery.isLoading || branchesQuery.isLoading;
  const busy = renameMutation.isPending || branchMutation.isPending || archiveMutation.isPending;

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="org-area-detail-page">
      <PageHeader
        title={area?.name ?? t("areas.detail.title")}
        description={t("areas.detail.lede")}
        backTo="/org/areas"
        backLabel={t("areas.title")}
        backTestId="page-header-back-areas"
        trailing={
          area ? (
            <StatusChip tone={area.status === "Active" ? "success" : "info"}>
              {area.status === "Active" ? t("areas.status.active") : t("areas.status.archived")}
            </StatusChip>
          ) : null
        }
      />

      {loading ? <LoadingSkeleton count={3} label={t("loading.label")} /> : null}

      {areasQuery.isError || branchesQuery.isError ? (
        <ErrorState title={t("error.title")} detail={t("areas.loadError")} />
      ) : null}

      {!loading && areasQuery.isSuccess && !area ? (
        <ErrorState title={t("error.title")} detail={t("areas.notFound")} />
      ) : null}

      {area ? (
        <>
          <section className="catalog-form-section exits-animate-panel gap-3" data-testid="org-area-rename">
            <div className="flex min-w-0 items-center justify-between gap-2">
              <h2 className="catalog-form-section__title m-0">{t("areas.rename")}</h2>
              {area.status === "Active" && !editing ? (
                <Button
                  type="button"
                  variant="outline"
                  data-testid="org-area-edit"
                  onClick={() => setEditing(true)}
                >
                  {t("areas.edit")}
                </Button>
              ) : null}
            </div>

            {!editing ? (
              <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="org-area-summary">
                <span className="font-medium">{area.name}</span>
                {area.code ? (
                  <>
                    <span className="text-muted"> · </span>
                    <span className="text-muted">{area.code}</span>
                  </>
                ) : null}
              </p>
            ) : (
              <>
                <label className="flex flex-col gap-1">
                  <span className="text-[length:var(--exits-text-sm)]">{t("areas.create.name")}</span>
                  <input
                    className="exits-input"
                    value={name}
                    maxLength={100}
                    disabled={busy}
                    data-testid="org-area-name"
                    onChange={(event) => setName(event.target.value)}
                  />
                </label>
                <label className="flex flex-col gap-1">
                  <span className="text-[length:var(--exits-text-sm)]">{t("areas.create.code")}</span>
                  <input
                    className="exits-input"
                    value={code}
                    maxLength={32}
                    disabled={busy}
                    data-testid="org-area-code"
                    onChange={(event) => setCode(event.target.value)}
                  />
                </label>
                <div className="flex flex-wrap justify-end gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    disabled={busy}
                    data-testid="org-area-edit-cancel"
                    onClick={() => {
                      setName(area.name);
                      setCode(area.code ?? "");
                      setEditing(false);
                    }}
                  >
                    {t("areas.cancel")}
                  </Button>
                  <Button
                    type="button"
                    disabled={busy || !name.trim()}
                    data-testid="org-area-save"
                    onClick={() => renameMutation.mutate()}
                  >
                    {renameMutation.isPending ? t("areas.saving") : t("areas.save")}
                  </Button>
                </div>
              </>
            )}
          </section>

          <section className="catalog-form-section exits-animate-panel gap-3" data-testid="org-area-branches">
            <h2 className="catalog-form-section__title">{t("areas.detail.assigned")}</h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {inArea.length === 1
                ? t("workspace.locationCountOne")
                : t("areas.locationCount").replace("{count}", String(inArea.length))}
            </p>
            {inArea.length === 0 ? (
              <p className="m-0 text-muted" data-testid="org-area-no-branches">
                {t("areas.detail.noLocations")}
              </p>
            ) : (
              <ul className="m-0 grid list-none gap-2 p-0" data-testid="org-area-branch-list">
                {inArea.map((branch) => {
                  const warehouse = isWarehouseBranch(branch.branchType);
                  return (
                    <li
                      key={branch.id}
                      className="exits-entity-card min-w-0"
                      data-testid={`org-area-assigned-${branch.id}`}
                    >
                      <div className="exits-entity-card__header">
                        <div className="exits-entity-card__identity min-w-0">
                          <h3 className="exits-entity-card__title m-0 truncate">{branch.name}</h3>
                        </div>
                        <div className="exits-entity-card__badges">
                          <StatusChip tone={warehouse ? "warning" : "info"}>
                            {warehouse ? t("branches.type.warehouse") : t("branches.type.retail")}
                          </StatusChip>
                          {!warehouse && branch.isPrimary ? (
                            <StatusChip tone="info">{t("branches.mgmt.primary")}</StatusChip>
                          ) : null}
                        </div>
                      </div>
                      <div className="exits-entity-card__actions">
                        <Button
                          type="button"
                          variant="outline"
                          disabled={busy}
                          data-testid={`org-area-remove-${branch.id}`}
                          onClick={() => setPendingAction({ kind: "remove", branch })}
                        >
                          <X className="size-4 shrink-0" aria-hidden />
                          {t("areas.detail.remove")}
                        </Button>
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </section>

          {area.status === "Active" && outsideArea.length > 0 ? (
            <section
              className="catalog-form-section exits-animate-panel gap-3"
              data-testid="org-area-available"
            >
              <h2 className="catalog-form-section__title">{t("areas.detail.available")}</h2>
              <ul className="m-0 grid list-none gap-2 p-0" data-testid="org-area-available-list">
                {outsideArea.map((branch) => {
                  const warehouse = isWarehouseBranch(branch.branchType);
                  const transfer = Boolean(branch.areaId);
                  return (
                    <li
                      key={branch.id}
                      className="exits-entity-card min-w-0"
                      data-testid={`org-area-available-${branch.id}`}
                    >
                      <div className="exits-entity-card__header">
                        <div className="exits-entity-card__identity min-w-0">
                          <h3 className="exits-entity-card__title m-0 truncate">{branch.name}</h3>
                          {branch.areaName ? (
                            <p className="exits-entity-card__subtitle m-0">{branch.areaName}</p>
                          ) : null}
                        </div>
                        <div className="exits-entity-card__badges">
                          <StatusChip tone={warehouse ? "warning" : "info"}>
                            {warehouse ? t("branches.type.warehouse") : t("branches.type.retail")}
                          </StatusChip>
                          {!warehouse && branch.isPrimary ? (
                            <StatusChip tone="info">{t("branches.mgmt.primary")}</StatusChip>
                          ) : null}
                        </div>
                      </div>
                      <div className="exits-entity-card__actions">
                        <Button
                          type="button"
                          variant="outline"
                          disabled={busy}
                          data-testid={
                            transfer
                              ? `org-area-transfer-${branch.id}`
                              : `org-area-add-${branch.id}`
                          }
                          onClick={() =>
                            setPendingAction({
                              kind: transfer ? "transfer" : "assign",
                              branch,
                            })
                          }
                        >
                          {transfer ? (
                            <ArrowRightLeft className="size-4 shrink-0" aria-hidden />
                          ) : (
                            <Plus className="size-4 shrink-0" aria-hidden />
                          )}
                          {transfer ? t("areas.detail.transfer") : t("areas.detail.assign")}
                        </Button>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </section>
          ) : null}

          {area.status === "Active" ? (
            <div className="exits-animate-toolbar">
              <Button
                type="button"
                variant="outline"
                disabled={busy}
                data-testid="org-area-archive"
                onClick={() => setArchiveOpen(true)}
              >
                {t("areas.archive")}
              </Button>
            </div>
          ) : null}
        </>
      ) : null}

      {actionError ? (
        <div className="exits-alert exits-alert--error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{actionError}</p>
        </div>
      ) : null}

      <ConfirmationDialog
        open={archiveOpen}
        title={t("areas.archiveConfirmTitle")}
        detail={t("areas.archiveConfirmDetail")}
        confirmLabel={t("areas.archiveConfirmAction")}
        cancelLabel={t("areas.cancel")}
        onCancel={() => setArchiveOpen(false)}
        onConfirm={() => archiveMutation.mutate()}
        busy={archiveMutation.isPending}
        testId="org-area-archive-confirm"
      />

      {pendingConfirm ? (
        <ConfirmationDialog
          open
          title={pendingConfirm.title}
          detail={pendingConfirm.detail}
          confirmLabel={pendingConfirm.confirmLabel}
          cancelLabel={t("areas.cancel")}
          onCancel={() => setPendingAction(null)}
          onConfirm={confirmPendingAction}
          busy={branchMutation.isPending}
          testId={pendingConfirm.testId}
        />
      ) : null}
    </div>
  );
}
