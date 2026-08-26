import { useMemo, useState, type FormEvent } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import {
  hasActiveOrganizationAuditFilters,
  ORGANIZATION_AUDIT_OUTCOMES,
  ORGANIZATION_AUDIT_PAGE_SIZE,
  organizationAuditSearchParams,
  parseOrganizationAuditSearchParams,
  type OrganizationAuditOutcome,
  type OrganizationAuditRecord,
  type OrganizationAuditUrlState,
} from "@/api/organizations/organization-audit-list-query";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useOrganizationAuditQuery } from "@/features/organizations/use-organization-workspace-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import {
  presentAuditAction,
  presentAuditActor,
  presentAuditType,
} from "@/lib/audit/audit-presentation";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function outcomeTone(outcome: string): "success" | "warning" | "danger" | "neutral" {
  if (outcome === "Succeeded") {
    return "success";
  }
  if (outcome === "Denied") {
    return "warning";
  }
  if (outcome === "Failed") {
    return "danger";
  }
  return "neutral";
}

function outcomeLabel(t: (key: MessageKey) => string, outcome: string): string {
  if (outcome === "Succeeded") {
    return t("dashboard.audit.outcome.Succeeded");
  }
  if (outcome === "Denied") {
    return t("dashboard.audit.outcome.Denied");
  }
  if (outcome === "Failed") {
    return t("dashboard.audit.outcome.Failed");
  }
  return outcome;
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

function branchContext(item: OrganizationAuditRecord): string | null {
  if (item.targetType === "OrganizationBranch" && item.targetId) {
    return item.targetId;
  }
  return null;
}

function AuditFilterForm({
  state,
  onReplaceState,
}: {
  state: OrganizationAuditUrlState;
  onReplaceState: (patch: Partial<OrganizationAuditUrlState>) => void;
}) {
  const { t } = usePreferences();
  const [actorDraft, setActorDraft] = useState(state.actor);
  const [actionDraft, setActionDraft] = useState(state.action);
  const [targetTypeDraft, setTargetTypeDraft] = useState(state.targetType);
  const [branchDraft, setBranchDraft] = useState(state.branchId);
  const [fromDraft, setFromDraft] = useState(state.fromUtc);
  const [toDraft, setToDraft] = useState(state.toUtc);

  function onFilterSubmit(event: FormEvent) {
    event.preventDefault();
    onReplaceState({
      actor: actorDraft.trim(),
      action: actionDraft.trim(),
      targetType: targetTypeDraft.trim(),
      branchId: branchDraft.trim(),
      fromUtc: fromDraft.trim(),
      toUtc: toDraft.trim(),
      page: 1,
    });
  }

  return (
    <form
      className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-2 lg:grid-cols-4"
      onSubmit={onFilterSubmit}
    >
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-audit-from"
      >
        {t("organization.activity.filter.from")}
        <Input
          id="org-audit-from"
          value={fromDraft}
          onChange={(event) => setFromDraft(event.target.value)}
          placeholder="2026-01-01T00:00:00Z"
          name="fromUtc"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-audit-to"
      >
        {t("organization.activity.filter.to")}
        <Input
          id="org-audit-to"
          value={toDraft}
          onChange={(event) => setToDraft(event.target.value)}
          placeholder="2026-12-31T23:59:59Z"
          name="toUtc"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-audit-actor"
      >
        {t("organization.activity.filter.actor")}
        <Input
          id="org-audit-actor"
          value={actorDraft}
          onChange={(event) => setActorDraft(event.target.value)}
          name="actor"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-audit-action"
      >
        {t("organization.activity.filter.action")}
        <Input
          id="org-audit-action"
          value={actionDraft}
          onChange={(event) => setActionDraft(event.target.value)}
          name="action"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-audit-target-type"
      >
        {t("organization.activity.filter.targetType")}
        <Input
          id="org-audit-target-type"
          value={targetTypeDraft}
          onChange={(event) => setTargetTypeDraft(event.target.value)}
          name="targetType"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-audit-outcome"
      >
        {t("organization.activity.filter.outcome")}
        <select
          id="org-audit-outcome"
          className={controlClass}
          value={state.outcome}
          onChange={(event) =>
            onReplaceState({
              outcome: event.target.value as OrganizationAuditOutcome | "",
              page: 1,
            })
          }
        >
          <option value="">{t("organization.activity.filter.outcome.all")}</option>
          {ORGANIZATION_AUDIT_OUTCOMES.map((outcome) => (
            <option key={outcome} value={outcome}>
              {outcomeLabel(t, outcome)}
            </option>
          ))}
        </select>
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-audit-branch"
      >
        {t("organization.activity.filter.branchId")}
        <Input
          id="org-audit-branch"
          value={branchDraft}
          onChange={(event) => setBranchDraft(event.target.value)}
          name="branchId"
          autoComplete="off"
        />
      </label>
      <div className="flex flex-wrap items-end gap-2">
        <Button type="submit" size="sm">
          {t("organization.activity.filter.apply")}
        </Button>
        {hasActiveOrganizationAuditFilters(state) ? (
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() =>
              onReplaceState({
                fromUtc: "",
                toUtc: "",
                actor: "",
                action: "",
                targetType: "",
                outcome: "",
                branchId: "",
                page: 1,
              })
            }
          >
            {t("organization.activity.filter.reset")}
          </Button>
        ) : null}
      </div>
    </form>
  );
}

