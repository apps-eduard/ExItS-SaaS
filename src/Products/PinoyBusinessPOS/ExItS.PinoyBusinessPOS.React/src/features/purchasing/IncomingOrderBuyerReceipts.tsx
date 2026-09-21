import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import type { IncomingOrderBuyerReceipt } from "@/api/pos/pos-connected-suppliers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatActivityDateTime } from "@/features/purchasing/purchase-order-activity";
import { formatStockQtyLabel } from "@/features/purchasing/incoming-order-stock-review";
import { navigateWithReturn } from "@/navigation/smart-back";
import { cn } from "@/lib/cn";

const HISTORY_PAGE_SIZE = 5;

export type IncomingOrderBuyerReceiptsProps = {
  receipts: readonly IncomingOrderBuyerReceipt[];
  buyerName: string;
  buyerLabel: string;
  connectedPurchaseOrderId: string;
  latestTitle: string;
  historyTitle: string;
  viewDetailsLabel: string;
  goodLabel: string;
  damagedLabel: string;
  missingLabel: string;
  deliveryRefLabel: string;
  notesLabel: string;
  loadMoreLabel: string;
  postedLabel: string;
  voidedLabel: string;
  emptyLabel: string;
};

function receiptStatusLabel(status: string, postedLabel: string, voidedLabel: string): string {
  return status === "Voided" ? voidedLabel : postedLabel;
}

function receiptStatusTone(status: string): "success" | "danger" | "info" {
  return status === "Voided" ? "danger" : "success";
}

function formatReceiptWhen(isoUtc: string): string {
  const { date, time } = formatActivityDateTime(isoUtc);
  return time ? `${date} ${time}` : date;
}

/**
 * Latest buyer GRN summary + compact clickable history for seller incoming order detail.
 */
