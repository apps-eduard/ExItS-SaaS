import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeftRight,
  CircleDollarSign,
  ClipboardList,
  Clock3,
  MonitorSmartphone,
  Receipt,
  ShoppingCart,
  Store,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import {
  canCreateSale,
  canManageShifts,
  canViewCustomerOrders,
  canViewDashboard,
  canViewRegisters,
  canViewReturns,
  canViewShifts,
  isPosCashierRole,
} from "@/access/pos-capabilities";
import { getDashboard } from "@/api/pos/pos-reporting-client";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  ManagerActionCard,
  ManagerActionGrid,
  ManagerHomeSection,
  ManagerMetricCard,
  ManagerMetricStrip,
} from "@/features/role/ManagerHomeShared";
import { resolveReportDatePreset } from "@/features/reports/report-date-range";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { useSellingMode } from "@/selling/SellingModeProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Cashier workspace home — front-counter focus only.
 * Presentation only: does not change role or permissions.
 */
export function CashierHomePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { enter } = useSellingMode();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const { hasOpenShift, currentShift, loading: shiftLoading } = useShiftContext();

  const canSell = canCreateSale(sessionGrant, boundWorkspace?.branchType);
  const canShifts = canViewShifts(sessionGrant);
  const canOpenShift = canManageShifts(sessionGrant);
  const canRegisters = canViewRegisters(sessionGrant);
  const canOrders = canViewCustomerOrders(sessionGrant);
  const canReturns = canViewReturns(sessionGrant);
  const canTodayMetrics = canViewDashboard(sessionGrant);
  const isCashierRole = isPosCashierRole(sessionGrant);

  const todayRange = resolveReportDatePreset("today");
  const organizationId = boundWorkspace?.organizationId ?? null;
  const branchId = boundWorkspace?.branchId ?? null;
  const workspaceScope =
    organizationId && branchId
      ? { organizationId, branchId }
      : null;

  const dashboardQuery = useQuery({
    queryKey: [
      "pos",
      "cashier-home",
      "dashboard",
      organizationId,
      branchId,
      todayRange.fromDate,
      todayRange.toDate,
    ],
    enabled: Boolean(canTodayMetrics && sessionGrant?.accessToken && workspaceScope),
    staleTime: 30_000,
    meta: { suppressGlobalError: true, operation: "cashier home dashboard" },
    queryFn: ({ signal }) => getDashboard(workspaceScope!, todayRange, signal, branchId),
  });

  const salesTotal = dashboardQuery.data?.completedSalesTotal ?? 0;
  const saleCount = dashboardQuery.data?.completedSaleCount ?? 0;

  const registerName = currentShift?.registerName?.trim() || undefined;
  const registerCode = currentShift?.registerCode?.trim() || undefined;
  const registerId = currentShift?.registerId?.trim() || undefined;
  const shiftNumber = currentShift?.shiftNumber?.trim() || undefined;
  const registerLabel =
    registerCode && registerName
      ? `${registerCode} — ${registerName}`
      : registerCode || registerName || t("managerHome.register.none");

  const shiftMetricTo =
    hasOpenShift && currentShift?.shiftId
      ? `/shifts/${currentShift.shiftId}`
      : canOpenShift
        ? "/shifts/open"
        : "/shifts";
  const registerMetricTo = registerId ? `/registers/${registerId}/history` : "/registers";

  function startSelling() {
    enter("/role/cashier");
    navigate("/sell");
  }

  const primaryNeedsShift = canSell && canOpenShift && !hasOpenShift && !shiftLoading;
  const primarySellLabel = hasOpenShift
    ? t("register.continueSelling")
    : t("role.startSelling");

  return (
    <div
      className="manager-ops-home manager-home-page exits-page mx-auto flex w-full max-w-[80rem] min-w-0 flex-col gap-2.5"
      data-testid="cashier-home"
      data-home-variant="cashier"
    >
      <PageHeader
        title={t("role.cashierTitle")}
        subtitle={boundWorkspace?.branchName?.trim() || undefined}
        description={t("role.cashierLede")}
        descriptionCollapsible={false}
      />

      <ManagerHomeSection title={t("managerHome.section.today")} testId="cashier-home-session">
        <ManagerMetricStrip>
          {canShifts ? (
            <ManagerMetricCard
              label={t("managerHome.today.shift")}
              icon={Clock3}
              tone={hasOpenShift ? "success" : "warning"}
              badge={
                hasOpenShift ? (
                  <StatusChip tone="success">{t("managerHome.shift.open")}</StatusChip>
                ) : undefined
              }
              value={
                shiftLoading
                  ? "…"
                  : hasOpenShift
                    ? (shiftNumber ?? t("managerHome.shift.open"))
                    : t("managerHome.shift.closed")
              }
              valueScale="restrained"
              testId="cashier-today-shift"
              to={shiftMetricTo}
            />
          ) : null}
          {canShifts ? (
            <ManagerMetricCard
              label={t("managerHome.today.register")}
              value={hasOpenShift ? registerLabel : t("managerHome.register.none")}
              icon={Store}
              tone="primary"
              valueScale="restrained"
              testId="cashier-today-register"
              to={hasOpenShift ? registerMetricTo : "/registers"}
            />
          ) : null}
          {canTodayMetrics ? (
            <ManagerMetricCard
              label={t("managerHome.today.sales")}
              value={formatPeso(salesTotal)}
              hint={salesTotal <= 0 ? t("managerHome.today.noSales") : undefined}
              icon={CircleDollarSign}
              tone="primary"
              testId="cashier-today-sales"
            />
          ) : null}
          {canTodayMetrics ? (
            <ManagerMetricCard
              label={t("managerHome.today.transactions")}
              value={saleCount}
              icon={Receipt}
              tone="primary"
              testId="cashier-today-transactions"
            />
          ) : null}
        </ManagerMetricStrip>
      </ManagerHomeSection>

      <ManagerHomeSection title={t("role.section.quickActions")} testId="cashier-quick-actions">
        <ManagerActionGrid>
          {primaryNeedsShift ? (
            <ManagerActionCard
              label={t("managerHome.shift.openAction")}
              icon={Clock3}
              testId="cashier-primary-open-shift"
              to="/shifts/open"
            />
          ) : canSell ? (
            <ManagerActionCard
              label={primarySellLabel}
              icon={ShoppingCart}
              testId="cashier-primary-sell"
              onClick={startSelling}
            />
          ) : null}

          {canOrders ? (
            <ManagerActionCard
              label={t("orders.openQueue")}
              icon={ClipboardList}
              testId="cashier-action-orders"
              to="/orders"
            />
          ) : null}

          {canReturns ? (
            <ManagerActionCard
              label={t("returns.open")}
              icon={Receipt}
              testId="cashier-action-returns"
              to="/returns"
            />
          ) : null}

          {canShifts ? (
            <ManagerActionCard
              label={
                hasOpenShift && currentShift?.shiftId
                  ? t("managerHome.shift.view")
                  : isCashierRole
                    ? t("shift.myHubTitle")
                    : t("org.more.shifts")
              }
              icon={Clock3}
              testId="cashier-action-shift"
              to={
                hasOpenShift && currentShift?.shiftId
                  ? `/shifts/${currentShift.shiftId}`
                  : "/shifts"
              }
            />
          ) : null}

          {canRegisters ? (
            <ManagerActionCard
              label={isCashierRole ? t("register.myTitle") : t("register.listTitle")}
              icon={MonitorSmartphone}
              testId="cashier-action-register"
              to="/registers"
            />
          ) : null}

          <ManagerActionCard
            label={t("workspace.switch")}
            icon={ArrowLeftRight}
            testId="cashier-action-switch-workspace"
            to="/workspace"
          />
        </ManagerActionGrid>
      </ManagerHomeSection>
    </div>
  );
}
