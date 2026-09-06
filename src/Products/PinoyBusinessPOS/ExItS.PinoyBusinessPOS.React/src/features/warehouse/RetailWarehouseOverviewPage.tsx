import { Link, useOutletContext } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, PackagePlus } from "lucide-react";
import { getOutgoingStockRequestSummary } from "@/api/pos/pos-stock-requests-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatTransferTimestamp } from "@/features/inventory/inventory-transfer-labels";
import {
  stockRequestStatusLabelKey,
  stockRequestStatusTone,
} from "@/features/replenishment/stock-request-helpers";
import type { RetailWarehouseResolveState } from "@/features/warehouse/retail-warehouse-resolve";
import { useRetailWarehouseResolve } from "@/features/warehouse/useRetailWarehouseResolve";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function useReadySupply(): Extract<RetailWarehouseResolveState, { kind: "ready" }> | null {
  const outlet = useOutletContext<RetailWarehouseResolveState | null>();
  const { resolveState } = useRetailWarehouseResolve();
  const state = outlet ?? resolveState;
  return state?.kind === "ready" ? state : null;
}

export function RetailWarehouseOverviewPage() {
  const { t } = useI18n();
  const { workspace } = useRetailWarehouseResolve();
  const supply = useReadySupply();

  const summaryQuery = useQuery({
    queryKey: ["stock-requests-outgoing-summary", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getOutgoingStockRequestSummary(workspace!, signal),
  });

  if (!workspace || !supply) {
    return null;
  }

  if (summaryQuery.isLoading) {
    return <LoadingState label={t("retailWarehouse.loading")} />;
  }

  if (summaryQuery.isError) {
    return (
      <ErrorState
        title={t("retailWarehouse.loadError")}
        detail={t("retailWarehouse.loadError")}
      />
    );
  }

  const summary = summaryQuery.data!;
  const metrics = [
    {
      key: "submitted",
      label: t("retailWarehouse.metric.submitted"),
      count: summary.submittedCount,
      to: "/warehouse/my-requests?tab=submitted",
      testId: "retail-warehouse-metric-submitted",
    },
    {
      key: "inProgress",
      label: t("retailWarehouse.metric.inProgress"),
      count: summary.inProgressCount,
      to: "/warehouse/my-requests?tab=inProgress",
      testId: "retail-warehouse-metric-in-progress",
    },
    {
      key: "inTransit",
      label: t("retailWarehouse.metric.inTransit"),
      count: summary.inTransitCount,
      to: "/warehouse/incoming",
      testId: "retail-warehouse-metric-in-transit",
    },
  ];

  return (
    <div className="flex flex-col gap-3" data-testid="retail-warehouse-overview">
      <section
        className="rounded-[var(--exits-radius-md)] border border-border p-3"
        data-testid="retail-warehouse-supply-card"
      >
        <div className="text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
          {t("retailWarehouse.supplyCard.eyebrow")}
        </div>
        <div className="mt-1 flex flex-wrap items-center justify-between gap-2">
          <div className="min-w-0">
            <div className="text-[length:var(--exits-text-lg)] font-semibold text-foreground">
              {supply.supplyWarehouseName}
            </div>
            <div className="mt-0.5 flex flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)] text-muted">
              {supply.isPreferred ? (
                <StatusChip tone="info">{t("retailWarehouse.supplyCard.preferred")}</StatusChip>
              ) : null}
              <StatusChip tone="success">{t("retailWarehouse.supplyCard.connected")}</StatusChip>
            </div>
          </div>
          <Button asChild data-testid="retail-warehouse-request-cta">
            <Link to="/warehouse/request-stock">
              <PackagePlus className="size-4" aria-hidden />
              {t("retailWarehouse.supplyCard.requestStock")}
            </Link>
          </Button>
        </div>
      </section>

      <section data-testid="retail-warehouse-attention">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("retailWarehouse.needsAttention")}
        </h2>
        <div className="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-3">
          {metrics.map((metric) => (
            <Link
              key={metric.key}
              to={metric.to}
              className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface)] px-3 py-2.5 no-underline"
              data-testid={metric.testId}
            >
              <div className="text-[length:var(--exits-text-xs)] text-muted">{metric.label}</div>
              <div className="mt-1 text-[length:var(--exits-text-xl)] font-semibold tabular-nums text-foreground">
                {metric.count}
              </div>
            </Link>
          ))}
        </div>
      </section>

      <section data-testid="retail-warehouse-recent">
        <div className="flex items-center justify-between gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {t("retailWarehouse.recentRequests")}
          </h2>
          <Link
            to="/warehouse/my-requests"
            className="text-[length:var(--exits-text-sm)] font-medium text-[var(--exits-primary)] no-underline"
            data-testid="retail-warehouse-view-all"
          >
            {t("retailWarehouse.viewAll")}
          </Link>
        </div>

        {summary.recent.length === 0 ? (
          <EmptyState
            title={t("retailWarehouse.recentEmpty")}
            detail={t("retailWarehouse.recentEmptyDetail")}
          />
        ) : (
          <ul className="m-0 mt-2 flex list-none flex-col gap-2 p-0">
            {summary.recent.map((item) => (
              <li key={item.stockRequestId}>
                <Link
                  to={`/warehouse/requests/${item.stockRequestId}`}
                  className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border p-3 no-underline"
                  data-testid={`retail-warehouse-recent-${item.stockRequestId}`}
                >
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-medium text-foreground">
                        {item.requestNumber ?? item.stockRequestId.slice(0, 8)}
                      </span>
                      <StatusChip tone={stockRequestStatusTone(item.status)}>
                        {t(stockRequestStatusLabelKey(item.status) as MessageKey)}
                      </StatusChip>
                    </div>
                    <div className="mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                      {item.requestedSourceLocationName ?? item.requestedSourceLocationId}
                      {" · "}
                      {formatTransferTimestamp(item.updatedAtUtc)}
                    </div>
                  </div>
                  <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden />
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
