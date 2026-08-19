import { Badge } from "@/components/ui/badge";

export type RecentActivityItem = {
  id: string;
  title: string;
  meta: string;
  outcomeLabel: string;
  tone: "success" | "warning" | "danger" | "info" | "neutral";
};

export function RecentActivityList({
  items,
  emptyLabel,
}: {
  items: RecentActivityItem[];
  emptyLabel: string;
}) {
  if (items.length === 0) {
    return (
      <p className="text-[length:var(--exits-text-sm)] text-muted break-words">{emptyLabel}</p>
    );
  }

  return (
    <ul className="divide-y divide-border">
      {items.map((item) => (
        <li
          key={item.id}
          className="flex min-w-0 items-start justify-between gap-3 py-2 first:pt-0 last:pb-0"
        >
          <div className="min-w-0">
            <p className="truncate text-[length:var(--exits-text-sm)] font-medium">{item.title}</p>
            <p className="truncate text-[length:var(--exits-text-xs)] text-muted">{item.meta}</p>
          </div>
          <Badge tone={item.tone}>{item.outcomeLabel}</Badge>
        </li>
      ))}
    </ul>
  );
}
