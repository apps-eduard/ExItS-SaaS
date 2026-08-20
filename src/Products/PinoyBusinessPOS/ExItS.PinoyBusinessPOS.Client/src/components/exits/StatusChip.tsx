import { cn } from "@/lib/cn";

export function StatusChip({
  children,
  tone = "info",
}: {
  children: string;
  tone?: "info" | "success" | "warning" | "danger";
}) {
  return (
    <span
      className={cn(
        "inline-flex min-h-8 items-center rounded-full px-3 text-[length:var(--exits-text-xs)] font-semibold",
        tone === "success" && "bg-[color-mix(in_srgb,var(--exits-success)_16%,transparent)]",
        tone === "warning" && "bg-[color-mix(in_srgb,var(--exits-warning)_16%,transparent)]",
        tone === "info" && "bg-[color-mix(in_srgb,var(--exits-info)_16%,transparent)]",
        tone === "danger" && "bg-[color-mix(in_srgb,var(--exits-danger)_16%,transparent)]",
      )}
    >
      {children}
    </span>
  );
}

/** Alias for StatusChip — same pill status treatment. */
export const StatusPill = StatusChip;
