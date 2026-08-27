import { Skeleton } from "@/components/ui/skeleton";
import { useDeferredVisible } from "@/components/exits/loading/useDeferredVisible";
import { cn } from "@/lib/cn";

export type PageSkeletonVariant = "list" | "cards" | "detail";

/** In-page skeleton for initial React Query loads — shell/nav stay visible. */
export function PageSkeleton({
  label,
  variant = "list",
  rows = 5,
  className,
  defer = true,
  testId = "page-skeleton",
}: {
  label: string;
  variant?: PageSkeletonVariant;
  rows?: number;
  className?: string;
  defer?: boolean;
  testId?: string;
}) {
  const visible = useDeferredVisible(true, defer ? undefined : { delayMs: 0, minVisibleMs: 0 });
  const show = defer ? visible : true;

  if (!show) {
    return (
      <div className="sr-only" role="status" aria-live="polite" aria-busy="true" data-testid={testId}>
        {label}
      </div>
    );
  }

  return (
    <div
      className={cn("exits-page-skeleton flex min-w-0 flex-col gap-3", className)}
      role="status"
      aria-live="polite"
      aria-busy="true"
      aria-label={label}
      data-testid={testId}
    >
      <span className="sr-only">{label}</span>
      {variant === "detail" ? (
        <>
          <Skeleton className="h-5 w-40" />
          <Skeleton className="h-24 w-full rounded-[var(--exits-radius-md)]" />
          <Skeleton className="h-16 w-full rounded-[var(--exits-radius-md)]" />
          <Skeleton className="h-16 w-3/4 rounded-[var(--exits-radius-md)]" />
        </>
      ) : null}
      {variant === "cards" ? (
        <div className="grid gap-3 sm:grid-cols-2">
          {Array.from({ length: Math.max(2, rows) }).map((_, index) => (
            <Skeleton
              key={index}
              className="h-28 w-full rounded-[var(--exits-radius-md)]"
            />
          ))}
        </div>
      ) : null}
      {variant === "list" ? (
        <div className="grid gap-2">
          {Array.from({ length: rows }).map((_, index) => (
            <Skeleton
              key={index}
              className="h-16 w-full rounded-[var(--exits-radius-md)]"
            />
          ))}
        </div>
      ) : null}
    </div>
  );
}
