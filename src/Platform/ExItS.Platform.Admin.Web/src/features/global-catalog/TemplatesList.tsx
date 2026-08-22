import { useMemo, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  globalCatalogTemplateListSearchParams,
  hasActiveGlobalCatalogTemplateFilters,
  parseGlobalCatalogTemplateListSearchParams,
} from "@/api/global-catalog/template-list-query";
import {
  GLOBAL_CATALOG_TEMPLATE_LIST_PAGE_SIZE,
  GLOBAL_CATALOG_TEMPLATE_LIST_SORT_BY,
  GLOBAL_CATALOG_TEMPLATE_STATUSES,
  type GlobalCatalogTemplateListSortBy,
  type GlobalCatalogTemplateStatus,
  type GlobalCatalogTemplateSummary,
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
  globalCatalogFieldLabelClass,
  globalCatalogFilterFormClass,
  globalCatalogListShellClass,
  globalCatalogMobileCardClass,
  globalCatalogStatusTone,
  globalCatalogTableShellClass,
} from "@/features/global-catalog/global-catalog-presentation";
import { useGlobalBusinessTypesQuery } from "@/features/global-catalog/use-global-business-types-query";
import { useGlobalCatalogTemplateListQuery } from "@/features/global-catalog/use-global-template-queries";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<GlobalCatalogTemplateListSortBy, MessageKey> = {
  Name: "globalCatalog.sort.name",
  Slug: "globalCatalog.templates.sort.slug",
  Status: "globalCatalog.sort.status",
  PrimaryBusinessType: "globalCatalog.templates.sort.primaryBusinessType",
  UpdatedAtUtc: "globalCatalog.sort.updatedAtUtc",
  CreatedAtUtc: "globalCatalog.sort.createdAtUtc",
  ProductCount: "globalCatalog.templates.sort.productCount",
};

const STATUS_LABELS: Record<GlobalCatalogTemplateStatus, MessageKey> = {
  Draft: "globalCatalog.status.Draft",
  Published: "globalCatalog.templates.status.Published",
  Archived: "globalCatalog.status.Archived",
};

export function TemplatesList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const businessTypesQuery = useGlobalBusinessTypesQuery(enabled);
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(
    () => parseGlobalCatalogTemplateListSearchParams(searchParams),
    [searchParams],
  );
  const [searchDraft, setSearchDraft] = useState(state.search);
  const [appliedSearch, setAppliedSearch] = useState(state.search);
  if (state.search !== appliedSearch) {
    setAppliedSearch(state.search);
    setSearchDraft(state.search);
  }

  const query = useGlobalCatalogTemplateListQuery(
    {
      page: state.page,
      pageSize: GLOBAL_CATALOG_TEMPLATE_LIST_PAGE_SIZE,
      status: state.status || undefined,
      primaryBusinessTypeId: state.primaryBusinessTypeId || undefined,
      search: state.search || undefined,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    },
    enabled,
  );

  function replaceState(patch: Partial<typeof state>) {
    const current = parseGlobalCatalogTemplateListSearchParams(
      new URLSearchParams(window.location.search),
    );
    setSearchParams(globalCatalogTemplateListSearchParams({ ...current, ...patch }), { replace: true });
  }

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    replaceState({ page: 1, search: searchDraft.trim() });
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load catalog templates" })
    : null;

  const totalPages = query.data
    ? Math.max(
        1,
        Math.ceil(
          query.data.totalCount / (query.data.pageSize || GLOBAL_CATALOG_TEMPLATE_LIST_PAGE_SIZE),
        ),
      )
    : 1;
  const businessTypes = businessTypesQuery.data?.items ?? [];

  function resetFilters() {
    setSearchDraft("");
    replaceState({
      page: 1,
      search: "",
      status: "",
      primaryBusinessTypeId: "",
      sortBy: "Name",
      sortDesc: false,
    });
  }

  return (
    <div className={globalCatalogListShellClass}>
      <form
        className={`${globalCatalogFilterFormClass} md:grid-cols-[minmax(0,1fr)_minmax(10rem,12rem)_minmax(10rem,12rem)_10rem_9rem_auto]`}
        onSubmit={onSearchSubmit}
      >
        <label className={globalCatalogFieldLabelClass} htmlFor="gc-tpl-search">
          {t("globalCatalog.search")}
          <Input
            id="gc-tpl-search"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            placeholder={t("globalCatalog.templates.searchPlaceholder")}
            autoComplete="off"
          />
        </label>
        <label className={globalCatalogFieldLabelClass} htmlFor="gc-tpl-status">
          {t("globalCatalog.status")}
          <select
            id="gc-tpl-status"
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
            {GLOBAL_CATALOG_TEMPLATE_STATUSES.map((status) => (
              <option key={status} value={status}>
                {t(STATUS_LABELS[status])}
              </option>
            ))}
          </select>
        </label>
        <label className={globalCatalogFieldLabelClass} htmlFor="gc-tpl-business-type">
          {t("globalCatalog.businessType")}
          <select
            id="gc-tpl-business-type"
            className={globalCatalogControlClass}
            value={state.primaryBusinessTypeId}
            onChange={(event) => replaceState({ primaryBusinessTypeId: event.target.value, page: 1 })}
          >
            <option value="">{t("globalCatalog.businessType.all")}</option>
            {businessTypes.map((businessType) => (
              <option key={businessType.id} value={businessType.id}>
                {businessType.name}
              </option>
            ))}
          </select>
        </label>
        <label className={globalCatalogFieldLabelClass} htmlFor="gc-tpl-sort">
          {t("globalCatalog.sort")}
          <select
            id="gc-tpl-sort"
            className={globalCatalogControlClass}
            value={state.sortBy}
            onChange={(event) =>
              replaceState({ sortBy: event.target.value as GlobalCatalogTemplateListSortBy, page: 1 })
            }
          >
            {GLOBAL_CATALOG_TEMPLATE_LIST_SORT_BY.map((sortBy) => (
              <option key={sortBy} value={sortBy}>
                {t(SORT_LABELS[sortBy])}
              </option>
            ))}
          </select>
        </label>
        <label className={globalCatalogFieldLabelClass} htmlFor="gc-tpl-order">
          {t("globalCatalog.sort.direction")}
          <select
            id="gc-tpl-order"
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
          {hasActiveGlobalCatalogTemplateFilters(state) ? (
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
        <TemplateResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActiveGlobalCatalogTemplateFilters(state)}
          language={language}
          showTable={showTable}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={resetFilters}
        />
      ) : null}
    </div>
  );
}

