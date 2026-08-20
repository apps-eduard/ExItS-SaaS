import { useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { productDetailHref } from "@/api/catalog/product-id";
import {
  hasActiveProductFilters,
  parseProductListSearchParams,
  productListSearchParams,
  type ProductListUrlState,
} from "@/api/catalog/product-list-query";
import type { CatalogProduct } from "@/api/catalog/product-catalog-client";
import {
  PRODUCT_LIST_PAGE_SIZE,
  PRODUCT_LIST_SORT_BY,
  PRODUCT_STATUSES,
  type ProductListSortBy,
} from "@/api/catalog/product-list-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useProductListQuery } from "@/features/products/use-product-list-query";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<ProductListSortBy, MessageKey> = {
  Code: "products.sort.code",
  DisplayName: "products.sort.displayName",
  Status: "products.sort.status",
  CreatedAtUtc: "products.sort.created",
  UpdatedAtUtc: "products.sort.updated",
};

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Inactive: "products.status.Inactive",
  Retired: "products.status.Retired",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Inactive") {
    return "warning";
  }
  if (status === "Retired") {
    return "danger";
  }
  return "neutral";
}

function formatInstant(value: string | undefined, language: string): string {
  if (!value) {
    return "—";
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

export function ProductsList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = parseProductListSearchParams(searchParams);
  const query = useProductListQuery(
    {
      page: state.page,
      pageSize: PRODUCT_LIST_PAGE_SIZE,
      status: state.status || undefined,
      search: state.search || undefined,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    },
    enabled,
  );

  function replaceState(patch: Partial<ProductListUrlState>) {
    const current = parseProductListSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(productListSearchParams({ ...current, ...patch }), { replace: true });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / PRODUCT_LIST_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load product catalog" })
    : null;

  return (
    <div className="grid gap-3">
      <ProductFilterForm
        key={`${state.search}|${state.status}|${state.sortBy}|${state.sortDesc}`}
        search={state.search}
        status={state.status}
        sortBy={state.sortBy}
        sortDesc={state.sortDesc}
        onSubmitSearch={(search) => replaceState({ search, page: 1 })}
        onReplace={replaceState}
      />

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("products.loading")}
        >
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("products.error")}
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
          filtered={hasActiveProductFilters(state)}
          showTable={showTable}
          language={language}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={() =>
            replaceState({
              page: 1,
              search: "",
              status: "",
              sortBy: "DisplayName",
              sortDesc: false,
            })
          }
        />
      ) : null}
    </div>
  );
}

function ProductFilterForm({
  search,
  status,
  sortBy,
  sortDesc,
  onSubmitSearch,
  onReplace,
}: {
  search: string;
  status: string;
  sortBy: ProductListSortBy;
  sortDesc: boolean;
  onSubmitSearch: (search: string) => void;
  onReplace: (patch: Partial<ProductListUrlState>) => void;
}) {
  const { t } = usePreferences();
  const [searchDraft, setSearchDraft] = useState(search);

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmitSearch(searchDraft.trim());
  }

  return (
    <form
      className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[minmax(0,1fr)_minmax(8rem,11rem)_minmax(8rem,11rem)_9rem_auto] md:items-end"
      onSubmit={onSearchSubmit}
    >
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="product-list-search"
      >
        {t("products.search")}
        <Input
          id="product-list-search"
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
          placeholder={t("products.searchPlaceholder")}
          name="search"
        />
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("products.status")}
        <select
          className={controlClass}
          value={status}
          aria-label={t("products.status")}
          onChange={(event) =>
            onReplace({ status: event.target.value as ProductListUrlState["status"], page: 1 })
          }
        >
          <option value="">{t("products.status.all")}</option>
          {PRODUCT_STATUSES.map((item) => (
            <option key={item} value={item}>
              {STATUS_LABELS[item] ? t(STATUS_LABELS[item]!) : item}
            </option>
          ))}
        </select>
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("products.sort")}
        <select
          className={controlClass}
          value={sortBy}
          aria-label={t("products.sort")}
          onChange={(event) =>
            onReplace({ sortBy: event.target.value as ProductListSortBy, page: 1 })
          }
        >
          {PRODUCT_LIST_SORT_BY.map((item) => (
            <option key={item} value={item}>
              {t(SORT_LABELS[item])}
            </option>
          ))}
        </select>
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("products.sort.direction")}
        <select
          className={controlClass}
          value={sortDesc ? "desc" : "asc"}
          aria-label={t("products.sort.direction")}
          onChange={(event) => onReplace({ sortDesc: event.target.value === "desc", page: 1 })}
        >
          <option value="asc">{t("products.sort.asc")}</option>
          <option value="desc">{t("products.sort.desc")}</option>
        </select>
      </label>
      <Button type="submit" className="min-h-[var(--exits-touch-target-min)]">
        {t("products.searchSubmit")}
      </Button>
    </form>
  );
}

