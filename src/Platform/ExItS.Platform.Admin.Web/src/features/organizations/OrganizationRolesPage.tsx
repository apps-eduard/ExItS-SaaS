import { useMemo, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import type { OrganizationRolesUrlState } from "@/api/organizations/organization-roles-client";
import { ORGANIZATION_ROLES_PAGE_SIZE } from "@/api/organizations/organization-types";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { Alert } from "@/components/ui/alert";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { organizationMutationFailureCopy } from "@/features/organizations/organization-mutation-feedback";
import {
  useActivateOrganizationRoleMutation,
  useCreateOrganizationRoleMutation,
  useDeactivateOrganizationRoleMutation,
  useRetireOrganizationRoleMutation,
} from "@/features/organizations/use-organization-mutations";
import {
  useOrganizationPermissionCatalogQuery,
  useOrganizationRolesQuery,
} from "@/features/organizations/use-organization-workspace-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const LIFECYCLE_COPY: Record<
  "activate" | "deactivate" | "retire",
  { title: MessageKey; description: MessageKey; confirm: MessageKey }
> = {
  activate: {
    title: "organization.roles.activate.title",
    description: "organization.roles.activate.description",
    confirm: "organization.roles.activate.confirm",
  },
  deactivate: {
    title: "organization.roles.deactivate.title",
    description: "organization.roles.deactivate.description",
    confirm: "organization.roles.deactivate.confirm",
  },
  retire: {
    title: "organization.roles.retire.title",
    description: "organization.roles.retire.description",
    confirm: "organization.roles.retire.confirm",
  },
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function parseRolesSearchParams(searchParams: URLSearchParams): OrganizationRolesUrlState {
  const page = Number.parseInt(searchParams.get("page") ?? "1", 10);
  return {
    page: Number.isFinite(page) && page > 0 ? page : 1,
    status: searchParams.get("status") ?? "",
    search: searchParams.get("search") ?? "",
  };
}

function rolesSearchParams(state: OrganizationRolesUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  if (state.status) {
    params.set("status", state.status);
  }
  if (state.search) {
    params.set("search", state.search);
  }
  return params;
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

export function OrganizationRolesPage() {
  const { t } = usePreferences();
  const params = useParams();
  const authorization = useAuthorization();
  const organizationId = parseOrganizationId(params.organizationId);
  const canAccess = authorization.hasPermission(PLATFORM_PERMISSIONS.manageMemberships);
  const canManage = canAccess;
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseRolesSearchParams(searchParams), [searchParams]);
  const query = useOrganizationRolesQuery(organizationId, state);
  const catalogQuery = useOrganizationPermissionCatalogQuery(canManage);
  const createMutation = useCreateOrganizationRoleMutation();
  const activateMutation = useActivateOrganizationRoleMutation();
  const deactivateMutation = useDeactivateOrganizationRoleMutation();
  const retireMutation = useRetireOrganizationRoleMutation();
  const [showCreate, setShowCreate] = useState(false);
  const [newCode, setNewCode] = useState("");
  const [newName, setNewName] = useState("");
  const [newPermissions, setNewPermissions] = useState<string[]>([]);
  const [lifecycleTarget, setLifecycleTarget] = useState<{
    id: string;
    action: "activate" | "deactivate" | "retire";
    version?: number;
  } | null>(null);
  const [formError, setFormError] = useState<{ title: string; detail: string } | null>(null);

  if (!organizationId) {
    return null;
  }

  if (!canAccess) {
    return (
      <section className="grid max-w-3xl gap-4">
        <PageHeader title={t("organization.roles.title")} description={t("organization.roles.description")} />
        <Alert title={t("organization.roles.unauthorized.title")}>
          {t("organization.roles.unauthorized.body")}
        </Alert>
      </section>
    );
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load organization roles" })
    : null;
  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ORGANIZATION_ROLES_PAGE_SIZE))
    : 1;
  const lifecyclePending =
    activateMutation.isPending || deactivateMutation.isPending || retireMutation.isPending;

  function replaceState(patch: Partial<OrganizationRolesUrlState>) {
    const current = parseRolesSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(rolesSearchParams({ ...current, ...patch }), { replace: true });
  }

  async function createRole() {
    if (!organizationId || createMutation.isPending) {
      return;
    }
    setFormError(null);
    try {
      await createMutation.mutateAsync({
        organizationId,
        body: { code: newCode.trim(), name: newName.trim(), permissions: newPermissions },
      });
      setShowCreate(false);
      setNewCode("");
      setNewName("");
      setNewPermissions([]);
    } catch (error) {
      setFormError(organizationMutationFailureCopy(error, t));
    }
  }

  async function runLifecycle() {
    if (!organizationId || !lifecycleTarget || lifecyclePending) {
      return;
    }
    const body = { expectedVersion: lifecycleTarget.version ?? null };
    if (lifecycleTarget.action === "activate") {
      await activateMutation.mutateAsync({ organizationId, roleId: lifecycleTarget.id, body });
    } else if (lifecycleTarget.action === "deactivate") {
      await deactivateMutation.mutateAsync({ organizationId, roleId: lifecycleTarget.id, body });
    } else {
      await retireMutation.mutateAsync({ organizationId, roleId: lifecycleTarget.id, body });
    }
    setLifecycleTarget(null);
  }

  return (
    <section className="grid max-w-4xl gap-4">
      <PageHeader
        title={t("organization.roles.title")}
        description={t("organization.roles.description")}
        actions={
          canManage ? (
            <Button type="button" size="sm" onClick={() => setShowCreate((current) => !current)}>
              {showCreate ? t("organization.admin.dialog.dismiss") : t("organization.roles.create.action")}
            </Button>
          ) : undefined
        }
      />
      {formError ? (
        <Alert title={formError.title} tone="danger">
          {formError.detail}
        </Alert>
      ) : null}
      {showCreate && canManage ? (
        <div className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="grid gap-1">
              <Label htmlFor="role-code">{t("organization.roles.create.code")}</Label>
              <Input id="role-code" value={newCode} onChange={(event) => setNewCode(event.target.value)} />
            </div>
            <div className="grid gap-1">
              <Label htmlFor="role-name">{t("organization.roles.create.name")}</Label>
              <Input id="role-name" value={newName} onChange={(event) => setNewName(event.target.value)} />
            </div>
          </div>
          <fieldset className="grid gap-2">
            <legend className="text-[length:var(--exits-text-xs)] font-medium text-muted">
              {t("organization.roles.create.permissions")}
            </legend>
            <div className="grid gap-1 sm:grid-cols-2">
              {(catalogQuery.data ?? []).map((permission) => (
                <label key={permission.code} className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                  <input
                    type="checkbox"
                    checked={newPermissions.includes(permission.code)}
                    onChange={(event) => {
                      setNewPermissions((current) =>
                        event.target.checked
                          ? [...current, permission.code]
                          : current.filter((code) => code !== permission.code),
                      );
                    }}
                  />
                  {permission.code}
                </label>
              ))}
            </div>
          </fieldset>
          <Button type="button" size="sm" disabled={createMutation.isPending} onClick={() => void createRole()}>
            {t("organization.roles.create.confirm")}
          </Button>
        </div>
      ) : null}
      <label className="grid max-w-xs gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("organization.people.status")}
        <select
          className={controlClass}
          value={state.status}
          onChange={(event) => replaceState({ status: event.target.value, page: 1 })}
        >
          <option value="">{t("organization.people.status.all")}</option>
          <option value="Active">{t("organization.roles.status.Active")}</option>
          <option value="Inactive">{t("organization.roles.status.Inactive")}</option>
          <option value="Retired">{t("organization.roles.status.Retired")}</option>
        </select>
      </label>
      {query.isPending ? <DashboardWidgetSkeleton rows={5} /> : null}
      {query.isError && isForbidden(query.error) ? (
        <Alert title={t("organization.roles.unauthorized.title")}>{t("organization.people.unavailable")}</Alert>
      ) : null}
      {query.isError && !isForbidden(query.error) && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.roles.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}
      {query.data ? (
        <>
          <AdminTable
            caption={t("organization.roles.caption")}
            empty={t("organization.roles.empty")}
            columns={[
              {
                id: "code",
                header: t("organization.roles.column.code"),
                cell: (role) => <span className="font-mono text-[length:var(--exits-text-xs)]">{role.code}</span>,
              },
              { id: "name", header: t("organization.roles.column.name"), cell: (role) => role.name },
              {
                id: "status",
                header: t("organization.people.column.status"),
                cell: (role) => (
                  <StatusIndicator
                    tone={
                      role.status === "Active" ? "success" : role.status === "Inactive" ? "warning" : "neutral"
                    }
                    label={role.status}
                  />
                ),
              },
              {
                id: "actions",
                header: t("organization.productAccess.column.actions"),
                cell: (role) =>
                  canManage ? (
                    <div className="flex flex-wrap gap-1">
                      {role.status !== "Active" ? (
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          onClick={() =>
                            setLifecycleTarget({ id: role.id, action: "activate", version: role.version })
                          }
                        >
                          {t("organization.roles.activate")}
                        </Button>
                      ) : null}
                      {role.status === "Active" ? (
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          onClick={() =>
                            setLifecycleTarget({ id: role.id, action: "deactivate", version: role.version })
                          }
                        >
                          {t("organization.roles.deactivate")}
                        </Button>
                      ) : null}
                      {role.status !== "Retired" ? (
                        <Button
                          type="button"
                          size="sm"
                          variant="destructive"
                          onClick={() =>
                            setLifecycleTarget({ id: role.id, action: "retire", version: role.version })
                          }
                        >
                          {t("organization.roles.retire")}
                        </Button>
                      ) : null}
                    </div>
                  ) : (
                    "—"
                  ),
              },
            ]}
            rows={query.data.items}
          />
          <div className="flex flex-wrap items-center gap-2">
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={state.page <= 1}
              onClick={() => replaceState({ page: state.page - 1 })}
            >
              {t("organizations.previous")}
            </Button>
            <p className="text-[length:var(--exits-text-xs)] text-muted">
              {t("organizations.page")} {state.page} / {totalPages}
            </p>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={state.page >= totalPages}
              onClick={() => replaceState({ page: state.page + 1 })}
            >
              {t("organizations.next")}
            </Button>
          </div>
        </>
      ) : null}
      {lifecycleTarget ? (
        <ConfirmActionDialog
          open
          title={t(LIFECYCLE_COPY[lifecycleTarget.action].title)}
          description={t(LIFECYCLE_COPY[lifecycleTarget.action].description)}
          confirmLabel={t(LIFECYCLE_COPY[lifecycleTarget.action].confirm)}
          cancelLabel={t("organization.admin.dialog.dismiss")}
          pendingLabel={t("organization.admin.submitting")}
          destructive={lifecycleTarget.action === "retire"}
          pending={lifecyclePending}
          onCancel={() => setLifecycleTarget(null)}
          onConfirm={() => void runLifecycle()}
        />
      ) : null}
    </section>
  );
}
