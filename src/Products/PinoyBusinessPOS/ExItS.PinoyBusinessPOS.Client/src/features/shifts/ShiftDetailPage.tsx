import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageShifts, canViewShifts } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  listCashDenominations,
  resolveCashCountRequired,
} from "@/api/pos/pos-operational-setup-client";
import {
  closeCashierShift,
  getCashierShift,
  getCashierShiftSummary,
  isOpenCashierShift,
  type CashCountDenominationLineDto,
} from "@/api/pos/pos-shifts-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { DenominationCountHelper } from "@/features/shifts/DenominationCountHelper";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ShiftDetailPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { shiftId = "" } = useParams();
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

  const [closingCash, setClosingCash] = useState("");
  const [denomLines, setDenomLines] = useState<CashCountDenominationLineDto[]>([]);
  const [closingNotes, setClosingNotes] = useState("");
  const [closingError, setClosingError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const shiftQuery = useQuery({
    queryKey: ["pos-cashier-shift", workspaceScope?.organizationId, shiftId],
    enabled: workspaceScope !== null && canView && Boolean(shiftId),
    queryFn: ({ signal }) => getCashierShift(workspaceScope!, shiftId, signal),
  });

  const summaryQuery = useQuery({
    queryKey: ["pos-cashier-shift-summary", workspaceScope?.organizationId, shiftId],
    enabled: workspaceScope !== null && canView && Boolean(shiftId),
    queryFn: ({ signal }) => getCashierShiftSummary(workspaceScope!, shiftId, signal),
  });

  const denomsQuery = useQuery({
    queryKey: ["pos-cash-denominations", workspaceScope?.organizationId],
    enabled: workspaceScope !== null && canManage,
    queryFn: ({ signal }) => listCashDenominations(workspaceScope!, signal),
  });

  if (!canView) {
    return (
      <div data-testid="shift-detail-denied" className="flex flex-col gap-3">
        <PageHeader title={t("shift.detailTitle")} description={t("shift.deniedDetail")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/">{t("notFound.home")}</Link>
        </Button>
      </div>
    );
  }

  if (!workspaceScope || shiftQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (shiftQuery.isError || !shiftQuery.data) {
    return (
      <div data-testid="shift-detail-missing" className="flex flex-col gap-3">
        <PageHeader title={t("shift.detailTitle")} description={t("shift.notFound")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/shifts">{t("shift.backToShifts")}</Link>
        </Button>
      </div>
    );
  }

  const shift = shiftQuery.data;
  const summary = summaryQuery.data;
  const open = isOpenCashierShift(shift);
  const closingMode = shift.effectiveClosingCashCountMode?.trim() || shift.effectiveCashCountMode;
  const closingRequired = resolveCashCountRequired(closingMode);
  const enabledDenoms = (denomsQuery.data ?? [])
    .filter((d) => d.isEnabled)
    .map((d) => ({ value: d.value, label: d.displayLabel }));

  async function onClose(skipClosingCash = false) {
    if (!canManage || !open || saving) {
      return;
    }
    setClosingError(null);

    let amount: number | null = null;
    if (!skipClosingCash) {
      if (closingCash.trim().length > 0 || closingRequired) {
        const parsed = Number(closingCash);
        if (!Number.isFinite(parsed) || parsed < 0) {
          setClosingError(
            closingRequired ? t("shift.closingCashRequired") : t("shift.closingCashInvalid"),
          );
          return;
        }
        amount = parsed;
      }
    } else if (closingRequired) {
      setClosingError(t("shift.closingCashRequired"));
      return;
    }

    setSaving(true);
    try {
      await closeCashierShift(workspaceScope!, shift.shiftId, {
        closingCashAmount: amount,
        notes: closingNotes.trim() || null,
        denominationLines: amount !== null && denomLines.length > 0 ? denomLines : null,
      });
      await refresh();
      await shiftQuery.refetch();
      await summaryQuery.refetch();
    } catch (error) {
      const message =
        error instanceof PosApiError
          ? (error.problem.detail ?? error.message)
          : error instanceof Error
            ? error.message
            : t("shift.closeError");
      setClosingError(message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div data-testid="shift-detail-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={shift.shiftNumber} description={t("shift.detailLede")} />
      <div data-testid="shift-status-chip">
        <StatusChip tone={open ? "success" : "info"}>
          {open ? t("shift.statusOpen") : shift.status}
        </StatusChip>
      </div>

      <Card>
        <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="shift-register-label">
          <span className="font-semibold">{t("shift.registerSection")}: </span>
          {shift.registerCode
            ? `${shift.registerCode} — ${shift.registerName ?? ""}`
            : t("shift.noRegisterOnShift")}
        </p>
        <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
          {t("shift.openingCashLabel")}: <MoneyDisplay amount={shift.openingCashAmount} />
        </p>
        {summary ? (
          <p
            className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="shift-expected-cash"
          >
            {t("shift.expectedCashLabel")}: <MoneyDisplay amount={summary.expectedCashAmount} />
          </p>
        ) : null}
      </Card>

      {open && canManage ? (
        <Card data-testid="shift-close-panel">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("shift.closeTitle")}
          </h2>
          <div className="mt-3">
            <DenominationCountHelper
              denominations={enabledDenoms}
              currencyCode="PHP"
              total={closingCash}
              onTotalChange={setClosingCash}
              onLinesChange={setDenomLines}
              disabled={saving}
              testIdPrefix="closing-denom"
            />
          </div>
          <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("shift.closingCashLabel")}</span>
            <input
              data-testid="shift-closing-cash"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
              value={closingCash}
              onChange={(event) => {
                setClosingCash(event.target.value);
                setDenomLines([]);
              }}
            />
          </label>
          <label className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("shift.closingNotesLabel")}</span>
            <input
              data-testid="shift-closing-notes"
              type="text"
              maxLength={512}
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={closingNotes}
              onChange={(event) => setClosingNotes(event.target.value)}
            />
          </label>
          {closingError ? (
            <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
              {closingError}
            </p>
          ) : null}
          <div className="mt-3 flex flex-wrap gap-2">
            {!closingRequired ? (
              <Button
                type="button"
                variant="ghost"
                className="min-h-11"
                disabled={saving}
                data-testid="shift-close-skip-cash"
                onClick={() => void onClose(true)}
              >
                {t("shift.skipClosingCash")}
              </Button>
            ) : null}
            <Button
              type="button"
              className="min-h-11"
              disabled={saving}
              data-testid="shift-close-confirm"
              onClick={() => void onClose(false)}
            >
              {saving ? t("shift.closing") : t("shift.closeConfirm")}
            </Button>
          </div>
        </Card>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/shifts">{t("shift.backToShifts")}</Link>
        </Button>
        {open ? (
          <Button
            type="button"
            className="min-h-11"
            data-testid="shift-go-sell"
            onClick={() => navigate("/sell")}
          >
            {t("role.openSellFloor")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
