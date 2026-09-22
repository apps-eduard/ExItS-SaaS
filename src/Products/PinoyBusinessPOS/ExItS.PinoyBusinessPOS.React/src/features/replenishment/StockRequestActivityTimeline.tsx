import {
  Ban,
  CheckCircle2,
  ClipboardCheck,
  FilePlus2,
  PackageCheck,
  Truck,
} from "lucide-react";
import type { StockRequestActivityEventDto } from "@/api/pos/pos-stock-requests-client";
import { ExitsActivityTimeline } from "@/components/exits/ExitsActivityTimeline";
import type { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

const EVENT_TITLE_KEY: Record<string, MessageKey> = {
  Requested: "stockRequest.activity.requested",
  Approved: "stockRequest.activity.approved",
  PreparingStarted: "stockRequest.activity.preparing",
  TransferPrepared: "stockRequest.activity.transferPrepared",
  TransferDispatched: "stockRequest.activity.transferDispatched",
  TransferReceipt: "stockRequest.activity.transferReceipt",
  TransferCompleted: "stockRequest.activity.transferCompleted",
  TransferRemainderClosed: "stockRequest.activity.transferRemainderClosed",
  TransferCancelled: "stockRequest.activity.transferCancelled",
  RequestFulfilled: "stockRequest.activity.requestFulfilled",
  RequestCancelled: "stockRequest.activity.cancelled",
  RequestRejected: "stockRequest.activity.rejected",
};

function eventTone(eventType: string) {
  switch (eventType) {
    case "Requested":
    case "Approved":
    case "PreparingStarted":
    case "TransferPrepared":
      return "info" as const;
    case "TransferDispatched":
    case "TransferReceipt":
      return "warning" as const;
    case "TransferCompleted":
    case "RequestFulfilled":
      return "success" as const;
    case "TransferRemainderClosed":
      return "warning" as const;
    case "TransferCancelled":
    case "RequestRejected":
    case "RequestCancelled":
      return "danger" as const;
    default:
      return "neutral" as const;
  }
}

function eventIcon(eventType: string) {
  const className = "size-4 shrink-0";
  switch (eventType) {
    case "Requested":
    case "TransferPrepared":
      return <FilePlus2 className={className} aria-hidden />;
    case "Approved":
    case "PreparingStarted":
      return <ClipboardCheck className={className} aria-hidden />;
    case "TransferDispatched":
      return <Truck className={className} aria-hidden />;
    case "TransferReceipt":
    case "TransferCompleted":
    case "RequestFulfilled":
      return <PackageCheck className={className} aria-hidden />;
    case "TransferRemainderClosed":
    case "TransferCancelled":
    case "RequestRejected":
    case "RequestCancelled":
      return <Ban className={className} aria-hidden />;
    default:
      return <CheckCircle2 className={className} aria-hidden />;
  }
}

function eventDescription(
  event: StockRequestActivityEventDto,
  t: (key: MessageKey) => string,
): string {
  const transferLabel = event.transferNumber?.trim() || event.transferId?.slice(0, 8) || "";
  switch (event.eventType) {
    case "TransferReceipt":
      if (event.receiptSequence != null) {
        return t("stockRequest.activity.transferReceiptDetail").replace(
          "{sequence}",
          String(event.receiptSequence),
        );
      }
      return t("stockRequest.activity.transferReceiptDetail").replace("{sequence}", "—");
    case "TransferPrepared":
    case "TransferDispatched":
    case "TransferCompleted":
    case "TransferRemainderClosed":
    case "TransferCancelled":
      return transferLabel
        ? t("stockRequest.activity.transferRefDetail").replace("{transfer}", transferLabel)
        : "";
    case "RequestRejected":
      return event.reason?.trim() ?? "";
    default:
      return "";
  }
}

export type StockRequestActivityTimelineProps = {
  events: readonly StockRequestActivityEventDto[];
  resolveActor: ReturnType<typeof useActorDirectory>["resolve"];
  isResolving: boolean;
};

export function StockRequestActivityTimeline({
  events,
  resolveActor,
  isResolving,
}: StockRequestActivityTimelineProps) {
  const { t } = useI18n();

  if (events.length === 0) {
    return (
      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="stock-request-activity-empty"
      >
        {t("stockRequest.activity.empty")}
      </p>
    );
  }

  const sorted = [...events].sort((a, b) => a.occurredAtUtc.localeCompare(b.occurredAtUtc));

  return (
    <ExitsActivityTimeline
      testId="stock-request-activity-timeline"
      items={sorted.map((event) => {
        const titleKey = EVENT_TITLE_KEY[event.eventType];
        const resolved = resolveActor(event.actorId);
        const actorName =
          resolved?.displayName && resolved.actorStatus !== "NotAvailable"
            ? resolved.displayName
            : null;
        const description = eventDescription(event, t);
        return {
          id: event.eventId,
          atUtc: event.occurredAtUtc,
          title: titleKey ? t(titleKey) : event.eventType,
          description: description || undefined,
          tone: eventTone(event.eventType),
          icon: eventIcon(event.eventType),
          actorName,
          actorLoading: Boolean(event.actorId) && isResolving && !actorName,
          testId: `stock-request-timeline-${event.eventType}`,
        };
      })}
    />
  );
}
