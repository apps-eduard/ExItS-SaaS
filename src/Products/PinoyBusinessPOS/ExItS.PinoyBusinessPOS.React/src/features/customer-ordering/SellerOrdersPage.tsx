import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight } from "lucide-react";
import { listSellerCustomerOrders, sellerWorkspace } from "@/api/pos/pos-customer-orders-client";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { pageBackNav } from "@/navigation/page-back-nav";
import {
  displayOrderStatusKey,
  filterSellerOrdersClientSide,
  sellerFilterApiStatus,
  type SellerOrderFilter,
} from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

const FILTERS: SellerOrderFilter[] = ["New", "Preparing", "Ready", "Issues", "All"];

export function SellerOrdersPage() {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const [filter, setFilter] = useState<SellerOrderFilter>("New");

  const workspace = useMemo(
    () =>
      boundWorkspace
        ? sellerWorkspace(boundWorkspace.organizationId, boundWorkspace.branchId)
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["seller-orders", workspace?.organizationId, filter],
    enabled: Boolean(workspace),
    staleTime: 15_000,
    meta: { suppressGlobalError: true, operation: "list seller orders" },
    queryFn: ({ signal }) =>
      listSellerCustomerOrders(
        workspace!,
        {
          status: sellerFilterApiStatus(filter),
          pageSize: filter === "New" ? 40 : 80,
        },
        signal,
      ),
  });

  if (!workspace) {
    return <BranchRequiredPanel title={t("orders.sellerTitle")} />;
  }

  const items = filterSellerOrdersClientSide(query.data?.items ?? [], filter);

  return (
    <div
      className="customer-orders-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="seller-orders-page"
    >
      <PageHeader
        title={t("orders.sellerTitle")}
        description={t("orders.sellerLede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-customer-orders"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("orders.filterLabel")}
        testId="seller-orders-filters"
        items={FILTERS.map((f) => ({
          key: f,
          label: t(`orders.filter${f}` as MessageKey),
          state: filter === f ? "active" : "idle",
          testId: `orders-filter-${f.toLowerCase()}`,
          onSelect: () => setFilter(f),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isFetching && !query.isLoading && query.data ? (
        <BackgroundRefreshIndicator active label={t("loading.updating")} />
      ) : null}

      {query.isError ? (
        <div className="flex min-w-0 flex-col gap-4">
          <ErrorState
            title={t("orders.error")}
            detail={describePosApiError(query.error, t, "error.detail")}
          />
          <Button type="button" className="w-fit" onClick={() => void query.refetch()}>
            {t("orders.retry")}
          </Button>
        </div>
      ) : null}

      {!query.isLoading && !query.isError ? (
        items.length === 0 ? (
          <EmptyState title={t("orders.emptyTitle")} detail={t("orders.emptySellerDetail")} />
        ) : (
          <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="seller-orders-list">
            {items.map((order) => (
              <li key={order.orderId}>
                <Link
                  className="exits-list__card customer-order-row block min-w-0 text-foreground no-underline"
                  to={`/orders/${order.orderId}`}
                  data-testid="seller-order-card"
                >
                  <div className="customer-order-row__main min-w-0">
                    <strong className="exits-list__name block truncate font-semibold">
                      {order.customerDisplayName}
                    </strong>
                    <p className="customer-order-row__meta mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                      #{order.orderNumber} · {order.fulfillmentType} · {order.lineCount}{" "}
                      {t("orders.items")}
                    </p>
                  </div>
                  <div className="customer-order-row__aside">
                    <span className="customer-order-row__total">{money(order.total)}</span>
                    <StatusChip tone="info">
                      {t(displayOrderStatusKey(order) as MessageKey)}
                    </StatusChip>
                    <ChevronRight
                      className="customer-order-row__chevron size-4 shrink-0 text-muted"
                      aria-hidden
                    />
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )
      ) : null}
    </div>
  );
}
