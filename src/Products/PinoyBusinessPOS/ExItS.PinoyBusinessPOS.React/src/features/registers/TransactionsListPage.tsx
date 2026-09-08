import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight } from "lucide-react";
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

/**
 * Shared transactions list scoped by route:
 * - `/registers/:registerId/transactions`
 * - `/shifts/:shiftId/transactions`
 */
export function TransactionsListPage() {
  const { t } = useI18n();
  const { registerId, shiftId } = useParams();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canView = canViewSales(sessionGrant);

  const scopedRegisterId = registerId?.trim() || undefined;
  const scopedShiftId = shiftId?.trim() || undefined;
  const isShiftScope = Boolean(scopedShiftId);
  const isRegisterScope = Boolean(scopedRegisterId) && !isShiftScope;

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
    enabled:
      workspaceScope !== null &&
      canView &&
      (isRegisterScope || isShiftScope),
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

  const contextLine = isShiftScope
    ? shiftQuery.data
      ? `${shiftQuery.data.shiftNumber}${
          shiftQuery.data.registerCode
            ? ` · ${shiftQuery.data.registerCode}`
            : ""
        }`
      : t("transactions.shiftLede")
    : registerQuery.data
      ? `${registerQuery.data.registerCode} — ${registerQuery.data.name}`
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

  return (
    <div
      data-testid="transactions-list-page"
      data-scope={isShiftScope ? "shift" : "register"}
      className="transactions-list-page exits-page mx-auto flex w-full max-w-[56rem] min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={title}
        description={contextLine}
        backTo={backTarget.to}
        backLabel={t(backTarget.labelKey)}
        backTestId="page-header-back-transactions"
      />

      {isRegisterScope ? (
        <>
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
        </>
      ) : null}

      {salesQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
      {salesQuery.isError ? (
        <ErrorState title={t("error.title")} detail={t("transactions.loadError")} />
      ) : null}
      {salesQuery.isSuccess && sales.length === 0 ? (
        <EmptyState title={t("transactions.empty")} detail={t("transactions.emptyDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="transactions-list">
        {sales.map((sale) => (
          <TransactionRow
            key={sale.saleId}
            sale={sale}
            cashierName={actors.resolve(sale.recordedBy)?.displayName ?? null}
          />
        ))}
      </ul>

      {totalCount > PAGE_SIZE ? (
        <div className="flex min-w-0 flex-wrap items-center justify-between gap-2">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("transactions.pageOf")
              .replace("{page}", String(page))
              .replace("{pages}", String(totalPages))}
          </p>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={page <= 1}
              data-testid="transactions-prev"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("transactions.prevPage")}
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              data-testid="transactions-next"
              onClick={() => setPage((current) => current + 1)}
            >
              {t("transactions.nextPage")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function TransactionRow({
  sale,
  cashierName,
}: {
  sale: PosSaleDto;
  cashierName: string | null;
}) {
  const { t } = useI18n();
  const voided = sale.status === "Voided" || Boolean(sale.voidedAtUtc);
  return (
    <li>
      <Link
        to={`/sell/sales/${sale.saleId}/summary`}
        className="exits-list__card flex min-w-0 items-center gap-2 p-3 text-foreground no-underline"
        data-testid={`transaction-row-${sale.saleId}`}
      >
        <span className="min-w-0 flex-1">
          <span className="flex min-w-0 flex-wrap items-center gap-2">
            <span className="truncate font-semibold">{sale.saleNumber}</span>
            <StatusChip tone={voided ? "danger" : sale.status === "Completed" ? "success" : "info"}>
              {sale.status}
            </StatusChip>
          </span>
          <span className="mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
            {formatPaymentMethodLabel(sale.paymentMethod)} · {formatRecordedWhen(sale.recordedAtUtc)}
            {cashierName ? ` · ${cashierName}` : null}
          </span>
        </span>
        <span className="flex shrink-0 items-center gap-2">
          <MoneyDisplay amount={sale.total} />
          <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
          <span className="sr-only">{t("transactions.viewSummary")}</span>
        </span>
      </Link>
    </li>
  );
}
