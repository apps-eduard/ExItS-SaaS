import { useEffect, useMemo, useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { useQueries, useQueryClient } from "@tanstack/react-query";
import { ChevronRight, RefreshCw } from "lucide-react";
import {
  canAccessReportsHub,
  canViewInventory,
  canViewPurchasing,
} from "@/access/pos-capabilities";
import { listInventory, listExpiringLots } from "@/api/pos/pos-inventory-client";
import { listInventoryTransfers } from "@/api/pos/pos-inventory-transfer-client";
import {
  getInventoryMovementsReport,
  getPurchasingSummaryReport,
} from "@/api/pos/pos-reporting-client";
import {
  isReceivablePurchaseOrderStatus,
  listPurchaseOrders,
} from "@/api/pos/pos-purchase-orders-client";
import { listIncomingStockRequests } from "@/api/pos/pos-stock-requests-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import {
  DashboardPanel,
  DashboardQuietEmpty,
} from "@/features/reports/dashboard/DashboardToolbar";
import {
  buildWarehouseAttentionItems,
  classifyTransferBuckets,
  stockRequestStatusCounts,
  summarizeMovementTypes,
  topDestinationsFromOutgoing,
  topMovedProductsFromRows,
} from "@/features/reports/warehouse-dashboard-helpers";
import {
  resolveReportDatePreset,
  type ReportDatePreset,
  type ReportDateRangeValue,
} from "@/features/reports/report-date-range";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PRESETS: ReportDatePreset[] = ["today", "yesterday", "thisWeek", "thisMonth", "custom"];
const STALE = 30_000;

function KpiChip({
  label,
  value,
  tone = "default",
  testId,
  href,
}: {
  label: string;
  value: ReactNode;
  tone?: "default" | "attention";
  testId: string;
  href?: string;
}) {
  const body = (
    <div
      className={cn(
        "dashboard-kpi-chip",
        tone === "attention" && "dashboard-kpi-chip--attention",
      )}
      data-testid={testId}
      role="listitem"
    >
      <span className="dashboard-kpi-chip__label">{label}</span>
      <span className="dashboard-kpi-chip__value">{value}</span>
    </div>
  );
  return href ? (
    <Link to={href} className="no-underline text-inherit">
      {body}
    </Link>
  ) : (
    body
  );
}

function MetricRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-2 border-b border-border py-1.5 last:border-0">
      <span className="text-[length:var(--exits-text-sm)] text-muted">{label}</span>
      <span className="text-[length:var(--exits-text-sm)] font-semibold tabular-nums">{value}</span>
    </div>
  );
}

export function WarehouseManagementDashboardPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [preset, setPreset] = useState<ReportDatePreset>("today");
  const [custom, setCustom] = useState<ReportDateRangeValue>(() => resolveReportDatePreset("today"));
  const [applied, setApplied] = useState<ReportDateRangeValue>(() => resolveReportDatePreset("today"));
  const [movedDir, setMovedDir] = useState<"outbound" | "inbound">("outbound");

  const canInventory = canViewInventory(sessionGrant);
  const canPurchasing = canViewPurchasing(sessionGrant);
  const canReports = canAccessReportsHub(sessionGrant);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const isWarehouse =
    Boolean(boundWorkspace?.branchId) && isWarehouseBranch(boundWorkspace?.branchType);

  useEffect(() => {
    if (preset !== "custom") {
      const next = resolveReportDatePreset(preset);
      setCustom(next);
      setApplied(next);
    }
  }, [preset]);

  const range = { fromDate: applied.fromDate, toDate: applied.toDate };
  const enabled = Boolean(isWarehouse && workspace);

  const results = useQueries({
    queries: [
      {
        queryKey: ["wh-dash", "tracked", workspace?.organizationId, workspace?.branchId],
        enabled: enabled && canInventory,
        staleTime: STALE,
        queryFn: ({ signal }) =>
          listInventory(workspace!, { tracked: true, page: 1, pageSize: 200 }, signal),
      },
      {
        queryKey: ["wh-dash", "low-stock", workspace?.organizationId, workspace?.branchId],
        enabled: enabled && canInventory,
        staleTime: STALE,
        queryFn: ({ signal }) =>
          listInventory(workspace!, { lowStock: true, page: 1, pageSize: 1 }, signal),
      },
      {
        queryKey: ["wh-dash", "expiry", workspace?.organizationId, workspace?.branchId],
        enabled: enabled && canInventory,
        staleTime: STALE,
        queryFn: ({ signal }) =>
          listExpiringLots(workspace!, { window: "Days30", page: 1, pageSize: 8 }, signal),
      },
      {
        queryKey: ["wh-dash", "tx-out", workspace?.organizationId, workspace?.branchId],
        enabled: enabled && canInventory,
        staleTime: STALE,
        queryFn: ({ signal }) =>
          listInventoryTransfers(workspace!, { direction: "outgoing", page: 1, pageSize: 40 }, signal),
      },
      {
        queryKey: ["wh-dash", "tx-in", workspace?.organizationId, workspace?.branchId],
        enabled: enabled && canInventory,
        staleTime: STALE,
        queryFn: ({ signal }) =>
          listInventoryTransfers(workspace!, { direction: "incoming", page: 1, pageSize: 40 }, signal),
      },
      {
        queryKey: ["wh-dash", "pos", workspace?.organizationId, workspace?.branchId],
        enabled: enabled && canPurchasing,
        staleTime: STALE,
        queryFn: ({ signal }) => listPurchaseOrders(workspace!, { page: 1, pageSize: 40 }, signal),
      },
      {
        queryKey: [
          "wh-dash",
          "movements",
          workspace?.organizationId,
          workspace?.branchId,
          range.fromDate,
          range.toDate,
        ],
        enabled: enabled && canInventory,
        staleTime: STALE,
        queryFn: ({ signal }) =>
          getInventoryMovementsReport(workspace!, range, signal, workspace!.branchId),
      },
      {
        queryKey: [
          "wh-dash",
          "purchasing-summary",
          workspace?.organizationId,
          workspace?.branchId,
          range.fromDate,
          range.toDate,
        ],
        enabled: enabled && canPurchasing,
        staleTime: STALE,
        queryFn: ({ signal }) => getPurchasingSummaryReport(workspace!, range, signal),
      },
      {
        queryKey: ["wh-dash", "stock-requests", workspace?.organizationId, workspace?.branchId],
        enabled: enabled && canInventory,
        staleTime: STALE,
        queryFn: ({ signal }) => listIncomingStockRequests(workspace!, 1, 40, signal),
      },
    ],
  });

  const [
    trackedQ,
    lowStockQ,
    expiryQ,
    txOutQ,
    txInQ,
    poQ,
    movementsQ,
    purchasingQ,
    stockReqQ,
  ] = results;

  const loading = results.some((q) => q.isLoading);
  const refreshing = results.some((q) => q.isFetching && !q.isLoading);

  const trackedItems = trackedQ.data?.items ?? [];
  const trackedCount = trackedQ.data?.totalCount ?? trackedItems.length;
  const lowStockCount = lowStockQ.data?.totalCount ?? 0;
  const fullTrackedPage =
    trackedQ.data != null && trackedItems.length >= Math.min(trackedCount, trackedItems.length)
      ? trackedCount <= trackedItems.length
      : false;
  const outOfStockCount = fullTrackedPage
    ? trackedItems.filter((i) => i.onHandQuantity <= 0).length
    : null;
  const healthyCount =
    outOfStockCount == null
      ? Math.max(0, trackedCount - lowStockCount)
      : Math.max(0, trackedCount - lowStockCount - outOfStockCount);

  const expiredCount = expiryQ.data?.expiredCount ?? 0;
  const nearExpiryCount = expiryQ.data?.nearExpiryCount ?? 0;

  const outgoing = txOutQ.data?.items ?? [];
  const incoming = txInQ.data?.items ?? [];
  const outBuckets = classifyTransferBuckets(outgoing, "outgoing");
  const inBuckets = classifyTransferBuckets(incoming, "incoming");

  const receivablePos = (poQ.data?.items ?? []).filter((po) =>
    isReceivablePurchaseOrderStatus(po.status),
  );
  const movementSummary = summarizeMovementTypes(movementsQ.data?.byType ?? []);
  const destinations = topDestinationsFromOutgoing(outgoing);
  const movedProducts = topMovedProductsFromRows(
    (movementsQ.data?.rows ?? []) as Record<string, unknown>[],
    movedDir,
  );
  const stockReqItems = stockReqQ.data?.items ?? [];
  const stockReqCounts = stockRequestStatusCounts(stockReqItems);
  const pendingRequests =
    stockReqCounts.pending + stockReqCounts.inProgress + stockReqCounts.partiallyFulfilled;

  const attention = buildWarehouseAttentionItems({
    lowStock: canInventory ? lowStockCount : 0,
    expiry: canInventory ? expiredCount + nearExpiryCount : 0,
    awaitingDispatch: canInventory ? outBuckets.awaitingDispatch : 0,
    incomingToReceive: canInventory ? inBuckets.incomingToReceive : 0,
    receivablePos: canPurchasing ? receivablePos.length : 0,
    pendingStockRequests: canInventory ? pendingRequests : 0,
  });

  const onRefresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["wh-dash"] });
  };

  if (!workspace || !isWarehouse) {
    return (
      <ErrorState
        title={t("warehouseDashboard.title")}
        detail={t("warehouseDashboard.needWarehouse")}
      />
    );
  }

  return (
    <div
      className="dashboard-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="warehouse-management-dashboard"
    >
      <PageHeader
        title={t("warehouseDashboard.title")}
        description={`${boundWorkspace?.branchName ?? ""} · ${t("warehouseDashboard.lede")}`}
        backTo="/warehouse"
        backLabel={t("nav.backToWarehouseHome")}
        backTestId="page-header-back-warehouse-dashboard"
        trailing={
          <span data-testid="warehouse-dashboard-type">
            <StatusChip tone="info">{t("warehouseDashboard.typeBadge")}</StatusChip>
          </span>
        }
      />

      <div className="dashboard-toolbar" data-testid="warehouse-dashboard-toolbar">
        <div className="dashboard-toolbar__row">
          <div
            className="dashboard-toolbar__presets"
            role="tablist"
            aria-label={t("reports.datePresets")}
          >
            {PRESETS.map((key) => (
              <button
                key={key}
                type="button"
                role="tab"
                aria-selected={preset === key}
                className={cn(
                  "dashboard-toolbar__preset",
                  preset === key && "dashboard-toolbar__preset--active",
                )}
                data-testid={`report-preset-${key}`}
                onClick={() => setPreset(key)}
              >
                {t(`reports.preset.${key}` as MessageKey)}
              </button>
            ))}
          </div>
          <div className="dashboard-toolbar__actions">
            {canReports ? (
              <Link to="/reports" className="dashboard-toolbar__link" data-testid="open-reports-hub">
                {t("reports.open")}
              </Link>
            ) : null}
            <button
              type="button"
              className="dashboard-toolbar__icon-btn"
              data-testid="dashboard-refresh"
              aria-label={t("dashboard.refresh")}
              disabled={refreshing}
              onClick={onRefresh}
            >
              <RefreshCw className={cn("size-4", refreshing && "dashboard-refresh-spin")} />
            </button>
          </div>
        </div>
        <p className="dashboard-toolbar__range m-0" data-testid="dashboard-period-range">
          {applied.fromDate === applied.toDate
            ? applied.fromDate
            : `${applied.fromDate} → ${applied.toDate}`}
        </p>
        {preset === "custom" ? (
          <div className="dashboard-toolbar__custom flex flex-wrap items-end gap-2">
            <label className="dashboard-toolbar__field">
              <span>{t("reports.fromDate")}</span>
              <input
                type="date"
                className="catalog-form-input"
                value={custom.fromDate}
                onChange={(e) => setCustom((c) => ({ ...c, fromDate: e.target.value }))}
              />
            </label>
            <label className="dashboard-toolbar__field">
              <span>{t("reports.toDate")}</span>
              <input
                type="date"
                className="catalog-form-input"
                value={custom.toDate}
                onChange={(e) => setCustom((c) => ({ ...c, toDate: e.target.value }))}
              />
            </label>
            <button
              type="button"
              className="dashboard-toolbar__preset"
              onClick={() => setApplied(custom)}
            >
              {t("reports.apply")}
            </button>
          </div>
        ) : null}
        <p
          className="m-0 text-[length:var(--exits-text-xs)] text-muted"
          data-testid="warehouse-scope-note"
        >
          {boundWorkspace?.branchName} · {t("warehouseDashboard.currentLocationOnly")}
        </p>
      </div>

      {loading ? <LoadingState label={t("warehouseDashboard.loading")} /> : null}

      {!loading && canInventory ? (
        <div className="dashboard-kpi-strip" role="list" data-testid="warehouse-kpi-strip">
          <KpiChip
            label={t("warehouseDashboard.kpi.tracked")}
            value={trackedCount}
            testId="wh-kpi-tracked"
            href="/inventory"
          />
          <KpiChip
            label={t("warehouseDashboard.kpi.lowStock")}
            value={lowStockCount}
            tone={lowStockCount > 0 ? "attention" : "default"}
            testId="wh-kpi-low-stock"
            href="/inventory"
          />
          <KpiChip
            label={t("warehouseDashboard.kpi.expiry")}
            value={expiredCount + nearExpiryCount}
            tone={expiredCount + nearExpiryCount > 0 ? "attention" : "default"}
            testId="wh-kpi-expiry"
            href="/inventory/expiration"
          />
          <KpiChip
            label={t("warehouseDashboard.kpi.pendingRequests")}
            value={pendingRequests}
            tone={pendingRequests > 0 ? "attention" : "default"}
            testId="wh-kpi-requests"
            href="/inventory/stock-requests"
          />
        </div>
      ) : null}

      <div className="grid min-w-0 gap-3 lg:grid-cols-2">
        <DashboardPanel
          title={t("warehouseDashboard.needsAttention")}
          testId="warehouse-needs-attention"
        >
          {attention.length === 0 ? (
            <DashboardQuietEmpty title={t("warehouseDashboard.healthy")} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-1 p-0">
              {attention.map((item) => (
                <li key={item.key}>
                  <Link
                    to={item.href}
                    className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-sm)] px-1 py-1.5 text-[length:var(--exits-text-sm)] text-foreground no-underline hover:bg-surface"
                    data-testid={`wh-attention-${item.key}`}
                  >
                    <span>
                      {item.count} {t(item.labelKey as MessageKey)}
                    </span>
                    <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </DashboardPanel>

        {canInventory ? (
          <DashboardPanel title={t("warehouseDashboard.currentStock")} testId="warehouse-stock-health">
            <MetricRow label={t("warehouseDashboard.health.healthy")} value={healthyCount} />
            <MetricRow label={t("warehouseDashboard.health.lowStock")} value={lowStockCount} />
            {outOfStockCount != null ? (
              <MetricRow label={t("warehouseDashboard.health.outOfStock")} value={outOfStockCount} />
            ) : null}
            <MetricRow label={t("warehouseDashboard.health.nearExpiry")} value={nearExpiryCount} />
            <MetricRow label={t("warehouseDashboard.health.expired")} value={expiredCount} />
            <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
              {t("warehouseDashboard.currentStockNote")}
            </p>
          </DashboardPanel>
        ) : null}

        {canInventory ? (
          <DashboardPanel
            title={`${t("warehouseDashboard.activity")} — ${
              applied.fromDate === applied.toDate
                ? applied.fromDate
                : `${applied.fromDate} → ${applied.toDate}`
            }`}
            testId="warehouse-stock-movement"
          >
            {movementsQ.isError ? (
              <DashboardQuietEmpty title={t("warehouseDashboard.movementUnavailable")} />
            ) : (
              <>
                <MetricRow
                  label={t("warehouseDashboard.movement.received")}
                  value={formatSigned(movementSummary.receivedFromSuppliers)}
                />
                <MetricRow
                  label={t("warehouseDashboard.movement.transferIn")}
                  value={formatSigned(movementSummary.transferIn)}
                />
                <MetricRow
                  label={t("warehouseDashboard.movement.transferOut")}
                  value={formatSigned(movementSummary.transferOut)}
                />
                <MetricRow
                  label={t("warehouseDashboard.movement.adjustments")}
                  value={formatSigned(movementSummary.adjustments)}
                />
                <MetricRow
                  label={t("warehouseDashboard.movement.waste")}
                  value={formatSigned(movementSummary.wasteLoss)}
                />
                <Link
                  to="/inventory/stock-use"
                  className="mt-2 inline-block text-[length:var(--exits-text-sm)] underline"
                >
                  {t("warehouseDashboard.viewMovements")}
                </Link>
              </>
            )}
          </DashboardPanel>
        ) : null}

        {canInventory ? (
          <DashboardPanel title={t("warehouseDashboard.transfers")} testId="warehouse-transfers">
            <MetricRow
              label={t("warehouseDashboard.transfers.awaitingDispatch")}
              value={outBuckets.awaitingDispatch}
            />
            <MetricRow
              label={t("warehouseDashboard.transfers.inTransit")}
              value={outBuckets.inTransit + inBuckets.inTransit}
            />
            <MetricRow
              label={t("warehouseDashboard.transfers.incomingToReceive")}
              value={inBuckets.incomingToReceive}
            />
            <MetricRow
              label={t("warehouseDashboard.transfers.partiallyReceived")}
              value={inBuckets.partiallyReceived}
            />
            <ul className="mt-2 m-0 flex list-none flex-col gap-1 p-0">
              {[...incoming, ...outgoing]
                .filter((i) => ["Draft", "InTransit", "PartiallyReceived"].includes(i.status))
                .slice(0, 3)
                .map((tr) => (
                  <li key={tr.transferId}>
                    <Link
                      to={`/inventory/transfers/${tr.transferId}`}
                      className="block text-[length:var(--exits-text-sm)] underline"
                    >
                      {tr.transferNumber ?? tr.transferId.slice(0, 8)} ·{" "}
                      {tr.sourceBranchName ?? "?"} → {tr.destinationBranchName ?? "?"} · {tr.status}
                    </Link>
                  </li>
                ))}
            </ul>
            <Link
              to="/inventory/transfers"
              className="mt-2 inline-block text-[length:var(--exits-text-sm)] underline"
            >
              {t("warehouseDashboard.viewTransfers")}
            </Link>
          </DashboardPanel>
        ) : null}

        {canPurchasing ? (
          <DashboardPanel title={t("warehouseDashboard.purchasing")} testId="warehouse-purchasing">
            <MetricRow
              label={t("warehouseDashboard.purchasing.receivable")}
              value={receivablePos.length}
            />
            <MetricRow
              label={t("warehouseDashboard.purchasing.ordersPeriod")}
              value={purchasingQ.data?.orderCount ?? "—"}
            />
            <MetricRow
              label={t("warehouseDashboard.purchasing.receivedQty")}
              value={purchasingQ.data?.receivedQuantity ?? "—"}
            />
            <ul className="mt-2 m-0 flex list-none flex-col gap-1 p-0">
              {receivablePos.slice(0, 3).map((po) => (
                <li key={po.purchaseOrderId}>
                  <Link
                    to={`/purchasing/orders/${po.purchaseOrderId}`}
                    className="block text-[length:var(--exits-text-sm)] underline"
                  >
                    {po.poNumber ?? po.purchaseOrderId.slice(0, 8)} · {po.supplierName ?? "Supplier"} ·{" "}
                    {po.status}
                  </Link>
                </li>
              ))}
            </ul>
            <Link
              to="/purchasing"
              className="mt-2 inline-block text-[length:var(--exits-text-sm)] underline"
            >
              {t("warehouseDashboard.viewPurchasing")}
            </Link>
          </DashboardPanel>
        ) : null}

        {canInventory ? (
          <DashboardPanel title={t("warehouseDashboard.expiry")} testId="warehouse-expiry">
            <MetricRow label={t("warehouseDashboard.health.expired")} value={expiredCount} />
            <MetricRow label={t("warehouseDashboard.health.nearExpiry")} value={nearExpiryCount} />
            <ul className="mt-2 m-0 flex list-none flex-col gap-1 p-0">
              {(expiryQ.data?.items ?? []).slice(0, 3).map((lot) => (
                <li key={lot.lotId} className="text-[length:var(--exits-text-sm)]">
                  {lot.productName}
                  {lot.lotNumber ? ` · ${lot.lotNumber}` : ""} · {lot.expirationDate}
                </li>
              ))}
            </ul>
            <Link
              to="/inventory/expiration"
              className="mt-2 inline-block text-[length:var(--exits-text-sm)] underline"
            >
              {t("warehouseDashboard.viewExpiry")}
            </Link>
          </DashboardPanel>
        ) : null}

        {canInventory ? (
          <DashboardPanel
            title={t("warehouseDashboard.replenishment")}
            testId="warehouse-replenishment"
          >
            <MetricRow label={t("warehouseDashboard.requests.pending")} value={stockReqCounts.pending} />
            <MetricRow
              label={t("warehouseDashboard.requests.inProgress")}
              value={stockReqCounts.inProgress}
            />
            <MetricRow
              label={t("warehouseDashboard.requests.partial")}
              value={stockReqCounts.partiallyFulfilled}
            />
            <MetricRow
              label={t("warehouseDashboard.requests.fulfilled")}
              value={stockReqCounts.fulfilled}
            />
            <ul className="mt-2 m-0 flex list-none flex-col gap-1 p-0">
              {stockReqItems
                .filter((r) => ["Pending", "InProgress", "PartiallyFulfilled"].includes(r.status))
                .slice(0, 3)
                .map((r) => (
                  <li key={r.stockRequestId}>
                    <Link
                      to={`/inventory/stock-requests/${r.stockRequestId}`}
                      className="block text-[length:var(--exits-text-sm)] underline"
                    >
                      {r.requestNumber ?? r.stockRequestId.slice(0, 8)} ·{" "}
                      {r.destinationLocationName ?? "Location"} · {r.lineCount}{" "}
                      {t("stockRequest.items")} · {r.status}
                    </Link>
                  </li>
                ))}
            </ul>
            <Link
              to="/inventory/stock-requests"
              className="mt-2 inline-block text-[length:var(--exits-text-sm)] underline"
            >
              {t("warehouseDashboard.viewRequests")}
            </Link>
          </DashboardPanel>
        ) : null}

        {canInventory ? (
          <DashboardPanel title={t("warehouseDashboard.destinations")} testId="warehouse-destinations">
            {destinations.length === 0 ? (
              <DashboardQuietEmpty title={t("warehouseDashboard.destinationsEmpty")} />
            ) : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0">
                {destinations.map((d) => (
                  <li
                    key={d.destinationBranchId}
                    className="flex justify-between gap-2 text-[length:var(--exits-text-sm)]"
                  >
                    <span>{d.name}</span>
                    <span className="font-semibold tabular-nums">
                      {d.units} {t("warehouseDashboard.units")}
                    </span>
                  </li>
                ))}
              </ul>
            )}
            <p className="m-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
              {t("warehouseDashboard.destinationsNote")}
            </p>
          </DashboardPanel>
        ) : null}

        {canInventory ? (
          <DashboardPanel title={t("warehouseDashboard.topMoved")} testId="warehouse-top-moved">
            <div className="mb-2 flex gap-2">
              <button
                type="button"
                className={cn(
                  "dashboard-toolbar__preset",
                  movedDir === "outbound" && "dashboard-toolbar__preset--active",
                )}
                onClick={() => setMovedDir("outbound")}
              >
                {t("warehouseDashboard.outbound")}
              </button>
              <button
                type="button"
                className={cn(
                  "dashboard-toolbar__preset",
                  movedDir === "inbound" && "dashboard-toolbar__preset--active",
                )}
                onClick={() => setMovedDir("inbound")}
              >
                {t("warehouseDashboard.inbound")}
              </button>
            </div>
            {movedProducts.length === 0 ? (
              <DashboardQuietEmpty title={t("warehouseDashboard.topMovedEmpty")} />
            ) : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0">
                {movedProducts.map((p) => (
                  <li
                    key={p.productId}
                    className="flex justify-between gap-2 text-[length:var(--exits-text-sm)]"
                  >
                    <Link to={`/inventory/${p.productId}`} className="truncate underline">
                      {p.productName}
                    </Link>
                    <span className="font-semibold tabular-nums">{p.quantity}</span>
                  </li>
                ))}
              </ul>
            )}
          </DashboardPanel>
        ) : null}
      </div>
    </div>
  );
}

function formatSigned(n: number): string {
  if (n > 0) return `+${n}`;
  return String(n);
}
