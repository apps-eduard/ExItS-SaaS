import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  CheckCircle2,
  CircleAlert,
  Clock3,
  ShoppingCart,
  Store,
} from "lucide-react";
import { canManageShifts, canViewShifts, isPosCashierRole } from "@/access/pos-capabilities";
import { listRegisters } from "@/api/pos/pos-registers-client";
import { listCashierShifts } from "@/api/pos/pos-shifts-client";
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
} from "@/features/role/ManagerHomeShared";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { ShiftHistoryResponsiveList } from "@/features/shifts/ShiftHistoryResponsiveList";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PRESETS: HistoryDatePreset[] = ["today", "last7Days", "thisWeek", "thisMonth"];

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
      className="shifts-hub-page exits-page mx-auto flex w-full max-w-[80rem] min-w-0 flex-col gap-4"
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
        <div
          className="shifts-hub-panel shifts-hub-page__current exits-animate-panel flex min-w-0 flex-col gap-3"
          data-testid="shift-current-banner"
        >
          <div className="shifts-hub-page__current-row grid min-w-0 grid-cols-1 gap-2 lg:grid-cols-[minmax(0,1.15fr)_minmax(0,1fr)_minmax(0,1fr)] lg:items-stretch">
            <div className="exits-metric-surface shifts-hub-page__current-detail flex h-full min-w-0 items-center gap-2.5 px-3 py-2.5">
              <Clock3 className="size-5 shrink-0 text-primary" aria-hidden />
              <div className="min-w-0 flex-1">
                <p className="shifts-hub-page__shift-number m-0 truncate text-[length:var(--exits-text-sm)] font-semibold text-foreground">
                  {currentShift.shiftNumber}
                </p>
                <p className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                  {registerLine}
                </p>
              </div>
              <StatusChip tone="success" className="shrink-0 self-center">
                {t("shift.statusOpen")}
              </StatusChip>
            </div>

            <ManagerActionCard
              to={`/shifts/${currentShift.shiftId}`}
              label={t("shift.viewCurrent")}
              icon={Store}
              testId="shift-open-detail"
            />
            <ManagerActionCard to="/sell" label={t("role.openSellFloor")} icon={ShoppingCart} />
          </div>
        </div>
      ) : (
        <div
          className="shifts-hub-panel shifts-hub-page__none exits-animate-panel flex min-w-0 flex-col gap-3"
          data-testid="shift-none-banner"
        >
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
        <section
          className="shifts-hub-panel exits-animate-panel flex min-w-0 flex-col gap-3"
          data-testid="cashier-shifts-history"
        >
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
          <ShiftHistoryResponsiveList
            shifts={myShifts}
            resolveCashierName={() => null}
            showCashier={false}
            showRegister
            rowTestIdPrefix="shift-history-row"
            viewShiftTestIdPrefix="shift-history-view"
            viewTxnsTestIdPrefix="shift-history-txns"
          />
        </section>
      ) : (
        <>
          <section
            className="shifts-hub-panel exits-animate-panel flex min-w-0 flex-col gap-3"
            data-testid="manager-open-shifts"
          >
            <div className="flex min-w-0 flex-wrap items-baseline justify-between gap-2">
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("shift.openShiftsTitle")}
              </h2>
              {boundWorkspace?.branchName ? (
                <p
                  className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                  data-testid="shifts-hub-branch"
                >
                  {boundWorkspace.branchName}
                </p>
              ) : null}
            </div>
            {openShiftsQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
            {openShiftsQuery.isSuccess && openShifts.length === 0 ? (
              <EmptyState title={t("shift.openShiftsEmpty")} />
            ) : null}
            <ShiftHistoryResponsiveList
              shifts={openShifts}
              resolveCashierName={(actorId) => actors.resolve(actorId)?.displayName ?? null}
              showCashier
              showRegister
              rowTestIdPrefix="shift-history-row"
              viewShiftTestIdPrefix="shift-history-view"
              viewTxnsTestIdPrefix="shift-history-txns"
            />
          </section>

          <section
            className="shifts-hub-panel exits-animate-panel flex min-w-0 flex-col gap-3"
            data-testid="manager-shifts-history"
          >
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
              className="shifts-hub-filters grid gap-2 sm:grid-cols-3"
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
            <ShiftHistoryResponsiveList
              shifts={historyShifts}
              resolveCashierName={(actorId) => actors.resolve(actorId)?.displayName ?? null}
              showCashier
              showRegister
              rowTestIdPrefix="shift-history-row"
              viewShiftTestIdPrefix="shift-history-view"
              viewTxnsTestIdPrefix="shift-history-txns"
            />
          </section>
        </>
      )}
    </div>
  );
}
