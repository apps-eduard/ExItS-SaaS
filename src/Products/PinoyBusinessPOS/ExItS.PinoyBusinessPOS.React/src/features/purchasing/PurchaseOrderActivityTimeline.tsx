import { useMemo, useState, type ReactNode } from "react";
import {
  Ban,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  ClipboardCheck,
  CreditCard,
  FilePlus2,
  PackageCheck,
  PackageX,
  Send,
  ShieldAlert,
  Timer,
  Truck,
  Undo2,
  Wallet,
  XCircle,
} from "lucide-react";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  ExitsActivityTimeline,
  type ExitsActivityTimelineTone,
} from "@/components/exits/ExitsActivityTimeline";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import type { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  buildPurchaseOrderActivityEvents,
  type PurchaseOrderActivityEvent,
} from "@/features/purchasing/purchase-order-activity";
import type {
  PosGoodsReceiptDto,
  PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/button";

type PurchaseOrderActivityTimelineProps = {
  po?: PosPurchaseOrderDto;
  receipts?: readonly PosGoodsReceiptDto[];
  /** When set, used instead of building from po + receipts (seller / precomputed). */
  events?: readonly PurchaseOrderActivityEvent[];
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
    case "supplier_preparing":
      return t("purchasing.activity.supplierPreparing");
    case "supplier_ready":
      return t("purchasing.activity.supplierReady");
    case "changes_proposed":
      return t("purchasing.activity.changesProposed");
    case "stock_reserved":
      return t("purchasing.activity.stockReserved");
    case "proposal_reservation":
      return t("purchasing.activity.proposalReservation");
    case "reservation_confirmed":
      return t("purchasing.activity.reservationConfirmed");
    case "reservation_released":
      return t("purchasing.activity.reservationReleased");
    case "reservation_expired":
      return t("purchasing.activity.reservationExpired");
    case "withdrawn":
      return t("purchasing.activity.withdrawn");
    case "cancelled":
      return t("purchasing.activity.cancelled");
    case "receipt":
      return t("purchasing.activity.receipt").replace("{grn}", event.grnNumber ?? "");
    case "receipt_reversed":
      return t("purchasing.activity.receiptReversed").replace("{grn}", event.grnNumber ?? "");
    case "remaining_closed":
      return t("purchasing.activity.remainingClosed");
    case "awaiting_payment":
      return t("purchasing.activity.awaitingPayment");
    case "payment_confirmed":
      return t("purchasing.activity.paymentConfirmed");
    case "no_payment_due":
      return t("purchasing.activity.noPaymentDue");
    case "completed":
      return t("purchasing.activity.completed");
  }
}

function eventTone(kind: PurchaseOrderActivityEvent["kind"]): ExitsActivityTimelineTone {
  switch (kind) {
    case "created":
    case "submitted":
    case "supplier_preparing":
      return "info";
    case "supplier_accepted":
    case "supplier_ready":
    case "stock_reserved":
    case "proposal_reservation":
    case "reservation_confirmed":
    case "payment_confirmed":
    case "no_payment_due":
    case "completed":
      return "success";
    case "changes_proposed":
    case "awaiting_payment":
    case "receipt":
      return "warning";
    case "supplier_declined":
    case "cancelled":
    case "withdrawn":
    case "receipt_reversed":
    case "reservation_released":
    case "reservation_expired":
    case "remaining_closed":
      return "danger";
    default:
      return "primary";
  }
}

function eventIcon(kind: PurchaseOrderActivityEvent["kind"]) {
  const className = "size-4 shrink-0";
  switch (kind) {
    case "created":
      return <FilePlus2 className={className} aria-hidden />;
    case "submitted":
      return <Send className={className} aria-hidden />;
    case "supplier_accepted":
    case "supplier_ready":
      return <ClipboardCheck className={className} aria-hidden />;
    case "supplier_declined":
    case "cancelled":
    case "withdrawn":
      return <XCircle className={className} aria-hidden />;
    case "supplier_preparing":
      return <Truck className={className} aria-hidden />;
    case "changes_proposed":
      return <ShieldAlert className={className} aria-hidden />;
    case "stock_reserved":
    case "proposal_reservation":
    case "reservation_confirmed":
      return <Timer className={className} aria-hidden />;
    case "reservation_released":
    case "reservation_expired":
      return <Ban className={className} aria-hidden />;
    case "receipt":
      return <PackageCheck className={className} aria-hidden />;
    case "receipt_reversed":
      return <Undo2 className={className} aria-hidden />;
    case "remaining_closed":
      return <PackageX className={className} aria-hidden />;
    case "awaiting_payment":
      return <Wallet className={className} aria-hidden />;
    case "payment_confirmed":
      return <CreditCard className={className} aria-hidden />;
    case "no_payment_due":
    case "completed":
      return <CheckCircle2 className={className} aria-hidden />;
  }
}

