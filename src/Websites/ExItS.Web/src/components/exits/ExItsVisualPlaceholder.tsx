import { cn } from "@/lib/utils";

export function ExItsVisualPlaceholder({
  title,
  caption,
  className,
}: {
  title: string;
  caption: string;
  className?: string;
}) {
  return (
    <figure
      className={cn(
        "overflow-hidden rounded-xl border border-borderDefault bg-surface",
        className,
      )}
    >
      <div
        className="relative aspect-[4/3] w-full p-5 sm:p-6"
        aria-hidden="true"
      >
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_10%,rgba(16,185,129,0.12),transparent_42%)]" />
        <div className="relative flex h-full flex-col gap-3">
          <div className="flex items-center justify-between border-b border-borderDefault pb-3">
            <span className="text-xs font-semibold uppercase tracking-[0.18em] text-muted">
              {title}
            </span>
            <span className="h-2 w-2 rounded-full bg-brand" />
          </div>
          <div className="grid flex-1 grid-cols-3 gap-3">
            <div className="col-span-2 rounded-lg border border-borderDefault bg-elevated" />
            <div className="rounded-lg border border-borderDefault bg-base" />
            <div className="rounded-lg border border-borderDefault bg-base" />
            <div className="col-span-2 rounded-lg border border-borderDefault bg-elevated" />
          </div>
        </div>
      </div>
      <figcaption className="border-t border-borderDefault px-5 py-3 text-xs leading-relaxed text-muted sm:px-6">
        {caption}
      </figcaption>
    </figure>
  );
}
