import { useMemo, useState, type FormEvent } from "react";
import { useSearchParams } from "react-router-dom";
import {
  hasActiveOrganizationFilters,
  organizationListSearchParams,
  parseOrganizationListSearchParams,
} from "@/api/organizations/organization-list-query";
import {
  ORGANIZATION_LIST_PAGE_SIZE,
  ORGANIZATION_LIST_SORT_BY,
  ORGANIZATION_STATUSES,
  type OrganizationListItem,
  type OrganizationListSortBy,
} from "@/api/organizations/organization-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { OrganizationWorkspaceLink } from "@/features/organizations/OrganizationWorkspaceLink";
import { useOrganizationListQuery } from "@/features/organizations/use-organization-list-query";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<OrganizationListSortBy, MessageKey> = {
  DisplayName: "organizations.sort.displayName",
  Slug: "organizations.sort.slug",
  Status: "organizations.sort.status",
  CreatedAtUtc: "organizations.sort.createdAtUtc",
  UpdatedAtUtc: "organizations.sort.updatedAtUtc",
};

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Suspended: "dashboard.status.Suspended",
  Closed: "dashboard.status.Closed",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Suspended") {
    return "warning";
  }
  if (status === "Closed") {
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
  }).format(date);
}

export function OrganizationsList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseOrganizationListSearchParams(searchParams), [searchParams]);
  const [searchDraft, setSearchDraft] = useState(state.search);
  const [appliedSearch, setAppliedSearch] = useState(state.search);
  if (state.search !== appliedSearch) {
    setAppliedSearch(state.search);
    setSearchDraft(state.search);
  }

  const query = useOrganizationListQuery(
    {
      page: state.page,
      pageSize: ORGANIZATION_LIST_PAGE_SIZE,
      status: state.status || undefined,
      search: state.search || undefined,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    },
    enabled,
  );

  function replaceState(patch: Partial<typeof state>) {
    const current = parseOrganizationListSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(organizationListSearchParams({ ...current, ...patch }), { replace: true });
  }

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    replaceState({ search: searchDraft.trim(), page: 1 });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ORGANIZATION_LIST_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load organizations",
      })
    : null;

  return (
    <div className="grid gap-3">
      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[minmax(0,1fr)_10rem_10rem_9rem_auto] md:items-end"
        onSubmit={onSearchSubmit}
      >
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-list-search"
        >
          {t("organizations.search")}
          <Input
            id="org-list-search"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            placeholder={t("organizations.searchPlaceholder")}
            name="search"
            autoComplete="off"
          />
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-list-status"
        >
          {t("organizations.status")}
          <select
            id="org-list-status"
            className={controlClass}
            value={state.status}
            onChange={(event) =>
              replaceState({
                status: event.target.value as typeof state.status,
                page: 1,
              })
            }
          >
            <option value="">{t("organizations.status.all")}</option>
            {ORGANIZATION_STATUSES.map((status) => (
              <option key={status} value={status}>
                {t(STATUS_LABELS[status] ?? "dashboard.status.Active")}
              </option>
            ))}
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-list-sort"
        >
          {t("organizations.sort")}
          <select
            id="org-list-sort"
            className={controlClass}
            value={state.sortBy}
            onChange={(event) =>
              replaceState({
                sortBy: event.target.value as OrganizationListSortBy,
                page: 1,
              })
            }
          >
            {ORGANIZATION_LIST_SORT_BY.map((sortBy) => (
              <option key={sortBy} value={sortBy}>
                {t(SORT_LABELS[sortBy])}
              </option>
            ))}
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-list-order"
        >
          {t("organizations.sort.direction")}
          <select
            id="org-list-order"
            className={controlClass}
            value={state.sortDesc ? "desc" : "asc"}
            onChange={(event) =>
              replaceState({
                sortDesc: event.target.value === "desc",
                page: 1,
              })
            }
          >
            <option value="asc">{t("organizations.sort.asc")}</option>
            <option value="desc">{t("organizations.sort.desc")}</option>
          </select>
        </label>
        <div className="flex flex-wrap gap-2">
          <Button type="submit" size="sm">
            {t("organizations.searchSubmit")}
          </Button>
          {hasActiveOrganizationFilters(state) ? (
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={() => {
                setSearchDraft("");
                replaceState({
                  page: 1,
                  search: "",
                  status: "",
                  sortBy: "DisplayName",
                  sortDesc: false,
                });
              }}
            >
              {t("organizations.reset")}
            </Button>
          ) : null}
        </div>
      </form>

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organizations.loading")}
        >
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organizations.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <OrganizationResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActiveOrganizationFilters(state)}
          language={language}
          showTable={showTable}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={() => {
            setSearchDraft("");
            replaceState({
              page: 1,
              search: "",
              status: "",
              sortBy: "DisplayName",
              sortDesc: false,
            });
          }}
        />
      ) : null}
    </div>
  );
}

