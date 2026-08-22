import { useMemo, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  hasActivePlatformAuditFilters,
  PLATFORM_AUDIT_OUTCOMES,
  PLATFORM_AUDIT_PAGE_SIZE,
  parsePlatformAuditSearchParams,
  platformAuditSearchParams,
  type PlatformAuditOutcome,
  type PlatformAuditRecord,
  type PlatformAuditUrlState,
} from "@/api/audit/audit-list-query";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { usePlatformAuditListQuery } from "@/features/audit/use-audit-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
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

function targetLabel(item: PlatformAuditRecord): string {
  return `${item.targetType} ${item.targetId}`.trim();
}

function AuditFilterForm({
  state,
  onReplaceState,
}: {
  state: PlatformAuditUrlState;
  onReplaceState: (patch: Partial<PlatformAuditUrlState>) => void;
}) {
  const { t } = usePreferences();
  const [fromDraft, setFromDraft] = useState(state.fromUtc);
  const [toDraft, setToDraft] = useState(state.toUtc);
  const [actorDraft, setActorDraft] = useState(state.actor);
  const [actionDraft, setActionDraft] = useState(state.action);
  const [organizationDraft, setOrganizationDraft] = useState(state.organizationId);
  const [productDraft, setProductDraft] = useState(state.productCode);

  function onFilterSubmit(event: FormEvent) {
    event.preventDefault();
    onReplaceState({
      fromUtc: fromDraft.trim(),
      toUtc: toDraft.trim(),
      actor: actorDraft.trim(),
      action: actionDraft.trim(),
      organizationId: organizationDraft.trim(),
      productCode: productDraft.trim(),
      page: 1,
    });
  }

  return (
    <form
      className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-2 lg:grid-cols-4"
      onSubmit={onFilterSubmit}
      data-testid="audit-filters"
    >
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="audit-from"
      >
        {t("audit.filter.from")}
        <Input
          id="audit-from"
          value={fromDraft}
          onChange={(event) => setFromDraft(event.target.value)}
          placeholder="yyyy-MM-ddTHH:mm:ssZ"
          name="fromUtc"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="audit-to"
      >
        {t("audit.filter.to")}
        <Input
          id="audit-to"
          value={toDraft}
          onChange={(event) => setToDraft(event.target.value)}
          placeholder="yyyy-MM-ddTHH:mm:ssZ"
          name="toUtc"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="audit-actor"
      >
        {t("audit.filter.actor")}
        <Input
          id="audit-actor"
          value={actorDraft}
          onChange={(event) => setActorDraft(event.target.value)}
          name="actor"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="audit-action"
      >
        {t("audit.filter.action")}
        <Input
          id="audit-action"
          value={actionDraft}
          onChange={(event) => setActionDraft(event.target.value)}
          name="action"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="audit-organization"
      >
        {t("audit.filter.organization")}
        <Input
          id="audit-organization"
          value={organizationDraft}
          onChange={(event) => setOrganizationDraft(event.target.value)}
          name="organizationId"
          placeholder="GUID"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="audit-product"
      >
        {t("audit.filter.product")}
        <Input
          id="audit-product"
          value={productDraft}
          onChange={(event) => setProductDraft(event.target.value)}
          name="productCode"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="audit-outcome"
      >
        {t("audit.filter.outcome")}
        <select
          id="audit-outcome"
          className={controlClass}
          value={state.outcome}
          onChange={(event) =>
            onReplaceState({
              outcome: event.target.value as PlatformAuditOutcome | "",
              page: 1,
            })
          }
        >
          <option value="">{t("audit.filter.outcome.all")}</option>
          {PLATFORM_AUDIT_OUTCOMES.map((outcome) => (
            <option key={outcome} value={outcome}>
              {outcomeLabel(t, outcome)}
            </option>
          ))}
        </select>
      </label>
      <div className="flex flex-wrap items-end gap-2">
        <Button type="submit" size="sm">
          {t("audit.filter.apply")}
        </Button>
        {hasActivePlatformAuditFilters(state) ? (
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
                organizationId: "",
                productCode: "",
                outcome: "",
                page: 1,
              })
            }
          >
            {t("audit.filter.reset")}
          </Button>
        ) : null}
      </div>
    </form>
  );
}

