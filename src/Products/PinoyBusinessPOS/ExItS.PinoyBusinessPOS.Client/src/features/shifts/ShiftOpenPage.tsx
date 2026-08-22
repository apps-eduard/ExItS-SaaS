import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
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
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { DenominationCountHelper } from "@/features/shifts/DenominationCountHelper";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
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
      <div data-testid="shift-open-denied" className="flex flex-col gap-3">
        <PageHeader title={t("shift.openTitle")} description={t("shift.deniedDetail")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/">{t("notFound.home")}</Link>
        </Button>
      </div>
    );
  }

  if (!canManage) {
    return (
      <div data-testid="shift-open-denied" className="flex flex-col gap-3">
        <PageHeader title={t("shift.openTitle")} description={t("shift.manageDeniedDetail")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/shifts">{t("shift.backToShifts")}</Link>
        </Button>
      </div>
    );
  }

  if (!workspaceScope) {
    return <LoadingSkeleton label={t("loading.label")} />;
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
    <div data-testid="shift-open-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("shift.openTitle")} description={t("shift.openLede")} />

      {submitError ? (
        <Card data-testid="shift-open-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {submitError}
          </p>
          <Button
            type="button"
            variant="ghost"
            className="mt-2 min-h-11"
            onClick={() => void registersQuery.refetch()}
          >
            {t("shift.retry")}
          </Button>
        </Card>
      ) : null}

      <Card>
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("shift.registerSection")}
        </h2>
        {registersQuery.isLoading ? (
          <LoadingSkeleton label={t("loading.label")} />
        ) : registersQuery.isError ? (
          <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("shift.registersLoadError")}
          </p>
        ) : registers.length === 0 ? (
          <p
            className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="shift-open-no-register"
          >
            {t("shift.noRegisterMessage")}
          </p>
        ) : (
          <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("shift.registerLabel")}</span>
            <select
              data-testid="shift-register-select"
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={selectedRegisterId}
              onChange={(event) => setSelectedRegisterId(event.target.value)}
            >
              <option value="">{t("shift.registerPlaceholder")}</option>
              {registers.map((register) => (
                <option key={register.registerId} value={register.registerId}>
                  {register.registerCode} — {register.name}
                </option>
              ))}
            </select>
          </label>
        )}
      </Card>

      {showOpeningCash ? (
        <Card>
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("shift.openingCashSection")}
          </h2>
          <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
            {openingRequired
              ? t("shift.openingCashHelpRequired")
              : t("shift.openingCashHelpOptional")}
          </p>
          <div className="mt-3">
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
          <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>
              {t("shift.openingCashLabel")} ({currencyCode})
            </span>
            <input
              data-testid="shift-opening-cash"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
              value={openingCash}
              onChange={(event) => {
                setOpeningCash(event.target.value);
                setDenomLines([]);
              }}
            />
          </label>
          {openingCashError ? (
            <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
              {openingCashError}
            </p>
          ) : null}
        </Card>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {showOpeningCash && !openingRequired ? (
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={startBlocked}
            data-testid="shift-open-skip-cash"
            onClick={() => void onOpen(true)}
          >
            {t("shift.skipOpeningCash")}
          </Button>
        ) : null}
        <Button
          type="button"
          className="min-h-11"
          disabled={startBlocked}
          data-testid="shift-open-confirm"
          onClick={() => void onOpen(!showOpeningCash)}
        >
          {saving ? t("shift.opening") : t("shift.openConfirm")}
        </Button>
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/shifts">{t("shift.backToShifts")}</Link>
        </Button>
      </div>
    </div>
  );
}
