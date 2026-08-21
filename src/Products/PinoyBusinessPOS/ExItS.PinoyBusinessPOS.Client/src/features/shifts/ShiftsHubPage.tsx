import { Link } from "react-router-dom";
import { canManageShifts, canViewShifts } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
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

  if (!canView) {
    return (
      <div data-testid="shifts-hub-denied" className="flex flex-col gap-3">
        <PageHeader title={t("shift.hubTitle")} description={t("shift.deniedDetail")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/">{t("notFound.home")}</Link>
        </Button>
      </div>
    );
  }

  return (
    <div data-testid="shifts-hub-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("shift.hubTitle")} description={t("shift.hubLede")} />

      {loading ? <LoadingSkeleton label={t("loading.label")} /> : null}

      {errorMessage ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {errorMessage}
          </p>
          <Button
            type="button"
            variant="ghost"
            className="mt-2 min-h-11"
            onClick={() => void refresh()}
          >
            {t("shift.retry")}
          </Button>
        </Card>
      ) : null}

      <Card data-testid="shift-readiness-card">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("shift.readinessLabel")}
        </p>
        <p
          className="mb-0 mt-2 text-[length:var(--exits-text-sm)]"
          data-testid="shift-readiness-status"
        >
          {readiness.status === "ready"
            ? t("shift.readinessReady")
            : readiness.status === "loading"
              ? t("loading.label")
              : readiness.status === "blocked_denied"
                ? t("shift.readinessDenied")
                : readiness.status === "blocked_closed"
                  ? t("shift.readinessClosed")
                  : readiness.status === "blocked_no_register"
                    ? t("shift.readinessNoRegister")
                    : t("shift.readinessBlocked")}
        </p>
      </Card>

      {hasOpenShift && currentShift ? (
        <Card data-testid="shift-current-banner">
          <div className="flex flex-wrap items-center gap-2">
            <StatusChip tone="success">{t("shift.statusOpen")}</StatusChip>
            <span className="text-[length:var(--exits-text-sm)] font-semibold">
              {currentShift.shiftNumber}
            </span>
          </div>
          <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {currentShift.registerCode
              ? `${currentShift.registerCode} — ${currentShift.registerName ?? ""}`
              : t("shift.noRegisterOnShift")}
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button asChild className="min-h-11">
              <Link to={`/shifts/${currentShift.shiftId}`} data-testid="shift-open-detail">
                {t("shift.viewCurrent")}
              </Link>
            </Button>
            <Button asChild variant="ghost" className="min-h-11">
              <Link to="/sell">{t("role.openSellFloor")}</Link>
            </Button>
          </div>
        </Card>
      ) : (
        <Card data-testid="shift-none-banner">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{t("shift.noneMessage")}</p>
          {canManage ? (
            <Button asChild className="mt-3 min-h-11" data-testid="shift-go-open">
              <Link to="/shifts/open">{t("shift.openTitle")}</Link>
            </Button>
          ) : (
            <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
              {t("shift.manageDeniedDetail")}
            </p>
          )}
        </Card>
      )}
    </div>
  );
}
