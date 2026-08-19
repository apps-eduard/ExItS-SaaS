import { Badge } from "@/components/ui/badge";
import { AdminTable } from "@/components/exits/AdminTable";

export type RecentActivityItem = {
  id: string;
  title: string;
  actor: string;
  context: string;
  time: string;
  outcomeLabel: string;
  tone: "success" | "warning" | "danger" | "info" | "neutral";
};

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
          cell: (item) => <span className="font-medium break-all">{item.title}</span>,
        },
        {
          id: "actor",
          header: columns.actor,
          cell: (item) => <span className="text-muted break-all">{item.actor}</span>,
        },
        {
          id: "context",
          header: columns.context,
          cell: (item) => <span className="text-muted">{item.context}</span>,
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
