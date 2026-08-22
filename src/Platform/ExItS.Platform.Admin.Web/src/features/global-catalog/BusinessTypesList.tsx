import { useMemo, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  globalBusinessTypeListSearchParams,
  hasActiveGlobalBusinessTypeFilters,
  parseGlobalBusinessTypeListSearchParams,
} from "@/api/global-catalog/business-type-list-query";
import {
  GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE,
  GLOBAL_BUSINESS_TYPE_LIST_SORT_BY,
  GLOBAL_BUSINESS_TYPE_STATUSES,
  type GlobalBusinessTypeItem,
  type GlobalBusinessTypeListSortBy,
  type GlobalBusinessTypeStatus,
} from "@/api/global-catalog/global-catalog-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ForbiddenState } from "@/components/exits/ForbiddenState";
import { LoadingState } from "@/components/exits/LoadingState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { isPlatformForbidden } from "@/api/platform-http-status";
import {
  formatGlobalCatalogInstant,
  globalCatalogControlClass,
  globalCatalogStatusTone,
} from "@/features/global-catalog/global-catalog-presentation";
import { useGlobalBusinessTypeListQuery } from "@/features/global-catalog/use-global-business-type-queries";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<GlobalBusinessTypeListSortBy, MessageKey> = {
  Name: "globalCatalog.sort.name",
  Code: "globalCatalog.sort.code",
  SortOrder: "globalCatalog.sort.sortOrder",
  Status: "globalCatalog.sort.status",
  UpdatedAtUtc: "globalCatalog.sort.updatedAtUtc",
  CreatedAtUtc: "globalCatalog.sort.createdAtUtc",
};

const STATUS_LABELS: Record<GlobalBusinessTypeStatus, MessageKey> = {
  Active: "globalCatalog.status.Active",
  Inactive: "globalCatalog.status.Inactive",
  Archived: "globalCatalog.status.Archived",
};

