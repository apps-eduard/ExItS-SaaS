import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  CheckCircle2,
  ChevronRight,
  CircleAlert,
  Clock3,
  ReceiptText,
  ShoppingCart,
  Store,
} from "lucide-react";
import { canManageShifts, canViewShifts, isPosCashierRole } from "@/access/pos-capabilities";
import { listRegisters } from "@/api/pos/pos-registers-client";
import {
  isOpenCashierShift,
  listCashierShifts,
  type PosCashierShiftDto,
} from "@/api/pos/pos-shifts-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { PageSkeleton } from "@/components/exits/loading/PageSkeleton";
import { StatusChip } from "@/components/exits/StatusChip";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  resolveHistoryDatePreset,
  type HistoryDatePreset,
} from "@/features/registers/date-range-presets";
import {
  ManagerActionCard,
  ManagerActionGrid,
} from "@/features/role/ManagerHomeShared";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PRESETS: HistoryDatePreset[] = ["today", "last7Days", "thisWeek", "thisMonth"];

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

export function ShiftsHubPage() {
  const { t } = useI18n();
  const { session } = useSession();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const { currentShift, loading, hasOpenShift, errorMessage, refresh, readiness } =
    useShiftContext();

  const canView = canViewShifts(sessionGrant);
  const canManage = canManageShifts(sessionGrant);
  const isCashier = isPosCashierRole(sessionGrant);
  const currentActorId = session?.userId ?? null;

  const workspaceScope = useMemo(() => {
    if (!boundWorkspace?.branchId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const [preset, setPreset] = useState<HistoryDatePreset>("last7Days");
  const [statusFilter, setStatusFilter] = useState("");
  const [registerFilter, setRegisterFilter] = useState("");
  const [cashierFilter, setCashierFilter] = useState("");
  const range = useMemo(() => resolveHistoryDatePreset(preset), [preset]);

  const readinessDetail =
    readiness.status === "ready"
      ? t("shift.readinessReady")
      : readiness.status === "loading"
        ? t("loading.label")
        : readiness.status === "blocked_denied"
          ? t("shift.readinessDenied")
          : readiness.status === "blocked_closed"
            ? t("shift.readinessClosed")
            : readiness.status === "blocked_no_register"
              ? t("shift.readinessNoRegister")
              : t("shift.readinessBlocked");

  const readinessOk = readiness.status === "ready";

  const myHistoryQuery = useQuery({
    queryKey: [
      "pos-cashier-shifts-mine",
      workspaceScope?.organizationId,
      currentActorId,
      range.fromDate,
      range.toDate,
    ],
    enabled: workspaceScope !== null && canView && isCashier && Boolean(currentActorId),
    queryFn: ({ signal }) =>
      listCashierShifts(
        workspaceScope!,
        {
          actorId: currentActorId!,
          fromBusinessDate: range.fromDate,
          toBusinessDate: range.toDate,
          page: 1,
          pageSize: 30,
        },
        signal,
      ),
  });

  const openShiftsQuery = useQuery({
    queryKey: ["pos-cashier-shifts-open", workspaceScope?.organizationId, workspaceScope?.branchId],
    enabled: workspaceScope !== null && canView && !isCashier,
    queryFn: ({ signal }) =>
      listCashierShifts(
        workspaceScope!,
        { status: "Open", page: 1, pageSize: 50 },
        signal,
      ),
  });

  const historyQuery = useQuery({
    queryKey: [
      "pos-cashier-shifts-history",
      workspaceScope?.organizationId,
      workspaceScope?.branchId,
      range.fromDate,
      range.toDate,
      statusFilter || null,
      registerFilter || null,
      cashierFilter || null,
    ],
    enabled: workspaceScope !== null && canView && !isCashier,
    queryFn: ({ signal }) =>
      listCashierShifts(
        workspaceScope!,
        {
          status: statusFilter || undefined,
          registerId: registerFilter || undefined,
          actorId: cashierFilter || undefined,
          fromBusinessDate: range.fromDate,
          toBusinessDate: range.toDate,
          page: 1,
          pageSize: 40,
        },
        signal,
      ),
  });

  const registersQuery = useQuery({
    queryKey: ["pos-registers-list", workspaceScope?.organizationId, "shifts-hub-filter"],
    enabled: workspaceScope !== null && canView && !isCashier,
    queryFn: ({ signal }) => listRegisters(workspaceScope!, { page: 1, pageSize: 50 }, signal),
  });

  const openShifts = openShiftsQuery.data?.items ?? [];
  const historyShifts = historyQuery.data?.items ?? [];
  const myShifts = myHistoryQuery.data?.items ?? [];

  const actorIds = useMemo(() => {
    const ids = [
      ...openShifts.map((shift) => shift.actorId),
      ...historyShifts.map((shift) => shift.actorId),
      ...myShifts.map((shift) => shift.actorId),
    ];
    return ids;
  }, [openShifts, historyShifts, myShifts]);

  const actors = useActorDirectory(workspaceScope?.organizationId, actorIds);

  const cashierOptions = useMemo(() => {
    const map = new Map<string, string>();
    for (const shift of [...openShifts, ...historyShifts]) {
      if (!shift.actorId || map.has(shift.actorId)) {
        continue;
      }
      map.set(
        shift.actorId,
        actors.resolve(shift.actorId)?.displayName?.trim() || shift.actorId.slice(0, 8),
      );
    }
    return [...map.entries()].sort((a, b) => a[1].localeCompare(b[1]));
  }, [openShifts, historyShifts, actors]);

  if (!canView) {
    return (
      <div data-testid="shifts-hub-denied" className="shifts-hub-page flex flex-col gap-3">
        <PageHeader
          title={isPosCashierRole(sessionGrant) ? t("shift.myHubTitle") : t("shift.hubTitle")}
          backTo={pageBackNav.managerHome.to}
          backLabel={t(pageBackNav.managerHome.labelKey)}
          backTestId="page-header-back-shifts"
        />
      </div>
    );
  }

  const registerLine = currentShift?.registerCode
    ? `${currentShift.registerCode} · ${currentShift.registerName ?? ""}`.trim()
    : t("shift.noRegisterOnShift");

  return (
    <div
      data-testid="shifts-hub-page"
      data-role-scope={isCashier ? "cashier" : "manager"}
      className="shifts-hub-page exits-page mx-auto flex w-full max-w-[56rem] min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={isCashier ? t("shift.myHubTitle") : t("shift.hubTitle")}
        description={isCashier ? t("shift.myHubDetail") : t("shift.hubDetail")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-shifts"
      />

      {loading && !currentShift && !errorMessage ? (
        <PageSkeleton label={t("loading.label")} variant="cards" rows={2} />
      ) : null}

      {errorMessage ? (
        <div className="exits-alert-surface flex items-start gap-2 px-3 py-2.5">
          <CircleAlert className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden />
          <div className="min-w-0 flex-1">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">{t("error.title")}</p>
            <p className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted wrap-break-word">
              {errorMessage}
            </p>
            <Button
              type="button"
              variant="ghost"
              className="mt-1.5 h-auto min-h-0 px-0 py-0"
              onClick={() => void refresh()}
            >
              {t("shift.retry")}
            </Button>
          </div>
        </div>
      ) : null}

      <div
        className={
          readinessOk
            ? "exits-alert-surface exits-alert-surface--success shifts-hub-page__readiness flex items-start gap-2 px-3 py-2"
            : "exits-alert-surface shifts-hub-page__readiness flex items-start gap-2 px-3 py-2"
        }
        data-testid="shift-readiness-card"
      >
        {readinessOk ? (
          <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden />
        ) : (
          <CircleAlert className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden />
        )}
        <div className="min-w-0 flex-1">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
            {t("shift.readinessLabel")}
          </p>
          <p
            className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted"
            data-testid="shift-readiness-status"
          >
            {readinessDetail}
          </p>
        </div>
      </div>

      {hasOpenShift && currentShift ? (
        <div className="shifts-hub-page__current flex min-w-0 flex-col gap-2.5" data-testid="shift-current-banner">
          <div className="exits-metric-surface flex min-w-0 flex-col gap-1 px-3 py-2.5">
            <div className="flex min-w-0 flex-wrap items-center gap-2">
              <StatusChip tone="success">{t("shift.statusOpen")}</StatusChip>
              <span className="shifts-hub-page__shift-number min-w-0 truncate text-[length:var(--exits-text-sm)] font-semibold text-foreground">
                {currentShift.shiftNumber}
              </span>
            </div>
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{registerLine}</p>
          </div>

          <ManagerActionGrid>
            <ManagerActionCard
              to={`/shifts/${currentShift.shiftId}`}
              label={t("shift.viewCurrent")}
              icon={Store}
              testId="shift-open-detail"
            />
            <ManagerActionCard to="/sell" label={t("role.openSellFloor")} icon={ShoppingCart} />
          </ManagerActionGrid>
        </div>
      ) : (
        <div className="shifts-hub-page__none flex min-w-0 flex-col gap-2.5" data-testid="shift-none-banner">
          <div className="exits-metric-surface flex items-start gap-2 px-3 py-2.5">
            <Clock3 className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden />
            <div className="min-w-0 flex-1">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">{t("shift.noneMessage")}</p>
              {!canManage ? (
                <p className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("shift.manageDeniedDetail")}
                </p>
              ) : null}
            </div>
          </div>
          {canManage ? (
            <ManagerActionCard
              to="/shifts/open"
              label={t("shift.openTitle")}
              icon={Clock3}
              testId="shift-go-open"
            />
          ) : null}
        </div>
      )}

      {isCashier ? (
        <section className="flex min-w-0 flex-col gap-2" data-testid="cashier-shifts-history">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("shift.myHistoryTitle")}
          </h2>
          <ExitsChipBar
            variant="filter"
            ariaLabel={t("shift.historyDateRange")}
            testId="cashier-shifts-date-presets"
            items={PRESETS.map((key) => ({
              key,
              label: t(`register.preset.${key}` as "register.preset.today"),
              state: preset === key ? "active" : "idle",
              testId: `cashier-shifts-preset-${key}`,
              onSelect: () => setPreset(key),
            }))}
          />
          {myHistoryQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
          {myHistoryQuery.isSuccess && myShifts.length === 0 ? (
            <EmptyState title={t("shift.historyEmpty")} />
          ) : null}
          <ul className="exits-list m-0 grid list-none gap-2 p-0">
            {myShifts.map((shift) => (
              <ShiftHistoryRow
                key={shift.shiftId}
                shift={shift}
                cashierName={null}
                showCashier={false}
              />
            ))}
          </ul>
        </section>
      ) : (
        <>
          <section className="flex min-w-0 flex-col gap-2" data-testid="manager-open-shifts">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {t("shift.openShiftsTitle")}
            </h2>
            {boundWorkspace?.branchName ? (
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid="shifts-hub-branch">
                {boundWorkspace.branchName}
              </p>
            ) : null}
            {openShiftsQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
            {openShiftsQuery.isSuccess && openShifts.length === 0 ? (
              <EmptyState title={t("shift.openShiftsEmpty")} />
            ) : null}
            <ul className="exits-list m-0 grid list-none gap-2 p-0">
              {openShifts.map((shift) => (
                <ShiftHistoryRow
                  key={shift.shiftId}
                  shift={shift}
                  cashierName={actors.resolve(shift.actorId)?.displayName ?? null}
                  showCashier
                />
              ))}
            </ul>
          </section>

          <section className="flex min-w-0 flex-col gap-2" data-testid="manager-shifts-history">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {t("shift.historyTitle")}
            </h2>
            <ExitsChipBar
              variant="filter"
              ariaLabel={t("shift.historyDateRange")}
              testId="manager-shifts-date-presets"
              items={PRESETS.map((key) => ({
                key,
                label: t(`register.preset.${key}` as "register.preset.today"),
                state: preset === key ? "active" : "idle",
                testId: `manager-shifts-preset-${key}`,
                onSelect: () => setPreset(key),
              }))}
            />

            <div
              className="grid gap-2 sm:grid-cols-3"
              data-testid="manager-shifts-filters"
            >
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                <span className="font-medium">{t("shift.filterStatus")}</span>
                <select
                  className="exits-select"
                  value={statusFilter}
                  data-testid="shifts-filter-status"
                  onChange={(event) => setStatusFilter(event.target.value)}
                >
                  <option value="">{t("shift.filterStatusAll")}</option>
                  <option value="Open">{t("shift.statusOpen")}</option>
                  <option value="Closed">{t("shift.statusClosed")}</option>
                </select>
              </label>
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                <span className="font-medium">{t("shift.filterRegister")}</span>
                <select
                  className="exits-select"
                  value={registerFilter}
                  data-testid="shifts-filter-register"
                  onChange={(event) => setRegisterFilter(event.target.value)}
                >
                  <option value="">{t("shift.filterRegisterAll")}</option>
                  {(registersQuery.data?.items ?? []).map((register) => (
                    <option key={register.registerId} value={register.registerId}>
                      {register.registerCode} — {register.name}
                    </option>
                  ))}
                </select>
              </label>
              <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                <span className="font-medium">{t("shift.filterCashier")}</span>
                <select
                  className="exits-select"
                  value={cashierFilter}
                  data-testid="shifts-filter-cashier"
                  onChange={(event) => setCashierFilter(event.target.value)}
                >
                  <option value="">{t("shift.filterCashierAll")}</option>
                  {cashierOptions.map(([actorId, name]) => (
                    <option key={actorId} value={actorId}>
                      {name}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            {historyQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
            {historyQuery.isSuccess && historyShifts.length === 0 ? (
              <EmptyState title={t("shift.historyEmpty")} />
            ) : null}
            <ul className="exits-list m-0 grid list-none gap-2 p-0">
              {historyShifts.map((shift) => (
                <ShiftHistoryRow
                  key={shift.shiftId}
                  shift={shift}
                  cashierName={actors.resolve(shift.actorId)?.displayName ?? null}
                  showCashier
                />
              ))}
            </ul>
          </section>
        </>
      )}
    </div>
  );
}

function ShiftHistoryRow({
  shift,
  cashierName,
  showCashier,
}: {
  shift: PosCashierShiftDto;
  cashierName: string | null;
  showCashier: boolean;
}) {
  const { t } = useI18n();
  const open = isOpenCashierShift(shift);
  const registerLabel =
    shift.registerCode && shift.registerName
      ? `${shift.registerCode} — ${shift.registerName}`
      : shift.registerCode || shift.registerName || t("shift.noRegisterOnShift");

  return (
    <li>
      <div
        className="exits-list__card flex min-w-0 flex-col gap-2 p-3"
        data-testid={`shift-history-row-${shift.shiftId}`}
      >
        <div className="flex min-w-0 flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="m-0 truncate font-semibold">{shift.shiftNumber}</p>
            <p className="mb-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
              {registerLabel}
              {" · "}
              {formatOpenedWhen(shift.openedAtUtc)}
              {showCashier && cashierName ? ` · ${cashierName}` : null}
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
            data-testid={`shift-history-view-${shift.shiftId}`}
          >
            {t("register.viewShift")}
            <ChevronRight className="size-3.5" aria-hidden />
          </Link>
          <Link
            to={`/shifts/${shift.shiftId}/transactions`}
            className="inline-flex min-h-9 items-center gap-1 rounded-[var(--exits-radius-md)] border border-border px-2.5 text-[length:var(--exits-text-sm)] font-medium text-foreground no-underline"
            data-testid={`shift-history-txns-${shift.shiftId}`}
          >
            <ReceiptText className="size-3.5 shrink-0" aria-hidden />
            {t("register.viewTransactions")}
          </Link>
        </div>
      </div>
    </li>
  );
}
