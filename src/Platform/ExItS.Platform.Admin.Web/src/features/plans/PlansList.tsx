import { useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { planDetailHref } from "@/api/catalog/plan-list-query";
import {
  hasActivePlanFilters,
  parsePlanListSearchParams,
  planListSearchParams,
  type PlanListUrlState,
} from "@/api/catalog/plan-list-query";
import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import {
  PLAN_LIST_PAGE_SIZE,
  PLAN_LIST_SORT_BY,
  PLAN_STATUSES,
  type PlanListSortBy,
} from "@/api/catalog/plan-catalog-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAuthorizedCatalogProductsQuery } from "@/features/navigation/use-catalog-products-query";
import { usePlanListQuery } from "@/features/plans/use-plan-list-query";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<PlanListSortBy, MessageKey> = {
  Code: "plans.sort.code",
  DisplayName: "plans.sort.displayName",
  Status: "plans.sort.status",
  CreatedAtUtc: "plans.sort.created",
  UpdatedAtUtc: "plans.sort.updated",
};

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Inactive: "plans.status.Inactive",
  Retired: "plans.status.Retired",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") return "success";
  if (status === "Inactive") return "warning";
  if (status === "Retired") return "danger";
  return "neutral";
}

function formatInstant(value: string | undefined, language: string): string {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function formatMoney(
  value: number | undefined,
  currency: string | undefined,
  language: string,
): string {
  if (value === undefined) return "—";
  const code = currency && currency.length > 0 ? currency : "PHP";
  try {
    return new Intl.NumberFormat(language === "fil-PH" ? "fil-PH" : "en-PH", {
      style: "currency",
      currency: code,
    }).format(value);
  } catch {
    return `${value} ${code}`;
  }
}

export function PlansList({ enabled }: { enabled: boolean }) {
  const { t, language } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = parsePlanListSearchParams(searchParams);
  const productsQuery = useAuthorizedCatalogProductsQuery();
  const query = usePlanListQuery(
    {
      page: state.page,
      pageSize: PLAN_LIST_PAGE_SIZE,
      productCode: state.productCode || undefined,
      status: state.status || undefined,
      search: state.search || undefined,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    },
    enabled,
  );

  function replaceState(patch: Partial<PlanListUrlState>) {
    const current = parsePlanListSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(planListSearchParams({ ...current, ...patch }), { replace: true });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / PLAN_LIST_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load plan catalog" })
    : null;

  return (
    <div className="grid gap-3">
      <PlanFilterForm
        key={`${state.search}|${state.status}|${state.productCode}|${state.sortBy}|${state.sortDesc}`}
        search={state.search}
        status={state.status}
        productCode={state.productCode}
        sortBy={state.sortBy}
        sortDesc={state.sortDesc}
        products={productsQuery.data?.items ?? []}
        onSubmitSearch={(search) => replaceState({ search, page: 1 })}
        onReplace={replaceState}
      />

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("plans.loading")}
        >
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("plans.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <PlanResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActivePlanFilters(state)}
          showTable={showTable}
          language={language}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={() =>
            replaceState({
              page: 1,
              search: "",
              status: "",
              productCode: "",
              sortBy: "DisplayName",
              sortDesc: false,
            })
          }
        />
      ) : null}
    </div>
  );
}

