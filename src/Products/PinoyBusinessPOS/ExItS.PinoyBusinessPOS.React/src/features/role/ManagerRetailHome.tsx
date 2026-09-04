import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeftRight,
  ClipboardList,
  PackagePlus,
  Receipt,
  ShoppingCart,
} from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import {
  canAccessReportsHub,
  canCreateSale,
  canManageExpenses,
  canManageInventory,
  canManagePurchasing,
  canManageShifts,
  canViewCustomerOrders,
  canViewCustomers,
  canViewDashboard,
  canViewInventory,
  canViewPurchasing,
  canViewShifts,
} from "@/access/pos-capabilities";
import { listSellerCustomerOrders, sellerWorkspace } from "@/api/pos/pos-customer-orders-client";
import { listInventoryTransfers } from "@/api/pos/pos-inventory-transfer-client";
import {
  isReceivablePurchaseOrderStatus,
  listPurchaseOrders,
} from "@/api/pos/pos-purchase-orders-client";
import {
  getDashboard,
  getManagementOverview,
} from "@/api/pos/pos-reporting-client";
import { ActionTileGrid, type ActionTileDef } from "@/components/exits/ActionTileGrid";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  buildManagerAttentionItems,
  buildRetailSnapshotModules,
  type ManagerAttentionItem,
} from "@/features/role/manager-home-data";
import {
  ManagerAttentionLink,
  ManagerHealthyAttention,
  ManagerHomeSection,
  ManagerInsightLink,
  ManagerMetricCard,
  ManagerSnapshotLink,
} from "@/features/role/ManagerHomeShared";
import { resolveReportDatePreset } from "@/features/reports/report-date-range";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";
import { useSellingMode } from "@/selling/SellingModeProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function attentionTitle(item: ManagerAttentionItem, t: (key: MessageKey) => string): string {
  switch (item.kind) {
    case "lowStock":
      return t("managerHome.attention.lowStock");
    case "expiry":
      return t("managerHome.attention.expiry");
    case "orders":
      return t("managerHome.attention.orders");
    case "purchasing":
      return t("managerHome.attention.purchasing");
    case "transfers":
      return t("managerHome.attention.transfers");
    case "utang":
      return t("managerHome.attention.utang");
    case "shift":
      return t("managerHome.attention.shift");
    default:
      return t("managerHome.attention.generic");
  }
}

function attentionDetail(item: ManagerAttentionItem, t: (key: MessageKey) => string): string {
  switch (item.kind) {
    case "lowStock":
      return t("managerHome.attention.lowStockDetail").replace("{count}", String(item.count));
    case "expiry":
      return t("managerHome.attention.expiryDetail").replace("{count}", String(item.count));
    case "orders":
      return t("managerHome.attention.ordersDetail").replace("{count}", String(item.count));
    case "purchasing":
      return t("managerHome.attention.purchasingDetail").replace("{count}", String(item.count));
    case "transfers":
      return t("managerHome.attention.transfersDetail").replace("{count}", String(item.count));
    case "utang":
      return t("managerHome.attention.utangDetail").replace(
        "{amount}",
        formatPeso(item.amount ?? 0),
      );
    case "shift":
      return t("managerHome.attention.shiftDetail");
    default:
      return "";
  }
}

