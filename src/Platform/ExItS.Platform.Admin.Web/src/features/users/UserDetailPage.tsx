import { useMemo } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  assignmentsSearchParams,
  parseAssignmentsSearchParams,
  type AssignmentsUrlState,
} from "@/api/authorization/assignment-list-query";
import {
  ASSIGNMENTS_PAGE_SIZE,
  ASSIGNMENT_STATUSES,
  type PlatformRoleAssignment,
} from "@/api/authorization/assignment-types";
import { PlatformApiError } from "@/api/platform-http";
import { parsePlatformUserId, usersListHref } from "@/api/users/user-id";
import type { PlatformUserDetail } from "@/api/users/user-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ForbiddenState } from "@/features/overview/ForbiddenState";
import { UserCredentialsPanel } from "@/features/users/UserCredentialsPanel";
import { UserLifecycleActions } from "@/features/users/UserLifecycleActions";
import { UserNotFoundPage } from "@/features/users/UserNotFoundPage";
import { UserProfileEditor } from "@/features/users/UserProfileEditor";
import {
  UserMembershipsPanel,
  UserProductAccessPanel,
} from "@/features/users/UserRelatedPanels";
import {
  platformUserDetailQueryKey,
  usePlatformUserAssignmentsQuery,
  usePlatformUserDetailQuery,
} from "@/features/users/use-user-detail-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Suspended: "dashboard.status.Suspended",
  Deactivated: "users.status.Deactivated",
  PendingVerification: "dashboard.status.PendingVerification",
};

const ASSIGNMENT_STATUS_LABELS: Record<string, MessageKey> = {
  Active: "users.detail.assignment.status.Active",
  Revoked: "users.detail.assignment.status.Revoked",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Suspended" || status === "PendingVerification") {
    return "warning";
  }
  if (status === "Deactivated" || status === "Revoked") {
    return "danger";
  }
  return "neutral";
}

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

function isNotFound(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 404;
}

function presentDetailFields(
  user: PlatformUserDetail,
): Array<{ label: MessageKey; value: string }> {
  const fields: Array<{ label: MessageKey; value: string | undefined }> = [
    { label: "users.column.username", value: user.username },
    { label: "users.column.email", value: user.email },
    { label: "users.detail.field.firstName", value: user.firstName },
    { label: "users.detail.field.lastName", value: user.lastName },
    { label: "users.detail.field.phone", value: user.phone },
    { label: "users.detail.field.employeeCode", value: user.employeeCode },
    { label: "users.detail.field.staffNumber", value: user.staffNumber },
    { label: "users.detail.field.createdBy", value: user.createdByUserId },
  ];
  return fields.flatMap((field) =>
    field.value && field.value.length > 0 ? [{ label: field.label, value: field.value }] : [],
  );
}

function assignmentRoleLabel(role: string, t: (key: MessageKey) => string): string {
  const key = `users.detail.role.${role}` as MessageKey;
  const translated = t(key);
  if (!translated || translated === key) {
    return role;
  }
  return translated;
}

