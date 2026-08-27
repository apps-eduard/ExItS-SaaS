import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Play, RotateCcw } from "lucide-react";
import { ActionButtonLoading } from "@/components/exits/loading/ActionButtonLoading";
import { canManageRegisters, canManageShifts, canViewShifts } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getOperationalSetup,
  listCashDenominations,
  mapEnabledCashDenominations,
  resolveCashCountRequired,
  resolveOpeningCashCountMode,
  resolveOpeningCashVisible,
} from "@/api/pos/pos-operational-setup-client";
import { listRegistersAvailableForShift } from "@/api/pos/pos-registers-client";
import {
  getCurrentCashierShift,
  openCashierShift,
  type CashCountDenominationLineDto,
} from "@/api/pos/pos-shifts-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { pageBackNav } from "@/navigation/page-back-nav";
import { DenominationCountHelper } from "@/features/shifts/DenominationCountHelper";
import {
  ensurePwaDefaultCashRegister,
  PWA_DEFAULT_REGISTER_NAME,
} from "@/features/shifts/ensure-pwa-default-register";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ShiftOpenPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant, deviceEnforcementEnabled } = useWorkspace();
  const { refresh } = useShiftContext();

  const canView = canViewShifts(sessionGrant);
  const canManage = canManageShifts(sessionGrant);
  const canCreateRegister = canManageRegisters(sessionGrant);
  // Pure React PWA: device enforcement paused → allow auto cash register PWA-0001.
  const pwaOptionalCashRegister = deviceEnforcementEnabled === false;

  const workspaceScope = useMemo(() => {
    if (!boundWorkspace?.branchId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const [selectedRegisterId, setSelectedRegisterId] = useState<string>("");
  const [openingCash, setOpeningCash] = useState<string>("");
  const [denomLines, setDenomLines] = useState<CashCountDenominationLineDto[]>([]);
  const [openingCashError, setOpeningCashError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [ensuringPwaRegister, setEnsuringPwaRegister] = useState(false);
  const [pwaRegisterError, setPwaRegisterError] = useState<string | null>(null);
  const pwaEnsureAttemptedRef = useRef(false);

  const registersQuery = useQuery({
    queryKey: ["pos-registers-available", workspaceScope?.organizationId, workspaceScope?.branchId],
    enabled: workspaceScope !== null && canManage,
    queryFn: ({ signal }) => listRegistersAvailableForShift(workspaceScope!, signal),
  });

  const setupQuery = useQuery({
    queryKey: ["pos-operational-setup", workspaceScope?.organizationId],
    enabled: workspaceScope !== null && canManage,
    queryFn: ({ signal }) => getOperationalSetup(workspaceScope!, signal),
  });

  const denomsQuery = useQuery({
    queryKey: ["pos-cash-denominations", workspaceScope?.organizationId],
    enabled: workspaceScope !== null && canManage,
    queryFn: ({ signal }) => listCashDenominations(workspaceScope!, signal),
    staleTime: 0,
    refetchOnMount: "always",
  });

  const registers = useMemo(() => registersQuery.data ?? [], [registersQuery.data]);
  const openingMode = resolveOpeningCashCountMode(setupQuery.data);
  const currencyCode = setupQuery.data?.currencyCode ?? "PHP";
  const showOpeningCash = resolveOpeningCashVisible(openingMode);
  const openingRequired = resolveCashCountRequired(openingMode);
  const enabledDenoms = useMemo(
    () => mapEnabledCashDenominations(denomsQuery.data),
    [denomsQuery.data],
  );

  const openingCashAmount = Number(openingCash);
  const openingCashPreview =
    openingCash.trim().length > 0 && Number.isFinite(openingCashAmount) && openingCashAmount >= 0
      ? formatPeso(openingCashAmount)
      : null;

  useEffect(() => {
    if (registers.length === 0) {
      setSelectedRegisterId("");
      return;
    }
    setSelectedRegisterId((current) => {
      if (current && registers.some((register) => register.registerId === current)) {
        return current;
      }
      return registers[0]!.registerId;
    });
  }, [registers]);

  // PWA: auto-provision cash register PWA-0001 when none are available for shift.
  useEffect(() => {
    if (
      !pwaOptionalCashRegister ||
      !canCreateRegister ||
      !workspaceScope ||
      !registersQuery.isSuccess ||
      registers.length > 0 ||
      pwaEnsureAttemptedRef.current
    ) {
      return;
    }

    pwaEnsureAttemptedRef.current = true;
    let cancelled = false;
    setEnsuringPwaRegister(true);
    setPwaRegisterError(null);
    void (async () => {
      try {
        const created = await ensurePwaDefaultCashRegister(workspaceScope);
        if (cancelled) {
          return;
        }
        await registersQuery.refetch();
        setSelectedRegisterId(created.registerId);
      } catch (error) {
        if (cancelled) {
          return;
        }
        pwaEnsureAttemptedRef.current = false;
        const message =
          error instanceof Error && error.message === "PWA_DEFAULT_REGISTER_BUSY"
            ? t("shift.pwaRegisterBusy")
            : error instanceof PosApiError
              ? (error.problem.detail ?? error.message)
              : error instanceof TypeError && /digest/i.test(error.message)
                ? t("shift.pwaRegisterError")
                : error instanceof Error
                  ? error.message
                  : t("shift.pwaRegisterError");
        setPwaRegisterError(message);
      } finally {
        if (!cancelled) {
          setEnsuringPwaRegister(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- one-shot ensure when empty
  }, [
    pwaOptionalCashRegister,
    canCreateRegister,
    workspaceScope,
    registersQuery.isSuccess,
    registers.length,
  ]);

  useEffect(() => {
    if (!workspaceScope || !canView) {
      return;
    }
    let cancelled = false;
    void (async () => {
      try {
        const current = await getCurrentCashierShift(workspaceScope);
        if (!cancelled && current && current.status.toLowerCase() === "open") {
          // Already open — go sell; close lives on Shifts hub / shift detail.
          navigate("/sell", { replace: true });
        }
      } catch {
        // Stay on open form; submit path will surface errors.
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [canView, navigate, workspaceScope]);

  if (!canView) {
    return (
      <div
        data-testid="shift-open-denied"
        className="shift-open-page exits-page flex flex-col gap-3"
      >
        <PageHeader
          title={t("shift.openTitle")}
          description={t("shift.deniedDetail")}
          backTo={pageBackNav.shifts.to}
          backLabel={t(pageBackNav.shifts.labelKey)}
          backTestId="page-header-back-shifts"
        />
      </div>
    );
  }

  if (!canManage) {
    return (
      <div
        data-testid="shift-open-denied"
        className="shift-open-page exits-page flex flex-col gap-3"
      >
        <PageHeader
          title={t("shift.openTitle")}
          description={t("shift.manageDeniedDetail")}
          backTo={pageBackNav.shifts.to}
          backLabel={t(pageBackNav.shifts.labelKey)}
          backTestId="page-header-back-shifts"
        />
      </div>
    );
  }

  if (!workspaceScope) {
    return <BranchRequiredPanel title={t("shift.openTitle")} />;
  }

  async function onOpen(skipOpeningCash: boolean) {
    if (saving || !selectedRegisterId) {
      return;
    }

    setOpeningCashError(null);
    setSubmitError(null);

    let amount: number | null = null;
    if (showOpeningCash && !skipOpeningCash) {
      const parsed = Number(openingCash);
      if (!Number.isFinite(parsed) || parsed < 0) {
        setOpeningCashError(t("shift.openingCashInvalid"));
        return;
      }
      amount = parsed;
    } else if (openingRequired && (skipOpeningCash || openingCash.trim().length === 0)) {
      setOpeningCashError(t("shift.openingCashRequired"));
      return;
    }

    setSaving(true);
    try {
      const existing = await getCurrentCashierShift(workspaceScope!);
      if (existing && existing.status.toLowerCase() === "open") {
        await refresh();
        navigate("/sell", { replace: true });
        return;
      }

      await openCashierShift(workspaceScope!, {
        registerId: selectedRegisterId,
        openingCashAmount: amount,
        denominationLines: amount !== null && denomLines.length > 0 ? denomLines : null,
      });
      await refresh();
      // After open, land on Sell — not shift detail (close is available from Shifts).
      navigate("/sell", { replace: true });
    } catch (error) {
      const message =
        error instanceof PosApiError
          ? (error.problem.detail ?? error.message)
          : error instanceof Error
            ? error.message
            : t("shift.openError");
      setSubmitError(message);
      void registersQuery.refetch();
    } finally {
      setSaving(false);
    }
  }

  const openingCashValid =
    !showOpeningCash ||
    !openingRequired ||
    (openingCash.trim().length > 0 &&
      Number.isFinite(Number(openingCash)) &&
      Number(openingCash) >= 0);

  const startBlocked =
    saving ||
    ensuringPwaRegister ||
    !selectedRegisterId ||
    registers.length === 0 ||
    registersQuery.isLoading ||
    Boolean(registersQuery.error) ||
    !openingCashValid;

  const startBlockedReason = ensuringPwaRegister
    ? t("shift.pwaRegisterPreparing")
    : !selectedRegisterId
      ? t("shift.openSelectRegisterHint")
      : !openingCashValid
        ? t("shift.openingCashRequired")
        : t("shift.openReadyHint");

  return (
    <div
      data-testid="shift-open-page"
      className="shift-open-page exits-page flex min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={t("shift.openTitle")}
        description={t("shift.openLede")}
        backTo={pageBackNav.shifts.to}
        backLabel={t(pageBackNav.shifts.labelKey)}
        backTestId="page-header-back-shifts"
      />

      {submitError ? (
        <div className="exits-alert exits-alert--error" data-testid="shift-open-error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t("error.title")}</p>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)]">{submitError}</p>
          <Button
            type="button"
            variant="outline"
            className="mt-3 min-h-11"
            onClick={() => void registersQuery.refetch()}
          >
            <RotateCcw className="size-4 shrink-0" aria-hidden />
            {t("shift.retry")}
          </Button>
        </div>
      ) : null}

      <section className="catalog-form-section exits-animate-panel">
        <h2 className="catalog-form-section__title">{t("shift.registerSection")}</h2>
        {registersQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        {registersQuery.isError ? (
          <ErrorState title={t("error.title")} detail={t("shift.registersLoadError")} />
        ) : null}
        {registersQuery.isSuccess && registers.length === 0 ? (
          <div data-testid="shift-open-no-register">
            {/*
              FUTURE CAPACITOR / strict cash-register required:
              Uncomment this block and remove the PWA auto-provision path below
              when PosDeviceAuthorization:EnforcementEnabled=true for native installs.

            <EmptyState title={t("shift.noRegisterTitle")} detail={t("shift.noRegisterMessage")} />
            <Button asChild variant="outline" className="mt-3 min-h-11">
              <Link to="/registers">{t("shift.goToRegisters")}</Link>
            </Button>
            */}

            {pwaOptionalCashRegister ? (
              <div className="flex flex-col gap-2" data-testid="shift-open-pwa-register">
                <EmptyState
                  title={t("shift.pwaRegisterTitle")}
                  detail={t("shift.pwaRegisterDetail").replace(
                    "{name}",
                    PWA_DEFAULT_REGISTER_NAME,
                  )}
                />
                {ensuringPwaRegister ? (
                  <LoadingState label={t("shift.pwaRegisterPreparing")} />
                ) : null}
                {pwaRegisterError ? (
                  <p
                    className="mb-0 text-[length:var(--exits-text-sm)] text-destructive"
                    role="alert"
                    data-testid="shift-open-pwa-register-error"
                  >
                    {pwaRegisterError}
                  </p>
                ) : null}
                {!ensuringPwaRegister && canCreateRegister ? (
                  <Button
                    type="button"
                    variant="outline"
                    className="mt-1 min-h-11"
                    data-testid="shift-open-pwa-register-retry"
                    onClick={() => {
                      pwaEnsureAttemptedRef.current = false;
                      setPwaRegisterError(null);
                      setEnsuringPwaRegister(true);
                      pwaEnsureAttemptedRef.current = true;
                      void ensurePwaDefaultCashRegister(workspaceScope!)
                        .then(async (created) => {
                          await registersQuery.refetch();
                          setSelectedRegisterId(created.registerId);
                        })
                        .catch((error: unknown) => {
                          pwaEnsureAttemptedRef.current = false;
                          const message =
                            error instanceof Error && error.message === "PWA_DEFAULT_REGISTER_BUSY"
                              ? t("shift.pwaRegisterBusy")
                              : error instanceof PosApiError
                                ? (error.problem.detail ?? error.message)
                                : error instanceof Error
                                  ? error.message
                                  : t("shift.pwaRegisterError");
                          setPwaRegisterError(message);
                        })
                        .finally(() => setEnsuringPwaRegister(false));
                    }}
                  >
                    {t("shift.pwaRegisterRetry")}
                  </Button>
                ) : null}
              </div>
            ) : (
              <>
                <EmptyState title={t("shift.noRegisterTitle")} detail={t("shift.noRegisterMessage")} />
                <Button asChild variant="outline" className="mt-3 min-h-11">
                  <Link to="/registers">{t("shift.goToRegisters")}</Link>
                </Button>
              </>
            )}
          </div>
        ) : null}
        {registers.length > 0 ? (
          <>
            <ExitsChipBar
              variant="filter"
              ariaLabel={t("shift.registerLabel")}
              testId="shift-register-chips"
              items={registers.map((register) => ({
                key: register.registerId,
                label: `${register.registerCode} — ${register.name}`,
                state: selectedRegisterId === register.registerId ? "active" : "idle",
                testId: `shift-register-chip-${register.registerId}`,
                onSelect: () => setSelectedRegisterId(register.registerId),
              }))}
            />
            {/* Native select kept for keyboard / e2e compatibility; chips are the primary UI. */}
            <label className="sr-only" htmlFor="shift-register-select">
              {t("shift.registerLabel")}
            </label>
            <select
              id="shift-register-select"
              data-testid="shift-register-select"
              className="sr-only"
              value={selectedRegisterId}
              onChange={(event) => setSelectedRegisterId(event.target.value)}
              tabIndex={-1}
              aria-hidden
            >
              <option value="">{t("shift.registerPlaceholder")}</option>
              {registers.map((register) => (
                <option key={register.registerId} value={register.registerId}>
                  {register.registerCode} — {register.name}
                </option>
              ))}
            </select>
          </>
        ) : null}
      </section>

      {showOpeningCash ? (
        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("shift.openingCashSection")}</h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
            {openingRequired
              ? t("shift.openingCashHelpRequired")
              : t("shift.openingCashHelpOptional")}
          </p>
          <div className="mt-1">
            <DenominationCountHelper
              denominations={enabledDenoms}
              currencyCode={currencyCode}
              total={openingCash}
              onTotalChange={setOpeningCash}
              onLinesChange={setDenomLines}
              disabled={saving || ensuringPwaRegister}
              testIdPrefix="opening-denom"
            />
          </div>
          <label className="mt-2 flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            <span>
              {t("shift.openingCashLabel")} ({currencyCode})
            </span>
            <input
              data-testid="shift-opening-cash"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              className="catalog-form-select tabular-nums"
              value={openingCash}
              onChange={(event) => {
                setOpeningCash(event.target.value);
                setDenomLines([]);
              }}
            />
          </label>
          {openingCashPreview ? (
            <p className="shift-open-cash-preview m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("shift.openingCashPreview").replace("{amount}", openingCashPreview)}
            </p>
          ) : null}
          {openingCashError ? (
            <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-destructive">
              {openingCashError}
            </p>
          ) : null}
        </section>
      ) : null}

      <div className="catalog-form-actions shift-open-actions">
        <div className="catalog-form-actions__primary">
          {showOpeningCash && !openingRequired ? (
            <Button
              type="button"
              variant="outline"
              className="catalog-form-actions__restore min-h-11 w-full sm:w-auto"
              disabled={
                saving ||
                !selectedRegisterId ||
                registers.length === 0 ||
                registersQuery.isLoading ||
                Boolean(registersQuery.error)
              }
              data-testid="shift-open-skip-cash"
              onClick={() => void onOpen(true)}
            >
              {t("shift.skipOpeningCash")}
            </Button>
          ) : (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted"
              data-testid="shift-open-hint"
            >
              {startBlockedReason}
            </p>
          )}
        </div>
        <div className="catalog-form-actions__secondary">
          <ActionButtonLoading
            type="button"
            className="catalog-form-actions__save min-h-11"
            disabled={startBlocked}
            loading={saving}
            data-testid="shift-open-confirm"
            data-blocked={startBlocked ? "true" : "false"}
            title={startBlocked ? startBlockedReason : undefined}
            onClick={() => void onOpen(!showOpeningCash)}
          >
            {!saving ? <Play className="size-4 shrink-0" aria-hidden /> : null}
            {saving ? t("shift.opening") : t("shift.openConfirm")}
          </ActionButtonLoading>
        </div>
      </div>
    </div>
  );
}