function TemplateResults({
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
  items: GlobalCatalogTemplateSummary[];
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
    : t("globalCatalog.templates.listEmpty");

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
    <div className={globalCatalogListShellClass}>
      {showTable ? (
        <div className={globalCatalogTableShellClass}>
          <AdminTable
            caption={t("globalCatalog.templates.caption")}
            empty={emptyTitle}
            rows={items}
            columns={[
              {
                id: "name",
                header: t("globalCatalog.column.name"),
                cell: (item) => (
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={`/admin/global-catalog/templates/${item.id}`}
                  >
                    {item.name}
                  </Link>
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
                id: "productCount",
                header: t("globalCatalog.templates.column.productCount"),
                cell: (item) => <span className="tabular-nums">{item.productCount}</span>,
              },
              {
                id: "primaryBusinessType",
                header: t("globalCatalog.templates.column.primaryBusinessType"),
                cell: (item) => (
                  <span className="text-[length:var(--exits-text-sm)]">{item.primaryBusinessType}</span>
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
            ]}
          />
        </div>
      ) : (
        <ul className="grid gap-2">
          {items.map((item) => (
            <li key={item.id} className={globalCatalogMobileCardClass}>
              <TemplateCard item={item} language={language} />
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

function TemplateCard({
  item,
  language,
}: {
  item: GlobalCatalogTemplateSummary;
  language: string;
}) {
  const { t } = usePreferences();
  const updated = formatGlobalCatalogInstant(item.updatedAtUtc, language);

  return (
    <div className="grid gap-1">
      <Link
        className="font-medium text-primary hover:underline"
        to={`/admin/global-catalog/templates/${item.id}`}
      >
        {item.name}
      </Link>
      <p className="font-mono text-[length:var(--exits-text-xs)] text-muted">{item.slug}</p>
      <div className="flex flex-wrap gap-3 text-[length:var(--exits-text-xs)] text-muted">
        <span>
          {t("globalCatalog.templates.column.productCount")}: {item.productCount}
        </span>
        <span>{item.primaryBusinessType}</span>
        {updated ? <span>{updated}</span> : null}
      </div>
      <div className="mt-1.5">
        <StatusIndicator label={t(STATUS_LABELS[item.status])} tone={globalCatalogStatusTone(item.status)} />
      </div>
    </div>
  );
}