export function AuditListPage() {
  const { t, language, theme, density } = usePreferences();
  const authorization = useAuthorization();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parsePlatformAuditSearchParams(searchParams), [searchParams]);
  const draftSyncKey = [
    state.fromUtc,
    state.toUtc,
    state.actor,
    state.action,
    state.organizationId,
    state.productCode,
  ].join("\u001f");

  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewAuditRecords);

  const query = usePlatformAuditListQuery(canView, state);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true" className="grid gap-4">
        <DashboardWidgetSkeleton />
      </section>
    );
  }

  if (!canView) {
    return <ShellNotFoundPage />;
  }

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  function replaceState(patch: Partial<PlatformAuditUrlState>) {
    const current = parsePlatformAuditSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(platformAuditSearchParams({ ...current, ...patch }), { replace: true });
  }

  const items = query.data?.items ?? [];
  const totalCount = query.data?.totalCount ?? 0;
  const page = query.data?.page ?? state.page;
  const pageSize = query.data?.pageSize ?? PLATFORM_AUDIT_PAGE_SIZE;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const hasFilters = hasActivePlatformAuditFilters(state);
  const emptyMessage = hasFilters ? t("audit.zeroResult") : t("audit.empty");

  return (
    <section className="grid gap-4" data-testid="audit-list-page">
      <PageHeader title={t("audit.title")} description={t("audit.description")} />

      <AuditFilterForm key={draftSyncKey} state={state} onReplaceState={replaceState} />

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("audit.loading")}
        >
          <DashboardWidgetSkeleton />
        </div>
      ) : null}

      {query.isError ? (
        <ErrorState
          diagnostic={normalizeDiagnosticError({
            error: query.error,
            operation: "Load platform audit",
            environment: { locale: language, theme, density },
          })}
          description={t("audit.error")}
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <>
          {showTable ? (
            <AdminTable
              caption={t("audit.caption")}
              empty={emptyMessage}
              columns={[
                {
                  id: "occurred",
                  header: t("audit.column.occurred"),
                  cell: (item) => (
                    <Link
                      className="text-primary underline-offset-4 hover:underline"
                      to={`/admin/audit/${item.id}`}
                    >
                      {formatInstant(item.occurredAtUtc, language) ?? item.occurredAtUtc}
                    </Link>
                  ),
                },
                {
                  id: "actor",
                  header: t("audit.column.actor"),
                  cell: (item) => item.actorIdentifier,
                },
                {
                  id: "action",
                  header: t("audit.column.action"),
                  cell: (item) => (
                    <span className="break-all font-mono text-[length:var(--exits-text-xs)]">
                      {item.actionCode}
                    </span>
                  ),
                },
                {
                  id: "target",
                  header: t("audit.column.target"),
                  cell: (item) => (
                    <span className="break-all">{targetLabel(item)}</span>
                  ),
                },
                {
                  id: "outcome",
                  header: t("audit.column.outcome"),
                  cell: (item) => (
                    <StatusIndicator tone={outcomeTone(item.outcome)} label={outcomeLabel(t, item.outcome)} />
                  ),
                },
              ]}
              rows={items}
            />
          ) : (
            <ul className="grid gap-2" aria-label={t("audit.caption")}>
              {items.length === 0 ? (
                <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 text-muted">
                  {emptyMessage}
                </li>
              ) : (
                items.map((item) => (
                  <li
                    key={item.id}
                    className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-3"
                  >
                    <Link
                      className="font-semibold text-primary underline-offset-4 hover:underline"
                      to={`/admin/audit/${item.id}`}
                    >
                      {formatInstant(item.occurredAtUtc, language) ?? item.occurredAtUtc}
                    </Link>
                    <p className="mt-1 text-[length:var(--exits-text-sm)] text-foreground">
                      {item.actorIdentifier}
                    </p>
                    <p className="mt-1 break-all font-mono text-[length:var(--exits-text-xs)] text-muted">
                      {item.actionCode}
                    </p>
                    <p className="mt-1 break-all text-[length:var(--exits-text-xs)] text-muted">
                      {targetLabel(item)}
                    </p>
                    <div className="mt-2">
                      <StatusIndicator tone={outcomeTone(item.outcome)} label={outcomeLabel(t, item.outcome)} />
                    </div>
                  </li>
                ))
              )}
            </ul>
          )}

          {totalCount > 0 ? (
            <div className="flex flex-wrap items-center justify-between gap-2 text-[length:var(--exits-text-sm)] text-muted">
              <p>
                {t("audit.page")} {page} / {totalPages} · {totalCount}
              </p>
              <div className="flex gap-2">
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={page <= 1}
                  onClick={() => replaceState({ page: Math.max(1, page - 1) })}
                >
                  {t("audit.previous")}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  disabled={page >= totalPages}
                  onClick={() => replaceState({ page: page + 1 })}
                >
                  {t("audit.next")}
                </Button>
              </div>
            </div>
          ) : null}
        </>
      ) : null}
    </section>
  );
}