function OrganizationResults({
  items,
  totalCount,
  page,
  totalPages,
  filtered,
  language,
  showTable,
  onPage,
  onReset,
}: {
  items: OrganizationListItem[];
  totalCount: number;
  page: number;
  totalPages: number;
  filtered: boolean;
  language: string;
  showTable: boolean;
  onPage: (page: number) => void;
  onReset: () => void;
}) {
  const { t } = usePreferences();
  const empty = filtered ? t("organizations.zeroResult") : t("organizations.empty");

  return (
    <div className="grid gap-3">
      {showTable ? (
        <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <AdminTable
            caption={t("organizations.caption")}
            empty={empty}
            columns={[
              {
                id: "name",
                header: t("organizations.column.organization"),
                cell: (organization) => (
                  <OrganizationWorkspaceLink
                    className="font-medium"
                    organizationId={organization.id}
                  >
                    {organization.displayName}
                  </OrganizationWorkspaceLink>
                ),
              },
              {
                id: "slug",
                header: t("organizations.column.identifier"),
                cell: (organization) => (
                  <span className="font-mono text-[length:var(--exits-text-xs)] text-muted">
                    {organization.slug}
                  </span>
                ),
              },
              {
                id: "status",
                header: t("organizations.column.status"),
                cell: (organization) => (
                  <StatusIndicator
                    tone={statusTone(organization.status)}
                    label={
                      STATUS_LABELS[organization.status]
                        ? t(STATUS_LABELS[organization.status]!)
                        : organization.status
                    }
                  />
                ),
              },
              {
                id: "created",
                header: t("organizations.column.created"),
                cell: (organization) => (
                  <span className="tabular-nums text-muted">
                    {formatInstant(organization.createdAtUtc, language) ?? "—"}
                  </span>
                ),
              },
              {
                id: "updated",
                header: t("organizations.column.updated"),
                cell: (organization) => (
                  <span className="tabular-nums text-muted">
                    {formatInstant(organization.updatedAtUtc, language) ?? "—"}
                  </span>
                ),
              },
            ]}
            rows={items}
          />
        </div>
      ) : (
        <ul className="grid gap-2">
          {items.length === 0 ? (
            <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
              {empty}
            </li>
          ) : (
            items.map((organization) => (
              <li
                key={organization.id}
                className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
              >
                <p className="font-medium">{organization.displayName}</p>
                <p className="mt-0.5 font-mono text-[length:var(--exits-text-xs)] text-muted">
                  {organization.slug}
                </p>
                <div className="mt-1.5 flex flex-wrap items-center gap-2">
                  <StatusIndicator
                    tone={statusTone(organization.status)}
                    label={
                      STATUS_LABELS[organization.status]
                        ? t(STATUS_LABELS[organization.status]!)
                        : organization.status
                    }
                  />
                  <OrganizationWorkspaceLink organizationId={organization.id}>
                    {t("organization.open")}
                  </OrganizationWorkspaceLink>
                </div>
              </li>
            ))
          )}
        </ul>
      )}

      {filtered && items.length === 0 ? (
        <Button type="button" size="sm" variant="outline" className="w-fit" onClick={onReset}>
          {t("organizations.reset")}
        </Button>
      ) : null}

      {totalCount > 0 ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={page <= 1}
            onClick={() => onPage(page - 1)}
          >
            {t("organizations.previous")}
          </Button>
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("organizations.page")} {page} / {totalPages}
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={page >= totalPages}
            onClick={() => onPage(page + 1)}
          >
            {t("organizations.next")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
