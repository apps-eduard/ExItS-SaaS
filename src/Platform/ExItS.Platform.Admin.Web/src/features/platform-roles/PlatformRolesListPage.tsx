import { Button } from "@/components/ui/button";
import { ForbiddenState } from "@/features/overview/ForbiddenState";
import { PermissionChecklist } from "@/features/platform-roles/PermissionChecklist";
import {
  usePlatformPermissionsQuery,
  usePlatformRolesListQuery,
} from "@/features/platform-roles/use-platform-roles-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import { createPlatformRoleDefinition } from "@/api/platform-roles/platform-roles-client";
import {
  hasActivePlatformRolesFilters,
  parsePlatformRolesSearchParams,
  platformRolesSearchParams,
  type PlatformRolesUrlState,
} from "@/api/platform-roles/platform-roles-query";
import { PLATFORM_ROLE_PAGE_SIZE } from "@/api/platform-roles/platform-roles-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Alert } from "@/components/ui/alert";
import { useMemo, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";

function formatInstant(value: string | undefined, language: string): string {
  if (!value) {
    return "—";
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-US", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC",
  }).format(date);
}

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Inactive") {
    return "warning";
  }
  if (status === "Retired") {
    return "danger";
  }
  return "neutral";
}

function CreateRoleForm({
  permissionOptions,
  onCreated,
}: {
  permissionOptions: { code: string; description: string }[];
  onCreated: (roleId: string) => void;
}) {
  const { t } = usePreferences();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [permissions, setPermissions] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{ title: string; detail?: string; conflict?: boolean } | null>(
    null,
  );

  async function handleCreate() {
    if (busy) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const created = await createPlatformRoleDefinition(env.platformApiBaseUrl, {
        code,
        name,
        description: description.trim() ? description : null,
        permissions,
      });
      onCreated(created.id);
    } catch (err) {
      const conflict = err instanceof PlatformApiError && err.status === 409;
      setError({
        title: conflict ? t("platformRoles.create.conflict") : t("platformRoles.create.failed"),
        detail:
          err instanceof PlatformApiError
            ? (err.problem.detail ?? err.message)
            : err instanceof Error
              ? err.message
              : undefined,
        conflict,
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <div
      className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4"
      data-testid="platform-roles-create-form"
    >
      <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
        {t("platformRoles.create.title")}
      </h2>
      {error ? (
        <Alert
          title={error.title}
          tone="danger"
          data-testid={error.conflict ? "platform-roles-create-conflict" : "platform-roles-create-error"}
        >
          {error.detail}
        </Alert>
      ) : null}
      <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="platform-role-new-code">
        {t("platformRoles.field.code")}
        <input
          id="platform-role-new-code"
          data-testid="platform-role-new-code"
          className="h-9 rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
          value={code}
          disabled={busy}
          onChange={(event) => setCode(event.target.value)}
        />
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="platform-role-new-name">
        {t("platformRoles.field.name")}
        <input
          id="platform-role-new-name"
          data-testid="platform-role-new-name"
          className="h-9 rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
          value={name}
          disabled={busy}
          onChange={(event) => setName(event.target.value)}
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-sm)]"
        htmlFor="platform-role-new-description"
      >
        {t("platformRoles.field.description")}
        <input
          id="platform-role-new-description"
          data-testid="platform-role-new-description"
          className="h-9 rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
          value={description}
          disabled={busy}
          onChange={(event) => setDescription(event.target.value)}
        />
      </label>
      <PermissionChecklist
        options={permissionOptions}
        value={permissions}
        disabled={busy}
        onChange={setPermissions}
      />
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          size="sm"
          disabled={busy}
          data-testid="platform-role-create-submit"
          onClick={() => void handleCreate()}
        >
          {busy ? t("platformRoles.create.creating") : t("platformRoles.create.submit")}
        </Button>
      </div>
    </div>
  );
}