function ProductResults({
  items,
  totalCount,
  page,
  totalPages,
  filtered,
  showTable,
  language,
  onPage,
  onReset,
}: {
  items: CatalogProduct[];
  totalCount: number;
  page: number;
  totalPages: number;
  filtered: boolean;
  showTable: boolean;
  language: string;
  onPage: (page: number) => void;
  onReset: () => void;
}) {
  const { t } = usePreferences();

  if (items.length === 0) {
    return (
      <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        <p className="text-[length:var(--exits-text-sm)] text-muted" role="status">
          {filtered ? t("products.zeroResult") : t("products.empty")}
        </p>
        {filtered ? (
          <Button type="button" variant="outline" size="sm" className="mt-2" onClick={onReset}>
            {t("products.reset")}
          </Button>
        ) : null}
      </div>
    );
  }

  return (
    <div className="grid gap-3">
      <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        {showTable ? (
          <AdminTable
            caption={t("products.caption")}
            empty={t("products.empty")}
            columns={[
              {
                id: "code",
                header: t("products.column.code"),
                cell: (product) => (
                  <Link className="font-mono text-primary hover:underline" to={productDetailHref(product.id)}>
                    {product.code}
                  </Link>
                ),
              },
              {
                id: "displayName",
                header: t("products.column.displayName"),
                cell: (product) => (
                  <Link className="font-medium text-primary hover:underline" to={productDetailHref(product.id)}>
                    {product.displayName}
                  </Link>
                ),
              },
              {
                id: "status",
                header: t("products.column.status"),
                cell: (product) => (
                  <StatusIndicator
                    tone={statusTone(product.status)}
                    label={
                      STATUS_LABELS[product.status]
                        ? t(STATUS_LABELS[product.status]!)
                        : product.status
                    }
                  />
                ),
              },
              {
                id: "updated",
                header: t("products.column.updated"),
                cell: (product) => (
                  <span className="tabular-nums text-muted">
                    {formatInstant(product.updatedAtUtc, language)}
                  </span>
                ),
              },
            ]}
            rows={items}
          />
        ) : (
          <ul className="grid gap-2">
            {items.map((product) => (
              <li
                key={product.id}
                className="rounded-[var(--exits-density-radius)] border border-border/80 px-2 py-2"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <Link className="font-medium text-primary hover:underline" to={productDetailHref(product.id)}>
                    {product.displayName}
                  </Link>
                  <StatusIndicator
                    tone={statusTone(product.status)}
                    label={
                      STATUS_LABELS[product.status]
                        ? t(STATUS_LABELS[product.status]!)
                        : product.status
                    }
                  />
                </div>
                <p className="mt-1 font-mono text-[length:var(--exits-text-xs)] text-muted">{product.code}</p>
              </li>
            ))}
          </ul>
        )}
      </div>

      {totalCount > PRODUCT_LIST_PAGE_SIZE ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="outline" size="sm" disabled={page <= 1} onClick={() => onPage(page - 1)}>
            {t("products.previous")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {t("products.page")} {page} / {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => onPage(page + 1)}
          >
            {t("products.next")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
