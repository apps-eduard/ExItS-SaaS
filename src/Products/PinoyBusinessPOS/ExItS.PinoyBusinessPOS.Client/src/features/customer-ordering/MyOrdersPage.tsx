import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { listMyCustomerOrders, sellerWorkspace } from "@/api/pos/pos-customer-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { displayOrderStatusKey } from "@/features/customer-ordering/seller-order-actions";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

/** Buyer list uses a synthetic workspace org header (first seller on items or placeholder). */
const BUYER_SCOPE_ORG = "00000000-0000-4000-8000-000000000001";

export function MyOrdersPage() {
  const { t } = useI18n();
  const [tokenReady, setTokenReady] = useState(false);

  useEffect(() => {
    void ensurePersonalBuyerPosToken().then((r) => setTokenReady(r.ok));
  }, []);

  const query = useQuery({
    queryKey: ["personal", "my-orders"],
    enabled: tokenReady,
    queryFn: ({ signal }) =>
      listMyCustomerOrders(
        sellerWorkspace(BUYER_SCOPE_ORG),
        { partyType: "Personal", pageSize: 40 },
        signal,
      ),
  });

  if (!tokenReady || query.isLoading) {
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

  const items = query.data?.items ?? [];

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="my-orders-page">
      <PageHeader title={t("personal.myOrdersTitle")} description={t("personal.myOrdersLede")} />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/linked-merchants">{t("personal.backToMerchants")}</Link>
      </Button>
      {items.length === 0 ? (
        <EmptyState title={t("orders.emptyTitle")} detail={t("orders.emptyBuyerDetail")} />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-3 p-0">
          {items.map((order) => (
            <li key={order.orderId}>
              <Card className="flex flex-col gap-2" data-testid="my-order-card">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <strong>#{order.orderNumber}</strong>
                  <StatusChip tone="info">
                    {t(displayOrderStatusKey(order) as MessageKey)}
                  </StatusChip>
                </div>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {order.branchNameSnapshot} · {order.fulfillmentType} · {money(order.total)}
                </p>
                <Button asChild className="min-h-11 w-fit">
                  <Link to={`/personal/orders/${order.orderId}`}>{t("orders.viewDetails")}</Link>
                </Button>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
