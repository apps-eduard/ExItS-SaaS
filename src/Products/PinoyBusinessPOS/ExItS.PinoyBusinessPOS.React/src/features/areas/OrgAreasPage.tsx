import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { canInviteOrganizationStaff, canManageStoreAreas } from "@/access/pos-capabilities";
import {
  createOrganizationArea,
  listOrganizationAreas,
} from "@/api/platform/organization-areas-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

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
  const activeAreas = useMemo(
    () => (data?.areas ?? []).filter((area) => area.status === "Active"),
    [data],
  );
  const archivedAreas = useMemo(
    () => (data?.areas ?? []).filter((area) => area.status === "Archived"),
    [data],
  );
  const atLimit = data != null && data.maxAreas > 0 && data.activeAreaCount >= data.maxAreas;

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
      />

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="org-areas-note">
        {t("areas.groupingOnlyNote")}
      </p>

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

      <div className="branch-mgmt-toolbar">
        <Button
          type="button"
          className="min-h-11"
          disabled={atLimit || createMutation.isPending}
          data-testid="org-areas-add"
          onClick={() => {
            setFormOpen((open) => !open);
            setSubmitError(null);
          }}
        >
          <Plus className="size-4" aria-hidden />
          {t("areas.add")}
        </Button>
      </div>

      {formOpen ? (
        <section className="catalog-form-section exits-animate-panel gap-3" data-testid="org-areas-form">
          <h2 className="catalog-form-section__title">{t("areas.create.title")}</h2>
          <label className="flex flex-col gap-1">
            <span className="text-[length:var(--exits-text-sm)]">{t("areas.create.name")}</span>
            <input
              className="exits-input min-h-11"
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
            <span className="text-[length:var(--exits-text-sm)]">{t("areas.create.code")}</span>
            <input
              className="exits-input min-h-11"
              value={code}
              maxLength={32}
              data-testid="org-areas-code"
              onChange={(event) => {
                setCode(event.target.value);
                setSubmitError(null);
              }}
            />
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {t("areas.create.codeHelper")}
            </span>
          </label>
          <Button
            type="button"
            className="min-h-11 w-full sm:w-auto"
            disabled={createMutation.isPending || !name.trim()}
            data-testid="org-areas-submit"
            onClick={() => createMutation.mutate()}
          >
            {createMutation.isPending ? t("areas.saving") : t("areas.create.submit")}
          </Button>
        </section>
      ) : null}

      {submitError ? (
        <div className="exits-alert exits-alert--error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{submitError}</p>
        </div>
      ) : null}

      {areasQuery.isLoading ? <LoadingSkeleton count={3} label={t("loading.label")} /> : null}

      {areasQuery.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            areasQuery.error instanceof Error ? areasQuery.error.message : t("areas.loadError")
          }
        />
      ) : null}

      {areasQuery.isSuccess && data ? (
        <>
          {data.areas.length === 0 ? (
            <EmptyState title={t("areas.emptyTitle")} detail={t("areas.emptyDetail")} />
          ) : (
            <ul className="m-0 grid list-none gap-2 p-0" data-testid="org-areas-items">
              {[...activeAreas, ...archivedAreas].map((area) => (
                <li key={area.id}>
                  <article
                    className="exits-list__card min-w-0"
                    data-testid={`org-area-card-${area.id}`}
                  >
                    <div className="branch-mgmt-card__header">
                      <div className="min-w-0">
                        <h2 className="branch-mgmt-card__title m-0 truncate">{area.name}</h2>
                        {area.code ? (
                          <p className="branch-mgmt-card__code m-0 mt-1 text-muted">{area.code}</p>
                        ) : null}
                      </div>
                      <StatusChip tone={area.status === "Active" ? "success" : "info"}>
                        {area.status === "Active"
                          ? t("areas.status.active")
                          : t("areas.status.archived")}
                      </StatusChip>
                    </div>
                    <p
                      className="m-0 text-muted"
                      data-testid={`org-area-branch-count-${area.id}`}
                    >
                      {t("areas.branchCount").replace("{count}", String(area.branchCount))}
                    </p>
                    <div className="branch-mgmt-card__actions">
                      <Button
                        asChild
                        variant="outline"
                        className="min-h-11"
                        data-testid={`org-area-open-${area.id}`}
                      >
                        <Link to={`/org/areas/${area.id}`}>{t("areas.open")}</Link>
                      </Button>
                    </div>
                  </article>
                </li>
              ))}
            </ul>
          )}

          <p className="m-0 text-muted" data-testid="org-areas-unassigned">
            {t("areas.unassignedCount").replace(
              "{count}",
              String(data.unassignedBranchCount),
            )}
          </p>
        </>
      ) : null}
    </div>
  );
}
