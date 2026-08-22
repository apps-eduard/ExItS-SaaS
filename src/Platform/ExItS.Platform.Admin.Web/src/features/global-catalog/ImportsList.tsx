import { useMemo } from "react";
import { Link, useSearchParams } from "react-router-dom";

import {
  globalCatalogImportListSearchParams,
  hasActiveGlobalCatalogImportFilters,
  parseGlobalCatalogImportListSearchParams,
} from "@/api/global-catalog/import-list-query";
import {
  GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE,
  GLOBAL_CATALOG_IMPORT_STATUSES,
  type GlobalCatalogImportListItem,
  type GlobalCatalogImportStatus,
} from "@/api/global-catalog/global-catalog-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ForbiddenState } from "@/components/exits/ForbiddenState";
import { LoadingState } from "@/components/exits/LoadingState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { Button } from "@/components/ui/button";
import { isPlatformForbidden } from "@/api/platform-http-status";
import {
  formatGlobalCatalogInstant,
  globalCatalogControlClass,
  globalCatalogFieldLabelClass,
  globalCatalogFilterFormClass,
  globalCatalogImportStatusTone,
  globalCatalogListShellClass,
  globalCatalogMobileCardClass,
  globalCatalogTableShellClass,
} from "@/features/global-catalog/global-catalog-presentation";
import { useGlobalCatalogImportListQuery } from "@/features/global-catalog/use-global-import-queries";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<GlobalCatalogImportStatus, MessageKey> = {
  Validated: "globalCatalog.imports.status.Validated",
  Queued: "globalCatalog.imports.status.Queued",
  Processing: "globalCatalog.imports.status.Processing",
  Completed: "globalCatalog.imports.status.Completed",
  CompletedWithWarnings: "globalCatalog.imports.status.CompletedWithWarnings",
  Failed: "globalCatalog.imports.status.Failed",
};

export function ImportsList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(
    () => parseGlobalCatalogImportListSearchParams(searchParams),
    [searchParams],
  );

  const query = useGlobalCatalogImportListQuery(
    {
      page: state.page,
      pageSize: GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE,
      status: state.status || undefined,
    },
    enabled,
  );

  function replaceState(patch: Partial<typeof state>) {
    const current = parseGlobalCatalogImportListSearchParams(
      new URLSearchParams(window.location.search),
    );
    setSearchParams(globalCatalogImportListSearchParams({ ...current, ...patch }), { replace: true });
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load imports" })
    : null;

  const totalPages = query.data
    ? Math.max(
        1,
        Math.ceil(query.data.totalCount / (query.data.pageSize || GLOBAL_CATALOG_IMPORT_LIST_PAGE_SIZE)),
      )
    : 1;

  function resetFilters() {
    replaceState({ page: 1, status: "" });
  }

  return (
    <div className={globalCatalogListShellClass}>
      <div
        className={`${globalCatalogFilterFormClass} md:grid-cols-[minmax(10rem,14rem)_auto]`}
      >
        <label className={globalCatalogFieldLabelClass} htmlFor="gc-import-status">
          {t("globalCatalog.status")}
          <select
            id="gc-import-status"
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
            {GLOBAL_CATALOG_IMPORT_STATUSES.map((status) => (
              <option key={status} value={status}>
                {t(STATUS_LABELS[status])}
              </option>
            ))}
          </select>
        </label>
        <div className="flex flex-wrap items-end gap-2">
          {hasActiveGlobalCatalogImportFilters(state) ? (
            <Button type="button" size="sm" variant="outline" onClick={resetFilters}>
              {t("globalCatalog.reset")}
            </Button>
          ) : null}
        </div>
      </div>

      {query.isPending ? <LoadingState /> : null}

      {query.isError && isPlatformForbidden(query.error) ? <ForbiddenState /> : null}

      {query.isError && !isPlatformForbidden(query.error) && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("globalCatalog.imports.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <ImportResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActiveGlobalCatalogImportFilters(state)}
          language={language}
          showTable={showTable}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={resetFilters}
        />
      ) : null}
    </div>
  );
}

function ImportResults({
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
  items: GlobalCatalogImportListItem[];
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
    : t("globalCatalog.imports.listEmpty");

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
            caption={t("globalCatalog.imports.caption")}
            empty={emptyTitle}
            rows={items}
            columns={[
              {
                id: "fileName",
                header: t("globalCatalog.imports.column.fileName"),
                cell: (item) => (
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={`/admin/global-catalog/imports/${item.id}`}
                  >
                    {item.fileName}
                  </Link>
                ),
              },
              {
                id: "status",
                header: t("globalCatalog.column.status"),
                cell: (item) => (
                  <StatusIndicator
                    tone={globalCatalogImportStatusTone(item.status)}
                    label={t(STATUS_LABELS[item.status])}
                  />
                ),
              },
              {
                id: "totalCount",
                header: t("globalCatalog.imports.column.total"),
                cell: (item) => <span className="tabular-nums">{item.totalCount}</span>,
              },
              {
                id: "importedCount",
                header: t("globalCatalog.imports.column.imported"),
                cell: (item) => <span className="tabular-nums">{item.importedCount}</span>,
              },
              {
                id: "failedCount",
                header: t("globalCatalog.imports.column.failed"),
                cell: (item) => <span className="tabular-nums">{item.failedCount}</span>,
              },
              {
                id: "created",
                header: t("globalCatalog.column.created"),
                cell: (item) => (
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {formatGlobalCatalogInstant(item.createdAtUtc, language) ?? "—"}
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
              <ImportJobCard item={item} language={language} />
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

function ImportJobCard({
  item,
  language,
}: {
  item: GlobalCatalogImportListItem;
  language: string;
}) {
  const { t } = usePreferences();
  const created = formatGlobalCatalogInstant(item.createdAtUtc, language);

  return (
    <div className="grid gap-1">
      <Link
        className="font-medium text-primary hover:underline"
        to={`/admin/global-catalog/imports/${item.id}`}
      >
        {item.fileName}
      </Link>
      <div className="flex flex-wrap gap-3 text-[length:var(--exits-text-xs)] text-muted">
        <span>
          {t("globalCatalog.imports.column.total")}: {item.totalCount}
        </span>
        <span>
          {t("globalCatalog.imports.column.imported")}: {item.importedCount}
        </span>
        <span>
          {t("globalCatalog.imports.column.failed")}: {item.failedCount}
        </span>
        {created ? <span>{created}</span> : null}
      </div>
      <div className="mt-1.5">
        <StatusIndicator
          label={t(STATUS_LABELS[item.status])}
          tone={globalCatalogImportStatusTone(item.status)}
        />
      </div>
    </div>
  );
}
