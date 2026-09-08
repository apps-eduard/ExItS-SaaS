import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, ReceiptText } from "lucide-react";
import { canViewRegisters, canViewShifts } from "@/access/pos-capabilities";
import {
  getRegister,
  getRegisterActivity,
} from "@/api/pos/pos-registers-client";
import {
  isOpenCashierShift,
  listCashierShifts,
  type PosCashierShiftDto,
} from "@/api/pos/pos-shifts-client";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  dateOnlyRangeToUtcBounds,
  resolveHistoryDatePreset,
  type HistoryDatePreset,
} from "@/features/registers/date-range-presets";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PRESETS: HistoryDatePreset[] = ["today", "last7Days", "last30Days"];

function formatOpenedWhen(iso: string): string {
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

export function RegisterHistoryPage() {
  const { t } = useI18n();
  const { registerId = "" } = useParams();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canView = canViewRegisters(sessionGrant);
  const canShifts = canViewShifts(sessionGrant);

  const [preset, setPreset] = useState<HistoryDatePreset>("last7Days");
  const range = useMemo(() => resolveHistoryDatePreset(preset), [preset]);
  const utcBounds = useMemo(() => dateOnlyRangeToUtcBounds(range), [range]);

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
    queryKey: ["pos-register", workspaceScope?.organizationId, registerId],
    enabled: workspaceScope !== null && canView && Boolean(registerId),
    queryFn: ({ signal }) => getRegister(workspaceScope!, registerId, signal),
  });

  const activityQuery = useQuery({
    queryKey: [
      "pos-register-activity",
      workspaceScope?.organizationId,
      registerId,
      range.fromDate,
      range.toDate,
    ],
    enabled: workspaceScope !== null && canView && Boolean(registerId),
    queryFn: ({ signal }) =>
      getRegisterActivity(
        workspaceScope!,
        registerId,
        { fromUtc: utcBounds.fromUtc, toUtc: utcBounds.toUtc },
        signal,
      ),
  });

  const shiftsQuery = useQuery({
    queryKey: [
      "pos-register-shifts",
      workspaceScope?.organizationId,
      registerId,
      range.fromDate,
      range.toDate,
    ],
    enabled: workspaceScope !== null && canView && canShifts && Boolean(registerId),
    queryFn: ({ signal }) =>
      listCashierShifts(
        workspaceScope!,
        {
          registerId,
          fromBusinessDate: range.fromDate,
          toBusinessDate: range.toDate,
          page: 1,
          pageSize: 50,
        },
        signal,
      ),
  });

  const shifts = shiftsQuery.data?.items ?? [];
  const actors = useActorDirectory(
    workspaceScope?.organizationId,
    shifts.map((shift) => shift.actorId),
  );

  if (!canView) {
    return (
      <div data-testid="register-history-denied" className="flex flex-col gap-3">
        <PageHeader
          title={t("register.historyTitle")}
          description={t("register.deniedDetail")}
          backTo={pageBackNav.registers.to}
          backLabel={t(pageBackNav.registers.labelKey)}
          backTestId="page-header-back-registers"
        />
      </div>
    );
  }

  if (!workspaceScope || registerQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (registerQuery.isError || !registerQuery.data) {
    return (
      <div data-testid="register-history-missing" className="flex flex-col gap-3">
        <PageHeader
          title={t("register.historyTitle")}
          description={t("register.notFound")}
          backTo={pageBackNav.registers.to}
          backLabel={t(pageBackNav.registers.labelKey)}
          backTestId="page-header-back-registers"
        />
      </div>
    );
  }

  const register = registerQuery.data;
  const activity = activityQuery.data;

  return (
    <div
      data-testid="register-history-page"
      className="register-history-page exits-page mx-auto flex w-full max-w-[56rem] min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={t("register.historyTitle")}
        description={`${register.registerCode} — ${register.name}`}
        backTo={pageBackNav.registers.to}
        backLabel={t(pageBackNav.registers.labelKey)}
        backTestId="page-header-back-registers"
        trailing={
          <StatusChip tone={register.status === "Active" ? "success" : "info"}>
            {register.status}
          </StatusChip>
        }
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("register.historyDateRange")}
        testId="register-history-date-presets"
        items={PRESETS.map((key) => ({
          key,
          label: t(`register.preset.${key}` as "register.preset.today"),
          state: preset === key ? "active" : "idle",
          testId: `register-history-preset-${key}`,
          onSelect: () => setPreset(key),
        }))}
      />

      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid="register-history-range">
        {range.fromDate === range.toDate ? range.fromDate : `${range.fromDate} → ${range.toDate}`}
      </p>

      {activityQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
      {activityQuery.isError ? (
        <ErrorState title={t("error.title")} detail={t("register.activityLoadError")} />
      ) : null}

      {activity ? (
        <div
          className="grid gap-2 sm:grid-cols-2"
          data-testid="register-history-activity"
        >
          <Card className="exits-metric-surface flex flex-col gap-0.5 p-3">
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("register.activityOpenShifts")}
            </span>
            <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
              {activity.openShiftCount}
            </span>
          </Card>
          <Card className="exits-metric-surface flex flex-col gap-0.5 p-3">
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("register.activityClosedShifts")}
            </span>
            <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
              {activity.closedShiftCount}
            </span>
          </Card>
          <Card className="exits-metric-surface flex flex-col gap-0.5 p-3">
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("register.activitySales")}
            </span>
            <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
              {activity.completedSaleCount}
            </span>
          </Card>
          <Card className="exits-metric-surface flex flex-col gap-0.5 p-3">
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("register.activityGross")}
            </span>
            <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
              {formatPeso(activity.grossSalesTotal)}
            </span>
          </Card>
          <Card className="exits-metric-surface flex flex-col gap-0.5 p-3">
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("register.activityCash")}
            </span>
            <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
              {formatPeso(activity.cashSalesTotal ?? 0)}
            </span>
          </Card>
          <Card className="exits-metric-surface flex flex-col gap-0.5 p-3">
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("register.activityGCash")}
            </span>
            <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
              {formatPeso(activity.manualGCashSalesTotal ?? 0)}
            </span>
          </Card>
          <Card className="exits-metric-surface flex flex-col gap-0.5 p-3">
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("register.activityUtang")}
            </span>
            <span className="text-[length:var(--exits-text-md)] font-semibold tabular-nums">
              {formatPeso(activity.utangSalesTotal ?? 0)}
            </span>
          </Card>
        </div>
      ) : null}

      <div className="flex min-w-0 flex-col gap-2" data-testid="register-history-shifts">
        <div className="flex min-w-0 flex-wrap items-center justify-between gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("register.historyShiftsTitle")}
          </h2>
          <Link
            to={`/registers/${registerId}/transactions`}
            className="inline-flex min-h-10 items-center gap-1.5 text-[length:var(--exits-text-sm)] font-medium text-foreground no-underline"
            data-testid="register-history-all-transactions"
          >
            <ReceiptText className="size-4 shrink-0" aria-hidden />
            {t("register.viewTransactions")}
          </Link>
        </div>

        {shiftsQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
        {shiftsQuery.isError ? (
          <ErrorState title={t("error.title")} detail={t("shift.loadHistoryError")} />
        ) : null}
        {shiftsQuery.isSuccess && shifts.length === 0 ? (
          <EmptyState title={t("register.historyShiftsEmpty")} />
        ) : null}

        <ul className="exits-list m-0 grid list-none gap-2 p-0">
          {shifts.map((shift) => (
            <RegisterHistoryShiftRow
              key={shift.shiftId}
              shift={shift}
              cashierName={actors.resolve(shift.actorId)?.displayName ?? null}
            />
          ))}
        </ul>
      </div>
    </div>
  );
}

