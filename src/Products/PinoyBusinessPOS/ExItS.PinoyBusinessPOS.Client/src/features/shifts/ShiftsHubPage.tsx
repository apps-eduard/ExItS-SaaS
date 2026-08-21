import { CheckCircle2, CircleAlert, Clock3, ShoppingCart, Store } from "lucide-react";
import { Link } from "react-router-dom";
import { canManageShifts, canViewShifts } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { RoleActionTile } from "@/components/exits/RoleActionTile";
import { StatusChip } from "@/components/exits/StatusChip";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ShiftsHubPage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const { currentShift, loading, hasOpenShift, errorMessage, refresh, readiness } =
    useShiftContext();

  const canView = canViewShifts(sessionGrant);
  const canManage = canManageShifts(sessionGrant);

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

  if (!canView) {
    return (
      <div data-testid="shifts-hub-denied" className="flex flex-col gap-3">
        <PageHeader title={t("shift.hubTitle")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/">{t("notFound.home")}</Link>
        </Button>
      </div>
    );
  }

  return (
    <div
      data-testid="shifts-hub-page"
      className="mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-4"
    >
      <PageHeader title={t("shift.hubTitle")} />

      {loading ? <LoadingSkeleton label={t("loading.label")} /> : null}

      {errorMessage ? (
        <Card>
          <div className="flex items-start gap-2.5">
            <CircleAlert className="mt-0.5 size-5 shrink-0 text-destructive" aria-hidden />
            <div className="min-w-0 flex-1">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("error.title")}
              </p>
              <p className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted wrap-break-word">
                {errorMessage}
              </p>
              <Button
                type="button"
                variant="ghost"
                className="mt-2 min-h-11 px-0"
                onClick={() => void refresh()}
              >
                {t("shift.retry")}
              </Button>
            </div>
          </div>
        </Card>
      ) : null}

      <Card data-testid="shift-readiness-card">
        <div className="flex items-start gap-2.5">
          {readinessOk ? (
            <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
          ) : (
            <CircleAlert className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
          )}
          <div className="min-w-0 flex-1">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
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
      </Card>

      {hasOpenShift && currentShift ? (
        <Card data-testid="shift-current-banner">
          <div className="flex items-start gap-2.5">
            <Clock3 className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <StatusChip tone="success">{t("shift.statusOpen")}</StatusChip>
                <span className="text-[length:var(--exits-text-sm)] font-semibold">
                  {currentShift.shiftNumber}
                </span>
              </div>
              <p className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                {currentShift.registerCode
                  ? `${currentShift.registerCode} — ${currentShift.registerName ?? ""}`
                  : t("shift.noRegisterOnShift")}
              </p>
            </div>
          </div>
          <div className="mt-3 grid grid-cols-2 gap-2">
            <RoleActionTile
              to={`/shifts/${currentShift.shiftId}`}
              label={t("shift.viewCurrent")}
              icon={Store}
              testId="shift-open-detail"
              primary
            />
            <RoleActionTile to="/sell" label={t("role.openSellFloor")} icon={ShoppingCart} />
          </div>
        </Card>
      ) : (
        <Card data-testid="shift-none-banner">
          <div className="flex items-start gap-2.5">
            <Clock3 className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
            <div className="min-w-0 flex-1">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("shift.noneMessage")}
              </p>
              {!canManage ? (
                <p className="mt-0.5 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("shift.manageDeniedDetail")}
                </p>
              ) : null}
            </div>
          </div>
          {canManage ? (
            <div className="mt-3 grid grid-cols-2 gap-2">
              <RoleActionTile
                to="/shifts/open"
                label={t("shift.openTitle")}
                icon={Clock3}
                testId="shift-go-open"
                primary
                className="col-span-2"
              />
            </div>
          ) : null}
        </Card>
      )}
    </div>
  );
}
