import { ExitsLoaderMark } from "@/components/exits/loading/ExitsLoaderMark";
import { useDeferredVisible } from "@/components/exits/loading/useDeferredVisible";
import { cn } from "@/lib/cn";

/** Full-viewport boot loader — session bootstrap / cold-start only. */
export function AppBootLoader({
  label,
  brand = "ExItS",
  defer = true,
  testId = "app-boot-loader",
}: {
  label: string;
  brand?: string;
  /** Soften flash for extremely fast boots. */
  defer?: boolean;
  testId?: string;
}) {
  const deferredShow = useDeferredVisible(true, defer ? undefined : { delayMs: 0, minVisibleMs: 0 });
  const show = defer ? deferredShow : true;

  if (!show) {
    return (
      <div
        className="sr-only"
        role="status"
        aria-live="polite"
        aria-busy="true"
        data-testid={testId}
      >
        {label}
      </div>
    );
  }

  return (
    <div
      className={cn(
        "exits-app-boot-loader flex min-h-[100dvh] w-full flex-1 flex-col items-center justify-center gap-5 px-6 py-16",
      )}
      data-testid={testId}
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <div className="flex flex-col items-center gap-3">
        <p className="exits-app-boot-loader__brand m-0 text-[length:var(--exits-text-lg)] font-semibold tracking-tight text-foreground">
          {brand}
        </p>
        <ExitsLoaderMark size="lg" />
      </div>
      <p className="m-0 max-w-xs text-center text-[length:var(--exits-text-md)] font-medium text-muted">
        {label}
      </p>
    </div>
  );
}