function RegisterHistoryShiftRow({
  shift,
  cashierName,
}: {
  shift: PosCashierShiftDto;
  cashierName: string | null;
}) {
  const { t } = useI18n();
  const open = isOpenCashierShift(shift);
  return (
    <li>
      <div
        className="exits-list__card flex min-w-0 flex-col gap-2 p-3"
        data-testid={`register-history-shift-${shift.shiftId}`}
      >
        <div className="flex min-w-0 flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="m-0 truncate font-semibold">{shift.shiftNumber}</p>
            <p className="mb-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
              {formatOpenedWhen(shift.openedAtUtc)}
              {cashierName ? ` · ${cashierName}` : null}
            </p>
          </div>
          <StatusChip tone={open ? "success" : "info"}>
            {open ? t("shift.statusOpen") : shift.status}
          </StatusChip>
        </div>
        {shift.completedTransactionCount != null || shift.completedSalesTotal != null ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            <span className="text-muted">{t("register.transactionsLabel")}: </span>
            <span className="font-medium tabular-nums">
              {shift.completedTransactionCount ?? 0}
            </span>
            {shift.completedSalesTotal != null ? (
              <>
                <span className="text-muted"> · </span>
                <span className="font-medium tabular-nums">
                  {formatPeso(shift.completedSalesTotal)}
                </span>
              </>
            ) : null}
          </p>
        ) : null}
        <div className="flex min-w-0 flex-wrap gap-2">
          <Link
            to={`/shifts/${shift.shiftId}`}
            className="inline-flex min-h-9 items-center gap-1 rounded-[var(--exits-radius-md)] border border-border px-2.5 text-[length:var(--exits-text-sm)] font-medium text-foreground no-underline"
            data-testid={`register-history-view-shift-${shift.shiftId}`}
          >
            {t("register.viewShift")}
            <ChevronRight className="size-3.5" aria-hidden />
          </Link>
          <Link
            to={`/shifts/${shift.shiftId}/transactions`}
            className="inline-flex min-h-9 items-center gap-1 rounded-[var(--exits-radius-md)] border border-border px-2.5 text-[length:var(--exits-text-sm)] font-medium text-foreground no-underline"
            data-testid={`register-history-shift-txns-${shift.shiftId}`}
          >
            {t("register.viewTransactions")}
          </Link>
        </div>
      </div>
    </li>
  );
}