export function OrganizationActivityPage() {
  const { t, language } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseOrganizationAuditSearchParams(searchParams), [searchParams]);
  const draftSyncKey = [
    state.actor,
    state.action,
    state.targetType,
    state.branchId,
    state.fromUtc,
    state.toUtc,
  ].join("\u001f");

  const query = useOrganizationAuditQuery(organizationId, state);

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  function replaceState(patch: Partial<OrganizationAuditUrlState>) {
    const current = parseOrganizationAuditSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(organizationAuditSearchParams({ ...current, ...patch }), { replace: true });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ORGANIZATION_AUDIT_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load organization audit",
      })
    : null;

  return (
    <section className="grid max-w-6xl gap-4">
      <PageHeader
        title={t("organization.activity.title")}
        description={t("organization.activity.description")}
      />

      <AuditFilterForm key={draftSyncKey} state={state} onReplaceState={replaceState} />

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.activity.loading")}
        >
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.activity.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <AuditResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActiveOrganizationAuditFilters(state)}
          language={language}
          showTable={showTable}
          onPage={(nextPage) => replaceState({ page: nextPage })}
        />
      ) : null}
    </section>
  );
}

function AuditResults({
  items,
  totalCount,
  page,
  totalPages,
  filtered,
  language,
  showTable,
  onPage,
}: {
  items: OrganizationAuditRecord[];
  totalCount: number;
  page: number;
  totalPages: number;
  filtered: boolean;
  language: string;
  showTable: boolean;
  onPage: (page: number) => void;
}) {
  const { t } = usePreferences();

  if (items.length === 0) {
    return (
      <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-6 text-[length:var(--exits-text-sm)] text-muted">
        {filtered ? t("organization.activity.zeroResult") : t("organization.activity.empty")}
      </p>
    );
  }

  return (
    <div className="grid gap-3">
      {showTable ? (
        <AdminTable
          caption={t("organization.activity.caption")}
          empty={
            filtered ? t("organization.activity.zeroResult") : t("organization.activity.empty")
          }
          columns={[
            {
              id: "when",
              header: t("organization.activity.column.when"),
              cell: (item) => formatInstant(item.occurredAtUtc, language) ?? "—",
            },
            {
              id: "actor",
              header: t("organization.activity.column.actor"),
              cell: (item) => {
                const actor = presentAuditActor(item.actorIdentifier, t);
                return (
                  <span className="break-words" title={actor.raw}>
                    {actor.label}
                    {actor.detail ? (
                      <span className="mt-0.5 block font-mono text-[length:var(--exits-text-xs)] text-muted">
                        {actor.detail}
                      </span>
                    ) : null}
                  </span>
                );
              },
            },
            {
              id: "action",
              header: t("organization.activity.column.action"),
              cell: (item) => {
                const action = presentAuditAction(item.actionCode, t);
                return (
                  <span className="break-words" title={action.raw}>
                    {action.label}
                  </span>
                );
              },
            },
            {
              id: "target",
              header: t("organization.activity.column.target"),
              cell: (item) => {
                const type = presentAuditType(item.targetType, t);
                const branch = branchContext(item);
                return (
                  <span className="break-words" title={`${type.raw} ${item.targetId}`.trim()}>
                    {type.label}
                    {item.targetId ? (
                      <span className="mt-0.5 block font-mono text-[length:var(--exits-text-xs)] text-muted">
                        {item.targetId}
                      </span>
                    ) : null}
                    {branch ? (
                      <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                        {t("organization.activity.branch")}: {branch}
                      </span>
                    ) : null}
                  </span>
                );
              },
            },
            {
              id: "outcome",
              header: t("organization.activity.column.outcome"),
              cell: (item) => (
                <StatusIndicator
                  tone={outcomeTone(item.outcome)}
                  label={outcomeLabel(t, item.outcome)}
                />
              ),
            },
            {
              id: "summary",
              header: t("organization.activity.column.summary"),
              cell: (item) => (
                <span className="break-words text-[length:var(--exits-text-sm)] text-muted">
                  {item.summary || item.reason || "—"}
                </span>
              ),
            },
          ]}
          rows={items}
        />
      ) : (
        <ul className="grid gap-2">
          {items.map((item) => {
            const action = presentAuditAction(item.actionCode, t);
            const actor = presentAuditActor(item.actorIdentifier, t);
            const type = presentAuditType(item.targetType, t);
            const branch = branchContext(item);
            return (
              <li
                key={item.id}
                className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-3"
              >
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <p className="text-[length:var(--exits-text-sm)] font-medium text-foreground">
                    {action.label}
                  </p>
                  <StatusIndicator
                    tone={outcomeTone(item.outcome)}
                    label={outcomeLabel(t, item.outcome)}
                  />
                </div>
                <p
                  className="mt-1 text-[length:var(--exits-text-xs)] text-muted"
                  title={action.raw}
                >
                  {action.raw}
                </p>
                <dl className="mt-2 grid gap-1 text-[length:var(--exits-text-sm)]">
                  <div>
                    <dt className="text-muted">{t("organization.activity.column.when")}</dt>
                    <dd>{formatInstant(item.occurredAtUtc, language) ?? "—"}</dd>
                  </div>
                  <div>
                    <dt className="text-muted">{t("organization.activity.column.actor")}</dt>
                    <dd className="break-words" title={actor.raw}>
                      {actor.label}
                      {actor.detail ? ` (${actor.detail})` : null}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-muted">{t("organization.activity.column.target")}</dt>
                    <dd className="break-words" title={type.raw}>
                      {type.label}
                      {item.targetId ? ` · ${item.targetId}` : null}
                    </dd>
                  </div>
                  {branch ? (
                    <div>
                      <dt className="text-muted">{t("organization.activity.branch")}</dt>
                      <dd className="break-all font-mono text-[length:var(--exits-text-xs)]">
                        {branch}
                      </dd>
                    </div>
                  ) : null}
                  {item.summary || item.reason ? (
                    <div>
                      <dt className="text-muted">{t("organization.activity.column.summary")}</dt>
                      <dd className="break-words">{item.summary || item.reason}</dd>
                    </div>
                  ) : null}
                </dl>
              </li>
            );
          })}
        </ul>
      )}

      <div className="flex flex-wrap items-center justify-between gap-2 text-[length:var(--exits-text-sm)] text-muted">
        <p>
          {t("organization.activity.page")} {page} / {totalPages} · {totalCount}
        </p>
        <div className="flex gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={page <= 1}
            onClick={() => onPage(page - 1)}
          >
            {t("organization.activity.previous")}
          </Button>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={page >= totalPages}
            onClick={() => onPage(page + 1)}
          >
            {t("organization.activity.next")}
          </Button>
        </div>
      </div>
    </div>
  );
}
