import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import { getOrganizationAudit } from "@/api/platform/organization-audit-client";
import {
  EMPTY_ORGANIZATION_AUDIT_QUERY,
  formatGovernanceAuditAction,
  ORGANIZATION_AUDIT_OUTCOMES,
  ORGANIZATION_AUDIT_PAGE_SIZE,
  type OrganizationAuditQuery,
} from "@/api/platform/organization-audit-query";
import { listOrganizationBranches } from "@/api/platform/organization-compliance-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { ExperienceAccessDeniedPage } from "@/features/role/ExperienceAccessDeniedPage";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function orgAuditQueryKey(organizationId: string, query: OrganizationAuditQuery) {
  return ["org", "governance-audit", organizationId, query] as const;
}

function problemText(error: unknown, fallback: string): string {
  if (error instanceof PlatformApiError) {
    return error.problem.detail ?? error.problem.title ?? fallback;
  }
  return fallback;
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 403;
}

export function OrgGovernanceAuditPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const canView = hasOrganizationManagementAuthority(sessionGrant);
  const [draft, setDraft] = useState<OrganizationAuditQuery>(EMPTY_ORGANIZATION_AUDIT_QUERY);
  const [applied, setApplied] = useState<OrganizationAuditQuery>(EMPTY_ORGANIZATION_AUDIT_QUERY);

  const branchesQuery = useQuery({
    queryKey: ["org", "governance-audit", "branches", organizationId],
    enabled: Boolean(organizationId) && canView,
    queryFn: ({ signal }) => listOrganizationBranches(organizationId!, signal),
  });

  const auditQuery = useQuery({
    queryKey: organizationId
      ? orgAuditQueryKey(organizationId, applied)
      : ["org", "governance-audit", "none"],
    enabled: Boolean(organizationId) && canView,
    queryFn: ({ signal }) => getOrganizationAudit(organizationId!, applied, signal),
  });

  if (!organizationId) {
    return (
      <div data-testid="audit-no-organization">
        <PageHeader title={t("orgAudit.title")} description={t("orgAudit.subtitle")} />
        <EmptyState title={t("orgAudit.noOrganization")} />
      </div>
    );
  }

  if (!canView || isForbidden(auditQuery.error) || isForbidden(branchesQuery.error)) {
    return <ExperienceAccessDeniedPage testId="org-audit-denied" />;
  }

  const total = auditQuery.data?.totalCount ?? 0;
  const pageCount = Math.max(1, Math.ceil(total / ORGANIZATION_AUDIT_PAGE_SIZE));

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="org-audit-page">
      <PageHeader title={t("orgAudit.title")} description={t("orgAudit.subtitle")} />
      <form
        className="flex flex-wrap items-end gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          setApplied({ ...draft, page: 1 });
        }}
      >
        <Input
          label={t("orgAudit.from")}
          name="audit-from"
          type="date"
          value={draft.fromDate}
          onChange={(event) => setDraft({ ...draft, fromDate: event.target.value })}
        />
        <Input
          label={t("orgAudit.to")}
          name="audit-to"
          type="date"
          value={draft.toDate}
          onChange={(event) => setDraft({ ...draft, toDate: event.target.value })}
        />
        <Input
          label={t("orgAudit.actor")}
          name="audit-actor"
          value={draft.actor}
          placeholder={t("orgAudit.actorPlaceholder")}
          onChange={(event) => setDraft({ ...draft, actor: event.target.value })}
        />
        <Input
          label={t("orgAudit.action")}
          name="audit-action"
          value={draft.action}
          placeholder={t("orgAudit.actionPlaceholder")}
          onChange={(event) => setDraft({ ...draft, action: event.target.value })}
        />
        <Input
          label={t("orgAudit.targetType")}
          name="audit-target-type"
          value={draft.targetType}
          onChange={(event) => setDraft({ ...draft, targetType: event.target.value })}
        />
        <label className="flex flex-col gap-1.5">
          {t("orgAudit.outcome")}
          <select
            aria-label={t("orgAudit.outcome")}
            value={draft.outcome}
            onChange={(event) =>
              setDraft({
                ...draft,
                outcome: event.target.value as OrganizationAuditQuery["outcome"],
              })
            }
          >
            <option value="">{t("orgAudit.outcomeAll")}</option>
            {ORGANIZATION_AUDIT_OUTCOMES.map((outcome) => (
              <option key={outcome} value={outcome}>
                {outcome}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1.5">
          {t("orgAudit.branch")}
          <select
            aria-label={t("orgAudit.branch")}
            value={draft.branchId}
            onChange={(event) => setDraft({ ...draft, branchId: event.target.value })}
          >
            <option value="">{t("orgAudit.branchAll")}</option>
            {(branchesQuery.data ?? [])
              .filter((branch) => branch.status.toLowerCase() !== "archived")
              .map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name} ({branch.code})
                </option>
              ))}
          </select>
        </label>
        <Button type="submit" data-testid="audit-apply">
          {t("orgAudit.apply")}
        </Button>
        <Button
          type="button"
          variant="ghost"
          data-testid="audit-clear"
          onClick={() => {
            setDraft(EMPTY_ORGANIZATION_AUDIT_QUERY);
            setApplied(EMPTY_ORGANIZATION_AUDIT_QUERY);
          }}
        >
          {t("orgAudit.clear")}
        </Button>
      </form>
      {auditQuery.isLoading ? (
        <div data-testid="audit-loading">
          <LoadingSkeleton />
        </div>
      ) : null}
      {auditQuery.isError ? (
        <div data-testid="audit-load-error">
          <ErrorState
            title={t("orgAudit.loadError")}
            detail={problemText(auditQuery.error, t("orgAudit.loadErrorDetail"))}
            error={auditQuery.error}
            operation="load organization audit"
          />
          <Button type="button" data-testid="audit-retry" onClick={() => void auditQuery.refetch()}>
            {t("orgAudit.retry")}
          </Button>
        </div>
      ) : null}
      {auditQuery.data && auditQuery.data.items.length === 0 ? (
        <div data-testid="audit-empty">
          <EmptyState title={t("orgAudit.emptyTitle")} detail={t("orgAudit.emptyDetail")} />
        </div>
      ) : null}
      {auditQuery.data && auditQuery.data.items.length > 0 ? (
        <>
          <div className="min-w-0 overflow-x-auto">
            <table className="w-full text-start">
              <thead>
                <tr>
                  <th>{t("orgAudit.when")}</th>
                  <th>{t("orgAudit.columnActor")}</th>
                  <th>{t("orgAudit.columnAction")}</th>
                  <th>{t("orgAudit.columnTarget")}</th>
                  <th>{t("orgAudit.columnOutcome")}</th>
                  <th>{t("orgAudit.columnSummary")}</th>
                </tr>
              </thead>
              <tbody>
                {auditQuery.data.items.map((row) => (
                  <tr key={row.id} data-testid="audit-row">
                    <td>{row.occurredAtUtc ? new Date(row.occurredAtUtc).toLocaleString() : "—"}</td>
                    <td>{row.actorIdentifier}</td>
                    <td>{formatGovernanceAuditAction(row.actionCode)}</td>
                    <td>
                      {row.targetType} · {row.targetId}
                    </td>
                    <td>{row.outcome}</td>
                    <td>{row.summary ?? row.reason ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Button
              type="button"
              variant="ghost"
              disabled={applied.page <= 1}
              data-testid="audit-previous"
              onClick={() => {
                const page = Math.max(1, applied.page - 1);
                setApplied({ ...applied, page });
                setDraft({ ...draft, page });
              }}
            >
              {t("orgAudit.previous")}
            </Button>
            <span data-testid="audit-page">
              {t("orgAudit.page")
                .replace("{page}", String(applied.page))
                .replace("{pages}", String(pageCount))}
            </span>
            <Button
              type="button"
              variant="ghost"
              disabled={applied.page >= pageCount}
              data-testid="audit-next"
              onClick={() => {
                const page = applied.page + 1;
                setApplied({ ...applied, page });
                setDraft({ ...draft, page });
              }}
            >
              {t("orgAudit.next")}
            </Button>
          </div>
        </>
      ) : null}
    </div>
  );
}