export function BusinessTypesList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(
    () => parseGlobalBusinessTypeListSearchParams(searchParams),
    [searchParams],
  );
  const [searchDraft, setSearchDraft] = useState(state.search);
  const [appliedSearch, setAppliedSearch] = useState(state.search);
  if (state.search !== appliedSearch) {
    setAppliedSearch(state.search);
    setSearchDraft(state.search);
  }

  const query = useGlobalBusinessTypeListQuery(
    {
      page: state.page,
      pageSize: GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE,
      status: state.status || undefined,
      search: state.search || undefined,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    },
    enabled,
  );

  function replaceState(patch: Partial<typeof state>) {
    const current = parseGlobalBusinessTypeListSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(globalBusinessTypeListSearchParams({ ...current, ...patch }), { replace: true });
  }

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    replaceState({ page: 1, search: searchDraft.trim() });
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load business types" })
    : null;

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / (query.data.pageSize || GLOBAL_BUSINESS_TYPE_LIST_PAGE_SIZE)))
    : 1;

  function resetFilters() {
    setSearchDraft("");
    replaceState({ page: 1, search: "", status: "", sortBy: "SortOrder", sortDesc: false });
  }

  return (
    <div className="grid gap-4">
      <form
        className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 md:grid-cols-2 xl:grid-cols-4"
        onSubmit={onSearchSubmit}
      >
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-bt-search">
          {t("globalCatalog.search")}
          <Input
            id="gc-bt-search"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            placeholder={t("globalCatalog.businessTypes.searchPlaceholder")}
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-bt-status">
          {t("globalCatalog.status")}
          <select
            id="gc-bt-status"
            className={globalCatalogControlClass}
            value={state.status}
            onChange={(event) =>
              replaceState({
                status: event.target.value as typeof state.status,
                page: 1,
              })
            }
          >
            <option value="">{t("globalCatalog.status.all")}</option>
            {GLOBAL_BUSINESS_TYPE_STATUSES.map((status) => (
              <option key={status} value={status}>
                {t(STATUS_LABELS[status])}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-bt-sort">
          {t("globalCatalog.sort")}
          <select
            id="gc-bt-sort"
            className={globalCatalogControlClass}
            value={state.sortBy}
            onChange={(event) =>
              replaceState({ sortBy: event.target.value as GlobalBusinessTypeListSortBy, page: 1 })
            }
          >
            {GLOBAL_BUSINESS_TYPE_LIST_SORT_BY.map((sortBy) => (
              <option key={sortBy} value={sortBy}>
                {t(SORT_LABELS[sortBy])}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-bt-order">
          {t("globalCatalog.sort.direction")}
          <select
            id="gc-bt-order"
            className={globalCatalogControlClass}
            value={state.sortDesc ? "desc" : "asc"}
            onChange={(event) =>
              replaceState({ sortDesc: event.target.value === "desc", page: 1 })
            }
          >
            <option value="asc">{t("globalCatalog.sort.asc")}</option>
            <option value="desc">{t("globalCatalog.sort.desc")}</option>
          </select>
        </label>
        <div className="flex flex-wrap gap-2 md:col-span-2 xl:col-span-4">
          <Button type="submit" size="sm">
            {t("globalCatalog.searchSubmit")}
          </Button>
          {hasActiveGlobalBusinessTypeFilters(state) ? (
            <Button type="button" size="sm" variant="outline" onClick={resetFilters}>
              {t("globalCatalog.reset")}
            </Button>
          ) : null}
        </div>
      </form>

      {query.isPending ? <LoadingState /> : null}

      {query.isError && isPlatformForbidden(query.error) ? <ForbiddenState /> : null}

      {query.isError && !isPlatformForbidden(query.error) && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("globalCatalog.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <BusinessTypeResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActiveGlobalBusinessTypeFilters(state)}
          language={language}
          showTable={showTable}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={resetFilters}
        />
      ) : null}
    </div>
  );
}

function BusinessTypeResults({
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
  items: GlobalBusinessTypeItem[];
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
  const emptyTitle = filtered
    ? t("globalCatalog.zeroResult")
    : t("globalCatalog.businessTypes.listEmpty");

  if (items.length === 0) {
    return (
      <EmptyState
        title={emptyTitle}
        actionLabel={filtered ? t("globalCatalog.reset") : undefined}
        onAction={filtered ? onReset : undefined}
      />
    );
  }

  return (
    <div className="grid gap-4">
      {showTable ? (
        <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <AdminTable
            caption={t("globalCatalog.businessTypes.caption")}
            empty={emptyTitle}
            rows={items}
            columns={[
              {
                id: "name",
                header: t("globalCatalog.column.name"),
                cell: (item) => (
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={`/admin/global-catalog/business-types/${item.id}`}
                  >
                    {item.name}
                  </Link>
                ),
              },
              {
                id: "code",
                header: t("globalCatalog.column.code"),
                cell: (item) => (
                  <span className="font-mono text-[length:var(--exits-text-sm)]">{item.code}</span>
                ),
              },
              {
                id: "status",
                header: t("globalCatalog.column.status"),
                cell: (item) => (
                  <StatusIndicator
                    tone={globalCatalogStatusTone(item.status)}
                    label={t(STATUS_LABELS[item.status])}
                  />
                ),
              },
              {
                id: "sortOrder",
                header: t("globalCatalog.column.sortOrder"),
                cell: (item) => <span className="tabular-nums text-muted">{item.sortOrder}</span>,
              },
              {
                id: "description",
                header: t("globalCatalog.column.description"),
                cell: (item) => (
                  <span className="max-w-xs truncate text-[length:var(--exits-text-sm)] text-muted">
                    {item.description ?? "—"}
                  </span>
                ),
              },
              {
                id: "updated",
                header: t("globalCatalog.column.updated"),
                cell: (item) => (
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {formatGlobalCatalogInstant(item.updatedAtUtc, language) ?? "—"}
                  </span>
                ),
              },
              {
                id: "actions",
                header: t("globalCatalog.open"),
                cell: (item) => (
                  <Button asChild size="sm" variant="outline">
                    <Link to={`/admin/global-catalog/business-types/${item.id}`}>{t("globalCatalog.open")}</Link>
                  </Button>
                ),
              },
            ]}
          />
        </div>
      ) : (
        <ul className="grid gap-3">
          {items.map((item) => (
            <li
              key={item.id}
              className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-4"
            >
              <BusinessTypeCard item={item} language={language} />
            </li>
          ))}
        </ul>
      )}

      {totalCount > 0 && totalPages > 1 ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={page <= 1}
            onClick={() => onPage(page - 1)}
          >
            {t("globalCatalog.previous")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {t("globalCatalog.page")} {page} / {totalPages}
          </span>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={page >= totalPages}
            onClick={() => onPage(page + 1)}
          >
            {t("globalCatalog.next")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}

function BusinessTypeCard({
  item,
  language,
}: {
  item: GlobalBusinessTypeItem;
  language: string;
}) {
  const { t } = usePreferences();
  const updated = formatGlobalCatalogInstant(item.updatedAtUtc, language);

  return (
    <div className="grid gap-2">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <Link className="font-medium text-primary hover:underline" to={`/admin/global-catalog/business-types/${item.id}`}>
            {item.name}
          </Link>
          <p className="font-mono text-[length:var(--exits-text-xs)] text-muted">{item.code}</p>
        </div>
        <StatusIndicator label={t(STATUS_LABELS[item.status])} tone={globalCatalogStatusTone(item.status)} />
      </div>
      {item.description ? (
        <p className="text-[length:var(--exits-text-sm)] text-muted">{item.description}</p>
      ) : null}
      <div className="flex flex-wrap items-center justify-between gap-2 text-[length:var(--exits-text-xs)] text-muted">
        <span>
          {t("globalCatalog.column.sortOrder")}: {item.sortOrder}
        </span>
        {updated ? <span>{updated}</span> : null}
      </div>
      <Button asChild size="sm" variant="outline" className="w-fit">
        <Link to={`/admin/global-catalog/business-types/${item.id}`}>{t("globalCatalog.open")}</Link>
      </Button>
    </div>
  );
}
