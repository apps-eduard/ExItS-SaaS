import { useMemo, useState } from "react";
import {
  Banknote,
  CircleAlert,
  DoorClosed,
  Loader2,
  MessageSquareText,
  ShoppingCart,
  SkipForward,
} from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageShifts, canViewShifts } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import {
  listCashDenominations,
  mapEnabledCashDenominations,
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
import { pageBackNav } from "@/navigation/page-back-nav";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { StatusChip } from "@/components/exits/StatusChip";
import { DenominationCountHelper } from "@/features/shifts/DenominationCountHelper";
import { ShiftCashHistoryPanel } from "@/features/shifts/ShiftCashHistoryPanel";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
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
    enabled:
      workspaceScope !== null &&
      canManage &&
      Boolean(shiftQuery.data) &&
      isOpenCashierShift(shiftQuery.data),
    queryFn: ({ signal }) => listCashDenominations(workspaceScope!, signal),
    staleTime: 0,
    refetchOnMount: "always",
  });

  const enabledDenoms = useMemo(
    () => mapEnabledCashDenominations(denomsQuery.data),
    [denomsQuery.data],
  );

  const shiftActors = useActorDirectory(workspaceScope?.organizationId, [
    shiftQuery.data?.openedBy,
    shiftQuery.data?.closedBy,
    shiftQuery.data?.cancelledBy,
  ]);

  if (!canView) {
    return (
      <div data-testid="shift-detail-denied" className="flex flex-col gap-3">
        <PageHeader
          title={t("shift.detailTitle")}
          description={t("shift.deniedDetail")}
          backTo={pageBackNav.managerHome.to}
          backLabel={t(pageBackNav.managerHome.labelKey)}
          backTestId="page-header-back-shifts"
        />
      </div>
    );
  }

  if (!workspaceScope || shiftQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (shiftQuery.isError || !shiftQuery.data) {
    return (
      <div data-testid="shift-detail-missing" className="flex flex-col gap-3">
        <PageHeader
          title={t("shift.detailTitle")}
          description={t("shift.notFound")}
          backTo={pageBackNav.shifts.to}
          backLabel={t(pageBackNav.shifts.labelKey)}
          backTestId="page-header-back-shifts"
        />
      </div>
    );
  }

  const shift = shiftQuery.data;
  const summary = summaryQuery.data;
  const open = isOpenCashierShift(shift);
  const closed = !open;
  const closingModeLive =
    shift.effectiveClosingCashCountMode?.trim() || shift.effectiveCashCountMode;
  const closingRequired = resolveCashCountRequired(closingModeLive);

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
      if (isLikelyNetworkFailure(error)) {
        setClosingError(t("checkout.confirmingTransaction"));
        try {
          const confirmed = await getCashierShift(workspaceScope!, shift.shiftId);
          if (confirmed.status.toLowerCase() === "closed") {
            await refresh();
            await shiftQuery.refetch();
            await summaryQuery.refetch();
            setClosingError(null);
            return;
          }
          setClosingError(
            error instanceof PosApiError
              ? (error.problem.detail ?? error.message)
              : t("shift.closeError"),
          );
        } catch {
          setClosingError(t("checkout.transactionStatusUnknown"));
        }
        return;
      }
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
    <div
      data-testid="shift-detail-page"
      className="mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-4"
    >
      <PageHeader
        title={shift.shiftNumber}
        description={t("shift.detailLede")}
        backTo={pageBackNav.shifts.to}
        backLabel={t(pageBackNav.shifts.labelKey)}
        backTestId="page-header-back-shifts"
      />

      <div data-testid="shift-status-chip">
        <StatusChip tone={open ? "success" : "info"}>
          {open ? t("shift.statusOpen") : shift.status}
        </StatusChip>
      </div>

      <div className="flex flex-col gap-3" data-testid="shift-actor-attribution">
        <ActorAttribution
          labelKey="common.openedBy"
          actorId={shift.openedBy}
          occurredAtUtc={shift.openedAtUtc}
          resolved={shiftActors.resolve(shift.openedBy)}
          isLoading={shiftActors.isResolving}
          testId="shift-opened-by"
        />
        {shift.closedAtUtc || shift.closedBy ? (
          <ActorAttribution
            labelKey="common.closedBy"
            actorId={shift.closedBy}
            occurredAtUtc={shift.closedAtUtc}
            resolved={shiftActors.resolve(shift.closedBy)}
            isLoading={shiftActors.isResolving}
            testId="shift-closed-by"
          />
        ) : null}
        {shift.cancelledAtUtc || shift.cancelledBy ? (
          <ActorAttribution
            labelKey="common.cancelledBy"
            actorId={shift.cancelledBy}
            occurredAtUtc={shift.cancelledAtUtc}
            resolved={shiftActors.resolve(shift.cancelledBy)}
            isLoading={shiftActors.isResolving}
            testId="shift-cancelled-by"
          />
        ) : null}
      </div>

      <ShiftCashHistoryPanel shift={shift} summary={summary} closed={closed} />

      {open && canManage ? (
        <Card
          data-testid="shift-close-panel"
          className="flex flex-col gap-4 border-primary/15 bg-surface"
        >
          <div className="flex items-start gap-2.5">
            <DoorClosed className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
            <div className="min-w-0 flex-1">
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("shift.closeTitle")}
              </h2>
              <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("shift.denomHelperHint")}
              </p>
            </div>
          </div>

          <div className="rounded-[var(--exits-radius-md)] border border-border bg-surface-muted/30 p-3">
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

          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
            <span className="inline-flex items-center gap-2 font-medium">
              <Banknote className="size-4 shrink-0 text-primary" aria-hidden />
              {t("shift.closingCashLabel")}
            </span>
            <input
              data-testid="shift-closing-cash"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums shadow-sm"
              value={closingCash}
              onChange={(event) => {
                setClosingCash(event.target.value);
                setDenomLines([]);
              }}
            />
          </label>

          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)]">
            <span className="inline-flex items-center gap-2 font-medium">
              <MessageSquareText className="size-4 shrink-0 text-primary" aria-hidden />
              {t("shift.closingNotesLabel")}
            </span>
            <input
              data-testid="shift-closing-notes"
              type="text"
              maxLength={512}
              placeholder={t("shift.closingNotesLabel")}
              className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 shadow-sm"
              value={closingNotes}
              onChange={(event) => setClosingNotes(event.target.value)}
            />
          </label>

          {closingError ? (
            <div
              className="flex items-start gap-2 rounded-[var(--exits-radius-md)] border border-destructive/30 bg-destructive/5 px-3 py-2"
              role="alert"
            >
              <CircleAlert className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden />
              <p className="mb-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
                {closingError}
              </p>
            </div>
          ) : null}

          <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
            {!closingRequired ? (
              <Button
                type="button"
                variant="ghost"
                className="w-full sm:w-auto"
                disabled={saving}
                data-testid="shift-close-skip-cash"
                onClick={() => void onClose(true)}
              >
                <SkipForward className="size-5 shrink-0" aria-hidden />
                {t("shift.skipClosingCash")}
              </Button>
            ) : null}
            <Button
              type="button"
              className="w-full flex-1 sm:w-auto"
              disabled={saving}
              data-testid="shift-close-confirm"
              onClick={() => void onClose(false)}
            >
              {saving ? (
                <Loader2 className="size-5 shrink-0 animate-spin" aria-hidden />
              ) : (
                <DoorClosed className="size-5 shrink-0" aria-hidden />
              )}
              {saving ? t("shift.closing") : t("shift.closeConfirm")}
            </Button>
          </div>
        </Card>
      ) : null}

      <div className="grid grid-cols-1 gap-2">
        {open ? (
          <Button
            type="button"
            className="inline-flex items-center justify-center gap-2"
            data-testid="shift-go-sell"
            onClick={() => navigate("/sell")}
          >
            <ShoppingCart className="size-5 shrink-0" aria-hidden />
            {t("role.openSellFloor")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
