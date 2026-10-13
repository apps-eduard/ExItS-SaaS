import { useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import {
  getOrganizationAudit,
  type OrganizationAuditRecord,
} from "@/api/platform/organization-audit-client";
import {
  EMPTY_ORGANIZATION_AUDIT_QUERY,
  auditActorUserId,
  formatAuditActor,
  formatAuditSummary,
  formatAuditTarget,
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
import { StatusChip, type StatusChipTone } from "@/components/exits/StatusChip";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
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

function outcomeTone(outcome: string): StatusChipTone {
  const value = outcome.toLowerCase();
  if (value === "succeeded" || value === "success") return "success";
  if (value === "denied" || value === "failed" || value === "failure") return "danger";
  return "neutral";
}

function formatWhen(value: string | null): string {
  if (!value) return "—";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return value;
  return parsed.toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
}

function AuditRecordView({
  row,
  actorName,
}: {
  row: OrganizationAuditRecord;
  actorName: string | null;
}) {
  return {
    when: formatWhen(row.occurredAtUtc),
    action: formatGovernanceAuditAction(row.actionCode),
    actor: formatAuditActor(row.actorIdentifier, actorName),
    target: formatAuditTarget(row.targetType, row.targetId),
    outcome: row.outcome,
    summary: formatAuditSummary(row.summary, row.reason),
  };
}

function AuditSelect({
  id,
  label,
  value,
  onChange,
  children,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  children: ReactNode;
}) {
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label htmlFor={id} className="exits-type-label">
        {label}
      </label>
      <select
        id={id}
        className="exits-select"
        aria-label={label}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      >
        {children}
      </select>
    </div>
  );
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

  const actors = useActorDirectory(
    organizationId,
    (auditQuery.data?.items ?? []).map((row) => auditActorUserId(row.actorIdentifier)),
  );

  function actorName(actorIdentifier: string): string | null {
    const userId = auditActorUserId(actorIdentifier);
    if (!userId) return null;
    const resolved = actors.resolve(userId);
    if (!resolved?.displayName || resolved.actorStatus === "NotAvailable" || resolved.displayName === "Not available") {
      return null;
    }
    return resolved.displayName;
  }

  if (!organizationId) {
    return (
      <div className="org-audit-page flex min-w-0 flex-col gap-4 p-4" data-testid="audit-no-organization">
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
    <div className="org-audit-page flex min-w-0 flex-col gap-4 p-4" data-testid="org-audit-page">
      <PageHeader title={t("orgAudit.title")} description={t("orgAudit.subtitle")} />
      <form
        className="catalog-form-section"
        onSubmit={(event) => {
          event.preventDefault();
          setApplied({ ...draft, page: 1 });
        }}
      >
        <div className="org-audit-filters">
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
          <AuditSelect
            id="audit-outcome"
            label={t("orgAudit.outcome")}
            value={draft.outcome}
            onChange={(outcome) =>
              setDraft({
                ...draft,
                outcome: outcome as OrganizationAuditQuery["outcome"],
              })
            }
          >
            <option value="">{t("orgAudit.outcomeAll")}</option>
            {ORGANIZATION_AUDIT_OUTCOMES.map((outcome) => (
              <option key={outcome} value={outcome}>
                {outcome}
              </option>
            ))}
          </AuditSelect>
          <AuditSelect
            id="audit-branch"
            label={t("orgAudit.branch")}
            value={draft.branchId}
            onChange={(branchId) => setDraft({ ...draft, branchId })}
          >
            <option value="">{t("orgAudit.branchAll")}</option>
            {(branchesQuery.data ?? [])
              .filter((branch) => branch.status.toLowerCase() !== "archived")
              .map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name} ({branch.code})
                </option>
              ))}
          </AuditSelect>
        </div>
        <div className="flex flex-wrap gap-2">
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
        </div>
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
        <section className="catalog-form-section org-audit-log" aria-label={t("orgAudit.title")}>
          <ul className="org-audit-list">
            {auditQuery.data.items.map((row) => {
              const view = AuditRecordView({ row, actorName: actorName(row.actorIdentifier) });
              return (
                <li key={row.id} className="org-audit-entry" data-testid="audit-row">
                  <div className="org-audit-entry__top">
                    <time className="org-audit-entry__when" dateTime={row.occurredAtUtc || undefined}>
                      {view.when}
                    </time>
                    <StatusChip tone={outcomeTone(view.outcome)} shape="auto">
                      {view.outcome}
                    </StatusChip>
                  </div>
                  <h2 className="org-audit-entry__action">{view.action}</h2>
                  <p className="org-audit-entry__meta">
                    {t("orgAudit.columnActor")}: {view.actor}
                  </p>
                  <p className="org-audit-entry__meta">
                    {t("orgAudit.columnTarget")}: {view.target}
                  </p>
                  <p className="org-audit-entry__summary">{view.summary}</p>
                </li>
              );
            })}
          </ul>
          <ExitsTableContainer className="org-audit-table">
            <ExitsTable>
              <ExitsTableHeader>
                <ExitsTableRow>
                  <ExitsTableHead>{t("orgAudit.when")}</ExitsTableHead>
                  <ExitsTableHead>{t("orgAudit.columnAction")}</ExitsTableHead>
                  <ExitsTableHead>{t("orgAudit.columnActor")}</ExitsTableHead>
                  <ExitsTableHead>{t("orgAudit.columnTarget")}</ExitsTableHead>
                  <ExitsTableHead>{t("orgAudit.columnOutcome")}</ExitsTableHead>
                  <ExitsTableHead>{t("orgAudit.columnSummary")}</ExitsTableHead>
                </ExitsTableRow>
              </ExitsTableHeader>
              <ExitsTableBody>
                {auditQuery.data.items.map((row) => {
                  const view = AuditRecordView({ row, actorName: actorName(row.actorIdentifier) });
                  return (
                    <ExitsTableRow key={row.id} data-testid="audit-row">
                      <ExitsTableCell>
                        <time dateTime={row.occurredAtUtc || undefined}>{view.when}</time>
                      </ExitsTableCell>
                      <ExitsTableCell>{view.action}</ExitsTableCell>
                      <ExitsTableCell>{view.actor}</ExitsTableCell>
                      <ExitsTableCell>{view.target}</ExitsTableCell>
                      <ExitsTableCell>
                        <StatusChip tone={outcomeTone(view.outcome)} shape="auto">
                          {view.outcome}
                        </StatusChip>
                      </ExitsTableCell>
                      <ExitsTableCell>{view.summary}</ExitsTableCell>
                    </ExitsTableRow>
                  );
                })}
              </ExitsTableBody>
            </ExitsTable>
          </ExitsTableContainer>
          <div className="exits-table-pagination">
            <p className="exits-table-pagination__range" data-testid="audit-page">
              {t("orgAudit.page")
                .replace("{page}", String(applied.page))
                .replace("{pages}", String(pageCount))}
            </p>
            <div className="exits-table-pagination__nav">
              <button
                type="button"
                className="exits-table-pagination__btn"
                disabled={applied.page <= 1}
                data-testid="audit-previous"
                onClick={() => {
                  const page = Math.max(1, applied.page - 1);
                  setApplied({ ...applied, page });
                  setDraft({ ...draft, page });
                }}
              >
                {t("orgAudit.previous")}
              </button>
              <button
                type="button"
                className="exits-table-pagination__btn"
                disabled={applied.page >= pageCount}
                data-testid="audit-next"
                onClick={() => {
                  const page = applied.page + 1;
                  setApplied({ ...applied, page });
                  setDraft({ ...draft, page });
                }}
              >
                {t("orgAudit.next")}
              </button>
            </div>
          </div>
        </section>
      ) : null}
    </div>
  );
}
