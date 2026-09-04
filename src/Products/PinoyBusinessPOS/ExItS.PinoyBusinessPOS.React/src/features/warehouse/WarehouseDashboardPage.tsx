import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, Navigate } from "react-router-dom";
import {
  ArrowLeftRight,
  Boxes,
  ClipboardList,
  PackagePlus,
  Truck,
} from "lucide-react";
import {
  canManageInventory,
  canViewInventory,
  canViewPurchasing,
  canViewSuppliers,
} from "@/access/pos-capabilities";
import { listInventoryTransfers } from "@/api/pos/pos-inventory-transfer-client";
import {
  isReceivablePurchaseOrderStatus,
  listPurchaseOrders,
} from "@/api/pos/pos-purchase-orders-client";
import { getManagementOverview } from "@/api/pos/pos-reporting-client";
import { ActionTileGrid, type ActionTileDef } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workingExperienceRoute } from "@/workspace/working-experience";

export function WarehouseDashboardPage() {
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
  const canPurchasing = canViewPurchasing(sessionGrant);
  const canReceive = canManageInventory(sessionGrant);
  const canSuppliers = canViewSuppliers(sessionGrant);

  const overviewQuery = useQuery({
    queryKey: ["warehouse", "overview", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(isWarehouse && workspace && (canInventory || canPurchasing)),
    queryFn: ({ signal }) => getManagementOverview(workspace!, signal),
  });

  const incomingTransfersQuery = useQuery({
    queryKey: [
      "warehouse",
      "incoming-transfers",
      workspace?.organizationId,
      workspace?.branchId,
    ],
    enabled: Boolean(isWarehouse && workspace && canInventory),
    queryFn: ({ signal }) =>
      listInventoryTransfers(
        workspace!,
        { direction: "incoming", page: 1, pageSize: 8 },
        signal,
      ),
  });

  const purchaseOrdersQuery = useQuery({
    queryKey: ["warehouse", "purchase-orders", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(isWarehouse && workspace && canPurchasing),
    queryFn: ({ signal }) =>
      listPurchaseOrders(workspace!, { page: 1, pageSize: 20 }, signal),
  });

  if (!boundWorkspace?.branchId) {
    return <Navigate to="/workspace" replace />;
  }

  if (!isWarehouseBranch(boundWorkspace.branchType)) {
    return <Navigate to={workingExperienceRoute(boundWorkspace.experience)} replace />;
  }

  const quickTiles: ActionTileDef[] = [];
  if (canReceive) {
    quickTiles.push({
      key: "receive",
      label: t("warehouse.action.receiveStock"),
      icon: PackagePlus,
      testId: "warehouse-action-receive",
      to: "/purchasing/receive-stock",
      primary: true,
    });
  }
  if (canInventory) {
    quickTiles.push({
      key: "transfer",
      label: t("warehouse.action.transferStock"),
      icon: ArrowLeftRight,
      testId: "warehouse-action-transfer",
      to: "/inventory/transfers",
    });
    quickTiles.push({
      key: "inventory",
      label: t("warehouse.action.inventory"),
      icon: Boxes,
      testId: "warehouse-action-inventory",
      to: "/inventory",
    });
  }
  if (canPurchasing) {
    quickTiles.push({
      key: "purchasing",
      label: t("warehouse.action.purchaseOrders"),
      icon: ClipboardList,
      testId: "warehouse-action-purchasing",
      to: "/purchasing/orders",
    });
  }
  if (canSuppliers) {
    quickTiles.push({
      key: "suppliers",
      label: t("warehouse.action.suppliers"),
      icon: Truck,
      testId: "warehouse-action-suppliers",
      to: "/suppliers",
    });
  }

  const incomingTransfers = (incomingTransfersQuery.data?.items ?? []).filter(
    (item) => item.status === "InTransit" || item.status === "PartiallyReceived",
  );
  const actionablePos = (purchaseOrdersQuery.data?.items ?? []).filter((po) =>
    isReceivablePurchaseOrderStatus(po.status),
  );
  const overview = overviewQuery.data;

  const loadError =
    overviewQuery.error ?? incomingTransfersQuery.error ?? purchaseOrdersQuery.error;
  const loading =
    overviewQuery.isLoading ||
    incomingTransfersQuery.isLoading ||
    purchaseOrdersQuery.isLoading;

  return (
    <div
      className="warehouse-home exits-page mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-3"
      data-testid="warehouse-dashboard"
    >
      <PageHeader
        title={t("warehouse.title")}
        description={
          boundWorkspace.branchName
            ? t("warehouse.lede").replace("{branch}", boundWorkspace.branchName)
            : t("warehouse.ledeFallback")
        }
      />

      {quickTiles.length > 0 ? (
        <section
          className="catalog-form-section exits-animate-panel gap-3"
          data-testid="warehouse-quick-actions"
        >
          <h2 className="catalog-form-section__title text-muted">{t("warehouse.quickActions")}</h2>
          <ActionTileGrid tiles={quickTiles} />
        </section>
      ) : null}

      {loading ? <LoadingState label={t("warehouse.loading")} /> : null}

      {loadError && !loading ? (
        <ErrorState
          title={t("warehouse.loadError")}
          detail={loadError instanceof Error ? loadError.message : t("warehouse.loadError")}
        />
      ) : null}

      {!loading && !loadError ? (
        <>
          <section
            className="catalog-form-section exits-animate-panel gap-3"
            data-testid="warehouse-incoming"
          >
            <h2 className="catalog-form-section__title text-muted">{t("warehouse.incoming")}</h2>
            {incomingTransfers.length === 0 && actionablePos.length === 0 ? (
              <EmptyState
                title={t("warehouse.incomingEmpty")}
                detail={t("warehouse.incomingEmptyDetail")}
              />
            ) : (
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {incomingTransfers.map((transfer) => (
                  <li key={transfer.transferId}>
                    <Link
                      to={`/inventory/transfers/${transfer.transferId}`}
                      className="exits-list-row flex min-h-11 items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 no-underline"
                      data-testid="warehouse-incoming-transfer"
                    >
                      <span className="font-medium text-foreground">
                        {transfer.transferNumber?.trim() || transfer.transferId.slice(0, 8)}
                      </span>
                      <span className="text-[length:var(--exits-text-sm)] text-muted">
                        {transfer.sourceBranchName ?? t("warehouse.fromBranch")} · {transfer.status}
                      </span>
                    </Link>
                  </li>
                ))}
                {actionablePos.map((po) => (
                  <li key={po.purchaseOrderId}>
                    <Link
                      to={`/purchasing/${po.purchaseOrderId}`}
                      className="exits-list-row flex min-h-11 items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 no-underline"
                      data-testid="warehouse-incoming-po"
                    >
                      <span className="font-medium text-foreground">
                        {po.poNumber?.trim() || po.purchaseOrderId.slice(0, 8)}
                      </span>
                      <span className="text-[length:var(--exits-text-sm)] text-muted">{po.status}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>

          {overview ? (
            <section
              className="catalog-form-section exits-animate-panel gap-3"
              data-testid="warehouse-stock-alerts"
            >
              <h2 className="catalog-form-section__title text-muted">{t("warehouse.stockAlerts")}</h2>
              <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
                <Link
                  to="/inventory"
                  className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3 no-underline"
                  data-testid="warehouse-alert-low-stock"
                >
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("warehouse.lowStock")}
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold text-foreground">
                    {overview.lowStockProductCount}
                  </p>
                </Link>
                <Link
                  to="/inventory/expiration"
                  className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3 no-underline"
                  data-testid="warehouse-alert-expired"
                >
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("warehouse.expiredLots")}
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold text-foreground">
                    {overview.expiredLotCount}
                  </p>
                </Link>
                <Link
                  to="/inventory/expiration"
                  className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3 no-underline"
                  data-testid="warehouse-alert-near-expiry"
                >
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("warehouse.nearExpiryLots")}
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-xl)] font-semibold text-foreground">
                    {overview.nearExpiryLotCount}
                  </p>
                </Link>
              </div>
              {overview.pendingTransferCount > 0 ? (
                <p
                  className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                  data-testid="warehouse-pending-transfers"
                >
                  {t("warehouse.pendingTransfers").replace(
                    "{count}",
                    String(overview.pendingTransferCount),
                  )}
                </p>
              ) : null}
            </section>
          ) : null}
        </>
      ) : null}
    </div>
  );
}
