import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus, Tags } from "lucide-react";
import { canManageExpenses } from "@/access/pos-capabilities";
import {
  expenseWorkspaceScope,
  getExpenseScopeOptions,
  getExpenseSummary,
  listExpenses,
} from "@/api/pos/pos-expense-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  expensePaymentLabelKey,
  expenseStatusLabelKey,
  formatExpenseDate,
} from "@/features/expenses/expense-labels";
import {
  defaultExpenseViewScope,
  expenseViewScopeSelectValue,
  expenseViewScopeToQuery,
  parseExpenseViewScopeSelectValue,
  shouldShowExpenseViewScopeSelector,
  type ExpenseViewScopeSelection,
} from "@/features/expenses/expense-scope";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

function expenseSharePercent(amount: number, total: number): number {
  if (!(total > 0) || !(amount >= 0) || !Number.isFinite(amount) || !Number.isFinite(total)) {
    return 0;
  }
  return Math.round((amount / total) * 1000) / 10;
}

function formatExpenseSharePercent(percent: number): string {
  return percent.toLocaleString("en-PH", {
    minimumFractionDigits: percent % 1 === 0 ? 0 : 1,
    maximumFractionDigits: 1,
  });
}

export function ExpenseListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageExpenses(sessionGrant);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("");
  const [expenseNumber, setExpenseNumber] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [viewScope, setViewScope] = useState<ExpenseViewScopeSelection | null>(null);

  const organizationId = boundWorkspace?.organizationId ?? null;
  const currentBranchId = boundWorkspace?.branchId ?? null;
  const workspace = useMemo(
    () => (organizationId ? expenseWorkspaceScope(organizationId, currentBranchId) : null),
    [organizationId, currentBranchId],
  );

  useEffect(() => {
    setPage(1);
    setStatus("");
    setPaymentMethod("");
    setExpenseNumber("");
    setFromDate("");
    setToDate("");
    setViewScope(null);
  }, [organizationId]);

  const scopeOptionsQuery = useQuery({
    queryKey: ["expenses-scope-options", organizationId],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) => getExpenseScopeOptions(workspace!, signal),
    staleTime: 60_000,
  });

  useEffect(() => {
    if (!scopeOptionsQuery.data || viewScope) {
      return;
    }
    setViewScope(defaultExpenseViewScope(scopeOptionsQuery.data, currentBranchId));
  }, [scopeOptionsQuery.data, currentBranchId, viewScope]);

  const scopeQuery = viewScope ? expenseViewScopeToQuery(viewScope) : null;
  const showScopeSelector =
    scopeOptionsQuery.data != null && shouldShowExpenseViewScopeSelector(scopeOptionsQuery.data);

  const listQuery = useQuery({
    queryKey: [
      "expenses",
      organizationId,
      page,
      status,
      paymentMethod,
      expenseNumber,
      fromDate,
      toDate,
      scopeQuery?.scope,
      scopeQuery?.branchId,
    ],
    enabled: Boolean(workspace) && online && Boolean(scopeQuery),
    queryFn: ({ signal }) =>
      listExpenses(
        workspace!,
        {
          page,
          pageSize: PAGE_SIZE,
          status: status || undefined,
          paymentMethod: paymentMethod || undefined,
          expenseNumber: expenseNumber.trim() || undefined,
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
          scope: scopeQuery!.scope,
          branchId: scopeQuery!.branchId,
        },
        signal,
      ),
  });

  const summaryQuery = useQuery({
    queryKey: [
      "expenses-summary",
      organizationId,
      fromDate,
      toDate,
      scopeQuery?.scope,
      scopeQuery?.branchId,
    ],
    enabled: Boolean(workspace) && online && Boolean(scopeQuery),
    queryFn: ({ signal }) =>
      getExpenseSummary(
        workspace!,
        {
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
          scope: scopeQuery!.scope,
          branchId: scopeQuery!.branchId,
        },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const items = listQuery.data?.items ?? [];
  const totalCount = listQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const summary = summaryQuery.data;
  const filtersActive = Boolean(status || paymentMethod || expenseNumber.trim() || fromDate || toDate);

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="expense-list-page">
      <PageHeader
        title={t("expense.title")}
        description={t("expense.orgScopeNote")}
        backTo={pageBackNav.more.to}
        backLabel={t(pageBackNav.more.labelKey)}
        backTestId="page-header-back-more"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("expense.offline")}</p>
      ) : null}

      {allowManage ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("expense.title")}
          testId="expense-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "record",
              label: t("expense.recordExpense"),
              icon: <Plus />,
              href: online ? "/expenses/new" : undefined,
              disabled: !online,
              testId: "expense-new",
              emphasis: "primary",
            },
            {
              key: "categories",
              label: t("expense.categories"),
              icon: <Tags />,
              href: "/expenses/categories",
              testId: "expense-open-categories",
            },
          ]}
        />
      ) : (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("expense.categories")}
          testId="expense-toolbar-readonly"
          items={[
            {
              key: "categories",
              label: t("expense.categories"),
              icon: <Tags />,
              href: "/expenses/categories",
              testId: "expense-open-categories",
            },
          ]}
        />
      )}

      {showScopeSelector && viewScope && scopeOptionsQuery.data ? (
        <label
          className="flex max-w-sm min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]"
          data-testid="expense-scope-control"
        >
          <span className="font-semibold">{t("expense.scope.label")}</span>
          <select
            className="exits-select"
            value={expenseViewScopeSelectValue(viewScope)}
            onChange={(e) => {
              const next = parseExpenseViewScopeSelectValue(e.target.value);
              if (next) {
                setViewScope(next);
                setPage(1);
              }
            }}
            data-testid="expense-scope-select"
          >
            {scopeOptionsQuery.data.branches.map((branch) => (
              <option key={branch.branchId} value={`branch:${branch.branchId}`}>
                {branch.name}
              </option>
            ))}
            {scopeOptionsQuery.data.canViewAllBranches ? (
              <option value="allBranches">{t("expense.scope.allBranches")}</option>
            ) : null}
            {scopeOptionsQuery.data.canViewOrganization ? (
              <option value="organization">{t("expense.scope.organization")}</option>
            ) : null}
            {scopeOptionsQuery.data.canViewAllExpenses ? (
              <option value="allExpenses">{t("expense.scope.allExpenses")}</option>
            ) : null}
          </select>
        </label>
      ) : null}

      {summary ? (
        <div
          className="grid gap-2 sm:grid-cols-3"
          data-testid="expense-summary-cards"
        >
          <div className="rounded-[var(--exits-radius-md)] border border-[var(--exits-border-strong)] bg-[var(--exits-surface-elevated)] p-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("expense.summary.net")}
            </p>
            <p className="mt-1 mb-0 text-[length:var(--exits-text-lg)] font-semibold" data-testid="expense-summary-net">
              <MoneyDisplay amount={summary.netTotal} />
            </p>
          </div>
          <div className="rounded-[var(--exits-radius-md)] border border-[var(--exits-border-strong)] bg-[var(--exits-surface-elevated)] p-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("expense.summary.gross")}
            </p>
            <p className="mt-1 mb-0 font-semibold" data-testid="expense-summary-gross">
              <MoneyDisplay amount={summary.grossTotal} />
            </p>
            <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("expense.summary.recordedCount").replace("{count}", String(summary.recordedCount))}
            </p>
          </div>
          <div className="rounded-[var(--exits-radius-md)] border border-[var(--exits-border-strong)] bg-[var(--exits-surface-elevated)] p-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("expense.summary.voided")}
            </p>
            <p className="mt-1 mb-0 font-semibold" data-testid="expense-summary-voided">
              <MoneyDisplay amount={summary.voidedTotal} />
            </p>
            <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("expense.summary.voidedCount").replace("{count}", String(summary.voidedCount))}
            </p>
          </div>
        </div>
      ) : null}

      {summary && (summary.byCategory.length > 0 || summary.byPaymentMethod.length > 0) ? (
        <div
          className="grid grid-cols-1 gap-3 lg:grid-cols-3"
          data-testid="expense-summary-breakdowns"
        >
          {summary.byCategory.length > 0 ? (
            <section
              data-testid="expense-summary-by-category"
              className="flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-md)] border border-[var(--exits-border-strong)] bg-[var(--exits-surface-elevated)] p-3 lg:col-span-2"
            >
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("expense.summary.byCategory")}
              </h2>
              <ul className="m-0 flex list-none flex-col gap-3 p-0">
                {summary.byCategory.map((row) => {
                  const share = expenseSharePercent(row.totalAmount, summary.grossTotal);
                  const label = row.categoryName ?? t("expense.category.unknown");
                  return (
                    <li key={row.categoryId} className="min-w-0">
                      <div className="flex items-baseline justify-between gap-3 text-[length:var(--exits-text-sm)]">
                        <span className="min-w-0 truncate font-medium">{label}</span>
                        <span className="shrink-0 font-semibold tabular-nums">
                          <MoneyDisplay amount={row.totalAmount} />
                        </span>
                      </div>
                      <div className="mt-1.5 flex items-center gap-2">
                        <div
                          className="h-1.5 min-w-0 flex-1 overflow-hidden rounded-full bg-[var(--exits-surface-muted)]"
                          role="presentation"
                        >
                          <div
                            className="h-full rounded-full bg-[var(--exits-primary)]"
                            style={{ width: `${Math.min(100, Math.max(0, share))}%` }}
                          />
                        </div>
                        <span
                          className="w-12 shrink-0 text-right text-[length:var(--exits-text-xs)] tabular-nums text-muted"
                          aria-label={t("expense.summary.shareOfTotal").replace(
                            "{percent}",
                            formatExpenseSharePercent(share),
                          )}
                        >
                          {formatExpenseSharePercent(share)}%
                        </span>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </section>
          ) : null}

          {summary.byPaymentMethod.length > 0 ? (
            <section
              data-testid="expense-summary-by-payment"
              className={cn(
                "flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-md)] border border-[var(--exits-border-strong)] bg-[var(--exits-surface-elevated)] p-3",
                summary.byCategory.length === 0 && "lg:col-span-3",
              )}
            >
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("expense.summary.byPayment")}
              </h2>
              <ul className="m-0 flex list-none flex-col gap-3 p-0">
                {summary.byPaymentMethod.map((row) => {
                  const share = expenseSharePercent(row.totalAmount, summary.grossTotal);
                  return (
                    <li key={row.paymentMethod} className="min-w-0">
                      <div className="flex items-baseline justify-between gap-3 text-[length:var(--exits-text-sm)]">
                        <span className="min-w-0 truncate font-medium">
                          {t(expensePaymentLabelKey(row.paymentMethod))}
                        </span>
                        <span className="shrink-0 font-semibold tabular-nums">
                          <MoneyDisplay amount={row.totalAmount} />
                        </span>
                      </div>
                      <div className="mt-1.5 flex items-center gap-2">
                        <div
                          className="h-1.5 min-w-0 flex-1 overflow-hidden rounded-full bg-[var(--exits-surface-muted)]"
                          role="presentation"
                        >
                          <div
                            className="h-full rounded-full bg-[var(--exits-primary)]"
                            style={{ width: `${Math.min(100, Math.max(0, share))}%` }}
                          />
                        </div>
                        <span
                          className="w-12 shrink-0 text-right text-[length:var(--exits-text-xs)] tabular-nums text-muted"
                          aria-label={t("expense.summary.shareOfTotal").replace(
                            "{percent}",
                            formatExpenseSharePercent(share),
                          )}
                        >
                          {formatExpenseSharePercent(share)}%
                        </span>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </section>
          ) : null}
        </div>
      ) : null}

      <div
        className="grid gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3 sm:grid-cols-2 lg:grid-cols-3"
        data-testid="expense-filters"
      >
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("expense.filter.status")}</span>
          <select
            className="exits-select"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
            data-testid="expense-filter-status"
          >
            <option value="">{t("expense.filter.all")}</option>
            <option value="Recorded">{t("expense.status.recorded")}</option>
            <option value="Voided">{t("expense.status.voided")}</option>
          </select>
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("expense.filter.payment")}</span>
          <select
            className="exits-select"
            value={paymentMethod}
            onChange={(e) => {
              setPaymentMethod(e.target.value);
              setPage(1);
            }}
            data-testid="expense-filter-payment"
          >
            <option value="">{t("expense.filter.all")}</option>
            <option value="Cash">{t("expense.payment.cash")}</option>
            <option value="ManualGCash">{t("expense.payment.manualGCash")}</option>
          </select>
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("expense.filter.expenseNumber")}</span>
          <input
            className="exits-input"
            value={expenseNumber}
            onChange={(e) => {
              setExpenseNumber(e.target.value);
              setPage(1);
            }}
            placeholder={t("expense.filter.expenseNumberPlaceholder")}
            data-testid="expense-filter-number"
          />
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("expense.filter.fromDate")}</span>
          <input
            type="date"
            className="exits-input"
            value={fromDate}
            onChange={(e) => {
              setFromDate(e.target.value);
              setPage(1);
            }}
            data-testid="expense-filter-from"
          />
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("expense.filter.toDate")}</span>
          <input
            type="date"
            className="exits-input"
            value={toDate}
            onChange={(e) => {
              setToDate(e.target.value);
              setPage(1);
            }}
            data-testid="expense-filter-to"
          />
        </label>
      </div>

      {listQuery.isLoading ? <LoadingState label={t("expense.loading")} /> : null}
      {listQuery.isError ? (
        <ErrorState title={t("expense.errorTitle")} detail={t("expense.loadFailed")} />
      ) : null}
      {listQuery.isSuccess && items.length === 0 ? (
        <EmptyState
          title={filtersActive ? t("expense.emptyFiltered") : t("expense.empty")}
          detail={
            filtersActive
              ? t("expense.emptyFilteredDetail")
              : allowManage
                ? t("expense.emptyDetail")
                : t("expense.emptyReadonly")
          }
        />
      ) : null}

      <ul
        className="m-0 grid list-none grid-cols-1 gap-2 p-0 md:grid-cols-2"
        data-testid="expense-list"
      >
        {items.map((item) => {
          const isVoided = item.status === "Voided";
          return (
            <li key={item.expenseId}>
              <Link
                to={`/expenses/${item.expenseId}`}
                className="exits-list__card expense-row flex h-full w-full min-w-0 items-center gap-3 text-foreground no-underline"
                data-testid={`expense-row-${item.expenseId}`}
              >
                <span className="flex min-w-0 flex-1 flex-col gap-1">
                  <span className="exits-list__name truncate font-semibold">
                    {item.expenseNumber}
                  </span>
                  <span className="truncate text-[length:var(--exits-text-sm)] text-muted">
                    {[
                      formatExpenseDate(item.expenseDate),
                      item.categoryName ?? t("expense.category.unknown"),
                      t(expensePaymentLabelKey(item.paymentMethod)),
                    ].join(" · ")}
                  </span>
                  <span className="truncate text-[length:var(--exits-text-sm)]">
                    {item.description}
                  </span>
                </span>
                <span className="flex shrink-0 items-center gap-2">
                  <span className="flex flex-col items-end gap-1.5">
                    <StatusChip tone={isVoided ? "danger" : "success"}>
                      {t(expenseStatusLabelKey(item.status))}
                    </StatusChip>
                    <MoneyDisplay
                      amount={item.amount}
                      className="text-[length:var(--exits-text-md)]"
                    />
                  </span>
                  <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
                </span>
              </Link>
            </li>
          );
        })}
      </ul>

      {totalCount > PAGE_SIZE ? (
        <div className="flex flex-wrap items-center gap-2" data-testid="expense-pagination">
          <Button
            type="button"
            variant="outline"
            disabled={page <= 1 || listQuery.isFetching}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            {t("expense.prevPage")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {t("expense.pageOf")
              .replace("{page}", String(page))
              .replace("{pages}", String(totalPages))}
          </span>
          <Button
            type="button"
            variant="outline"
            disabled={page >= totalPages || listQuery.isFetching}
            onClick={() => setPage((p) => p + 1)}
          >
            {t("expense.nextPage")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
