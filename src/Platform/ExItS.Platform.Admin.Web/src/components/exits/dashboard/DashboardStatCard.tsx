import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

export function DashboardStatCard({
  label,
  value,
  tone,
  badge,
}: {
  label: string;
  value: string;
  tone?: "success" | "warning" | "danger" | "info" | "neutral";
  badge?: string;
}) {
  return (
    <div className="min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-surface-muted/40 px-3 py-2.5">
      <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-wide text-muted break-words">
        {label}
      </p>
      <p
        className={cn(
          "mt-1 font-[family-name:var(--exits-font-tabular)] text-[length:var(--exits-text-xl)] font-bold tabular-nums leading-tight",
        )}
      >
        {value}
      </p>
      {badge ? (
        <Badge className="mt-2" tone={tone ?? "neutral"}>
          {badge}
        </Badge>
      ) : null}
    </div>
  );
}
