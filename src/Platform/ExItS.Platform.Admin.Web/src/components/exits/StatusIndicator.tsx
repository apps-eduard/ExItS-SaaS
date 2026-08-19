import { cn } from "@/lib/utils";

export function StatusIndicator({
  label,
  tone = "neutral",
}: {
  label: string;
  tone?: "success" | "warning" | "danger" | "info" | "neutral";
}) {
  return (
    <span className="inline-flex items-center gap-1.5 text-[length:var(--exits-text-sm)]">
      <span
        aria-hidden="true"
        className={cn(
          "size-1.5 shrink-0 rounded-full",
          tone === "success" && "bg-success",
          tone === "warning" && "bg-warning",
          tone === "danger" && "bg-destructive",
          tone === "info" && "bg-info",
          tone === "neutral" && "bg-muted",
        )}
      />
      <span>{label}</span>
    </span>
  );
}
