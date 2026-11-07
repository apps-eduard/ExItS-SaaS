import { cn } from "@/lib/utils";

type ExitsLogoProps = {
  className?: string;
  markClassName?: string;
  showWordmark?: boolean;
  compact?: boolean;
};

function ExitsLogo({
  className,
  markClassName,
  showWordmark = true,
  compact = false,
}: ExitsLogoProps) {
  return (
    <div className={cn("inline-flex items-center gap-2", className)}>
      <span
        className={cn(
          "inline-flex items-center justify-center rounded-[0.55rem] bg-primary text-primary-foreground",
          compact ? "size-7" : "size-8",
          markClassName,
        )}
        aria-hidden
      >
        <svg
          viewBox="0 0 32 32"
          className={compact ? "size-3.5" : "size-4"}
          fill="none"
          aria-hidden
        >
          <path
            d="M9 8h14v3.25H12.75v2.85H21.5v3.1H12.75v2.85H23.5V24H9V8Z"
            fill="currentColor"
          />
        </svg>
      </span>
      {showWordmark ? (
        <span className="flex min-w-0 flex-col leading-none">
          <span
            className={cn(
              "font-semibold tracking-tight text-foreground",
              compact ? "text-[0.9375rem]" : "text-base",
            )}
          >
            ExItS
          </span>
          {!compact ? (
            <span className="mt-0.5 text-[9px] font-semibold uppercase tracking-[0.14em] text-primary">
              V2
            </span>
          ) : null}
        </span>
      ) : null}
    </div>
  );
}

export { ExitsLogo };