export function ManagerRetailHome() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { enter } = useSellingMode();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const { currentShift, hasOpenShift } = useShiftContext();

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? {
            organizationId: boundWorkspace.organizationId,
            branchId: boundWorkspace.branchId,
          }
        : null,
    [boundWorkspace],
  );

  const branchId = boundWorkspace?.branchId ?? null;
  const todayRange = useMemo(() => resolveReportDatePreset("today"), []);

  const canSell = canCreateSale(sessionGrant, boundWorkspace?.branchType);
  const canInventory = canViewInventory(sessionGrant);
  const canManageInv = canManageInventory(sessionGrant);
  const canPurchasing = canViewPurchasing(sessionGrant);
  const canCreatePo = canManagePurchasing(sessionGrant);
  const canOrders = canViewCustomerOrders(sessionGrant);
  const canCustomers = canViewCustomers(sessionGrant);
  const canShifts = canViewShifts(sessionGrant);
  const canOpenShift = canManageShifts(sessionGrant);
  const canExpenses = canManageExpenses(sessionGrant);
  const canDashboard = canViewDashboard(sessionGrant);
  const canReports = canAccessReportsHub(sessionGrant);

  const dashboardQuery = useQuery({
    queryKey: [
      "manager-home",
      "dashboard",
      workspace?.organizationId,
      branchId,
      todayRange.fromDate,
      todayRange.toDate,
    ],
    enabled: Boolean(workspace && branchId),
    staleTime: 30_000,
    queryFn: ({ signal }) => getDashboard(workspace!, todayRange, signal, branchId),
  });

  const overviewQuery = useQuery({
    queryKey: ["manager-home", "overview", workspace?.organizationId, branchId],
    enabled: Boolean(workspace && (canInventory || canPurchasing)),
    staleTime: 30_000,
    queryFn: ({ signal }) => getManagementOverview(workspace!, signal),
  });

  const ordersQuery = useQuery({
    queryKey: ["manager-home", "orders-submitted", workspace?.organizationId, branchId],
    enabled: Boolean(workspace && branchId && canOrders),
    staleTime: 30_000,
    queryFn: ({ signal }) =>
      listSellerCustomerOrders(
        sellerWorkspace(workspace!.organizationId, branchId),
        { status: "Submitted", branchId: branchId!, page: 1, pageSize: 1 },
        signal,
      ),
  });

  const purchaseOrdersQuery = useQuery({
    queryKey: ["manager-home", "purchase-orders", workspace?.organizationId, branchId],
    enabled: Boolean(workspace && canPurchasing),
    staleTime: 30_000,
    queryFn: ({ signal }) =>
      listPurchaseOrders(workspace!, { page: 1, pageSize: 40 }, signal),
  });

  const transfersQuery = useQuery({
    queryKey: ["manager-home", "incoming-transfers", workspace?.organizationId, branchId],
    enabled: Boolean(workspace && canInventory),
    staleTime: 30_000,
    queryFn: ({ signal }) =>
      listInventoryTransfers(
        workspace!,
        { direction: "incoming", page: 1, pageSize: 40 },
        signal,
      ),
  });

  const dashboard = dashboardQuery.data;
  const overview = overviewQuery.data;
  const receivableCount = (purchaseOrdersQuery.data?.items ?? []).filter((po) =>
    isReceivablePurchaseOrderStatus(po.status),
  ).length;
  const pendingTransfers = (transfersQuery.data?.items ?? []).filter(
    (item) => item.status === "InTransit" || item.status === "PartiallyReceived",
  ).length;
  const submittedOrders = ordersQuery.data?.totalCount ?? 0;
  const lowStock = overview?.lowStockProductCount ?? dashboard?.lowStockProductCount ?? 0;
  const expiry =
    (overview?.expiredLotCount ?? 0) + (overview?.nearExpiryLotCount ?? 0);
  const overdueUtang = dashboard?.overdueUtangAmount ?? 0;
  const outstandingUtang = dashboard?.activeCustomerUtangOutstanding ?? 0;

  const attentionItems = buildManagerAttentionItems(
    {
      lowStockProductCount: canInventory ? lowStock : 0,
      expiredLotCount: canInventory ? (overview?.expiredLotCount ?? 0) : 0,
      nearExpiryLotCount: canInventory ? (overview?.nearExpiryLotCount ?? 0) : 0,
      submittedOrderCount: canOrders ? submittedOrders : 0,
      receivablePoCount: canPurchasing ? receivableCount : 0,
      pendingIncomingTransferCount: canInventory ? pendingTransfers : 0,
      overdueUtangAmount: canCustomers ? overdueUtang : 0,
      shiftNeedsOpen: false,
    },
    { includeOrders: canOrders, includeShift: false },
  );

  const snapshotModules = buildRetailSnapshotModules({
    canInventory,
    canOrders,
    canPurchasing,
    canCustomers,
    lowStock,
    expiry,
    orderCount: submittedOrders,
    receivableCount,
    overdueAmount: overdueUtang,
    outstandingAmount: outstandingUtang,
  });

  function startSelling() {
    enter("/role/manager");
    navigate("/sell");
  }

  const quickTiles: ActionTileDef[] = [];
  if (canSell) {
    quickTiles.push({
      key: "sell",
      label: t("role.startSelling"),
      icon: ShoppingCart,
      testId: "manager-action-sell",
      primary: true,
      onClick: startSelling,
    });
  }
  if (canManageInv) {
    quickTiles.push({
      key: "receive",
      label: t("purchasing.receiveStock"),
      icon: PackagePlus,
      testId: "manager-action-receive",
      to: "/purchasing/receive-stock",
    });
  }
  if (canCreatePo) {
    quickTiles.push({
      key: "create-po",
      label: t("managerHome.action.createPo"),
      icon: ClipboardList,
      testId: "manager-action-create-po",
      to: "/purchasing/new",
    });
  }
  if (canInventory) {
    quickTiles.push({
      key: "transfer",
      label: t("warehouse.action.transferStock"),
      icon: ArrowLeftRight,
      testId: "manager-action-transfer",
      to: "/inventory/transfers",
    });
  }
  if (canExpenses && quickTiles.length < 5) {
    quickTiles.push({
      key: "expense",
      label: t("managerHome.action.recordExpense"),
      icon: Receipt,
      testId: "manager-action-expense",
      to: "/expenses/new",
    });
  }

  const salesTotal = dashboard?.completedSalesTotal ?? 0;
  const saleCount = dashboard?.completedSaleCount ?? 0;
  const loading =
    dashboardQuery.isLoading ||
    (canInventory && overviewQuery.isLoading) ||
    (canOrders && ordersQuery.isLoading);

  const loadError =
    dashboardQuery.error ??
    overviewQuery.error ??
    ordersQuery.error ??
    purchaseOrdersQuery.error ??
    transfersQuery.error;

  const shiftValue = hasOpenShift
    ? t("managerHome.shift.open")
    : t("managerHome.shift.closed");
  const shiftHint = hasOpenShift
    ? currentShift?.registerName?.trim() ||
      (currentShift ? t("managerHome.shift.view") : undefined)
    : canOpenShift
      ? t("managerHome.shift.openAction")
      : undefined;
  const registerValue =
    hasOpenShift && currentShift?.registerName?.trim()
      ? currentShift.registerName.trim()
      : t("managerHome.register.none");

  return (
    <div
      className="manager-ops-home manager-home-page exits-page mx-auto flex w-full max-w-[72rem] min-w-0 flex-col gap-4"
      data-testid="manager-home"
      data-home-variant="retail"
    >
      <PageHeader
        title={t("role.managerTitle")}
        subtitle={boundWorkspace?.branchName?.trim() || undefined}
        description={t("managerHome.lede")}
        descriptionCollapsible={false}
        trailing={
          <span data-testid="manager-home-badge">
            <StatusChip tone="neutral">{t("role.managerBadge")}</StatusChip>
          </span>
        }
      />

      {loading ? <LoadingState label={t("managerHome.loading")} /> : null}

      {loadError && !loading ? (
        <ErrorState
          title={t("managerHome.loadError")}
          detail={loadError instanceof Error ? loadError.message : t("managerHome.loadError")}
        />
      ) : null}

      {!loading ? (
        <>
          <ManagerHomeSection title={t("managerHome.section.today")} testId="manager-home-today">
            <div className="grid grid-cols-2 gap-2 md:grid-cols-4">
              <ManagerMetricCard
                label={t("managerHome.today.sales")}
                value={formatPeso(salesTotal)}
                hint={salesTotal <= 0 ? t("managerHome.today.noSales") : undefined}
                testId="manager-today-sales"
              />
              <ManagerMetricCard
                label={t("managerHome.today.transactions")}
                value={saleCount}
                testId="manager-today-transactions"
              />
              {canShifts ? (
                <ManagerMetricCard
                  label={t("managerHome.today.shift")}
                  value={shiftValue}
                  hint={shiftHint}
                  tone={hasOpenShift ? "success" : "default"}
                  testId="manager-today-shift"
                />
              ) : null}
              {canShifts && hasOpenShift ? (
                <ManagerMetricCard
                  label={t("managerHome.today.register")}
                  value={registerValue}
                  testId="manager-today-register"
                />
              ) : null}
            </div>
            {canShifts ? (
              <div className="flex flex-wrap gap-2">
                {hasOpenShift && currentShift ? (
                  <Link
                    className="text-[length:var(--exits-text-sm)] font-medium text-primary no-underline hover:underline"
                    to={`/shifts/${currentShift.shiftId}`}
                    data-testid="manager-shift-view"
                  >
                    {t("managerHome.shift.view")}
                  </Link>
                ) : null}
                {!hasOpenShift && canOpenShift ? (
                  <Link
                    className="text-[length:var(--exits-text-sm)] font-medium text-primary no-underline hover:underline"
                    to="/shifts/open"
                    data-testid="manager-shift-open"
                  >
                    {t("managerHome.shift.openAction")}
                  </Link>
                ) : null}
              </div>
            ) : null}
          </ManagerHomeSection>

          <ManagerHomeSection
            title={t("managerHome.section.needsAttention")}
            testId="manager-home-attention"
          >
            {attentionItems.length === 0 ? (
              <ManagerHealthyAttention
                title={t("managerHome.attention.healthy")}
                detail={t("managerHome.attention.healthyDetail")}
              />
            ) : (
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {attentionItems.map((item) => (
                  <li key={item.kind}>
                    <ManagerAttentionLink
                      title={attentionTitle(item, t)}
                      detail={attentionDetail(item, t)}
                      href={item.href}
                      testId={item.testId}
                    />
                  </li>
                ))}
              </ul>
            )}
          </ManagerHomeSection>

          {quickTiles.length > 0 ? (
            <ManagerHomeSection
              title={t("managerHome.section.quickActions")}
              testId="manager-home-quick-actions"
            >
              <ActionTileGrid tiles={quickTiles.slice(0, 5)} emphasizePrimary={false} />
            </ManagerHomeSection>
          ) : null}

          {snapshotModules.length > 0 ? (
            <ManagerHomeSection
              title={t("managerHome.section.snapshot")}
              testId="manager-home-snapshot"
            >
              <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                {snapshotModules.map((mod) => {
                  let detail = "";
                  if (mod.summaryKind === "inventory") {
                    detail =
                      (mod.lowStock ?? 0) > 0 || (mod.expiry ?? 0) > 0
                        ? t("managerHome.snapshot.inventoryDetail")
                            .replace("{low}", String(mod.lowStock ?? 0))
                            .replace("{expiry}", String(mod.expiry ?? 0))
                        : t("managerHome.snapshot.inventoryClear");
                  } else if (mod.summaryKind === "orders") {
                    detail =
                      (mod.orderCount ?? 0) > 0
                        ? t("managerHome.snapshot.ordersDetail").replace(
                            "{count}",
                            String(mod.orderCount ?? 0),
                          )
                        : t("managerHome.snapshot.ordersClear");
                  } else if (mod.summaryKind === "purchasing") {
                    detail =
                      (mod.receivableCount ?? 0) > 0
                        ? t("managerHome.snapshot.purchasingDetail").replace(
                            "{count}",
                            String(mod.receivableCount ?? 0),
                          )
                        : t("managerHome.snapshot.purchasingClear");
                  } else if (mod.summaryKind === "utang") {
                    detail = t("managerHome.snapshot.utangDetail")
                      .replace("{outstanding}", formatPeso(mod.outstandingAmount ?? 0))
                      .replace("{overdue}", formatPeso(mod.overdueAmount ?? 0));
                  }
                  const titleKey =
                    mod.key === "inventory"
                      ? "managerHome.snapshot.inventory"
                      : mod.key === "orders"
                        ? "managerHome.snapshot.orders"
                        : mod.key === "purchasing"
                          ? "managerHome.snapshot.purchasing"
                          : "managerHome.snapshot.utang";
                  return (
                    <ManagerSnapshotLink
                      key={mod.key}
                      title={t(titleKey)}
                      detail={detail}
                      href={mod.href}
                      testId={mod.testId}
                    />
                  );
                })}
              </div>
            </ManagerHomeSection>
          ) : null}

          {canDashboard || canReports ? (
            <ManagerHomeSection
              title={t("managerHome.section.insights")}
              testId="manager-home-insights"
            >
              <div className="flex flex-wrap gap-4">
                {canDashboard ? (
                  <ManagerInsightLink
                    label={t("dashboard.open")}
                    href="/dashboard"
                    testId="manager-insight-dashboard"
                  />
                ) : null}
                {canReports ? (
                  <ManagerInsightLink
                    label={t("reports.open")}
                    href="/reports"
                    testId="manager-insight-reports"
                  />
                ) : null}
              </div>
            </ManagerHomeSection>
          ) : null}
        </>
      ) : null}
    </div>
  );
}