export function PurchaseOrderActivityTimeline({
  po,
  receipts = [],
  events: eventsProp,
  resolveActor,
  isResolving,
  renderReceiptDetail,
}: PurchaseOrderActivityTimelineProps) {
  const { t } = useI18n();
  const events = useMemo(
    () =>
      eventsProp
        ? [...eventsProp]
        : po
          ? buildPurchaseOrderActivityEvents({ po, receipts })
          : [],
    [eventsProp, po, receipts],
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
    <ExitsActivityTimeline
      testId="po-activity-timeline"
      items={events.map((event) => {
        const canExpand =
          event.kind === "receipt" && Boolean(event.receiptId) && Boolean(renderReceiptDetail);
        const expanded = canExpand && expandedReceiptId === event.receiptId;
        const resolved = resolveActor(event.actorId);
        const actorName =
          resolved?.displayName && resolved.actorStatus !== "NotAvailable"
            ? resolved.displayName
            : null;

        const chips =
          event.receiptResult === "partial" ? (
            <StatusChip tone="warning">{t("purchasing.activity.partialReceipt")}</StatusChip>
          ) : event.receiptResult === "fully_received" ? (
            <StatusChip tone="success">{t("purchasing.activity.fullyReceived")}</StatusChip>
          ) : event.receiptResult === "reversed" ? (
            <StatusChip tone="danger">{t("purchasing.receiptStatus.voided")}</StatusChip>
          ) : null;

        const hasDescription =
          Boolean(chips) || Boolean(event.actorId) || Boolean(event.note?.trim());

        return {
          id: event.id,
          atUtc: event.atUtc,
          title: eventTitle(event, t),
          description: hasDescription ? (
            <div className="flex flex-col gap-1.5">
              {chips ? <div className="flex flex-wrap gap-1.5">{chips}</div> : null}
              {event.actorId ? (
                <ActorAttribution
                  labelKey="common.by"
                  actorId={event.actorId}
                  hideTimestamp
                  resolved={resolved}
                  isLoading={isResolving}
                  testId={`po-activity-actor-${event.id}`}
                />
              ) : null}
              {event.note?.trim() ? (
                <p className="m-0">
                  {t("incomingOrders.sellerRemarks")}: {event.note.trim()}
                </p>
              ) : null}
            </div>
          ) : undefined,
          tone: eventTone(event.kind),
          icon: eventIcon(event.kind),
          actorName,
          actorLoading: Boolean(event.actorId) && isResolving && !actorName,
          testId: `po-activity-${event.kind}-${event.id}`,
          children: (
            <>
              {event.kind === "changes_proposed" && event.proposalSummary ? (
                <div
                  className="space-y-1 text-[length:var(--exits-text-sm)]"
                  data-testid={`po-activity-proposal-summary-${event.id}`}
                >
                  {event.proposalSummary.changedLines.map((line) => (
                    <p key={`${event.id}-${line.productName}`} className="m-0">
                      <span className="font-medium">{line.productName}</span>
                      <span className="text-muted"> — {line.detail}</span>
                    </p>
                  ))}
                  {event.proposalSummary.originalTotal != null &&
                  event.proposalSummary.proposedTotal != null ? (
                    <p className="m-0 text-muted">
                      {t("incomingOrders.originalOrderTotal")}: ₱
                      {event.proposalSummary.originalTotal.toFixed(2)}
                      {" · "}
                      {t("incomingOrders.proposedOrderTotal")}: ₱
                      {event.proposalSummary.proposedTotal.toFixed(2)}
                    </p>
                  ) : null}
                  {event.proposalSummary.reservationExpiresAtUtc ? (
                    <p className="m-0 text-muted">
                      {t("purchasing.reservedUntil").replace(
                        "{datetime}",
                        new Date(event.proposalSummary.reservationExpiresAtUtc).toLocaleString(),
                      )}
                    </p>
                  ) : null}
                </div>
              ) : null}

              {event.kind === "receipt" && event.lines && event.lines.length > 0 ? (
                <ul className="exits-activity-timeline__lines m-0 list-none space-y-1 p-0 text-[length:var(--exits-text-sm)]">
                  {event.lines.map((line) => (
                    <li
                      key={`${event.id}-${line.productName}-${line.uom}`}
                      className="flex items-start gap-2 text-muted"
                    >
                      <PackageCheck className="mt-0.5 size-3.5 shrink-0 opacity-70" aria-hidden />
                      <span>
                        <span className="font-medium text-foreground">{line.productName}</span>
                        {" · "}
                        {line.orderedQty != null
                          ? `${line.goodQty} / ${line.orderedQty}`
                          : `${line.goodQty} ${line.uom}`}
                        {line.damagedQty > 0
                          ? ` · ${t("purchasing.damaged")} ${line.damagedQty}`
                          : ""}
                        {line.cancelledRemainingQty > 0
                          ? ` · ${t("purchasing.cancelRemaining")} ${line.cancelledRemainingQty}`
                          : ""}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : null}

              {canExpand ? (
                <div className="mt-1">
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
            </>
          ),
        };
      })}
    />
  );
}
