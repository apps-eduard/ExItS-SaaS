import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Loader2, Play, RotateCcw } from "lucide-react";
import { canManageShifts, canViewShifts } from "@/access/pos-capabilities";
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
import { pageBackNav } from "@/navigation/page-back-nav";
import { DenominationCountHelper } from "@/features/shifts/DenominationCountHelper";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ShiftOpenPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const fromSell = searchParams.get("from") === "sell";
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const { refresh } = useShiftContext();

  const canView = canViewShifts(sessionGrant);
  const canManage = canManageShifts(sessionGrant);

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
    if (registers.length === 1) {
      setSelectedRegisterId(registers[0]!.registerId);
    }
  }, [registers]);

  useEffect(() => {
    if (!workspaceScope || !canView) {
      return;
    }
    let cancelled = false;
    void (async () => {
      try {
        const current = await getCurrentCashierShift(workspaceScope);
        if (!cancelled && current && current.status.toLowerCase() === "open") {
          navigate(`/shifts/${current.shiftId}`, { replace: true });
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
    return <LoadingState label={t("loading.label")} />;
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
        navigate(fromSell ? "/sell" : `/shifts/${existing.shiftId}`, { replace: true });
        return;
      }

      const opened = await openCashierShift(workspaceScope!, {
        registerId: selectedRegisterId,
        openingCashAmount: amount,
        denominationLines: amount !== null && denomLines.length > 0 ? denomLines : null,
      });
      await refresh();
      navigate(fromSell ? "/sell" : `/shifts/${opened.shiftId}`, { replace: true });
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

  const startBlocked =
    saving ||
    !selectedRegisterId ||
    registers.length === 0 ||
    registersQuery.isLoading ||
    Boolean(registersQuery.error);

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
            <EmptyState title={t("shift.noRegisterTitle")} detail={t("shift.noRegisterMessage")} />
            <Button asChild variant="outline" className="mt-3 min-h-11">
              <Link to="/registers">{t("shift.goToRegisters")}</Link>
            </Button>
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
              disabled={saving}
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
              disabled={startBlocked}
              data-testid="shift-open-skip-cash"
              onClick={() => void onOpen(true)}
            >
              {t("shift.skipOpeningCash")}
            </Button>
          ) : (
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-muted">
              {selectedRegisterId ? t("shift.openReadyHint") : t("shift.openSelectRegisterHint")}
            </p>
          )}
        </div>
        <div className="catalog-form-actions__secondary">
          <Button
            type="button"
            className="catalog-form-actions__save min-h-11"
            disabled={startBlocked}
            data-testid="shift-open-confirm"
            onClick={() => void onOpen(!showOpeningCash)}
          >
            {saving ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Play className="size-4 shrink-0" aria-hidden />
            )}
            {saving ? t("shift.opening") : t("shift.openConfirm")}
          </Button>
        </div>
      </div>
    </div>
  );
}
