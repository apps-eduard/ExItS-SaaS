import { useDeferredVisible } from "@/components/exits/loading/useDeferredVisible";
import { cn } from "@/lib/cn";

/** Subtle indicator while background refetch preserves existing content. */
export function BackgroundRefreshIndicator({
  active,
  label,
  className,
  testId = "background-refresh-indicator",
}: {
  active: boolean;
  label: string;
  className?: string;
  testId?: string;
}) {
  const visible = useDeferredVisible(active, { delayMs: 180, minVisibleMs: 200 });

  if (!visible) {
    return null;
  }

  return (
    <p
      className={cn(
        "exits-background-refresh m-0 text-[length:var(--exits-text-xs)] font-medium text-muted",
        className,
      )}
      role="status"
      aria-live="polite"
      data-testid={testId}
    >
      {label}
    </p>
  );
}