export function PlatformRolesListPage() {
  const { t, language, theme, density } = usePreferences();
  const authorization = useAuthorization();
  const navigate = useNavigate();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parsePlatformRolesSearchParams(searchParams), [searchParams]);
  const [draftSearch, setDraftSearch] = useState(state.search);
  const [draftKind, setDraftKind] = useState(state.kind);
  const [draftStatus, setDraftStatus] = useState(state.status);
  const [showCreate, setShowCreate] = useState(false);

  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.managePlatformUsers);

  const listQuery = usePlatformRolesListQuery(canManage, state);
  const permissionsQuery = usePlatformPermissionsQuery(canManage && showCreate);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true" className="grid gap-4">
        <DashboardWidgetSkeleton />
      </section>
    );
  }

  if (!canManage) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.managePlatformUsers} />;
  }

  if (
    listQuery.error instanceof PlatformApiError &&
    (listQuery.error.status === 401 || listQuery.error.status === 403)
  ) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.managePlatformUsers} />;
  }

  function replaceState(patch: Partial<PlatformRolesUrlState>) {
    const current = parsePlatformRolesSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(platformRolesSearchParams({ ...current, ...patch }), { replace: true });
  }

  const items = listQuery.data?.items ?? [];
  const totalCount = listQuery.data?.totalCount ?? 0;
  const page = listQuery.data?.page ?? state.page;
  const pageSize = listQuery.data?.pageSize ?? PLATFORM_ROLE_PAGE_SIZE;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const emptyMessage = hasActivePlatformRolesFilters(state)
    ? t("platformRoles.zeroResult")
    : t("platformRoles.empty");

  return (
    <section className="grid gap-4" data-testid="platform-roles-list-page">
      <PageHeader
        title={t("platformRoles.title")}
        description={t("platformRoles.description")}
        actions={
          <Button
            type="button"
            size="sm"
            variant={showCreate ? "secondary" : "default"}
            data-testid="platform-roles-toggle-create"
            onClick={() => setShowCreate((value) => !value)}
          >
            {showCreate ? t("platformRoles.create.hide") : t("platformRoles.create.show")}
          </Button>
        }
      />

      <form
        className="flex flex-wrap items-end gap-2"
        data-testid="platform-roles-filters"
        onSubmit={(event) => {
          event.preventDefault();
          replaceState({
            search: draftSearch.trim(),
            kind: draftKind,
            status: draftStatus,
            page: 1,
          });
        }}
      >
        <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="platform-roles-search">
          {t("platformRoles.filter.search")}
          <input
            id="platform-roles-search"
            className="h-9 min-w-[12rem] rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
            value={draftSearch}
            placeholder={t("platformRoles.filter.searchPlaceholder")}
            onChange={(event) => setDraftSearch(event.target.value)}
          />
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="platform-roles-kind">
          {t("platformRoles.filter.kind")}
          <select
            id="platform-roles-kind"
            className="h-9 min-w-[8rem] rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
            value={draftKind}
            onChange={(event) =>
              setDraftKind(event.target.value as PlatformRolesUrlState["kind"])
            }
          >
            <option value="">{t("platformRoles.filter.kind.all")}</option>
            <option value="BuiltIn">{t("platformRoles.kind.BuiltIn")}</option>
            <option value="Custom">{t("platformRoles.kind.Custom")}</option>
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="platform-roles-status">
          {t("platformRoles.filter.status")}
          <select
            id="platform-roles-status"
            className="h-9 min-w-[8rem] rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
            value={draftStatus}
            onChange={(event) =>
              setDraftStatus(event.target.value as PlatformRolesUrlState["status"])
            }
          >
            <option value="">{t("platformRoles.filter.status.all")}</option>
            <option value="Active">{t("platformRoles.status.Active")}</option>
            <option value="Inactive">{t("platformRoles.status.Inactive")}</option>
            <option value="Retired">{t("platformRoles.status.Retired")}</option>
          </select>
        </label>
        <Button type="submit" size="sm">
          {t("platformRoles.filter.apply")}
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => {
            setDraftSearch("");
            setDraftKind("");
            setDraftStatus("");
            replaceState({ search: "", kind: "", status: "", page: 1 });
          }}
        >
          {t("platformRoles.filter.reset")}
        </Button>
      </form>

      {showCreate ? (
        permissionsQuery.isError ? (
          <ErrorState
            diagnostic={normalizeDiagnosticError({
              error: permissionsQuery.error,
              operation: "Load platform permissions catalog",
              environment: { locale: language, theme, density },
            })}
            description={t("platformRoles.permissions.error")}
            onRetry={() => void permissionsQuery.refetch()}
          />
        ) : (
          <CreateRoleForm
            permissionOptions={(permissionsQuery.data ?? []).map((item) => ({
              code: item.code,
              description: item.description,
            }))}
            onCreated={(roleId) => {
              navigate(`/admin/platform-roles/${roleId}`);
            }}
          />
        )
      ) : null}

      {listQuery.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("platformRoles.loading")}>
          <DashboardWidgetSkeleton />
        </div>
      ) : null}

      {listQuery.isError ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: listQuery.error,
            operation: "Load platform role definitions",
            environment: { locale: language, theme, density },
          })}
          description={t("platformRoles.error")}
          onRetry={() => void listQuery.refetch()}
        />
      ) : null}

      {listQuery.data ? (
        <>
          {showTable ? (
            <AdminTable
              caption={t("platformRoles.caption")}
              empty={emptyMessage}
              rows={items}
              columns={[
                {
                  id: "code",
                  header: t("platformRoles.column.code"),
                  cell: (row) => (
                    <Link
                      className="font-mono text-[length:var(--exits-text-xs)] text-primary underline-offset-4 hover:underline"
                      to={`/admin/platform-roles/${row.id}`}
                    >
                      {row.code}
                    </Link>
                  ),
                },
                {
                  id: "name",
                  header: t("platformRoles.column.name"),
                  cell: (row) => row.name,
                },
                {
                  id: "kind",
                  header: t("platformRoles.column.kind"),
                  cell: (row) => row.kind,
                },
                {
                  id: "status",
                  header: t("platformRoles.column.status"),
                  cell: (row) => (
                    <StatusIndicator label={row.status} tone={statusTone(row.status)} />
                  ),
                },
                {
                  id: "updated",
                  header: t("platformRoles.column.updated"),
                  cell: (row) => formatInstant(row.updatedAtUtc, language),
                },
              ]}
            />
          ) : (
            <ul className="grid gap-3">
              {items.length === 0 ? (
                <li className="text-[length:var(--exits-text-sm)] text-muted">{emptyMessage}</li>
              ) : (
                items.map((row) => (
                  <li
                    key={row.id}
                    className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-3"
                  >
                    <Link
                      className="font-semibold text-primary underline-offset-4 hover:underline"
                      to={`/admin/platform-roles/${row.id}`}
                    >
                      {row.code}
                    </Link>
                    <p className="mt-1 text-[length:var(--exits-text-sm)]">{row.name}</p>
                    <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
                      {row.kind} · {row.status}
                    </p>
                  </li>
                ))
              )}
            </ul>
          )}

          <div className="flex flex-wrap items-center gap-2">
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={page <= 1}
              onClick={() => replaceState({ page: page - 1 })}
            >
              {t("platformRoles.previous")}
            </Button>
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {t("platformRoles.page")} {page} / {totalPages}
            </span>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={page >= totalPages}
              onClick={() => replaceState({ page: page + 1 })}
            >
              {t("platformRoles.next")}
            </Button>
          </div>
        </>
      ) : null}
    </section>
  );
}