function PlanFilterForm({
  search,
  status,
  productCode,
  sortBy,
  sortDesc,
  products,
  onSubmitSearch,
  onReplace,
}: {
  search: string;
  status: string;
  productCode: string;
  sortBy: PlanListSortBy;
  sortDesc: boolean;
  products: { code: string; displayName: string }[];
  onSubmitSearch: (search: string) => void;
  onReplace: (patch: Partial<PlanListUrlState>) => void;
}) {
  const { t } = usePreferences();
  const [searchDraft, setSearchDraft] = useState(search);

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmitSearch(searchDraft.trim());
  }

  return (
    <form
      className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[minmax(0,1fr)_minmax(8rem,11rem)_minmax(8rem,11rem)_minmax(8rem,11rem)_9rem_auto] md:items-end"
      onSubmit={onSearchSubmit}
    >
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="plan-list-search"
      >
        {t("plans.search")}
        <Input
          id="plan-list-search"
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
          placeholder={t("plans.searchPlaceholder")}
          name="search"
        />
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("plans.product")}
        <select
          className={controlClass}
          value={productCode}
          aria-label={t("plans.product")}
          onChange={(event) => onReplace({ productCode: event.target.value, page: 1 })}
        >
          <option value="">{t("plans.product.all")}</option>
          {products.map((product) => (
            <option key={product.code} value={product.code}>
              {product.displayName}
            </option>
          ))}
        </select>
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("plans.status")}
        <select
          className={controlClass}
          value={status}
          aria-label={t("plans.status")}
          onChange={(event) =>
            onReplace({ status: event.target.value as PlanListUrlState["status"], page: 1 })
          }
        >
          <option value="">{t("plans.status.all")}</option>
          {PLAN_STATUSES.map((item) => (
            <option key={item} value={item}>
              {STATUS_LABELS[item] ? t(STATUS_LABELS[item]!) : item}
            </option>
          ))}
        </select>
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("plans.sort")}
        <select
          className={controlClass}
          value={sortBy}
          aria-label={t("plans.sort")}
          onChange={(event) => onReplace({ sortBy: event.target.value as PlanListSortBy, page: 1 })}
        >
          {PLAN_LIST_SORT_BY.map((item) => (
            <option key={item} value={item}>
              {t(SORT_LABELS[item])}
            </option>
          ))}
        </select>
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("plans.sort.direction")}
        <select
          className={controlClass}
          value={sortDesc ? "desc" : "asc"}
          aria-label={t("plans.sort.direction")}
          onChange={(event) => onReplace({ sortDesc: event.target.value === "desc", page: 1 })}
        >
          <option value="asc">{t("plans.sort.asc")}</option>
          <option value="desc">{t("plans.sort.desc")}</option>
        </select>
      </label>
      <Button type="submit" className="min-h-[var(--exits-touch-target-min)]">
        {t("plans.searchSubmit")}
      </Button>
    </form>
  );
}

function PlanResults({
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
  items: CatalogPlan[];
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
          {filtered ? t("plans.zeroResult") : t("plans.empty")}
        </p>
        {filtered ? (
          <Button type="button" variant="outline" size="sm" className="mt-2" onClick={onReset}>
            {t("plans.reset")}
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
            caption={t("plans.caption")}
            empty={t("plans.empty")}
            columns={[
              {
                id: "displayName",
                header: t("plans.column.displayName"),
                cell: (plan) => (
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={planDetailHref(plan.id)}
                  >
                    {plan.displayName}
                  </Link>
                ),
              },
              {
                id: "code",
                header: t("plans.column.code"),
                cell: (plan) => (
                  <Link
                    className="font-mono text-primary hover:underline"
                    to={planDetailHref(plan.id)}
                  >
                    {plan.code}
                  </Link>
                ),
              },
              {
                id: "product",
                header: t("plans.column.product"),
                cell: (plan) => (
                  <span className="break-words text-muted">
                    {plan.productDisplayName ?? plan.productCode}
                  </span>
                ),
              },
              {
                id: "status",
                header: t("plans.column.status"),
                cell: (plan) => (
                  <StatusIndicator
                    tone={statusTone(plan.status)}
                    label={
                      STATUS_LABELS[plan.status] ? t(STATUS_LABELS[plan.status]!) : plan.status
                    }
                  />
                ),
              },
              {
                id: "monthlyPrice",
                header: t("plans.column.monthlyPrice"),
                cell: (plan) => (
                  <span className="tabular-nums text-muted">
                    {formatMoney(plan.monthlyPrice, plan.currencyCode, language)}
                  </span>
                ),
              },
              {
                id: "updated",
                header: t("plans.column.updated"),
                cell: (plan) => (
                  <span className="tabular-nums text-muted">
                    {formatInstant(plan.updatedAtUtc, language)}
                  </span>
                ),
              },
            ]}
            rows={items}
          />
        ) : (
          <ul className="grid gap-2">
            {items.map((plan) => (
              <li
                key={plan.id}
                className="rounded-[var(--exits-density-radius)] border border-border/80 px-2 py-2"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={planDetailHref(plan.id)}
                  >
                    {plan.displayName}
                  </Link>
                  <StatusIndicator
                    tone={statusTone(plan.status)}
                    label={
                      STATUS_LABELS[plan.status] ? t(STATUS_LABELS[plan.status]!) : plan.status
                    }
                  />
                </div>
                <p className="mt-1 font-mono text-[length:var(--exits-text-xs)] text-muted">
                  {plan.code}
                </p>
              </li>
            ))}
          </ul>
        )}
      </div>

      {totalCount > PLAN_LIST_PAGE_SIZE ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => onPage(page - 1)}
          >
            {t("plans.previous")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {t("plans.page")} {page} / {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => onPage(page + 1)}
          >
            {t("plans.next")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
