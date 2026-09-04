import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeftRight,
  Boxes,
  ClipboardList,
  PackagePlus,
} from "lucide-react";
import { Navigate } from "react-router-dom";
import {
  canAccessReportsHub,
  canManageInventory,
  canManagePurchasing,
  canViewDashboard,
  canViewInventory,
  canViewPurchasing,
} from "@/access/pos-capabilities";
import { listInventoryTransfers } from "@/api/pos/pos-inventory-transfer-client";
import {
  isReceivablePurchaseOrderStatus,
  listPurchaseOrders,
} from "@/api/pos/pos-purchase-orders-client";
import { getManagementOverview } from "@/api/pos/pos-reporting-client";
import { ActionTileGrid, type ActionTileDef } from "@/components/exits/ActionTileGrid";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import {
  buildManagerAttentionItems,
  buildWarehouseSnapshotModules,
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
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workingExperienceRoute } from "@/workspace/working-experience";

function attentionTitle(item: ManagerAttentionItem, t: (key: MessageKey) => string): string {
  switch (item.kind) {
    case "lowStock":
      return t("managerHome.attention.lowStock");
    case "expiry":
      return t("managerHome.attention.expiry");
    case "purchasing":
      return t("managerHome.attention.purchasing");
    case "transfers":
      return t("managerHome.attention.transfers");
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
    case "purchasing":
      return t("managerHome.attention.purchasingDetail").replace("{count}", String(item.count));
    case "transfers":
      return t("managerHome.attention.transfersDetail").replace("{count}", String(item.count));
    default:
      return "";
  }
}

export type ManagerWarehouseHomeProps = {
  /** When true, redirect away if current branch is not warehouse. */
  enforceWarehouseBranch?: boolean;
  homeTestId?: string;
};

