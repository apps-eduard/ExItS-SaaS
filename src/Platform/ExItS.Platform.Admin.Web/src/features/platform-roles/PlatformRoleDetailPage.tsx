import { useQueryClient } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import {
  activatePlatformRoleDefinition,
  deactivatePlatformRoleDefinition,
  retirePlatformRoleDefinition,
  updatePlatformRoleDefinition,
} from "@/api/platform-roles/platform-roles-client";
import { parsePlatformRoleId } from "@/api/platform-roles/platform-roles-query";
import type { PlatformRoleDefinition } from "@/api/platform-roles/platform-roles-types";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { PermissionChecklist } from "@/features/platform-roles/PermissionChecklist";
import {
  platformRoleDetailQueryKey,
  usePlatformPermissionsQuery,
  usePlatformRoleDetailQuery,
} from "@/features/platform-roles/use-platform-roles-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

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

function DetailField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="grid gap-0.5">
      <dt className="text-[length:var(--exits-text-xs)] text-muted">{label}</dt>
      <dd className="break-words text-[length:var(--exits-text-sm)]">{children}</dd>
    </div>
  );
}

function CustomRoleEditor({
  role,
  permissionOptions,
  onUpdated,
}: {
  role: PlatformRoleDefinition;
  permissionOptions: { code: string; description: string }[];
  onUpdated: (updated: PlatformRoleDefinition) => void;
}) {
  const { t } = usePreferences();
  const [name, setName] = useState(role.name);
  const [description, setDescription] = useState(role.description ?? "");
  const [permissions, setPermissions] = useState<string[]>([...role.permissions]);
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState<{
    tone: "success" | "danger";
    title: string;
    detail?: string;
  } | null>(null);

  async function runMutation(
    action: () => Promise<PlatformRoleDefinition>,
    successTitle: string,
  ) {
    if (busy) {
      return;
    }
    setBusy(true);
    setFeedback(null);
    try {
      const updated = await action();
      onUpdated(updated);
      setName(updated.name);
      setDescription(updated.description ?? "");
      setPermissions([...updated.permissions]);
      setFeedback({ tone: "success", title: successTitle });
    } catch (error) {
      setFeedback({
        tone: "danger",
        title: t("platformRoles.mutation.failed"),
        detail:
          error instanceof PlatformApiError
            ? (error.problem.detail ?? error.message)
            : error instanceof Error
              ? error.message
              : undefined,
      });
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="grid gap-3" data-testid="platform-role-manage">
      <div className="flex flex-wrap gap-2">
        <label className="grid gap-1 text-[length:var(--exits-text-sm)]" htmlFor="platform-role-edit-name">
          {t("platformRoles.field.name")}
          <input
            id="platform-role-edit-name"
            data-testid="platform-role-edit-name"
            className="h-9 min-w-[12rem] rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
            value={name}
            disabled={busy}
            onChange={(event) => setName(event.target.value)}
          />
        </label>
        <label
          className="grid min-w-[14rem] flex-1 gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="platform-role-edit-description"
        >
          {t("platformRoles.field.description")}
          <input
            id="platform-role-edit-description"
            data-testid="platform-role-edit-description"
            className="h-9 w-full rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
            value={description}
            disabled={busy}
            onChange={(event) => setDescription(event.target.value)}
          />
        </label>
      </div>

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
          data-testid="platform-role-save"
          onClick={() =>
            void runMutation(
              () =>
                updatePlatformRoleDefinition(env.platformApiBaseUrl, role.id, {
                  name,
                  description: description.trim() ? description : null,
                  permissions,
                  expectedVersion: role.version,
                }),
              t("platformRoles.mutation.saved"),
            )
          }
        >
          {busy ? t("platformRoles.mutation.saving") : t("platformRoles.mutation.save")}
        </Button>

        {role.status === "Inactive" ? (
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={busy}
            data-testid="platform-role-activate"
            onClick={() => {
              if (!window.confirm(t("platformRoles.confirm.activate"))) {
                return;
              }
              void runMutation(
                () =>
                  activatePlatformRoleDefinition(env.platformApiBaseUrl, role.id, {
                    expectedVersion: role.version,
                  }),
                t("platformRoles.mutation.updated"),
              );
            }}
          >
            {t("platformRoles.lifecycle.activate")}
          </Button>
        ) : null}

        {role.status === "Active" ? (
          <Button
            type="button"
            size="sm"
            variant="destructive"
            disabled={busy}
            data-testid="platform-role-deactivate"
            onClick={() => {
              if (!window.confirm(t("platformRoles.confirm.deactivate"))) {
                return;
              }
              void runMutation(
                () =>
                  deactivatePlatformRoleDefinition(env.platformApiBaseUrl, role.id, {
                    expectedVersion: role.version,
                  }),
                t("platformRoles.mutation.updated"),
              );
            }}
          >
            {t("platformRoles.lifecycle.deactivate")}
          </Button>
        ) : null}

        <Button
          type="button"
          size="sm"
          variant="destructive"
          disabled={busy}
          data-testid="platform-role-retire"
          onClick={() => {
            if (!window.confirm(t("platformRoles.confirm.retire"))) {
              return;
            }
            void runMutation(
              () =>
                retirePlatformRoleDefinition(env.platformApiBaseUrl, role.id, {
                  expectedVersion: role.version,
                }),
              t("platformRoles.mutation.updated"),
            );
          }}
        >
          {t("platformRoles.lifecycle.retire")}
        </Button>
      </div>

      {feedback ? (
        <Alert
          title={feedback.title}
          tone={feedback.tone}
          data-testid={
            feedback.tone === "success"
              ? "platform-role-mutation-success"
              : "platform-role-mutation-error"
          }
        >
          {feedback.detail}
        </Alert>
      ) : null}
    </div>
  );
}

export function PlatformRoleDetailPage() {
  const { t, language, theme, density } = usePreferences();
  const authorization = useAuthorization();
  const queryClient = useQueryClient();
  const params = useParams();
  const roleId = parsePlatformRoleId(params.roleId);

  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.managePlatformUsers);

  const query = usePlatformRoleDetailQuery(canManage ? roleId : null);
  const permissionsQuery = usePlatformPermissionsQuery(
    canManage && query.data?.kind === "Custom" && query.data.status !== "Retired",
  );

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true" className="grid gap-4">
        <DashboardWidgetSkeleton />
      </section>
    );
  }

  if (!canManage || roleId == null) {
    return <ShellNotFoundPage />;
  }

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  if (query.isPending) {
    return (
      <section aria-busy="true" className="grid gap-4" data-testid="platform-role-detail-loading">
        <DashboardWidgetSkeleton />
      </section>
    );
  }

  if (query.isError) {
    if (query.error instanceof PlatformApiError && query.error.status === 404) {
      return (
        <section className="grid gap-4" data-testid="platform-role-detail-not-found">
          <PageHeader title={t("platformRoles.detail.title")} description={t("platformRoles.detail.notFound")} />
          <Link to="/admin/platform-roles">{t("platformRoles.detail.back")}</Link>
        </section>
      );
    }
    return (
      <section className="grid gap-4">
        <PageHeader title={t("platformRoles.detail.title")} description={t("platformRoles.description")} />
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: query.error,
            operation: "Load platform role definition",
            environment: { locale: language, theme, density },
          })}
          description={t("platformRoles.detail.error")}
          onRetry={() => void query.refetch()}
        />
        <Link to="/admin/platform-roles">{t("platformRoles.detail.back")}</Link>
      </section>
    );
  }

  const role = query.data!;
  const canEditCustom = role.kind === "Custom" && role.status !== "Retired";

  return (
    <section className="grid gap-4" data-testid="platform-role-detail-page">
      <PageHeader
        title={t("platformRoles.detail.title")}
        description={role.code}
        actions={
          <Button asChild variant="outline" size="sm">
            <Link to="/admin/platform-roles">{t("platformRoles.detail.back")}</Link>
          </Button>
        }
      />

      <Alert title={t("platformRoles.warning.title")} tone="info" data-testid="platform-roles-pos-warning">
        {t("platformRoles.warning.body")}
      </Alert>

      <DashboardSection title={t("platformRoles.detail.section")}>
        <dl className="grid gap-3 sm:grid-cols-2">
          <DetailField label={t("platformRoles.field.code")}>
            <span className="font-mono text-[length:var(--exits-text-xs)]">{role.code}</span>
          </DetailField>
          <DetailField label={t("platformRoles.field.kind")}>{role.kind}</DetailField>
          <DetailField label={t("platformRoles.field.status")}>
            <StatusIndicator label={role.status} tone={statusTone(role.status)} />
          </DetailField>
          <DetailField label={t("platformRoles.field.version")}>{role.version}</DetailField>
          <DetailField label={t("platformRoles.field.description")}>
            {role.description?.trim() ? role.description : "—"}
          </DetailField>
          <DetailField label={t("platformRoles.permissions")}>
            <ul className="flex flex-wrap gap-1">
              {role.permissions.length === 0 ? (
                <li className="text-muted">—</li>
              ) : (
                role.permissions.map((permission) => (
                  <li
                    key={permission}
                    className="rounded border border-border px-1.5 py-0.5 font-mono text-[length:var(--exits-text-xs)]"
                  >
                    {permission}
                  </li>
                ))
              )}
            </ul>
          </DetailField>
        </dl>
      </DashboardSection>

      {canEditCustom ? (
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
          <CustomRoleEditor
            key={role.id}
            role={role}
            permissionOptions={(permissionsQuery.data ?? []).map((item) => ({
              code: item.code,
              description: item.description,
            }))}
            onUpdated={(updated) => {
              queryClient.setQueryData(platformRoleDetailQueryKey(updated.id), updated);
            }}
          />
        )
      ) : role.kind === "BuiltIn" ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted" data-testid="platform-role-builtin-readonly">
          {t("platformRoles.builtin.readonly")}
        </p>
      ) : null}
    </section>
  );
}
