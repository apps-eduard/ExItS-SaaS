import { useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Clock3, DoorClosed, History, Play, ReceiptText, ShoppingCart } from "lucide-react";
import {
  canManageRegisters,
  canManageShifts,
  canViewRegisters,
  isPosCashierRole,
} from "@/access/pos-capabilities";
import { listRegisters, type PosRegisterDto } from "@/api/pos/pos-registers-client";
import {
  getCashierShiftSummary,
  getCurrentCashierShift,
  isOpenCashierShift,
} from "@/api/pos/pos-shifts-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { StatusChip } from "@/components/exits/StatusChip";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { ensurePwaDefaultCashRegister } from "@/features/shifts/ensure-pwa-default-register";
import { ManagerActionCard } from "@/features/role/ManagerHomeShared";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function formatOpenedWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString(undefined, {
    hour: "numeric",
    minute: "2-digit",
  });
}

export function RegistersListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant, deviceEnforcementEnabled } = useWorkspace();
  const { session } = useSession();
  const canView = canViewRegisters(sessionGrant);
  const canManage = canManageRegisters(sessionGrant);
  const canOpenShift = canManageShifts(sessionGrant);
  const isCashier = isPosCashierRole(sessionGrant);
  const pwaOptional = deviceEnforcementEnabled === false;
  const currentActorId = session?.userId ?? null;
  const displayName = session?.displayName?.trim() || null;

  const workspaceScope = useMemo(() => {
    if (!boundWorkspace?.branchId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const pageTitle = isCashier ? t("register.myTitle") : t("register.listTitle");
  const pageLede = isCashier ? t("register.myLede") : t("register.listLede");
  const shiftsNavLabel = isCashier ? t("shift.myHubTitle") : t("shift.hubTitle");
  const shiftsNavDetail = isCashier ? t("shift.myHubDetail") : t("shift.hubDetail");

  const currentShiftQuery = useQuery({
    queryKey: ["pos-cashier-shift-current", workspaceScope?.organizationId, currentActorId],
    enabled: workspaceScope !== null && canView && isCashier,
    queryFn: ({ signal }) => getCurrentCashierShift(workspaceScope!, signal),
  });

  const ownOpenShift =
    currentShiftQuery.data && isOpenCashierShift(currentShiftQuery.data)
      ? currentShiftQuery.data
      : null;

  const summaryQuery = useQuery({
    queryKey: [
      "pos-cashier-shift-summary",
      workspaceScope?.organizationId,
      ownOpenShift?.shiftId,
    ],
    enabled: workspaceScope !== null && Boolean(ownOpenShift?.shiftId),
    queryFn: ({ signal }) => getCashierShiftSummary(workspaceScope!, ownOpenShift!.shiftId, signal),
  });

  const registersQuery = useQuery({
    queryKey: [
      "pos-registers-list",
      workspaceScope?.organizationId,
      workspaceScope?.branchId,
      isCashier ? "cashier" : "manager",
    ],
    enabled: workspaceScope !== null && canView && !isCashier,
    queryFn: ({ signal }) => listRegisters(workspaceScope!, { page: 1, pageSize: 50 }, signal),
  });

  const registers = registersQuery.data?.items ?? [];
  const busyActorIds = useMemo(
    () => registers.map((register) => register.openShiftActorId ?? null),
    [registers],
  );
  const actors = useActorDirectory(
    !isCashier ? workspaceScope?.organizationId : undefined,
    busyActorIds,
  );

  const [ensuringPwa, setEnsuringPwa] = useState(false);
  const [pwaError, setPwaError] = useState<string | null>(null);
  const pwaAttemptedRef = useRef(false);

  useEffect(() => {
    if (
      !isCashier ||
      !pwaOptional ||
      !canOpenShift ||
      !workspaceScope ||
      ownOpenShift ||
      currentShiftQuery.isLoading ||
      currentShiftQuery.isFetching ||
      pwaAttemptedRef.current
    ) {
      return;
    }

    pwaAttemptedRef.current = true;
    let cancelled = false;
    setEnsuringPwa(true);
    setPwaError(null);
    void (async () => {
      try {
        await ensurePwaDefaultCashRegister(workspaceScope);
      } catch (error) {
        if (cancelled) {
          return;
        }
        pwaAttemptedRef.current = false;
        setPwaError(
          error instanceof Error ? error.message : t("register.cashierNoRegister"),
        );
      } finally {
        if (!cancelled) {
          setEnsuringPwa(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [
    isCashier,
    pwaOptional,
    canOpenShift,
    workspaceScope,
    ownOpenShift,
    currentShiftQuery.isLoading,
    currentShiftQuery.isFetching,
    t,
  ]);

  if (!canView) {
    return (
      <div data-testid="registers-denied" className="flex flex-col gap-3">
        <PageHeader
          title={pageTitle}
          description={t("register.deniedDetail")}
          backTo={pageBackNav.managerHome.to}
          backLabel={t(pageBackNav.managerHome.labelKey)}
          backTestId="page-header-back-registers"
        />
      </div>
    );
  }

  return (
    <div
      data-testid="registers-list-page"
      data-role-scope={isCashier ? "cashier" : "manager"}
      className="registers-page exits-page flex min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={pageTitle}
        description={pageLede}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-registers"
      />

      {isCashier ? (
        <CashierMyRegisterPanel
          branchName={boundWorkspace?.branchName ?? null}
          displayName={displayName}
          loading={currentShiftQuery.isLoading}
          openShift={ownOpenShift}
          summary={summaryQuery.data ?? null}
          ensuringPwa={ensuringPwa}
          pwaError={pwaError}
          canOpenShift={canOpenShift}
        />
      ) : (
        <ManagerRegistersPanel
          canManage={canManage}
          loading={registersQuery.isLoading}
          error={registersQuery.isError}
          registers={registers}
          resolveActorName={(actorId) => actors.resolve(actorId)?.displayName ?? null}
        />
      )}

      <div className="mt-1" data-testid="registers-shifts-nav">
        <ManagerActionCard
          to="/shifts"
          label={shiftsNavLabel}
          detail={shiftsNavDetail}
          icon={Clock3}
          testId="registers-shifts-nav-link"
        />
      </div>
    </div>
  );
}

function CashierMyRegisterPanel({
  branchName,
  displayName,
  loading,
  openShift,
  summary,
  ensuringPwa,
  pwaError,
  canOpenShift,
}: {
  branchName: string | null;
  displayName: string | null;
  loading: boolean;
  openShift: Awaited<ReturnType<typeof getCurrentCashierShift>>;
  summary: Awaited<ReturnType<typeof getCashierShiftSummary>> | null;
  ensuringPwa: boolean;
  pwaError: string | null;
  canOpenShift: boolean;
}) {
  const { t } = useI18n();
  if (loading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (openShift) {
    const registerLabel =
      openShift.registerCode && openShift.registerName
        ? `${openShift.registerCode} — ${openShift.registerName}`
        : openShift.registerCode || openShift.registerName || t("shift.noRegisterOnShift");
    const transactionCount =
      summary != null
        ? summary.completedCashCount + summary.completedGCashCount + summary.completedUtangCount
        : null;

    return (
      <Card
        className="flex min-w-0 flex-col gap-3 p-4"
        data-testid="cashier-my-register-open"
      >
        <div className="flex min-w-0 flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="m-0 truncate text-[length:var(--exits-text-md)] font-semibold">
              {registerLabel}
            </p>
            {branchName ? (
              <p className="mb-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">{branchName}</p>
            ) : null}
          </div>
          <StatusChip tone="success">{t("shift.statusOpen")}</StatusChip>
        </div>

        <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
          {displayName ? (
            <div>
              <dt className="m-0 text-muted">{t("register.cashierLabel")}</dt>
              <dd className="m-0 font-semibold">{displayName}</dd>
            </div>
          ) : null}
          <div>
            <dt className="m-0 text-muted">{t("register.openedLabel")}</dt>
            <dd className="m-0 font-semibold">{formatOpenedWhen(openShift.openedAtUtc)}</dd>
          </div>
          <div>
            <dt className="m-0 text-muted">{t("register.openingCashLabel")}</dt>
            <dd className="m-0 font-semibold tabular-nums">
              {formatPeso(openShift.openingCashAmount)}
            </dd>
          </div>
          {transactionCount != null ? (
            <div>
              <dt className="m-0 text-muted">{t("register.transactionsLabel")}</dt>
              <dd className="m-0 font-semibold tabular-nums">{transactionCount}</dd>
            </div>
          ) : null}
          {summary != null ? (
            <div>
              <dt className="m-0 text-muted">{t("register.cashSalesLabel")}</dt>
              <dd className="m-0 font-semibold tabular-nums">
                {formatPeso(summary.cashSalesTotal)}
              </dd>
            </div>
          ) : null}
        </dl>

        <div className="flex min-w-0 flex-col gap-2 sm:flex-row sm:flex-wrap">
          <Button asChild className="min-h-11 w-full sm:w-auto" data-testid="cashier-continue-selling">
            <Link to="/sell">
              <ShoppingCart className="size-4 shrink-0" aria-hidden />
              {t("register.continueSelling")}
            </Link>
          </Button>
          <Button
            asChild
            variant="outline"
            className="min-h-11 w-full sm:w-auto"
            data-testid="cashier-view-my-shift"
          >
            <Link to={`/shifts/${openShift.shiftId}`}>{t("register.viewMyShift")}</Link>
          </Button>
          <Button
            asChild
            variant="outline"
            className="min-h-11 w-full sm:w-auto"
            data-testid="cashier-close-shift"
          >
            <Link to={`/shifts/${openShift.shiftId}`}>
              <DoorClosed className="size-4 shrink-0" aria-hidden />
              {t("shift.closeTitle")}
            </Link>
          </Button>
        </div>
      </Card>
    );
  }

  return (
    <Card className="flex min-w-0 flex-col gap-3 p-4" data-testid="cashier-my-register-ready">
      {branchName ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{branchName}</p>
      ) : null}
      <div className="flex min-w-0 flex-wrap items-center gap-2">
        <StatusChip tone="info">{t("register.statusReady")}</StatusChip>
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("register.noOpenShift")}
        </span>
      </div>

      {ensuringPwa ? (
        <LoadingState label={t("register.preparingMine")} />
      ) : null}
      {pwaError && !ensuringPwa ? (
        <p
          className="mb-0 text-[length:var(--exits-text-sm)] text-destructive"
          role="alert"
          data-testid="cashier-register-prepare-error"
        >
          {t("register.cashierNoRegister")}
        </p>
      ) : null}

      {canOpenShift && !ensuringPwa ? (
        <Button asChild className="min-h-11 w-full sm:w-auto" data-testid="cashier-open-shift">
          <Link to="/shifts/open">
            <Play className="size-4 shrink-0" aria-hidden />
            {t("shift.openTitle")}
          </Link>
        </Button>
      ) : null}
    </Card>
  );
}

function ManagerRegistersPanel({
  canManage,
  loading,
  error,
  registers,
  resolveActorName,
}: {
  canManage: boolean;
  loading: boolean;
  error: boolean;
  registers: PosRegisterDto[];
  resolveActorName: (actorId: string | null | undefined) => string | null;
}) {
  const { t } = useI18n();
  return (
    <>
      {!canManage ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="register-view-only"
        >
          {t("register.viewOnly")}
        </p>
      ) : null}

      {loading ? <LoadingSkeleton label={t("loading.label")} /> : null}
      {error ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("register.loadError")}
          </p>
        </Card>
      ) : null}

      <ul
        className="exits-list m-0 grid list-none gap-2 p-0 sm:grid-cols-2"
        data-testid="registers-list"
      >
        {registers.map((register) => {
          const open = register.hasOpenShift;
          const cashierName = open
            ? resolveActorName(register.openShiftActorId)?.trim() ||
              t("shift.registerInUseUnknownOpener")
            : null;
          return (
            <li key={register.registerId}>
              <Card
                data-testid={`register-row-${register.registerId}`}
                className="flex min-w-0 flex-col gap-2 p-3"
              >
                <div className="flex min-w-0 flex-wrap items-start justify-between gap-2">
                  <p className="m-0 min-w-0 truncate text-[length:var(--exits-text-sm)] font-semibold">
                    {register.registerCode} — {register.name}
                  </p>
                  <StatusChip tone={register.status === "Active" ? "success" : "info"}>
                    {register.status}
                  </StatusChip>
                </div>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {open ? t("register.openShiftStatus") : t("register.availableStatus")}
                </p>
                {open && cashierName ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)]">
                    <span className="text-muted">{t("register.cashierLabel")}: </span>
                    <span className="font-medium">{cashierName}</span>
                  </p>
                ) : null}
                {open && register.openShiftOpenedAtUtc ? (
                  <p className="m-0 text-[length:var(--exits-text-sm)]">
                    <span className="text-muted">{t("register.openedLabel")}: </span>
                    <span className="font-medium">
                      {formatOpenedWhen(register.openShiftOpenedAtUtc)}
                    </span>
                  </p>
                ) : null}
                <div className="mt-1 flex min-w-0 flex-col gap-2 sm:flex-row sm:flex-wrap">
                  {open && register.openShiftId ? (
                    <Button
                      asChild
                      variant="outline"
                      size="sm"
                      className="w-full sm:w-fit"
                      data-testid={`register-view-shift-${register.registerId}`}
                    >
                      <Link to={`/shifts/${register.openShiftId}`}>{t("register.viewShift")}</Link>
                    </Button>
                  ) : open ? (
                    <Button
                      asChild
                      variant="outline"
                      size="sm"
                      className="w-full sm:w-fit"
                      data-testid={`register-view-shift-${register.registerId}`}
                    >
                      <Link to="/shifts">{t("register.viewShift")}</Link>
                    </Button>
                  ) : null}
                  <Button
                    asChild
                    variant="outline"
                    size="sm"
                    className="w-full sm:w-fit"
                    data-testid={`register-history-${register.registerId}`}
                  >
                    <Link to={`/registers/${register.registerId}/history`}>
                      <History className="size-3.5 shrink-0" aria-hidden />
                      {t("register.viewHistory")}
                    </Link>
                  </Button>
                  <Button
                    asChild
                    variant="outline"
                    size="sm"
                    className="w-full sm:w-fit"
                    data-testid={`register-transactions-${register.registerId}`}
                  >
                    <Link to={`/registers/${register.registerId}/transactions`}>
                      <ReceiptText className="size-3.5 shrink-0" aria-hidden />
                      {t("register.viewTransactions")}
                    </Link>
                  </Button>
                </div>
              </Card>
            </li>
          );
        })}
      </ul>

      {registers.length === 0 && !loading ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="registers-empty"
        >
          {t("register.empty")}
        </p>
      ) : null}
    </>
  );
}