export function ManagerWarehouseHome({
  enforceWarehouseBranch = false,
  homeTestId = "manager-home",
}: ManagerWarehouseHomeProps) {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();

  const isWarehouse =
    Boolean(boundWorkspace?.branchId) && isWarehouseBranch(boundWorkspace?.branchType);

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

  const canInventory = canViewInventory(sessionGrant);
  const canManageInv = canManageInventory(sessionGrant);
  const canPurchasing = canViewPurchasing(sessionGrant);
  const canCreatePo = canManagePurchasing(sessionGrant);
  const canDashboard = canViewDashboard(sessionGrant);
  const canReports = canAccessReportsHub(sessionGrant);

  const overviewQuery = useQuery({
    queryKey: ["manager-home", "warehouse-overview", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(isWarehouse && workspace && (canInventory || canPurchasing)),
    staleTime: 30_000,
    queryFn: ({ signal }) => getManagementOverview(workspace!, signal),
  });

  const incomingTransfersQuery = useQuery({
    queryKey: [
      "manager-home",
      "warehouse-incoming-transfers",
      workspace?.organizationId,
      workspace?.branchId,
    ],
    enabled: Boolean(isWarehouse && workspace && canInventory),
    staleTime: 30_000,
    queryFn: ({ signal }) =>
      listInventoryTransfers(
        workspace!,
        { direction: "incoming", page: 1, pageSize: 40 },
        signal,
      ),
  });

  const purchaseOrdersQuery = useQuery({
    queryKey: [
      "manager-home",
      "warehouse-purchase-orders",
      workspace?.organizationId,
      workspace?.branchId,
    ],
    enabled: Boolean(isWarehouse && workspace && canPurchasing),
    staleTime: 30_000,
    queryFn: ({ signal }) =>
      listPurchaseOrders(workspace!, { page: 1, pageSize: 40 }, signal),
  });

  if (!boundWorkspace?.branchId) {
    return <Navigate to="/workspace" replace />;
  }

  if (enforceWarehouseBranch && !isWarehouseBranch(boundWorkspace.branchType)) {
    return <Navigate to={workingExperienceRoute(boundWorkspace.experience)} replace />;
  }

  const overview = overviewQuery.data;
  const incomingTransfers = (incomingTransfersQuery.data?.items ?? []).filter(
    (item) => item.status === "InTransit" || item.status === "PartiallyReceived",
  );
  const receivablePos = (purchaseOrdersQuery.data?.items ?? []).filter((po) =>
    isReceivablePurchaseOrderStatus(po.status),
  );
  const lowStock = overview?.lowStockProductCount ?? 0;
  const expiry = (overview?.expiredLotCount ?? 0) + (overview?.nearExpiryLotCount ?? 0);

  const attentionItems = buildManagerAttentionItems(
    {
      lowStockProductCount: canInventory ? lowStock : 0,
      expiredLotCount: canInventory ? (overview?.expiredLotCount ?? 0) : 0,
      nearExpiryLotCount: canInventory ? (overview?.nearExpiryLotCount ?? 0) : 0,
      receivablePoCount: canPurchasing ? receivablePos.length : 0,
      pendingIncomingTransferCount: canInventory ? incomingTransfers.length : 0,
    },
    { includeOrders: false, includeShift: false },
  );

  const snapshotModules = buildWarehouseSnapshotModules({
    canInventory,
    canPurchasing,
    lowStock,
    expiry,
    receivableCount: receivablePos.length,
    transferCount: incomingTransfers.length,
  });

  const quickTiles: ActionTileDef[] = [];
  if (canManageInv) {
    quickTiles.push({
      key: "receive",
      label: t("purchasing.receiveStock"),
      icon: PackagePlus,
      testId: "manager-action-receive",
      primary: true,
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
    quickTiles.push({
      key: "inventory",
      label: t("warehouse.action.inventory"),
      icon: Boxes,
      testId: "manager-action-inventory",
      to: "/inventory",
    });
  }

  const loading =
    overviewQuery.isLoading ||
    incomingTransfersQuery.isLoading ||
    purchaseOrdersQuery.isLoading;
  const loadError =
    overviewQuery.error ?? incomingTransfersQuery.error ?? purchaseOrdersQuery.error;

  return (
    <div
      className="manager-ops-home manager-home-page exits-page mx-auto flex w-full max-w-[72rem] min-w-0 flex-col gap-4"
      data-testid={homeTestId}
      data-home-variant="warehouse"
    >
      <PageHeader
        title={t("managerHome.warehouseTitle")}
        subtitle={boundWorkspace.branchName?.trim() || undefined}
        description={t("managerHome.warehouseLede")}
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
            <div className="grid grid-cols-2 gap-2 md:grid-cols-3">
              <ManagerMetricCard
                label={t("managerHome.warehouse.incomingTransfers")}
                value={incomingTransfers.length}
                hint={
                  incomingTransfers.length === 0
                    ? t("managerHome.warehouse.noIncoming")
                    : undefined
                }
                testId="manager-today-transfers"
              />
              <ManagerMetricCard
                label={t("managerHome.warehouse.receivablePos")}
                value={receivablePos.length}
                hint={
                  receivablePos.length === 0
                    ? t("managerHome.warehouse.noReceivable")
                    : undefined
                }
                testId="manager-today-receiving"
              />
              <ManagerMetricCard
                label={t("managerHome.warehouse.stockAlerts")}
                value={lowStock + expiry}
                hint={
                  lowStock + expiry === 0
                    ? t("managerHome.snapshot.inventoryClear")
                    : t("managerHome.snapshot.inventoryDetail")
                        .replace("{low}", String(lowStock))
                        .replace("{expiry}", String(expiry))
                }
                tone={lowStock + expiry > 0 ? "attention" : "default"}
                testId="manager-today-stock-alerts"
              />
            </div>
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
              title={t("managerHome.section.stockSnapshot")}
              testId="manager-home-snapshot"
            >
              <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                {snapshotModules.map((mod) => {
                  let detail = "";
                  let titleKey: MessageKey = "managerHome.snapshot.inventory";
                  if (mod.summaryKind === "inventory") {
                    titleKey = "managerHome.snapshot.inventory";
                    detail =
                      (mod.lowStock ?? 0) > 0 || (mod.expiry ?? 0) > 0
                        ? t("managerHome.snapshot.inventoryDetail")
                            .replace("{low}", String(mod.lowStock ?? 0))
                            .replace("{expiry}", String(mod.expiry ?? 0))
                        : t("managerHome.snapshot.inventoryClear");
                  } else if (mod.summaryKind === "transfers") {
                    titleKey = "managerHome.snapshot.transfers";
                    detail =
                      (mod.transferCount ?? 0) > 0
                        ? t("managerHome.snapshot.transfersDetail").replace(
                            "{count}",
                            String(mod.transferCount ?? 0),
                          )
                        : t("managerHome.snapshot.transfersClear");
                  } else if (mod.summaryKind === "purchasing") {
                    titleKey = "managerHome.snapshot.purchasing";
                    detail =
                      (mod.receivableCount ?? 0) > 0
                        ? t("managerHome.snapshot.purchasingDetail").replace(
                            "{count}",
                            String(mod.receivableCount ?? 0),
                          )
                        : t("managerHome.snapshot.purchasingClear");
                  }
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