export function UserDetailPage() {
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const queryClient = useQueryClient();
  const params = useParams();
  const userId = parsePlatformUserId(params.userId);
  const [searchParams, setSearchParams] = useSearchParams();
  const assignmentState = useMemo(() => parseAssignmentsSearchParams(searchParams), [searchParams]);
  const showTable = useMediaQuery("(min-width: 768px)");

  const canView =
    authorization.status === "loaded" &&
    authorization.hasAnyPermission([PLATFORM_PERMISSIONS.managePlatformUsers]);

  const userQuery = usePlatformUserDetailQuery(canView ? userId : null);
  const assignmentsQuery = usePlatformUserAssignmentsQuery(canView ? userId : null, {
    page: assignmentState.page,
    status: assignmentState.status || undefined,
  });

  function replaceAssignmentState(patch: Partial<AssignmentsUrlState>) {
    const current = parseAssignmentsSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(assignmentsSearchParams({ ...current, ...patch }), { replace: true });
  }

  function onUserUpdated(next: PlatformUserDetail) {
    queryClient.setQueryData(platformUserDetailQueryKey(next.id), next);
    void queryClient.invalidateQueries({ queryKey: ["users", "list"] });
  }

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canView) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.managePlatformUsers} />;
  }

  if (userId == null) {
    return <UserNotFoundPage />;
  }

  if (userQuery.isPending) {
    return (
      <section
        className="grid max-w-3xl gap-3"
        role="status"
        aria-busy="true"
        aria-label={t("users.detail.loading")}
      >
        <DashboardWidgetSkeleton rows={8} />
      </section>
    );
  }

  if (userQuery.isError && isForbidden(userQuery.error)) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.managePlatformUsers} />;
  }

  if (userQuery.isError && isNotFound(userQuery.error)) {
    return <UserNotFoundPage />;
  }

  if (userQuery.isError) {
    const diagnostic = normalizeDiagnosticError({
      error: userQuery.error,
      operation: "Load platform user",
    });
    return (
      <ErrorState
        diagnostic={diagnostic}
        title={t("users.detail.error")}
        headingLevel="h1"
        onRetry={() => void userQuery.refetch()}
      />
    );
  }

  const user = userQuery.data;
  if (!user) {
    return <UserNotFoundPage />;
  }

  const detailFields = presentDetailFields(user);
  const assignments = assignmentsQuery.data;
  const assignmentTotalPages = assignments
    ? Math.max(1, Math.ceil(assignments.totalCount / ASSIGNMENTS_PAGE_SIZE))
    : 1;

  return (
    <section className="grid max-w-3xl gap-4" data-testid="users-detail-page">
      <p className="text-[length:var(--exits-text-sm)]">
        <Link className="text-primary hover:underline" to={usersListHref()}>
          {t("users.detail.back")}
        </Link>
      </p>

      <PageHeader
        title={user.displayName}
        description={user.username}
        actions={
          <StatusIndicator
            tone={statusTone(user.status)}
            label={STATUS_LABELS[user.status] ? t(STATUS_LABELS[user.status]!) : user.status}
          />
        }
      />

      <DashboardSection title={t("users.lifecycle.title")}>
        <UserLifecycleActions user={user} onUpdated={onUserUpdated} />
      </DashboardSection>

      <DashboardSection title={t("users.detail.identity")}>
        <UserProfileEditor user={user} onUpdated={onUserUpdated} />
        <dl className="mt-3 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <div className="min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("users.detail.field.id")}
            </dt>
            <dd className="break-all font-mono text-[length:var(--exits-text-xs)]">{user.id}</dd>
          </div>
          {detailFields.map((field) => (
            <div key={field.label} className="min-w-0">
              <dt className="text-[length:var(--exits-text-xs)] text-muted">{t(field.label)}</dt>
              <dd className="break-words">{field.value}</dd>
            </div>
          ))}
          <div className="min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("users.column.accountClass")}
            </dt>
            <dd className="break-words">
              {user.accountClasses.length > 0 ? user.accountClasses.join(", ") : "—"}
            </dd>
          </div>
          <div className="min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("users.detail.field.created")}
            </dt>
            <dd className="tabular-nums text-muted">
              {formatInstant(user.createdAtUtc, language) ?? "—"}
            </dd>
          </div>
          <div className="min-w-0">
            <dt className="text-[length:var(--exits-text-xs)] text-muted">
              {t("users.detail.field.updated")}
            </dt>
            <dd className="tabular-nums text-muted">
              {formatInstant(user.updatedAtUtc, language) ?? "—"}
            </dd>
          </div>
          {user.suspendedAtUtc ? (
            <div className="min-w-0 sm:col-span-2">
              <dt className="text-[length:var(--exits-text-xs)] text-muted">
                {t("users.detail.field.suspended")}
              </dt>
              <dd className="tabular-nums text-muted">
                {formatInstant(user.suspendedAtUtc, language) ?? user.suspendedAtUtc}
                {user.suspensionReason ? ` · ${user.suspensionReason}` : ""}
              </dd>
            </div>
          ) : null}
        </dl>
      </DashboardSection>

      <UserCredentialsPanel userId={user.id} />

      <DashboardSection title={t("users.detail.organizations")}>
        {user.organizations && user.organizations.length > 0 ? (
          <ul className="grid gap-2 text-[length:var(--exits-text-sm)]">
            {user.organizations.map((org) => (
              <li key={`${org.name}-${org.role ?? ""}`} className="min-w-0 break-words">
                <span>{org.name}</span>
                {org.roleDisplay || org.role ? (
                  <span className="text-muted"> · {org.roleDisplay ?? org.role}</span>
                ) : null}
              </li>
            ))}
          </ul>
        ) : user.organizationNames.length > 0 ? (
          <p className="break-words text-[length:var(--exits-text-sm)]">
            {user.organizationNames.join(", ")}
          </p>
        ) : (
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("users.detail.organizations.empty")}
          </p>
        )}
      </DashboardSection>

      <UserMembershipsPanel userId={user.id} />
      <UserProductAccessPanel userId={user.id} />

      <DashboardSection
        title={t("users.detail.assignments")}
        description={t("users.detail.assignments.hint")}
      >
        <div className="mb-3 flex flex-wrap items-end gap-2">
          <label className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("users.detail.assignment.status")}
            <select
              className={controlClass}
              value={assignmentState.status}
              aria-label={t("users.detail.assignment.status")}
              onChange={(event) =>
                replaceAssignmentState({
                  status: event.target.value as AssignmentsUrlState["status"],
                  page: 1,
                })
              }
            >
              <option value="">{t("users.status.all")}</option>
              {ASSIGNMENT_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {ASSIGNMENT_STATUS_LABELS[status] ? t(ASSIGNMENT_STATUS_LABELS[status]!) : status}
                </option>
              ))}
            </select>
          </label>
        </div>

        {assignmentsQuery.isPending ? (
          <div role="status" aria-busy="true" aria-label={t("users.detail.assignments.loading")}>
            <DashboardWidgetSkeleton rows={4} />
          </div>
        ) : null}

        {assignmentsQuery.isError ? (
          <ErrorState
            diagnostic={normalizeDiagnosticError({
              error: assignmentsQuery.error,
              operation: "Load role assignments",
            })}
            title={t("users.detail.assignments.error")}
            headingLevel="h2"
            onRetry={() => void assignmentsQuery.refetch()}
          />
        ) : null}

        {assignments && assignments.items.length === 0 ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
            {t("users.detail.assignments.empty")}
          </p>
        ) : null}

        {assignments && assignments.items.length > 0 ? (
          <>
            {showTable ? (
              <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
                <AdminTable
                  caption={t("users.detail.assignments.caption")}
                  empty={t("users.detail.assignments.empty")}
                  columns={[
                    {
                      id: "role",
                      header: t("users.detail.assignment.role"),
                      cell: (assignment) => assignmentRoleLabel(assignment.role, t),
                    },
                    {
                      id: "organization",
                      header: t("users.detail.assignment.organization"),
                      cell: (assignment) => (
                        <span className="break-all font-mono text-[length:var(--exits-text-xs)]">
                          {assignment.organizationId ?? "—"}
                        </span>
                      ),
                    },
                    {
                      id: "status",
                      header: t("users.detail.assignment.status"),
                      cell: (assignment) => (
                        <StatusIndicator
                          tone={statusTone(assignment.status)}
                          label={
                            ASSIGNMENT_STATUS_LABELS[assignment.status]
                              ? t(ASSIGNMENT_STATUS_LABELS[assignment.status]!)
                              : assignment.status
                          }
                        />
                      ),
                    },
                    {
                      id: "granted",
                      header: t("users.detail.assignment.granted"),
                      cell: (assignment) => (
                        <span className="tabular-nums text-muted">
                          {formatInstant(assignment.grantedAtUtc, language) ??
                            assignment.grantedAtUtc}
                          {assignment.grantedByActor ? ` · ${assignment.grantedByActor}` : ""}
                        </span>
                      ),
                    },
                  ]}
                  rows={assignments.items}
                />
              </div>
            ) : (
              <ul className="grid gap-3">
                {assignments.items.map((assignment) => (
                  <AssignmentCard
                    key={assignment.id}
                    assignment={assignment}
                    language={language}
                    t={t}
                  />
                ))}
              </ul>
            )}
          </>
        ) : null}

        {assignments && assignments.totalCount > ASSIGNMENTS_PAGE_SIZE ? (
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={assignmentState.page <= 1}
              onClick={() => replaceAssignmentState({ page: assignmentState.page - 1 })}
            >
              {t("users.previous")}
            </Button>
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {t("users.page")} {assignmentState.page} / {assignmentTotalPages}
            </span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={assignmentState.page >= assignmentTotalPages}
              onClick={() => replaceAssignmentState({ page: assignmentState.page + 1 })}
            >
              {t("users.next")}
            </Button>
          </div>
        ) : null}
      </DashboardSection>
    </section>
  );
}

function AssignmentCard({
  assignment,
  language,
  t,
}: {
  assignment: PlatformRoleAssignment;
  language: string;
  t: (key: MessageKey) => string;
}) {
  return (
    <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-medium">{assignmentRoleLabel(assignment.role, t)}</span>
        <StatusIndicator
          tone={statusTone(assignment.status)}
          label={
            ASSIGNMENT_STATUS_LABELS[assignment.status]
              ? t(ASSIGNMENT_STATUS_LABELS[assignment.status]!)
              : assignment.status
          }
        />
      </div>
      {assignment.organizationId ? (
        <p className="mt-1 break-all font-mono text-[length:var(--exits-text-xs)] text-muted">
          {assignment.organizationId}
        </p>
      ) : null}
      <p className="mt-1 text-[length:var(--exits-text-xs)] text-muted">
        {formatInstant(assignment.grantedAtUtc, language) ?? assignment.grantedAtUtc}
        {assignment.grantedByActor ? ` · ${assignment.grantedByActor}` : ""}
      </p>
    </li>
  );
}
