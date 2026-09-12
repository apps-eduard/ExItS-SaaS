import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Clock3 } from "lucide-react";
import { canViewSales } from "@/access/pos-capabilities";
import { getCashierShift } from "@/api/pos/pos-shifts-client";
import { getRegister } from "@/api/pos/pos-registers-client";
import {
  formatPaymentMethodLabel,
  listSales,
  type PosSaleDto,
} from "@/api/pos/pos-sales-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  resolveHistoryDatePreset,
  type HistoryDatePreset,
} from "@/features/registers/date-range-presets";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 25;
const PRESETS: HistoryDatePreset[] = ["today", "last7Days", "last30Days"];

function formatRecordedWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

function saleStatusTone(status: string, voided: boolean): "danger" | "success" | "info" {
  if (voided) {
    return "danger";
  }
  if (status === "Completed") {
    return "success";
  }
  return "info";
}

/**
 * Shared transactions list scoped by route:
 * - `/registers/:registerId/transactions`
 * - `/shifts/:shiftId/transactions`
 *
 * Mobile (&lt; lg): compact cards. Desktop (≥ lg): semantic data table.
 */
export function TransactionsListPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { registerId, shiftId } = useParams();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canView = canViewSales(sessionGrant);

  const scopedRegisterId = registerId?.trim() || undefined;
  const scopedShiftId = shiftId?.trim() || undefined;
  const isShiftScope = Boolean(scopedShiftId);
  const isRegisterScope = Boolean(scopedRegisterId) && !isShiftScope;
  const showCashierColumn = isRegisterScope;

  const [preset, setPreset] = useState<HistoryDatePreset>("last7Days");
  const [page, setPage] = useState(1);
  const range = useMemo(() => resolveHistoryDatePreset(preset), [preset]);

  const workspaceScope = useMemo(() => {
    if (!boundWorkspace?.branchId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const registerQuery = useQuery({
    queryKey: ["pos-register", workspaceScope?.organizationId, scopedRegisterId],
    enabled: workspaceScope !== null && canView && isRegisterScope && Boolean(scopedRegisterId),
    queryFn: ({ signal }) => getRegister(workspaceScope!, scopedRegisterId!, signal),
  });

  const shiftQuery = useQuery({
    queryKey: ["pos-cashier-shift", workspaceScope?.organizationId, scopedShiftId],
    enabled: workspaceScope !== null && canView && isShiftScope && Boolean(scopedShiftId),
    queryFn: ({ signal }) => getCashierShift(workspaceScope!, scopedShiftId!, signal),
  });

  const salesQuery = useQuery({
    queryKey: [
      "pos-transactions",
      workspaceScope?.organizationId,
      workspaceScope?.branchId,
      scopedRegisterId ?? null,
      scopedShiftId ?? null,
      isShiftScope ? null : range.fromDate,
      isShiftScope ? null : range.toDate,
      page,
    ],
    enabled: workspaceScope !== null && canView && (isRegisterScope || isShiftScope),
    queryFn: ({ signal }) =>
      listSales(
        workspaceScope!,
        {
          registerId: isRegisterScope ? scopedRegisterId : undefined,
          cashierShiftId: isShiftScope ? scopedShiftId : undefined,
          shiftId: isShiftScope ? scopedShiftId : undefined,
          branchId: workspaceScope!.branchId,
          fromDate: isShiftScope ? undefined : range.fromDate,
          toDate: isShiftScope ? undefined : range.toDate,
          page,
          pageSize: PAGE_SIZE,
        },
        signal,
      ),
  });

  const sales = salesQuery.data?.items ?? [];
  const actors = useActorDirectory(
    workspaceScope?.organizationId,
    sales.map((sale) => sale.recordedBy),
  );

  const backTarget = isShiftScope
    ? scopedShiftId
      ? { to: `/shifts/${scopedShiftId}`, labelKey: "shift.backToShift" as const }
      : pageBackNav.shifts
    : scopedRegisterId
      ? { to: `/registers/${scopedRegisterId}/history`, labelKey: "register.backToHistory" as const }
      : pageBackNav.registers;

  const title = isShiftScope
    ? t("transactions.shiftTitle")
    : t("transactions.registerTitle");

  const branchName = boundWorkspace?.branchName?.trim() || null;

  const contextLine = isShiftScope
    ? shiftQuery.data
      ? [
          shiftQuery.data.shiftNumber,
          shiftQuery.data.registerName || shiftQuery.data.registerCode || null,
          branchName,
        ]
          .filter(Boolean)
          .join(" • ")
      : t("transactions.shiftLede")
    : registerQuery.data
      ? [registerQuery.data.name, registerQuery.data.registerCode, branchName]
          .filter(Boolean)
          .join(" • ")
      : t("transactions.registerLede");

  if (!canView) {
    return (
      <div data-testid="transactions-list-denied" className="flex flex-col gap-3">
        <PageHeader
          title={title}
          description={t("transactions.deniedDetail")}
          backTo={pageBackNav.managerHome.to}
          backLabel={t(pageBackNav.managerHome.labelKey)}
          backTestId="page-header-back-transactions"
        />
      </div>
    );
  }

  if (!workspaceScope) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  const totalCount = salesQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const rangeStart = totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;
  const rangeEnd = Math.min(page * PAGE_SIZE, totalCount);

  return (
    <div
      data-testid="transactions-list-page"
      data-scope={isShiftScope ? "shift" : "register"}
      className="transactions-list-page exits-page mx-auto flex w-full max-w-[80rem] min-w-0 flex-col gap-4"
    >
      <PageHeader
        title={title}
        description={contextLine}
        backTo={backTarget.to}
        backLabel={t(backTarget.labelKey)}
        backTestId="page-header-back-transactions"
      />

      <section className="transactions-list-panel exits-animate-panel flex min-w-0 flex-col gap-3">
        {isRegisterScope ? (
          <div className="flex min-w-0 flex-col gap-2">
            <ExitsChipBar
              variant="filter"
              ariaLabel={t("transactions.dateRange")}
              testId="transactions-date-presets"
              items={PRESETS.map((key) => ({
                key,
                label: t(`register.preset.${key}` as "register.preset.today"),
                state: preset === key ? "active" : "idle",
                testId: `transactions-preset-${key}`,
                onSelect: () => {
                  setPreset(key);
                  setPage(1);
                },
              }))}
            />
            <p
              className="m-0 text-[length:var(--exits-text-xs)] text-muted"
              data-testid="transactions-range"
            >
              {range.fromDate === range.toDate
                ? range.fromDate
                : `${range.fromDate} → ${range.toDate}`}
            </p>
          </div>
        ) : null}

        {salesQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
        {salesQuery.isError ? (
          <ErrorState title={t("error.title")} detail={t("transactions.loadError")} />
        ) : null}
        {salesQuery.isSuccess && sales.length === 0 ? (
          <EmptyState
              align="center"
              icon={<Clock3 className="size-5" strokeWidth={1.75} />} title={t("transactions.empty")} detail={t("transactions.emptyDetail")} />
        ) : null}

        {sales.length > 0 ? (
          <>
            <ul
              className="exits-list m-0 grid list-none gap-2 p-0 lg:hidden"
              data-testid="transactions-list-cards"
            >
              {sales.map((sale) => (
                <TransactionCardRow
                  key={sale.saleId}
                  sale={sale}
                  cashierName={
                    showCashierColumn
                      ? actors.resolve(sale.recordedBy)?.displayName ?? null
                      : null
                  }
                  showCashier={showCashierColumn}
                />
              ))}
            </ul>

            <div
              className="transactions-list-table-shell hidden min-w-0 overflow-x-auto lg:block"
              data-testid="transactions-list-table"
            >
              <table className="transactions-list-table w-full min-w-[42rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                <thead>
                  <tr className="transactions-list-table__head border-b border-border">
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("transactions.col.saleNumber")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("transactions.col.dateTime")}
                    </th>
                    {showCashierColumn ? (
                      <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                        {t("transactions.col.cashier")}
                      </th>
                    ) : null}
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("transactions.col.payment")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-center text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("transactions.col.status")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("transactions.col.amount")}
                    </th>
                    <th className="w-8 px-3 py-2.5">
                      <span className="sr-only">{t("transactions.viewSummary")}</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {sales.map((sale) => {
                    const voided = sale.status === "Voided" || Boolean(sale.voidedAtUtc);
                    const cashierName = showCashierColumn
                      ? actors.resolve(sale.recordedBy)?.displayName ?? null
                      : null;
                    const summaryPath = `/sell/sales/${sale.saleId}/summary`;
                    return (
                      <tr
                        key={sale.saleId}
                        role="link"
                        tabIndex={0}
                        className="transactions-list-table__row cursor-pointer border-b border-border transition-colors focus-visible:outline-none"
                        data-testid={`transaction-table-row-${sale.saleId}`}
                        aria-label={`${sale.saleNumber}. ${t("transactions.viewSummary")}`}
                        onClick={() => navigate(summaryPath)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            navigate(summaryPath);
                          }
                        }}
                      >
                        <td
                          className="px-3 py-2.5 align-middle font-semibold"
                          data-testid={`transaction-row-${sale.saleId}`}
                        >
                          {sale.saleNumber}
                        </td>
                        <td className="whitespace-nowrap px-3 py-2.5 align-middle text-muted">
                          {formatRecordedWhen(sale.recordedAtUtc)}
                        </td>
                        {showCashierColumn ? (
                          <td className="max-w-[14rem] truncate px-3 py-2.5 align-middle">
                            {cashierName ?? "—"}
                          </td>
                        ) : null}
                        <td className="whitespace-nowrap px-3 py-2.5 align-middle">
                          {formatPaymentMethodLabel(sale.paymentMethod)}
                        </td>
                        <td className="px-3 py-2.5 text-center align-middle">
                          <StatusChip tone={saleStatusTone(sale.status, voided)}>
                            {sale.status}
                          </StatusChip>
                        </td>
                        <td className="px-3 py-2.5 align-middle text-right font-semibold tabular-nums">
                          <MoneyDisplay amount={sale.total} />
                        </td>
                        <td className="px-3 py-2.5 align-middle text-right text-muted">
                          <ChevronRight className="ml-auto size-4 shrink-0" aria-hidden />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </>
        ) : null}

        {totalCount > 0 ? (
          <div className="flex min-w-0 flex-wrap items-center justify-between gap-2 border-t border-border pt-3">
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="transactions-page-count"
            >
              {totalCount > PAGE_SIZE
                ? t("transactions.pageRange")
                    .replace("{start}", String(rangeStart))
                    .replace("{end}", String(rangeEnd))
                    .replace("{total}", String(totalCount))
                : t("transactions.pageOf")
                    .replace("{page}", String(page))
                    .replace("{pages}", String(totalPages))}
            </p>
            {totalCount > PAGE_SIZE ? (
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  className="h-8 min-h-8 px-3"
                  disabled={page <= 1}
                  data-testid="transactions-prev"
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                >
                  {t("transactions.prevPage")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="h-8 min-h-8 px-3"
                  disabled={page >= totalPages}
                  data-testid="transactions-next"
                  onClick={() => setPage((current) => current + 1)}
                >
                  {t("transactions.nextPage")}
                </Button>
              </div>
            ) : null}
          </div>
        ) : null}
      </section>
    </div>
  );
}

function TransactionCardRow({
  sale,
  cashierName,
  showCashier,
}: {
  sale: PosSaleDto;
  cashierName: string | null;
  showCashier: boolean;
}) {
  const { t } = useI18n();
  const voided = sale.status === "Voided" || Boolean(sale.voidedAtUtc);
  return (
    <li>
      <Link
        to={`/sell/sales/${sale.saleId}/summary`}
        className="exits-list__card transactions-list-card flex min-w-0 items-center gap-2 p-3 text-foreground no-underline"
        data-testid={`transaction-card-row-${sale.saleId}`}
      >
        <span className="min-w-0 flex-1">
          <span className="block min-w-0 truncate font-semibold">{sale.saleNumber}</span>
          <span className="mt-1 block text-[length:var(--exits-text-sm)] text-muted">
            {formatPaymentMethodLabel(sale.paymentMethod)} · {formatRecordedWhen(sale.recordedAtUtc)}
            {showCashier && cashierName ? ` · ${cashierName}` : null}
          </span>
        </span>
        <span className="flex shrink-0 flex-col items-end gap-1">
          <StatusChip tone={saleStatusTone(sale.status, voided)}>{sale.status}</StatusChip>
          <span className="flex items-center gap-2">
            <MoneyDisplay amount={sale.total} />
            <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
            <span className="sr-only">{t("transactions.viewSummary")}</span>
          </span>
        </span>
      </Link>
    </li>
  );
}
