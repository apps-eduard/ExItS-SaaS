import { useMemo, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  globalCategoryListSearchParams,
  hasActiveGlobalCategoryFilters,
  parseGlobalCategoryListSearchParams,
} from "@/api/global-catalog/category-list-query";
import {
  GLOBAL_CATEGORY_LIST_PAGE_SIZE,
  GLOBAL_CATEGORY_LIST_SORT_BY,
  GLOBAL_CATEGORY_STATUSES,
  type GlobalCategoryListItem,
  type GlobalCategoryListSortBy,
  type GlobalCategoryStatus,
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
import { useGlobalBusinessTypesQuery } from "@/features/global-catalog/use-global-business-types-query";
import {
  useGlobalCategoryListQuery,
  useGlobalCategoryLookupQuery,
} from "@/features/global-catalog/use-global-category-queries";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<GlobalCategoryListSortBy, MessageKey> = {
  Name: "globalCatalog.sort.name",
  SortOrder: "globalCatalog.sort.sortOrder",
  Status: "globalCatalog.sort.status",
  UpdatedAtUtc: "globalCatalog.sort.updatedAtUtc",
  CreatedAtUtc: "globalCatalog.sort.createdAtUtc",
};

const STATUS_LABELS: Record<GlobalCategoryStatus, MessageKey> = {
  Active: "globalCatalog.status.Active",
  Inactive: "globalCatalog.status.Inactive",
  Archived: "globalCatalog.status.Archived",
};

export function CategoriesList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(
    () => parseGlobalCategoryListSearchParams(searchParams),
    [searchParams],
  );
  const [searchDraft, setSearchDraft] = useState(state.search);
  const [appliedSearch, setAppliedSearch] = useState(state.search);
  if (state.search !== appliedSearch) {
    setAppliedSearch(state.search);
    setSearchDraft(state.search);
  }

  const businessTypesQuery = useGlobalBusinessTypesQuery(enabled);
  const lookupQuery = useGlobalCategoryLookupQuery(enabled);
  const parentNames = useMemo(() => {
    const map = new Map<string, string>();
    for (const item of lookupQuery.data?.items ?? []) {
      map.set(item.id, item.name);
    }
    return map;
  }, [lookupQuery.data?.items]);

  const query = useGlobalCategoryListQuery(
    {
      page: state.page,
      pageSize: GLOBAL_CATEGORY_LIST_PAGE_SIZE,
      status: state.status || undefined,
      parentId: state.parentId || undefined,
      businessTypeId: state.businessTypeId || undefined,
      search: state.search || undefined,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    },
    enabled,
  );

  function replaceState(patch: Partial<typeof state>) {
    const current = parseGlobalCategoryListSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(globalCategoryListSearchParams({ ...current, ...patch }), { replace: true });
  }

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    replaceState({ search: searchDraft.trim(), page: 1 });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / GLOBAL_CATEGORY_LIST_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load global categories" })
    : null;

  return (
    <div className="grid gap-3">
      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[minmax(0,1fr)_minmax(10rem,12rem)_minmax(10rem,12rem)_10rem_9rem_auto] md:items-end"
        onSubmit={onSearchSubmit}
      >
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-category-search">
          {t("globalCatalog.search")}
          <Input
            id="gc-category-search"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            placeholder={t("globalCatalog.searchPlaceholder")}
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-category-status">
          {t("globalCatalog.status")}
          <select
            id="gc-category-status"
            className={globalCatalogControlClass}
            value={state.status}
            onChange={(event) =>
              replaceState({ status: event.target.value as typeof state.status, page: 1 })
            }
          >
            <option value="">{t("globalCatalog.status.all")}</option>
            {GLOBAL_CATEGORY_STATUSES.map((status) => (
              <option key={status} value={status}>
                {t(STATUS_LABELS[status])}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-category-business-type">
          {t("globalCatalog.businessType")}
          <select
            id="gc-category-business-type"
            className={globalCatalogControlClass}
            value={state.businessTypeId}
            disabled={businessTypesQuery.isPending}
            onChange={(event) => replaceState({ businessTypeId: event.target.value, page: 1 })}
          >
            <option value="">{t("globalCatalog.businessType.all")}</option>
            {(businessTypesQuery.data?.items ?? []).map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-category-sort">
          {t("globalCatalog.sort")}
          <select
            id="gc-category-sort"
            className={globalCatalogControlClass}
            value={state.sortBy}
            onChange={(event) =>
              replaceState({ sortBy: event.target.value as GlobalCategoryListSortBy, page: 1 })
            }
          >
            {GLOBAL_CATEGORY_LIST_SORT_BY.map((sortBy) => (
              <option key={sortBy} value={sortBy}>
                {t(SORT_LABELS[sortBy])}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-category-order">
          {t("globalCatalog.sort.direction")}
          <select
            id="gc-category-order"
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
        <div className="flex flex-wrap gap-2">
          <Button type="submit" size="sm">
            {t("globalCatalog.searchSubmit")}
          </Button>
          {hasActiveGlobalCategoryFilters(state) ? (
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
                  parentId: "",
                  businessTypeId: "",
                  sortBy: "SortOrder",
                  sortDesc: false,
                });
              }}
            >
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
        <CategoryResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActiveGlobalCategoryFilters(state)}
          language={language}
          showTable={showTable}
          parentNames={parentNames}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={() => {
            setSearchDraft("");
            replaceState({
              page: 1,
              search: "",
              status: "",
              parentId: "",
              businessTypeId: "",
              sortBy: "SortOrder",
              sortDesc: false,
            });
          }}
        />
      ) : null}
    </div>
  );
}

function CategoryResults({
  items,
  totalCount,
  page,
  totalPages,
  filtered,
  language,
  showTable,
  parentNames,
  onPage,
  onReset,
}: {
  items: GlobalCategoryListItem[];
  totalCount: number;
  page: number;
  totalPages: number;
  filtered: boolean;
  language: string;
  showTable: boolean;
  parentNames: Map<string, string>;
  onPage: (page: number) => void;
  onReset: () => void;
}) {
  const { t } = usePreferences();
  const emptyTitle = filtered ? t("globalCatalog.zeroResult") : t("globalCatalog.categories.empty");

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
    <div className="grid gap-3">
      {showTable ? (
        <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <AdminTable
            caption={t("globalCatalog.categories.caption")}
            empty={emptyTitle}
            columns={[
              {
                id: "name",
                header: t("globalCatalog.column.name"),
                cell: (category) => (
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={`/admin/global-catalog/categories/${category.id}`}
                  >
                    {category.name}
                  </Link>
                ),
              },
              {
                id: "parent",
                header: t("globalCatalog.column.parent"),
                cell: (category) =>
                  category.parentId
                    ? (parentNames.get(category.parentId) ?? category.parentId)
                    : "—",
              },
              {
                id: "status",
                header: t("globalCatalog.column.status"),
                cell: (category) => (
                  <StatusIndicator
                    tone={globalCatalogStatusTone(category.status)}
                    label={t(STATUS_LABELS[category.status])}
                  />
                ),
              },
              {
                id: "sortOrder",
                header: t("globalCatalog.column.sortOrder"),
                cell: (category) => (
                  <span className="tabular-nums text-muted">{category.sortOrder}</span>
                ),
              },
              {
                id: "updated",
                header: t("globalCatalog.column.updated"),
                cell: (category) => (
                  <span className="tabular-nums text-muted">
                    {formatGlobalCatalogInstant(category.updatedAtUtc, language) ?? "—"}
                  </span>
                ),
              },
            ]}
            rows={items}
          />
        </div>
      ) : (
        <ul className="grid gap-2">
          {items.map((category) => (
            <li
              key={category.id}
              className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
            >
              <p className="font-medium">{category.name}</p>
              <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                {category.parentId
                  ? `${t("globalCatalog.column.parent")}: ${parentNames.get(category.parentId) ?? category.parentId}`
                  : t("globalCatalog.parent.root")}
              </p>
              <div className="mt-1.5 flex flex-wrap items-center gap-2">
                <StatusIndicator
                  tone={globalCatalogStatusTone(category.status)}
                  label={t(STATUS_LABELS[category.status])}
                />
                <Link
                  className="text-primary hover:underline"
                  to={`/admin/global-catalog/categories/${category.id}`}
                >
                  {t("globalCatalog.open")}
                </Link>
              </div>
            </li>
          ))}
        </ul>
      )}

      {totalCount > 0 ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" size="sm" variant="outline" disabled={page <= 1} onClick={() => onPage(page - 1)}>
            {t("globalCatalog.previous")}
          </Button>
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("globalCatalog.page")} {page} / {totalPages}
          </p>
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
