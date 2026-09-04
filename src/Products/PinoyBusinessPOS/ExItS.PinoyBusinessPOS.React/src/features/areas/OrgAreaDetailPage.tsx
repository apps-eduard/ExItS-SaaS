import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canInviteOrganizationStaff } from "@/access/pos-capabilities";
import {
  archiveOrganizationArea,
  listOrganizationAreas,
  setBranchArea,
  updateOrganizationArea,
} from "@/api/platform/organization-areas-client";
import { listBranchManagementSummaries } from "@/api/platform/organization-branches-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

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
  const [archiveOpen, setArchiveOpen] = useState(false);
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
      setActionError(null);
      await refresh();
    },
    onError: (error: Error) => setActionError(error.message),
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
            <h2 className="catalog-form-section__title">{t("areas.rename")}</h2>
            <label className="flex flex-col gap-1">
              <span className="text-[length:var(--exits-text-sm)]">{t("areas.create.name")}</span>
              <input
                className="exits-input"
                value={name}
                maxLength={100}
                disabled={busy || area.status !== "Active"}
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
                disabled={busy || area.status !== "Active"}
                data-testid="org-area-code"
                onChange={(event) => setCode(event.target.value)}
              />
            </label>
            <Button
              type="button"
              className="w-full sm:w-auto"
              disabled={busy || area.status !== "Active" || !name.trim()}
              data-testid="org-area-save"
              onClick={() => renameMutation.mutate()}
            >
              {renameMutation.isPending ? t("areas.saving") : t("areas.save")}
            </Button>
          </section>

          <section className="catalog-form-section exits-animate-panel gap-3" data-testid="org-area-branches">
            <h2 className="catalog-form-section__title">{t("areas.detail.branches")}</h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("areas.groupingOnlyNote")}
            </p>
            {inArea.length === 0 ? (
              <p className="m-0 text-muted" data-testid="org-area-no-branches">
                {t("areas.detail.noBranches")}
              </p>
            ) : (
              <ul className="m-0 grid list-none gap-2 p-0" data-testid="org-area-branch-list">
                {inArea.map((branch) => (
                  <li key={branch.id} className="flex items-center justify-between gap-2">
                    <span className="min-w-0 truncate">
                      {branch.name}
                      {branch.code ? ` · ${branch.code}` : ""}
                    </span>
                    <Button
                      type="button"
                      variant="outline"
                      disabled={busy}
                      data-testid={`org-area-remove-${branch.id}`}
                      onClick={() => branchMutation.mutate({ branchId: branch.id, target: null })}
                    >
                      {t("areas.detail.remove")}
                    </Button>
                  </li>
                ))}
              </ul>
            )}

            {area.status === "Active" && outsideArea.length > 0 ? (
              <>
                <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("areas.detail.availableBranches")}
                </h3>
                <ul className="m-0 grid list-none gap-2 p-0" data-testid="org-area-available-list">
                  {outsideArea.map((branch) => (
                    <li key={branch.id} className="flex items-center justify-between gap-2">
                      <span className="min-w-0 truncate">
                        {branch.name}
                        {branch.areaName ? ` · ${branch.areaName}` : ""}
                      </span>
                      <Button
                        type="button"
                        variant="outline"
                        disabled={busy}
                        data-testid={`org-area-add-${branch.id}`}
                        onClick={() =>
                          branchMutation.mutate({ branchId: branch.id, target: areaId ?? null })
                        }
                      >
                        {branch.areaId ? t("areas.detail.move") : t("areas.detail.assign")}
                      </Button>
                    </li>
                  ))}
                </ul>
              </>
            ) : null}
          </section>

          {area.status === "Active" ? (
            <div className="exits-animate-toolbar">
              <Button
                type="button"
                variant="outline"
                className="w-full sm:w-auto"
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
        testId="org-area-archive-confirm"
      />
    </div>
  );
}
