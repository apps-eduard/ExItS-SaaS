import { Badge } from "@/components/ui/badge";

export type StatusBreakdownItem = {
  key: string;
  label: string;
  value: string;
  tone: "success" | "warning" | "danger" | "info" | "neutral";
};

export function StatusBreakdown({ items }: { items: StatusBreakdownItem[] }) {
  return (
    <ul className="grid grid-cols-2 gap-2">
      {items.map((item) => (
        <li
          key={item.key}
          className="min-w-0 rounded-[var(--exits-density-radius)] border border-border px-3 py-2"
        >
          <p className="text-[length:var(--exits-text-xs)] text-muted break-words">{item.label}</p>
          <p className="mt-1 font-[family-name:var(--exits-font-tabular)] text-[length:var(--exits-text-lg)] font-semibold tabular-nums">
            {item.value}
          </p>
          <Badge className="mt-2" tone={item.tone}>
            {item.label}
          </Badge>
        </li>
      ))}
    </ul>
  );
}
