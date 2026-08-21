import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { listSellerCustomerOrders, sellerWorkspace } from "@/api/pos/pos-customer-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  displayOrderStatusKey,
  filterSellerOrdersClientSide,
  sellerFilterApiStatus,
  type SellerOrderFilter,
} from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

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
    return <LoadingState label={t("session.loading")} />;
  }

  if (query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (query.isError) {
    return (
      <div className="flex min-w-0 flex-col gap-4">
        <ErrorState
          title={t("orders.error")}
          detail={query.error instanceof Error ? query.error.message : t("error.detail")}
        />
        <Button type="button" className="min-h-11 w-fit" onClick={() => void query.refetch()}>
          {t("orders.retry")}
        </Button>
      </div>
    );
  }

  const items = filterSellerOrdersClientSide(query.data?.items ?? [], filter);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="seller-orders-page">
      <PageHeader title={t("orders.sellerTitle")} description={t("orders.sellerLede")} />
      <div className="flex flex-wrap gap-2" role="group" aria-label={t("orders.filterLabel")}>
        {FILTERS.map((f) => (
          <Button
            key={f}
            type="button"
            variant={filter === f ? "default" : "ghost"}
            className="min-h-11"
            data-testid={`orders-filter-${f.toLowerCase()}`}
            onClick={() => setFilter(f)}
          >
            {t(`orders.filter${f}` as MessageKey)}
          </Button>
        ))}
      </div>
      {items.length === 0 ? (
        <EmptyState title={t("orders.emptyTitle")} detail={t("orders.emptySellerDetail")} />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-3 p-0" data-testid="seller-orders-list">
          {items.map((order) => (
            <li key={order.orderId}>
              <Card className="flex flex-col gap-2" data-testid="seller-order-card">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <strong>{order.customerDisplayName}</strong>
                  <StatusChip tone="info">
                    {t(displayOrderStatusKey(order) as MessageKey)}
                  </StatusChip>
                </div>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  #{order.orderNumber} · {order.fulfillmentType} · {money(order.total)} ·{" "}
                  {order.lineCount} {t("orders.items")}
                </p>
                <Button asChild className="min-h-11 w-fit">
                  <Link to={`/orders/${order.orderId}`}>{t("orders.open")}</Link>
                </Button>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