export function IncomingOrderBuyerReceipts({
  receipts,
  buyerName,
  buyerLabel,
  connectedPurchaseOrderId,
  latestTitle,
  historyTitle,
  viewDetailsLabel,
  goodLabel,
  damagedLabel,
  missingLabel,
  deliveryRefLabel,
  notesLabel,
  loadMoreLabel,
  postedLabel,
  voidedLabel,
  emptyLabel,
}: IncomingOrderBuyerReceiptsProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const [historyLimit, setHistoryLimit] = useState(HISTORY_PAGE_SIZE);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const posted = useMemo(
    () => receipts.filter((r) => r.status !== "Voided"),
    [receipts],
  );
  const latest = posted[0] ?? receipts[0] ?? null;
  const history = receipts.slice(0, historyLimit);
  const hasMore = receipts.length > historyLimit;

  function openReceipt(receiptId: string) {
    setExpandedId((prev) => (prev === receiptId ? null : receiptId));
    navigateWithReturn(
      navigate,
      `/purchasing/incoming-orders/${connectedPurchaseOrderId}/receipts/${receiptId}`,
      location,
    );
  }

  if (receipts.length === 0) {
    return (
      <Card className="flex flex-col gap-2 p-4" data-testid="incoming-order-buyer-receipts-empty">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">{latestTitle}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{emptyLabel}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-3" data-testid="incoming-order-buyer-receipts">
      {latest ? (
        <Card className="incoming-order-latest-receipt flex flex-col gap-3 p-4 px-5" data-testid="incoming-order-latest-receipt">
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div>
              <h2 className="incoming-order-latest-receipt__title m-0 text-[length:var(--exits-text-md)] font-semibold text-[var(--exits-primary)]">
                {latestTitle}
              </h2>
              <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {latest.grnNumber} · {formatReceiptWhen(latest.receivedAtUtc)}
              </p>
            </div>
            <StatusChip tone={receiptStatusTone(latest.status)}>
              {receiptStatusLabel(latest.status, postedLabel, voidedLabel)}
            </StatusChip>
          </div>

          <dl
            className="incoming-order-latest-receipt__meta m-0"
            data-testid="incoming-order-latest-receipt-meta"
          >
            <div className="incoming-order-latest-receipt__field incoming-order-latest-receipt__field--buyer min-w-0">
              <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">{buyerLabel}</dt>
              <dd className="m-0 truncate font-semibold" title={buyerName}>
                {buyerName}
              </dd>
            </div>
            <div className="incoming-order-latest-receipt__field min-w-0">
              <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">{deliveryRefLabel}</dt>
              <dd className="m-0 truncate font-semibold" title={latest.deliveryReference?.trim() || undefined}>
                {latest.deliveryReference?.trim() || "—"}
              </dd>
            </div>
            <div className="incoming-order-latest-receipt__field min-w-0">
              <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">{goodLabel}</dt>
              <dd className="m-0 tabular-nums font-semibold">
                {formatStockQtyLabel(latest.goodQtyTotal)}
              </dd>
            </div>
            <div className="incoming-order-latest-receipt__field min-w-0">
              <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">{damagedLabel}</dt>
              <dd className="m-0 tabular-nums font-semibold">
                {formatStockQtyLabel(latest.damagedQtyTotal)}
              </dd>
            </div>
            {latest.missingQtyTotal > 0 ? (
              <div className="incoming-order-latest-receipt__field min-w-0">
                <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">{missingLabel}</dt>
                <dd className="m-0 tabular-nums font-semibold">
                  {formatStockQtyLabel(latest.missingQtyTotal)}
                </dd>
              </div>
            ) : null}
            {latest.notes?.trim() ? (
              <div className="incoming-order-latest-receipt__field min-w-0">
                <dt className="m-0 text-[length:var(--exits-text-xs)] text-muted">{notesLabel}</dt>
                <dd className="m-0 truncate font-medium" title={latest.notes.trim()}>
                  {latest.notes.trim()}
                </dd>
              </div>
            ) : null}
          </dl>

          <div>
            <Button
              type="button"
              variant="outline"
              data-testid="incoming-order-view-latest-receipt"
              onClick={() => openReceipt(latest.goodsReceiptId)}
            >
              {viewDetailsLabel}
            </Button>
          </div>
        </Card>
      ) : null}

      {receipts.length > 1 ? (
        <Card className="flex flex-col gap-2 p-4" data-testid="incoming-order-receipt-history">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">{historyTitle}</h2>
          <ul className="m-0 list-none flex-col gap-1 p-0">
            {history.map((receipt) => (
              <li key={receipt.goodsReceiptId}>
                <button
                  type="button"
                  className={cn(
                    "flex w-full flex-wrap items-center justify-between gap-2 rounded-md border border-border bg-background px-3 py-2 text-left",
                    expandedId === receipt.goodsReceiptId && "ring-1 ring-[var(--exits-primary)]",
                  )}
                  data-testid={`incoming-order-receipt-row-${receipt.goodsReceiptId}`}
                  onClick={() => openReceipt(receipt.goodsReceiptId)}
                >
                  <span className="font-medium">{receipt.grnNumber}</span>
                  <span className="text-[length:var(--exits-text-sm)] text-muted tabular-nums">
                    {formatReceiptWhen(receipt.receivedAtUtc)}
                  </span>
                  <span className="text-[length:var(--exits-text-sm)] tabular-nums">
                    {goodLabel} {formatStockQtyLabel(receipt.goodQtyTotal)}
                    {" · "}
                    {damagedLabel} {formatStockQtyLabel(receipt.damagedQtyTotal)}
                  </span>
                  <StatusChip tone={receiptStatusTone(receipt.status)}>
                    {receiptStatusLabel(receipt.status, postedLabel, voidedLabel)}
                  </StatusChip>
                </button>
              </li>
            ))}
          </ul>
          {hasMore ? (
            <Button
              type="button"
              variant="ghost"
              data-testid="incoming-order-receipt-history-more"
              onClick={() => setHistoryLimit((n) => n + HISTORY_PAGE_SIZE)}
            >
              {loadMoreLabel}
            </Button>
          ) : null}
        </Card>
      ) : null}
    </div>
  );
}
