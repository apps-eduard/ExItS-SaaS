import { CheckCircle2, CircleAlert, Clock3, ShoppingCart, Store } from "lucide-react";
import { canManageShifts, canViewShifts } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { PageSkeleton } from "@/components/exits/loading/PageSkeleton";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  ManagerActionCard,
  ManagerActionGrid,
} from "@/features/role/ManagerHomeShared";
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
      <div data-testid="shifts-hub-denied" className="shifts-hub-page flex flex-col gap-3">
        <PageHeader
          title={t("shift.hubTitle")}
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
      className="shifts-hub-page exits-page mx-auto flex w-full max-w-[56rem] min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={t("shift.hubTitle")}
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
    </div>
  );
}
