import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import {
  cancelMyCustomerOrder,
  getMyCustomerOrder,
  sellerWorkspace,
} from "@/api/pos/pos-customer-orders-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
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

export function MyOrderDetailPage() {
  const { t } = useI18n();
  const { orderId = "" } = useParams();
  const [tokenReady, setTokenReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    void ensurePersonalBuyerPosToken().then((r) => setTokenReady(r.ok));
  }, []);

  const query = useQuery({
    queryKey: ["personal", "my-order", orderId],
    enabled: tokenReady && Boolean(orderId),
    queryFn: ({ signal }) =>
      getMyCustomerOrder(
        sellerWorkspace("00000000-0000-4000-8000-000000000001"),
        orderId,
        { partyType: "Personal" },
        signal,
      ),
  });

  async function cancelOrder() {
    if (!query.data) return;
    setBusy(true);
    setActionError(null);
    try {
      await cancelMyCustomerOrder(
        sellerWorkspace(query.data.sellerOrganizationId),
        query.data.sellerOrganizationId,
        orderId,
      );
      await query.refetch();
    } catch (err) {
      setActionError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : err instanceof Error
            ? err.message
            : t("orders.error"),
      );
    } finally {
      setBusy(false);
    }
  }

  if (!tokenReady || query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (query.isError || !query.data) {
    return (
      <div className="flex min-w-0 flex-col gap-4">
        <ErrorState title={t("orders.notFound")} detail={t("orders.notFoundHelp")} />
        <Button asChild className="min-h-11 w-fit">
          <Link to="/personal/orders">{t("personal.myOrdersLink")}</Link>
        </Button>
      </div>
    );
  }

  const order = query.data;
  const canCancel =
    order.status.localeCompare("Submitted", undefined, { sensitivity: "accent" }) === 0;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="my-order-detail-page">
      <PageHeader
        title={`#${order.orderNumber}`}
        description={`${order.branchNameSnapshot} · ${order.fulfillmentType}`}
      />
      <StatusChip tone="info">{t(displayOrderStatusKey(order) as MessageKey)}</StatusChip>
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/orders">{t("personal.myOrdersLink")}</Link>
      </Button>
      {actionError ? <ErrorState title={t("orders.error")} detail={actionError} /> : null}
      <Card className="flex flex-col gap-2" data-testid="order-lines">
        {order.lines.map((line) => (
          <div key={line.lineId} className="flex justify-between gap-2">
            <span>
              {line.nameSnapshot} × {line.quantity}
            </span>
            <strong>{money(line.lineTotal)}</strong>
          </div>
        ))}
        <div className="flex justify-between">
          <span>{t("orders.subtotal")}</span>
          <strong>{money(order.merchandiseSubtotal)}</strong>
        </div>
        <div className="flex justify-between">
          <span>{t("orders.deliveryFee")}</span>
          <strong>{money(order.deliveryFee)}</strong>
        </div>
        <div className="flex justify-between">
          <span>{t("orders.total")}</span>
          <strong>{money(order.total)}</strong>
        </div>
      </Card>
      {order.delivery ? (
        <Card data-testid="order-delivery">
          <p className="m-0 font-semibold">{t("orders.deliveryAddress")}</p>
          <p className="m-0">{order.delivery.recipientName}</p>
          <p className="m-0">{order.delivery.addressLine1}</p>
        </Card>
      ) : null}
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("orders.noLiveTracking")}
      </p>
      {canCancel ? (
        <Button
          type="button"
          variant="ghost"
          className="min-h-11 w-fit"
          data-testid="cancel-order"
          disabled={busy}
          onClick={() => void cancelOrder()}
        >
          {t("orders.cancel")}
        </Button>
      ) : null}
    </div>
  );
}
