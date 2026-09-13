import { useMemo, useState, type ReactNode } from "react";
import { StatusChip } from "@/components/exits/StatusChip";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  buildPurchaseOrderActivityEvents,
  formatActivityDateTime,
  type PurchaseOrderActivityEvent,
} from "@/features/purchasing/purchase-order-activity";
import type {
  PosGoodsReceiptDto,
  PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/button";
import { ChevronDown, ChevronRight } from "lucide-react";

type PurchaseOrderActivityTimelineProps = {
  po: PosPurchaseOrderDto;
  receipts: readonly PosGoodsReceiptDto[];
  resolveActor: ReturnType<typeof useActorDirectory>["resolve"];
  isResolving: boolean;
  /** Expand receipt detail (reverse / lines) when user opens a receipt node. */
  renderReceiptDetail?: (receiptId: string) => ReactNode;
};

function eventTitle(
  event: PurchaseOrderActivityEvent,
  t: (key: Parameters<ReturnType<typeof useI18n>["t"]>[0]) => string,
): string {
  switch (event.kind) {
    case "created":
      return t("purchasing.activity.created");
    case "submitted":
      return t("purchasing.activity.submitted");
    case "supplier_accepted":
      return t("purchasing.activity.supplierAccepted");
    case "supplier_declined":
      return t("purchasing.activity.supplierDeclined");
    case "changes_proposed":
      return t("purchasing.activity.changesProposed");
    case "withdrawn":
      return t("purchasing.activity.withdrawn");
    case "receipt":
      return t("purchasing.activity.receipt").replace("{grn}", event.grnNumber ?? "");
    case "receipt_reversed":
      return t("purchasing.activity.receiptReversed").replace("{grn}", event.grnNumber ?? "");
    case "completed":
      return t("purchasing.activity.completed");
  }
}

export function PurchaseOrderActivityTimeline({
  po,
  receipts,
  resolveActor,
  isResolving,
  renderReceiptDetail,
}: PurchaseOrderActivityTimelineProps) {
  const { t } = useI18n();
  const events = useMemo(
    () => buildPurchaseOrderActivityEvents({ po, receipts }),
    [po, receipts],
  );
  const [expandedReceiptId, setExpandedReceiptId] = useState<string | null>(null);

  if (events.length === 0) {
    return (
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="po-activity-empty">
        {t("purchasing.activity.empty")}
      </p>
    );
  }

  return (
    <ol className="po-activity-timeline m-0 flex list-none flex-col p-0" data-testid="po-activity-timeline">
      {events.map((event, index) => {
        const { date, time } = formatActivityDateTime(event.atUtc);
        const isLast = index === events.length - 1;
        const canExpand =
          event.kind === "receipt" && Boolean(event.receiptId) && Boolean(renderReceiptDetail);
        const expanded = canExpand && expandedReceiptId === event.receiptId;

        return (
          <li
            key={event.id}
            className="po-activity-timeline__item"
            data-testid={`po-activity-${event.kind}-${event.id}`}
          >
            <div className="po-activity-timeline__rail" aria-hidden>
              <span className="po-activity-timeline__dot" />
              {!isLast ? <span className="po-activity-timeline__line" /> : null}
            </div>
            <div className="po-activity-timeline__body min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <p className="m-0 font-medium text-[length:var(--exits-text-sm)]">
                  {eventTitle(event, t)}
                </p>
                {event.receiptResult === "partial" ? (
                  <StatusChip tone="warning">{t("purchasing.activity.partialReceipt")}</StatusChip>
                ) : null}
                {event.receiptResult === "fully_received" ? (
                  <StatusChip tone="success">{t("purchasing.activity.fullyReceived")}</StatusChip>
                ) : null}
                {event.receiptResult === "reversed" ? (
                  <StatusChip tone="danger">{t("purchasing.receiptStatus.voided")}</StatusChip>
                ) : null}
              </div>
              <p className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                {date}
                {time ? ` · ${time}` : ""}
              </p>
              {event.actorId ? (
                <div className="mt-1">
                  <ActorAttribution
                    labelKey="common.by"
                    actorId={event.actorId}
                    occurredAtUtc={undefined}
                    hideTimestamp
                    resolved={resolveActor(event.actorId)}
                    isLoading={isResolving}
                    testId={`po-activity-actor-${event.id}`}
                  />
                </div>
              ) : null}

              {event.kind === "receipt" && event.lines && event.lines.length > 0 ? (
                <ul className="m-0 mt-2 list-none space-y-0.5 p-0 text-[length:var(--exits-text-sm)]">
                  {event.lines.map((line) => (
                    <li key={`${event.id}-${line.productName}-${line.uom}`}>
                      <span className="text-muted">{t("purchasing.activity.receivedLabel")}: </span>
                      {line.productName}{" "}
                      {line.orderedQty != null
                        ? `${line.goodQty} / ${line.orderedQty}`
                        : `${line.goodQty} ${line.uom}`}
                      {line.damagedQty > 0
                        ? ` · ${t("purchasing.damaged")} ${line.damagedQty}`
                        : ""}
                      {line.cancelledRemainingQty > 0
                        ? ` · ${t("purchasing.cancelRemaining")} ${line.cancelledRemainingQty}`
                        : ""}
                    </li>
                  ))}
                </ul>
              ) : null}

              {canExpand ? (
                <div className="mt-2">
                  <Button
                    type="button"
                    variant="ghost"
                    className="h-auto px-0 py-0 text-[length:var(--exits-text-sm)]"
                    data-testid={`po-activity-expand-${event.receiptId}`}
                    onClick={() =>
                      setExpandedReceiptId((prev) =>
                        prev === event.receiptId ? null : (event.receiptId ?? null),
                      )
                    }
                  >
                    {expanded ? (
                      <ChevronDown className="size-4 shrink-0" aria-hidden />
                    ) : (
                      <ChevronRight className="size-4 shrink-0" aria-hidden />
                    )}
                    {expanded
                      ? t("purchasing.activity.hideReceiptDetail")
                      : t("purchasing.activity.showReceiptDetail")}
                  </Button>
                  <div
                    className={cn("mt-2", !expanded && "hidden")}
                    data-testid={`po-activity-detail-${event.receiptId}`}
                  >
                    {expanded && event.receiptId
                      ? renderReceiptDetail?.(event.receiptId)
                      : null}
                  </div>
                </div>
              ) : null}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
