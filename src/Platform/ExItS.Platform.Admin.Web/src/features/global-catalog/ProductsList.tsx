import { useMemo, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  globalProductListSearchParams,
  hasActiveGlobalProductFilters,
  parseGlobalProductListSearchParams,
} from "@/api/global-catalog/product-list-query";
import {
  GLOBAL_PRODUCT_LIST_PAGE_SIZE,
  GLOBAL_PRODUCT_LIST_SORT_BY,
  GLOBAL_PRODUCT_STATUSES,
  type GlobalProductListItem,
  type GlobalProductListSortBy,
  type GlobalProductStatus,
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
import { useGlobalCategoryLookupQuery } from "@/features/global-catalog/use-global-category-queries";
import { useGlobalProductListQuery } from "@/features/global-catalog/use-global-product-queries";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<GlobalProductListSortBy, MessageKey> = {
  Name: "globalCatalog.sort.name",
  Sku: "globalCatalog.sort.sku",
  Barcode: "globalCatalog.sort.barcode",
  Brand: "globalCatalog.sort.brand",
  Category: "globalCatalog.sort.category",
  Unit: "globalCatalog.sort.unit",
  Status: "globalCatalog.sort.status",
  UpdatedAtUtc: "globalCatalog.sort.updatedAtUtc",
  CreatedAtUtc: "globalCatalog.sort.createdAtUtc",
  CostPrice: "globalCatalog.sort.costPrice",
  SellingPrice: "globalCatalog.sort.sellingPrice",
};

const STATUS_LABELS: Record<GlobalProductStatus, MessageKey> = {
  Draft: "globalCatalog.status.Draft",
  Active: "globalCatalog.status.Active",
  Archived: "globalCatalog.status.Archived",
};

export function ProductsList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseGlobalProductListSearchParams(searchParams), [searchParams]);
  const [searchDraft, setSearchDraft] = useState(state.search);
  const [appliedSearch, setAppliedSearch] = useState(state.search);
  if (state.search !== appliedSearch) {
    setAppliedSearch(state.search);
    setSearchDraft(state.search);
  }

  const businessTypesQuery = useGlobalBusinessTypesQuery(enabled);
  const lookupQuery = useGlobalCategoryLookupQuery(enabled);
  const categoryNames = useMemo(() => {
    const map = new Map<string, string>();
    for (const item of lookupQuery.data?.items ?? []) {
      map.set(item.id, item.name);
    }
    return map;
  }, [lookupQuery.data?.items]);

  const query = useGlobalProductListQuery(
    {
      page: state.page,
      pageSize: GLOBAL_PRODUCT_LIST_PAGE_SIZE,
      status: state.status || undefined,
      categoryId: state.categoryId || undefined,
      businessTypeId: state.businessTypeId || undefined,
      search: state.search || undefined,
      barcode: state.barcode || undefined,
      sku: state.sku || undefined,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    },
    enabled,
  );

  function replaceState(patch: Partial<typeof state>) {
    const current = parseGlobalProductListSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(globalProductListSearchParams({ ...current, ...patch }), { replace: true });
  }

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    replaceState({ search: searchDraft.trim(), page: 1 });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / GLOBAL_PRODUCT_LIST_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load global products" })
    : null;

  return (
    <div className="grid gap-3">
      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[minmax(0,1fr)_minmax(9rem,11rem)_minmax(9rem,11rem)_minmax(8rem,10rem)_minmax(8rem,10rem)_10rem_9rem_auto] md:items-end"
        onSubmit={onSearchSubmit}
      >
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-product-search">
          {t("globalCatalog.search")}
          <Input
            id="gc-product-search"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            placeholder={t("globalCatalog.products.searchPlaceholder")}
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-product-status">
          {t("globalCatalog.status")}
          <select
            id="gc-product-status"
            className={globalCatalogControlClass}
            value={state.status}
            onChange={(event) =>
              replaceState({ status: event.target.value as typeof state.status, page: 1 })
            }
          >
            <option value="">{t("globalCatalog.status.all")}</option>
            {GLOBAL_PRODUCT_STATUSES.map((status) => (
              <option key={status} value={status}>
                {t(STATUS_LABELS[status])}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-product-category">
          {t("globalCatalog.column.category")}
          <select
            id="gc-product-category"
            className={globalCatalogControlClass}
            value={state.categoryId}
            disabled={lookupQuery.isPending}
            onChange={(event) => replaceState({ categoryId: event.target.value, page: 1 })}
          >
            <option value="">{t("globalCatalog.category.all")}</option>
            {(lookupQuery.data?.items ?? []).map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-product-business-type">
          {t("globalCatalog.businessType")}
          <select
            id="gc-product-business-type"
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
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-product-sku">
          {t("globalCatalog.column.sku")}
          <Input
            id="gc-product-sku"
            value={state.sku}
            onChange={(event) => replaceState({ sku: event.target.value, page: 1 })}
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-product-sort">
          {t("globalCatalog.sort")}
          <select
            id="gc-product-sort"
            className={globalCatalogControlClass}
            value={state.sortBy}
            onChange={(event) =>
              replaceState({ sortBy: event.target.value as GlobalProductListSortBy, page: 1 })
            }
          >
            {GLOBAL_PRODUCT_LIST_SORT_BY.map((sortBy) => (
              <option key={sortBy} value={sortBy}>
                {t(SORT_LABELS[sortBy])}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted" htmlFor="gc-product-order">
          {t("globalCatalog.sort.direction")}
          <select
            id="gc-product-order"
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
          {hasActiveGlobalProductFilters(state) ? (
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
                  categoryId: "",
                  businessTypeId: "",
                  barcode: "",
                  sku: "",
                  sortBy: "Name",
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
        <ProductResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActiveGlobalProductFilters(state)}
          language={language}
          showTable={showTable}
          categoryNames={categoryNames}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={() => {
            setSearchDraft("");
            replaceState({
              page: 1,
              search: "",
              status: "",
              categoryId: "",
              businessTypeId: "",
              barcode: "",
              sku: "",
              sortBy: "Name",
              sortDesc: false,
            });
          }}
        />
      ) : null}
    </div>
  );
}

function ProductResults({
  items,
  totalCount,
  page,
  totalPages,
  filtered,
  language,
  showTable,
  categoryNames,
  onPage,
  onReset,
}: {
  items: GlobalProductListItem[];
  totalCount: number;
  page: number;
  totalPages: number;
  filtered: boolean;
  language: string;
  showTable: boolean;
  categoryNames: Map<string, string>;
  onPage: (page: number) => void;
  onReset: () => void;
}) {
  const { t } = usePreferences();
  const emptyTitle = filtered ? t("globalCatalog.zeroResult") : t("globalCatalog.products.empty");

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
            caption={t("globalCatalog.products.caption")}
            empty={emptyTitle}
            columns={[
              {
                id: "name",
                header: t("globalCatalog.column.name"),
                cell: (product) => (
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={`/admin/global-catalog/products/${product.id}`}
                  >
                    {product.name}
                  </Link>
                ),
              },
              {
                id: "sku",
                header: t("globalCatalog.column.sku"),
                cell: (product) => (
                  <span className="font-mono text-[length:var(--exits-text-xs)] text-muted">
                    {product.sku}
                  </span>
                ),
              },
              {
                id: "category",
                header: t("globalCatalog.column.category"),
                cell: (product) =>
                  product.globalCategoryId
                    ? (categoryNames.get(product.globalCategoryId) ?? "—")
                    : "—",
              },
              {
                id: "status",
                header: t("globalCatalog.column.status"),
                cell: (product) => (
                  <StatusIndicator
                    tone={globalCatalogStatusTone(product.status)}
                    label={t(STATUS_LABELS[product.status])}
                  />
                ),
              },
              {
                id: "price",
                header: t("globalCatalog.column.sellingPrice"),
                cell: (product) => (
                  <span className="tabular-nums text-muted">
                    {product.sellingPrice != null ? product.sellingPrice.toFixed(2) : "—"}
                  </span>
                ),
              },
              {
                id: "updated",
                header: t("globalCatalog.column.updated"),
                cell: (product) => (
                  <span className="tabular-nums text-muted">
                    {formatGlobalCatalogInstant(product.updatedAtUtc, language) ?? "—"}
                  </span>
                ),
              },
            ]}
            rows={items}
          />
        </div>
      ) : (
        <ul className="grid gap-2">
          {items.map((product) => (
            <li
              key={product.id}
              className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
            >
              <p className="font-medium">{product.name}</p>
              <p className="mt-0.5 font-mono text-[length:var(--exits-text-xs)] text-muted">{product.sku}</p>
              <div className="mt-1.5 flex flex-wrap items-center gap-2">
                <StatusIndicator
                  tone={globalCatalogStatusTone(product.status)}
                  label={t(STATUS_LABELS[product.status])}
                />
                <Link
                  className="text-primary hover:underline"
                  to={`/admin/global-catalog/products/${product.id}`}
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
