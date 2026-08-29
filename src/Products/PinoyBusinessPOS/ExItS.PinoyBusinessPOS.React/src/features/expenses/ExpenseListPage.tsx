import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus, Tags } from "lucide-react";
import { canManageExpenses } from "@/access/pos-capabilities";
import {
  expenseWorkspaceScope,
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
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

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

  const organizationId = boundWorkspace?.organizationId ?? null;
  const workspace = useMemo(
    () => (organizationId ? expenseWorkspaceScope(organizationId) : null),
    [organizationId],
  );

  useEffect(() => {
    setPage(1);
    setStatus("");
    setPaymentMethod("");
    setExpenseNumber("");
    setFromDate("");
    setToDate("");
  }, [organizationId]);

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
    ],
    enabled: Boolean(workspace) && online,
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
        },
        signal,
      ),
  });

  const summaryQuery = useQuery({
    queryKey: ["expenses-summary", organizationId, fromDate, toDate],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      getExpenseSummary(
        workspace!,
        { fromDate: fromDate || undefined, toDate: toDate || undefined },
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

      <p
        className="m-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="expense-org-scope-banner"
      >
        {t("expense.orgScopeBanner")}
      </p>

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

      {summary ? (
        <div
          className="grid gap-2 sm:grid-cols-3"
          data-testid="expense-summary-cards"
        >
          <div className="rounded-[var(--exits-radius-md)] border border-border bg-surface p-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("expense.summary.net")}
            </p>
            <p className="mt-1 mb-0 text-[length:var(--exits-text-lg)] font-semibold" data-testid="expense-summary-net">
              <MoneyDisplay amount={summary.netTotal} />
            </p>
          </div>
          <div className="rounded-[var(--exits-radius-md)] border border-border bg-surface p-3">
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
          <div className="rounded-[var(--exits-radius-md)] border border-border bg-surface p-3">
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

      {summary && summary.byCategory.length > 0 ? (
        <section data-testid="expense-summary-by-category" className="flex flex-col gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
            {t("expense.summary.byCategory")}
          </h2>
          <ul className="m-0 grid list-none gap-1 p-0 sm:grid-cols-2">
            {summary.byCategory.map((row) => (
              <li
                key={row.categoryId}
                className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 text-[length:var(--exits-text-sm)]"
              >
                <span className="truncate">{row.categoryName ?? t("expense.category.unknown")}</span>
                <MoneyDisplay amount={row.totalAmount} />
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {summary && summary.byPaymentMethod.length > 0 ? (
        <section data-testid="expense-summary-by-payment" className="flex flex-col gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
            {t("expense.summary.byPayment")}
          </h2>
          <ul className="m-0 grid list-none gap-1 p-0 sm:grid-cols-2">
            {summary.byPaymentMethod.map((row) => (
              <li
                key={row.paymentMethod}
                className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 text-[length:var(--exits-text-sm)]"
              >
                <span>{t(expensePaymentLabelKey(row.paymentMethod))}</span>
                <MoneyDisplay amount={row.totalAmount} />
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <div
        className="grid gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3 sm:grid-cols-2 lg:grid-cols-3"
        data-testid="expense-filters"
      >
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{t("expense.filter.status")}</span>
          <select
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
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
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3"
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

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="expense-list">
        {items.map((item) => {
          const isVoided = item.status === "Voided";
          return (
            <li key={item.expenseId}>
              <Link
                to={`/expenses/${item.expenseId}`}
                className="exits-list__card block min-w-0 text-foreground no-underline"
                data-testid={`expense-row-${item.expenseId}`}
              >
                <span className="min-w-0">
                  <span className="exits-list__name block truncate font-semibold">
                    {item.expenseNumber}
                  </span>
                  <span className="mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {[
                      formatExpenseDate(item.expenseDate),
                      item.categoryName ?? t("expense.category.unknown"),
                      t(expensePaymentLabelKey(item.paymentMethod)),
                    ].join(" · ")}
                  </span>
                  <span className="mt-1 block truncate text-[length:var(--exits-text-sm)]">
                    {item.description}
                  </span>
                </span>
                <span className="flex shrink-0 flex-col items-end gap-2">
                  <MoneyDisplay amount={item.amount} />
                  <span className="flex items-center gap-2">
                    <StatusChip tone={isVoided ? "danger" : "success"}>
                      {t(expenseStatusLabelKey(item.status))}
                    </StatusChip>
                    <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
                  </span>
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
            className="min-h-11"
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
            className="min-h-11"
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
