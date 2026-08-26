import { Badge } from "@/components/ui/badge";
import { AdminTable } from "@/components/exits/AdminTable";

export type RecentActivityItem = {
  id: string;
  title: string;
  rawTitle: string;
  actor: string;
  actorDetail?: string;
  rawActor: string;
  context: string;
  rawContext: string;
  time: string;
  outcomeLabel: string;
  tone: "success" | "warning" | "danger" | "info" | "neutral";
};

function FidelityText({ label, raw, detail }: { label: string; raw: string; detail?: string }) {
  return (
    <span className="grid min-w-0" title={raw}>
      <span className="truncate font-medium">{label}</span>
      {detail ? (
        <span className="truncate font-[family-name:var(--exits-font-tabular)] text-[length:var(--exits-text-xs)] text-muted">
          {detail}
        </span>
      ) : null}
      <span className="sr-only">{raw}</span>
    </span>
  );
}

export function RecentActivityList({
  items,
  emptyLabel,
  caption,
  columns,
}: {
  items: RecentActivityItem[];
  emptyLabel: string;
  caption: string;
  columns: {
    action: string;
    actor: string;
    context: string;
    time: string;
    outcome: string;
  };
}) {
  return (
    <AdminTable
      caption={caption}
      empty={emptyLabel}
      columns={[
        {
          id: "action",
          header: columns.action,
          cell: (item) => <FidelityText label={item.title} raw={item.rawTitle} />,
        },
        {
          id: "actor",
          header: columns.actor,
          cell: (item) => (
            <FidelityText label={item.actor} raw={item.rawActor} detail={item.actorDetail} />
          ),
        },
        {
          id: "context",
          header: columns.context,
          cell: (item) => (
            <span className="text-muted" title={item.rawContext}>
              {item.context}
              <span className="sr-only">{item.rawContext}</span>
            </span>
          ),
        },
        {
          id: "time",
          header: columns.time,
          cell: (item) => <span className="text-muted whitespace-nowrap">{item.time}</span>,
        },
        {
          id: "outcome",
          header: columns.outcome,
          align: "right",
          cell: (item) => <Badge tone={item.tone}>{item.outcomeLabel}</Badge>,
        },
      ]}
      rows={items}
    />
  );
}
