import { Ban, CheckCircle2, FilePlus2, Truck } from "lucide-react";
import { ExitsActivityTimeline } from "@/components/exits/ExitsActivityTimeline";
import type { useActorDirectory } from "@/features/actors/useActorDirectory";
import type { TransferActivityEvent } from "@/features/inventory/inventory-transfer-activity";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

const TITLE_KEY: Record<TransferActivityEvent["kind"], MessageKey> = {
  created: "transfer.activity.created",
  dispatched: "transfer.activity.dispatched",
  received: "transfer.activity.received",
  receipt: "transfer.activity.receipt",
  closedRemainder: "transfer.activity.closedRemainder",
  cancelled: "transfer.activity.cancelled",
};

const DETAIL_KEY: Record<TransferActivityEvent["kind"], MessageKey> = {
  created: "transfer.activity.createdDetail",
  dispatched: "transfer.activity.dispatchedDetail",
  received: "transfer.activity.receivedDetail",
  receipt: "transfer.activity.receiptDetail",
  closedRemainder: "transfer.activity.closedRemainderDetail",
  cancelled: "transfer.activity.cancelledDetail",
};

function transferTone(kind: TransferActivityEvent["kind"]) {
  switch (kind) {
    case "created":
      return "info" as const;
    case "dispatched":
      return "warning" as const;
    case "received":
    case "receipt":
      return "success" as const;
    case "closedRemainder":
      return "warning" as const;
    case "cancelled":
      return "danger" as const;
  }
}

function transferIcon(kind: TransferActivityEvent["kind"]) {
  const className = "size-4 shrink-0";
  switch (kind) {
    case "created":
      return <FilePlus2 className={className} aria-hidden />;
    case "dispatched":
      return <Truck className={className} aria-hidden />;
    case "received":
    case "receipt":
      return <CheckCircle2 className={className} aria-hidden />;
    case "closedRemainder":
      return <Ban className={className} aria-hidden />;
    case "cancelled":
      return <Ban className={className} aria-hidden />;
  }
}

export type InventoryTransferActivityTimelineProps = {
  events: readonly TransferActivityEvent[];
  resolveActor: ReturnType<typeof useActorDirectory>["resolve"];
  isResolving: boolean;
};

export function InventoryTransferActivityTimeline({
  events,
  resolveActor,
  isResolving,
}: InventoryTransferActivityTimelineProps) {
  const { t } = useI18n();

  if (events.length === 0) {
    return (
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="transfer-activity-empty">
        {t("transfer.activity.empty")}
      </p>
    );
  }

  return (
    <ExitsActivityTimeline
      testId="transfer-activity-timeline"
      items={events.map((event) => {
        const resolved = resolveActor(event.actorId);
        const actorName =
          resolved?.displayName && resolved.actorStatus !== "NotAvailable"
            ? resolved.displayName
            : null;
        return {
          id: event.id,
          atUtc: event.atUtc,
          title: t(TITLE_KEY[event.kind]),
          description: (() => {
            if (event.kind === "receipt" && event.receiptSequence != null) {
              const base = t(DETAIL_KEY[event.kind]).replace(
                "{sequence}",
                String(event.receiptSequence),
              );
              const lines = event.receiptLines ?? [];
              if (lines.length === 0) {
                return base;
              }
              const totals = lines.reduce(
                (acc, line) => ({
                  good: acc.good + line.quantityReceived,
                  damaged: acc.damaged + (line.quantityDamaged ?? 0),
                  missing: acc.missing + (line.quantityMissing ?? 0),
                }),
                { good: 0, damaged: 0, missing: 0 },
              );
              const parts: string[] = [];
              if (totals.good > 0) {
                parts.push(
                  t("transfer.activity.receiptGood").replace("{qty}", String(totals.good)),
                );
              }
              if (totals.damaged > 0) {
                parts.push(
                  t("transfer.activity.receiptDamaged").replace("{qty}", String(totals.damaged)),
                );
              }
              if (totals.missing > 0) {
                parts.push(
                  t("transfer.activity.receiptMissing").replace("{qty}", String(totals.missing)),
                );
              }
              return parts.length > 0 ? `${base} · ${parts.join(" · ")}` : base;
            }
            return t(DETAIL_KEY[event.kind]);
          })(),
          tone: transferTone(event.kind),
          icon: transferIcon(event.kind),
          actorName,
          actorLoading: Boolean(event.actorId) && isResolving && !actorName,
          testId: `transfer-timeline-event-${event.kind}`,
        };
      })}
    />
  );
}
